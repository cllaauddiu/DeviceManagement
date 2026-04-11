using DeviceManagement.Api.Models;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace DeviceManagement.Api.Services;

public class DeviceService
{
    private readonly IMongoCollection<Device> _devices;
    private static readonly Regex NonAlphaNumericRegex = new(@"[^\p{L}\p{Nd}]+", RegexOptions.Compiled);
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions.Compiled);

    public DeviceService(MongoDbService mongoDbService)
    {
        _devices = mongoDbService.Database!.GetCollection<Device>("Devices");
    }

    public async Task<List<Device>> GetAllAsync() =>
        await _devices.Find(_ => true).ToListAsync();

    public async Task<Device?> GetByIdAsync(string id) =>
        await _devices.Find(d => d.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Device device) =>
        await _devices.InsertOneAsync(device);

    public async Task UpdateAsync(string id, Device device) =>
        await _devices.ReplaceOneAsync(d => d.Id == id, device);

    public async Task DeleteAsync(string id) =>
        await _devices.DeleteOneAsync(d => d.Id == id);

    public async Task<List<Device>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = NormalizeText(query);
        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return [];
        }

        var tokens = Tokenize(normalizedQuery)
            .Distinct()
            .ToArray();

        if (tokens.Length == 0)
        {
            return [];
        }

        var devices = await GetAllAsync();

        return devices
            .Select(d => new
            {
                Device = d,
                Score = CalculateRelevanceScore(d, normalizedQuery, tokens)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Device.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Device.Manufacturer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Device.Id, StringComparer.Ordinal)
            .Select(x => x.Device)
            .ToList();
    }

    private static int CalculateRelevanceScore(Device device, string normalizedQuery, IReadOnlyCollection<string> tokens)
    {
        var name = NormalizeText(device.Name);
        var manufacturer = NormalizeText(device.Manufacturer);
        var processor = NormalizeText(device.Processor);
        var ram = NormalizeText($"{device.RamAmount} {device.RamAmount}gb");

        var nameTokenSet = Tokenize(name).ToHashSet();
        var manufacturerTokenSet = Tokenize(manufacturer).ToHashSet();
        var processorTokenSet = Tokenize(processor).ToHashSet();
        var ramTokenSet = Tokenize(ram).ToHashSet();

        var score = 0;

        foreach (var token in tokens)
        {
            score += ScoreTokenMatch(name, nameTokenSet, token, exactWeight: 12, partialWeight: 8);
            score += ScoreTokenMatch(manufacturer, manufacturerTokenSet, token, exactWeight: 9, partialWeight: 6);
            score += ScoreTokenMatch(processor, processorTokenSet, token, exactWeight: 7, partialWeight: 4);
            score += ScoreTokenMatch(ram, ramTokenSet, token, exactWeight: 5, partialWeight: 3);
        }

        if (name.Contains(normalizedQuery, StringComparison.Ordinal)) score += 10;
        if (manufacturer.Contains(normalizedQuery, StringComparison.Ordinal)) score += 5;
        if (processor.Contains(normalizedQuery, StringComparison.Ordinal)) score += 4;

        return score;
    }

    private static int ScoreTokenMatch(string normalizedField, IReadOnlySet<string> fieldTokens, string token, int exactWeight, int partialWeight)
    {
        if (fieldTokens.Contains(token))
        {
            return exactWeight;
        }

        return normalizedField.Contains(token, StringComparison.Ordinal) ? partialWeight : 0;
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.ToLowerInvariant();
        var withoutPunctuation = NonAlphaNumericRegex.Replace(lower, " ");
        return MultiSpaceRegex.Replace(withoutPunctuation, " ").Trim();
    }

    private static IEnumerable<string> Tokenize(string normalizedText) =>
        normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
