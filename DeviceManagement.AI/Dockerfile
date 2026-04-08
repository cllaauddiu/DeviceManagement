FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY DeviceManagement.AI/DeviceManagement.AI.csproj DeviceManagement.AI/
RUN dotnet restore DeviceManagement.AI/DeviceManagement.AI.csproj

COPY DeviceManagement.AI/ DeviceManagement.AI/
RUN dotnet publish DeviceManagement.AI/DeviceManagement.AI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "DeviceManagement.AI.dll"]
