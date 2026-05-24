# LiteNetLib Reference Example

A complete, working implementation of AetherNet networking using [LiteNetLib](https://github.com/RevenantX/LiteNetLib).

## Files

- `Server/LiteNetLibNetworkProvider.cs` — implements `INetworkStateProvider`, broadcasts full snapshots each tick to all connected clients via UDP (unreliable).
- `Unity/LiteNetLibClientBridge.cs` — Unity `MonoBehaviour` that receives packets, feeds them into `StateInterpolator`, and applies interpolated state.

## Setup

### Server

```bash
dotnet add package LiteNetLib
```

```csharp
var provider = new LiteNetLibNetworkProvider(port: 7777);
world.SetNetworkProvider(provider);

loop.Run(cts.Token); // while running, call provider.PollEvents() each tick
```

### Unity Client

1. Import LiteNetLib DLL into `Assets/Plugins/`.
2. Add `LiteNetLibClientBridge` to your scene manager GameObject.
3. Set `_serverAddress` and `_serverPort` in the Inspector.

## Swapping the Transport

Replace `LiteNetLibNetworkProvider` with any class that implements `INetworkStateProvider`.
`StateSerializer` provides zero-alloc byte-level serialization for `EntityState[]` arrays
that works with any transport's send buffer API.
