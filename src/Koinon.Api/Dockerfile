# Multi-stage build for ASP.NET Core API
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

# Copy project files
COPY ["koinon-rms.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]

# Copy all project files
COPY ["src/Koinon.Domain/Koinon.Domain.csproj", "src/Koinon.Domain/"]
COPY ["src/Koinon.Application/Koinon.Application.csproj", "src/Koinon.Application/"]
COPY ["src/Koinon.Infrastructure/Koinon.Infrastructure.csproj", "src/Koinon.Infrastructure/"]
COPY ["src/Koinon.Api/Koinon.Api.csproj", "src/Koinon.Api/"]
COPY ["tests/Koinon.Domain.Tests/Koinon.Domain.Tests.csproj", "tests/Koinon.Domain.Tests/"]
COPY ["tests/Koinon.Application.Tests/Koinon.Application.Tests.csproj", "tests/Koinon.Application.Tests/"]
COPY ["tests/Koinon.Infrastructure.Tests/Koinon.Infrastructure.Tests.csproj", "tests/Koinon.Infrastructure.Tests/"]
COPY ["tests/Koinon.Api.Tests/Koinon.Api.Tests.csproj", "tests/Koinon.Api.Tests/"]

# Copy source code
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

# Restore and build
RUN dotnet restore

RUN dotnet build -c Release --no-restore

RUN dotnet publish -c Release --no-build -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published application
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s \
    CMD curl -f http://localhost:8080/health || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "Koinon.Api.dll"]
