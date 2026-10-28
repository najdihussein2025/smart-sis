# Smart SIS API (.NET 10). Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between code changes.
COPY global.json Directory.Build.props ./
COPY src/SmartSis.Api/SmartSis.Api.csproj src/SmartSis.Api/
RUN dotnet restore src/SmartSis.Api/SmartSis.Api.csproj

COPY src/ src/
RUN dotnet publish src/SmartSis.Api/SmartSis.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Non-root user that ships with the .NET images. Listens on 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartSis.Api.dll"]
