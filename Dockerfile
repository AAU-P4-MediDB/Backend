# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore dependencies first (layer cache friendly)
COPY Backend.csproj .
RUN dotnet restore --runtime linux-musl-x64

# Copy source and publish
COPY . .
RUN dotnet publish Backend.csproj \
    --configuration Release \
    --runtime linux-musl-x64 \
    --self-contained false \
    --output /app/publish \
    -p:PublishSingleFile=false \
    -p:UseAppHost=false

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create a non-root user to run the app
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup --no-create-home appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Lock down ownership
RUN chown -R appuser:appgroup /app

USER appuser

# ASP.NET Core listens on 8080 by default in .NET 8+
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend.dll"]
