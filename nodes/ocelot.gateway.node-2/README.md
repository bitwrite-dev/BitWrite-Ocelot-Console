# ocelot.gateway.node-2

A placeholder for the second node in the fleet. Nothing here yet — this folder
records that the node belongs in the layout, and holds the steps to make it real.

## To make this a node

1. Copy the working node, folder for folder:
   ```bash
   cp -r nodes/ocelot.gateway.node-1/. nodes/ocelot.gateway.node-2/
   rm -rf nodes/ocelot.gateway.node-2/bin nodes/ocelot.gateway.node-2/obj
   ```
2. Rename the project file and fix the namespace in it:
   ```bash
   cd nodes/ocelot.gateway.node-2
   mv ocelot.gateway.node-1.csproj ocelot.gateway.node-2.csproj
   ```
   In `ocelot.gateway.node-2.csproj`, set `<RootNamespace>ocelot.gateway.node2</RootNamespace>`
   (a namespace cannot hold the hyphen the folder name has, so the hyphen is
   dropped there only). Then replace `ocelot.gateway.node1` with
   `ocelot.gateway.node2` in every `.cs` file under the folder.
3. Add the project to `BitWrite.OcelotConsole.slnx`:
   ```xml
   <Project Path="nodes/ocelot.gateway.node-2/ocelot.gateway.node-2.csproj" />
   ```
   inside the `/nodes/` folder.
4. Build and run:
   ```bash
   dotnet run --project nodes/ocelot.gateway.node-2 \
     --ASPNETCORE_URLS=http://localhost:8082
   ```
   Registration uses the assembly name (`ocelot.gateway.node-2`) by default, so no
   settings are needed for the node to register as itself.

## What makes this node different from node-1

Nothing at first — the point of a copy is that three nodes are one layout and one
code path. The differences that matter are settings, not code:

| | node-1 | node-2 |
| --- | --- | --- |
| Gateway name | `ocelot.gateway.node-1` | `ocelot.gateway.node-2` |
| Port | 8080 (or the one you give it) | 8082 in the example above |
| Environment | `Production` by default | set `Gateway:Environment` to serve another one |

The fleet README covers what each setting does, and why the gateway name is the
one that must be unique: the registration is remembered in Redis under it.
