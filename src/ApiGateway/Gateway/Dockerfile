# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props for consistent versioning
COPY Directory.Build.props .

# Copy project file
COPY src/ApiGateway/Gateway/Gateway.csproj src/ApiGateway/Gateway/

# Restore dependencies
RUN dotnet restore src/ApiGateway/Gateway/Gateway.csproj

# Copy source code
COPY src/ApiGateway/Gateway/ src/ApiGateway/Gateway/

# Build
WORKDIR /src/src/ApiGateway/Gateway
RUN dotnet build Gateway.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Gateway.dll"]
