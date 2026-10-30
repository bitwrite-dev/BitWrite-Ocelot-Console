# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY BitWrite.OcelotConsole.slnx .
COPY nodes/ocelot.gateway.node-2/ocelot.gateway.node-2.csproj nodes/ocelot.gateway.node-2/
COPY src/BitWrite.OcelotConsole.Application/BitWrite.OcelotConsole.Application.csproj src/BitWrite.OcelotConsole.Application/
COPY src/BitWrite.OcelotConsole.Domain/BitWrite.OcelotConsole.Domain.csproj src/BitWrite.OcelotConsole.Domain/
COPY src/BitWrite.OcelotConsole.Infrastructure/BitWrite.OcelotConsole.Infrastructure.csproj src/BitWrite.OcelotConsole.Infrastructure/

# Restore dependencies
RUN dotnet restore BitWrite.OcelotConsole.slnx

# Copy source
COPY . .

# Build the node
WORKDIR /src/nodes/ocelot.gateway.node-2
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Copy Ocelot config files. The environment the node serves is a setting, so the
# file that has to be present under that name ships with the build output; this
# copy is the belt to the csproj's braces, for a runtime stage that starts from
# a published folder alone.
COPY nodes/ocelot.gateway.node-2/ocelot.json ./ocelot.json
COPY nodes/ocelot.gateway.node-2/ocelot.Production.json ./ocelot.Production.json

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8081

# Which environment's configuration this node serves, and what it is called.
# Overridden per node by the fleet, so three nodes are one compose file apart.
ENV GATEWAY_ENVIRONMENT=Production
ENV GATEWAY_NODENAME=ocelot.gateway.node-2

# Expose port
EXPOSE 8081

# Run as non-root
USER appuser

ENTRYPOINT ["dotnet", "ocelot.gateway.node-2.dll"]