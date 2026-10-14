# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY BitWrite.OcelotControl.slnx .
COPY nodes/ocelot.gateway.node-1/ocelot.gateway.node-1.csproj nodes/ocelot.gateway.node-1/
COPY src/BitWrite.OcelotControl.Application/BitWrite.OcelotControl.Application.csproj src/BitWrite.OcelotControl.Application/
COPY src/BitWrite.OcelotControl.Domain/BitWrite.OcelotControl.Domain.csproj src/BitWrite.OcelotControl.Domain/
COPY src/BitWrite.OcelotControl.Infrastructure/BitWrite.OcelotControl.Infrastructure.csproj src/BitWrite.OcelotControl.Infrastructure/

# Restore dependencies
RUN dotnet restore BitWrite.OcelotControl.slnx

# Copy source
COPY . .

# Build the node
WORKDIR /src/nodes/ocelot.gateway.node-1
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published app
COPY --from=build /app/publish .

# Copy Ocelot config files. The environment the node serves is a setting, so the
# file that has to be present under that name ships with the build output; this
# copy is the belt to the csproj's braces, for a runtime stage that starts from
# a published folder alone.
COPY nodes/ocelot.gateway.node-1/ocelot.json ./ocelot.json
COPY nodes/ocelot.gateway.node-1/ocelot.Production.json ./ocelot.Production.json

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Which environment's configuration this node serves, and what it is called.
# Overridden per node by the fleet, so three nodes are one compose file apart.
ENV GATEWAY_ENVIRONMENT=Production
ENV GATEWAY_NODENAME=ocelot.gateway.node-1

# Expose port
EXPOSE 8080

# Run as non-root
USER appuser

ENTRYPOINT ["dotnet", "ocelot.gateway.node-1.dll"]
