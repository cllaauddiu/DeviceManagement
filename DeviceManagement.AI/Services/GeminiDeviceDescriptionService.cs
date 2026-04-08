using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DeviceManagement.AI.Models;
using DeviceManagement.AI.Options;
using Microsoft.Extensions.Options;

namespace DeviceManagement.AI.Services;

public class GeminiDeviceDescriptionService : IDeviceDescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;

    public GeminiDeviceDescriptionService(HttpClient httpClient, IOptions<AiSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> GenerateDescriptionAsync(GenerateDescriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.GeminiApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Set AiSettings:GeminiApiKey or AiSettings__GeminiApiKey.");
        }

        var prompt = BuildPrompt(request);

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _settings.Temperature,
                maxOutputTokens = _settings.MaxOutputTokens
            }
        };

        var fallbackErrors = new List<string>();
        string? bestShortDescription = null;

        foreach (var apiVersion in GetApiVersionCandidates())
        {
            var models = await GetModelCandidatesAsync(apiVersion, cancellationToken);

            foreach (var model in models)
            {
                var endpoint = BuildEndpointUrl(apiVersion, model);
                using var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken);
                    fallbackErrors.Add($"{apiVersion}/{model} -> {(int)response.StatusCode}: {errorPayload}");

                    // Retry next candidate only for "model/version not found" style errors.
                    if ((int)response.StatusCode == 404 ||
                        (int)response.StatusCode == 400 ||
                        (int)response.StatusCode == 429)
                    {
                        continue;
                    }

                    throw new HttpRequestException($"Gemini request failed with status {(int)response.StatusCode}: {errorPayload}");
                }

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var json = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

                if (TryExtractDescription(json.RootElement, out var description))
                {
                    if (CountWords(description) >= 45)
                    {
                        return description;
                    }

                    // If the first answer is too short, ask the same model to expand it.
                    var expanded = await TryExpandDescriptionAsync(endpoint, request, description, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(expanded) && CountWords(expanded) >= 45)
                    {
                        return expanded;
                    }

                    bestShortDescription = ChooseLonger(bestShortDescription, expanded);
                    bestShortDescription = ChooseLonger(bestShortDescription, description);
                    fallbackErrors.Add($"{apiVersion}/{model} -> description too short ({CountWords(description)} words)");
                    continue;
                }

                var raw = json.RootElement.GetRawText();
                fallbackErrors.Add($"{apiVersion}/{model} -> 200 but empty content: {raw}");
            }
        }

        if (!string.IsNullOrWhiteSpace(bestShortDescription))
        {
            return BuildDeterministicDescription(request);
        }

        throw new InvalidOperationException("Gemini response did not contain a valid description. Tried candidates: " + string.Join(" | ", fallbackErrors));
    }

    private async Task<string?> TryExpandDescriptionAsync(string endpoint, GenerateDescriptionRequest request, string shortDescription, CancellationToken cancellationToken)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = BuildExpansionPrompt(request, shortDescription) }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _settings.Temperature,
                maxOutputTokens = Math.Max(_settings.MaxOutputTokens, 220)
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
        return TryExtractDescription(json.RootElement, out var expanded) ? expanded : null;
    }

    private async Task<IReadOnlyList<string>> GetModelCandidatesAsync(string apiVersion, CancellationToken cancellationToken)
    {
        var candidates = new List<string>();

        // Keep explicit config first.
        candidates.AddRange(GetConfiguredModelCandidates());

        // Then try models discovered for this key/version from ListModels.
        var discoveredModels = await DiscoverAvailableModelsAsync(apiVersion, cancellationToken);
        candidates.AddRange(discoveredModels);

        return candidates
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IEnumerable<string> GetConfiguredModelCandidates()
    {
        var configured = _settings.GeminiModel.Trim();
        yield return configured;

        if (!configured.EndsWith("-latest", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"{configured}-latest";
        }

        // Common defaults that are usually available when 1.5 is unavailable for a key.
        yield return "gemini-2.0-flash";
        yield return "gemini-2.0-flash-lite";
    }

    private async Task<IReadOnlyList<string>> DiscoverAvailableModelsAsync(string apiVersion, CancellationToken cancellationToken)
    {
        var endpoint = BuildListModelsUrl(apiVersion);
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<string>();
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("models", out var modelsElement) || modelsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var flashFirst = new List<string>();
        var others = new List<string>();

        foreach (var model in modelsElement.EnumerateArray())
        {
            if (!model.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            var fullName = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!SupportsGenerateContent(model))
            {
                continue;
            }

            var shortName = fullName["models/".Length..];

            if (shortName.Contains("flash", StringComparison.OrdinalIgnoreCase))
            {
                flashFirst.Add(shortName);
            }
            else
            {
                others.Add(shortName);
            }
        }

        return flashFirst.Concat(others).ToList();
    }

    private static bool SupportsGenerateContent(JsonElement model)
    {
        if (!model.TryGetProperty("supportedGenerationMethods", out var methodsElement) || methodsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var method in methodsElement.EnumerateArray())
        {
            var methodName = method.GetString();
            if (string.Equals(methodName, "generateContent", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryExtractDescription(JsonElement root, out string description)
    {
        description = string.Empty;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var textElement))
                {
                    continue;
                }

                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    description = text.Trim();
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerable<string> GetApiVersionCandidates()
    {
        var configured = string.IsNullOrWhiteSpace(_settings.GeminiApiVersion) ? "v1beta" : _settings.GeminiApiVersion.Trim();
        yield return configured;

        var fallback = string.Equals(configured, "v1beta", StringComparison.OrdinalIgnoreCase) ? "v1" : "v1beta";
        yield return fallback;
    }

    private string BuildEndpointUrl(string apiVersion, string model)
    {
        var baseUrl = _settings.GeminiBaseUrl.TrimEnd('/');
        var cleanVersion = apiVersion.Trim().Trim('/');
        var cleanModel = model.Trim();
        var key = Uri.EscapeDataString(_settings.GeminiApiKey.Trim());
        return $"{baseUrl}/{cleanVersion}/models/{cleanModel}:generateContent?key={key}";
    }

    private string BuildListModelsUrl(string apiVersion)
    {
        var baseUrl = _settings.GeminiBaseUrl.TrimEnd('/');
        var cleanVersion = apiVersion.Trim().Trim('/');
        var key = Uri.EscapeDataString(_settings.GeminiApiKey.Trim());
        return $"{baseUrl}/{cleanVersion}/models?key={key}";
    }

    private static string BuildPrompt(GenerateDescriptionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a device catalog assistant.");
        sb.AppendLine("Create a single concise and informative product description in English.");
        sb.AppendLine("Constraints:");
        sb.AppendLine("- 1 paragraph");
        sb.AppendLine("- Aim for about 50 words (acceptable range: 45 to 60 words)");
        sb.AppendLine("- Human-readable and neutral tone");
        sb.AppendLine("- Mention only facts provided below");
        sb.AppendLine("- Do not invent specs");
        sb.AppendLine("- Include at least 3 concrete specs when available (e.g., CPU, RAM, OS, storage)");
        sb.AppendLine("- End with one short practical-use sentence");
        sb.AppendLine();
        sb.AppendLine("Device specs:");
        sb.AppendLine($"- Brand: {request.Brand}");
        sb.AppendLine($"- Model: {request.Model}");
        sb.AppendLine($"- Type: {request.Type}");

        if (!string.IsNullOrWhiteSpace(request.Cpu))
        {
            sb.AppendLine($"- CPU: {request.Cpu}");
        }

        if (request.RamGb.HasValue)
        {
            sb.AppendLine($"- RAM: {request.RamGb.Value} GB");
        }

        if (request.StorageGb.HasValue)
        {
            sb.AppendLine($"- Storage: {request.StorageGb.Value} GB");
        }

        if (!string.IsNullOrWhiteSpace(request.OperatingSystem))
        {
            sb.AppendLine($"- OS: {request.OperatingSystem}");
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            sb.AppendLine($"- Extra notes: {request.Notes}");
        }

        return sb.ToString();
    }

    private static string BuildExpansionPrompt(GenerateDescriptionRequest request, string shortDescription)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Rewrite and expand the following device description.");
        sb.AppendLine("Requirements:");
        sb.AppendLine("- Exactly one paragraph");
        sb.AppendLine("- 45 to 60 words");
        sb.AppendLine("- Keep a neutral, catalog tone");
        sb.AppendLine("- Mention only these known specs; do not invent details");
        sb.AppendLine();
        sb.AppendLine("Known specs:");
        sb.AppendLine($"- Brand: {request.Brand}");
        sb.AppendLine($"- Model: {request.Model}");
        sb.AppendLine($"- Type: {request.Type}");

        if (!string.IsNullOrWhiteSpace(request.Cpu)) sb.AppendLine($"- CPU: {request.Cpu}");
        if (request.RamGb.HasValue) sb.AppendLine($"- RAM: {request.RamGb.Value} GB");
        if (request.StorageGb.HasValue) sb.AppendLine($"- Storage: {request.StorageGb.Value} GB");
        if (!string.IsNullOrWhiteSpace(request.OperatingSystem)) sb.AppendLine($"- OS: {request.OperatingSystem}");
        if (!string.IsNullOrWhiteSpace(request.Notes)) sb.AppendLine($"- Notes: {request.Notes}");

        sb.AppendLine();
        sb.AppendLine("Current short description:");
        sb.AppendLine(shortDescription);

        return sb.ToString();
    }

    private static int CountWords(string text)
    {
        return text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static string? ChooseLonger(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first)) return string.IsNullOrWhiteSpace(second) ? null : second;
        if (string.IsNullOrWhiteSpace(second)) return first;
        return CountWords(second) > CountWords(first) ? second : first;
    }

    private static string BuildDeterministicDescription(GenerateDescriptionRequest request)
    {
        var chunks = new List<string>
        {
            $"{request.Brand} {request.Model} is a {request.Type.ToLowerInvariant()} built for dependable everyday use"
        };

        var specs = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Cpu)) specs.Add($"{request.Cpu} processor");
        if (request.RamGb.HasValue) specs.Add($"{request.RamGb.Value} GB RAM");
        if (request.StorageGb.HasValue) specs.Add($"{request.StorageGb.Value} GB storage");
        if (!string.IsNullOrWhiteSpace(request.OperatingSystem)) specs.Add($"{request.OperatingSystem}");

        if (specs.Count > 0)
        {
            chunks.Add("It includes " + string.Join(", ", specs.Take(specs.Count - 1)) + (specs.Count > 1 ? $" and {specs[^1]}" : specs[0]));
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            chunks.Add(request.Notes.TrimEnd('.'));
        }

        chunks.Add("This setup is well suited for multitasking, media, and daily productivity tasks with consistent performance in regular workflows");

        return string.Join(". ", chunks.Select(c => c.Trim())).Trim() + ".";
    }
}
