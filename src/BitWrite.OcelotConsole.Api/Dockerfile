# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY BitWrite.OcelotConsole.slnx .
COPY src/BitWrite.OcelotConsole.Api/BitWrite.OcelotConsole.Api.csproj src/BitWrite.OcelotConsole.Api/
COPY src/BitWrite.OcelotConsole.Application/BitWrite.OcelotConsole.Application.csproj src/BitWrite.OcelotConsole.Application/
COPY src/BitWrite.OcelotConsole.Domain/BitWrite.OcelotConsole.Domain.csproj src/BitWrite.OcelotConsole.Domain/
COPY src/BitWrite.OcelotConsole.Infrastructure/BitWrite.OcelotConsole.Infrastructure.csproj src/BitWrite.OcelotConsole.Infrastructure/

# Restore dependencies
RUN dotnet restore BitWrite.OcelotConsole.slnx

# Copy source
COPY . .

# Build API
WORKDIR /src/src/BitWrite.OcelotConsole.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Run as non-root
USER appuser

ENTRYPOINT ["dotnet", "BitWrite.OcelotConsole.Api.dll"]