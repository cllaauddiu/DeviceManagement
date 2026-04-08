FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY DeviceManagement.Api/DeviceManagement.Api.csproj DeviceManagement.Api/
RUN dotnet restore DeviceManagement.Api/DeviceManagement.Api.csproj

COPY DeviceManagement.Api/ DeviceManagement.Api/
RUN dotnet publish DeviceManagement.Api/DeviceManagement.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "DeviceManagement.Api.dll"]
