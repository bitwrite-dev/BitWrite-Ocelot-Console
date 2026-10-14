# Gateway node fleet

The nodes that serve traffic. A node is an Ocelot 18 process that learns what to
route from the control plane: it subscribes to the publication channels in Redis,
loads the snapshot a notification names, writes the file Ocelot reads, and reloads
its pipeline without restarting.

The control plane holds the desired state — routes, services, settings, snapshots,
publications — and nothing in it serves a request. These processes are the other
half, and the only half a caller ever talks to.

```
nodes/
├── ocelot.gateway.node-1/    the working node
├── ocelot.gateway.node-2/    placeholder — see its README
└── ocelot.gateway.node-3/    placeholder — see its README
```

## What a node is made of

| File | What it is |
| --- | --- |
| `Program.cs` | The host: Ocelot's pipeline, the file it reads, the services that fill it. |
| `Configuration/ConfigurationSubscriber` | Takes a publication notification and loads the snapshot it names. |
| `Configuration/OcelotConfigurationWriter` | Writes the file Ocelot reads and nothing else. |
| `Configuration/FileConfigurationPoller`, `ConfigurationReloader` | Ocelot reloads the file without a restart. |
| `Configuration/PublishedEnvironment` | The one setting that names both the Redis keys and the file. |
| `Configuration/GatewayRegistrar` | Registers the node with the control plane so a publication can name it. |
| `Configuration/GatewayStartupValidator` | Refuses to start on an environment mismatch before it routes anything. |

## The settings a node reads

All of them live under `Gateway:` in appsettings, and each has an environment
variable spelling for a container start.

| Setting | Variable | Default | What it does |
| --- | --- | --- | --- |
| `Gateway:Environment` | `GATEWAY_ENVIRONMENT` | `Production` | Which environment's configuration this node serves. Names both the Ocelot file (`ocelot.{Environment}.json`) and the Redis keys the snapshots live under. |
| `Gateway:NodeName` | `GATEWAY_NODENAME` | the assembly name | What the node registers itself as. Two nodes serving the same environment must differ, or the second shares the first's gateway id. |
| `Gateway:ControlPlaneUrl` | — | `http://localhost:20642` | Where the control plane is. Without it the node still takes publications from Redis but cannot be registered or published to. |
| `Gateway:HeartbeatSeconds` | — | `30` | How often the node reports that it is still there. |

`ConnectionStrings:redis` is the other one, and unlike the rest it has no sensible
default in a real deployment.

## Running three nodes

Each node needs its own name and its own port; the environment can be the same
(three nodes serving production is a cluster) or different (each node serving one
environment is the isolation layout).

```bash
# node-1 (this folder)
dotnet run --project nodes/ocelot.gateway.node-1 \
  --Gateway:ControlPlaneUrl=http://localhost:20642

# node-2 and node-3, once instantiated from node-1
dotnet run --project nodes/ocelot.gateway.node-2 --Gateway:NodeName=ocelot.gateway.node-2 --ASPNETCORE_URLS=http://localhost:8082
dotnet run --project nodes/ocelot.gateway.node-3 --Gateway:NodeName=ocelot.gateway.node-3 --ASPNETCORE_URLS=http://localhost:8083
```

By default a node registers under its assembly name, so node-1 registers as
`ocelot.gateway.node-1` with no settings at all. A node that is copied to another
folder must set `Gateway:NodeName` to the new folder's name — the registration is
remembered in Redis under that name, so the assembled node reuses its gateway id
across restarts instead of registering a duplicate on every start.

## Adding a node

1. Copy `ocelot.gateway.node-1` to `ocelot.gateway.node-N`.
2. Rename the project file to `ocelot.gateway.node-N.csproj`, and its
   `RootNamespace` to `ocelot.gateway.node-N` — a namespace cannot hold the hyphen
   the folder name has, so the hyphen is dropped there only.
3. Add the project to `BitWrite.OcelotControl.slnx`.
4. Copy the test project or extend it; it references the node it tests by path.
5. Set `Gateway:NodeName` (or let the assembly name carry it) and, if the nodes are
   to run on one machine, `ASPNETCORE_URLS`.

See each placeholder folder's README for the same steps in checklist form.

## What a node guarantees

- **It serves the last configuration it could read.** A notification naming a
  snapshot it cannot load leaves the current file in place — a node that emptied
  its configuration on a failed read would stop routing, which is worse than being
  briefly behind.
- **It never follows another environment's publication.** A notification naming a
  different environment is refused, and a node whose startup finds the published
  version for another environment refuses to start. That is the cross-environment
  leak isolation exists to prevent.
- **It never serves editable state.** Only published snapshots are read; a node
  that started late asks which version is current rather than reading what is
  being edited.
- **Registration is best-effort.** A node that cannot reach the control plane
  still serves what it has and retries later — a registration problem is a
  logging problem, not a routing one.
