# Multi-stage build for FraudWatch API
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy solution and project files
COPY FraudWatch.sln .
COPY FraudWatch.Api/FraudWatch.Api.csproj FraudWatch.Api/
COPY FraudWatch.Core/FraudWatch.Core.csproj FraudWatch.Core/
COPY FraudWatch.Infrastructure/FraudWatch.Infrastructure.csproj FraudWatch.Infrastructure/
COPY FraudWatch.Tests/FraudWatch.Tests.csproj FraudWatch.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build in Release mode
RUN dotnet build -c Release --no-restore

# Run tests
RUN dotnet test -c Release --no-build --verbosity normal

# Publish the API project
RUN dotnet publish FraudWatch.Api/FraudWatch.Api.csproj -c Release --no-build -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser

# Copy published output
COPY --from=build /app/publish .

# Configure environment
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Switch to non-root user
USER appuser

EXPOSE 80

ENTRYPOINT ["dotnet", "FraudWatch.Api.dll"]
