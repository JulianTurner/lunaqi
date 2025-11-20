# ---------- Frontend: Build ----------
FROM node:22-alpine AS fe-build
WORKDIR /app

ENV NG_CLI_ANALYTICS=false

COPY frontend/package*.json ./
RUN npm ci

# Build Angular; with the new application builder the artifacts often land under "browser"
COPY frontend/ .
RUN npm run build -- --configuration=production --output-path=/out \
  && mkdir -p /out_flat \
  && if [ -d /out/browser ]; then cp -r /out/browser/* /out_flat/; else cp -r /out/* /out_flat/; fi

# ---------- Backend: Restore/Publish ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS api-build
WORKDIR /src/api

COPY api/LunaQi.Api/*.csproj ./
RUN dotnet restore

COPY api/LunaQi.Api/. .
RUN dotnet publish -c Release -o /out

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0 \
    COMPlus_EnableDiagnostics=0

# API
COPY --chown=app:app --from=api-build /out ./

# Frontend static files (flattened)
COPY --chown=app:app --from=fe-build /out_flat/ ./wwwroot/

# Use existing non-root 'app' user in base image
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet","LunaQi.Api.dll"]