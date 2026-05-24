using System;
using System.IO;
using System.Threading;
using VelcroNet;

namespace VelcroNet.Server;

internal static class Program
{
    private static void Main(string[] args)
    {
        string mapPath = args.Length > 0 ? args[0] : Path.Combine("maps", "level01.json");

        Console.WriteLine($"[VelcroNet Server] Starting — map: {mapPath}");
        Console.WriteLine($"[VelcroNet Server] Tick rate: {1f / SimulationConstants.FixedTimestep:F0} Hz");

        var world = new PhysicsWorldManager(WorldConfig.Default);

        if (File.Exists(mapPath))
        {
            var loader = new MapLoader();
            loader.LoadInto(world, mapPath);
            Console.WriteLine($"[VelcroNet Server] Map loaded: {mapPath}");
        }
        else
        {
            Console.WriteLine($"[VelcroNet Server] Warning: map file '{mapPath}' not found. Starting with empty world.");
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("[VelcroNet Server] Shutdown requested.");
        };

        var loop = new ServerTickLoop(world);
        loop.SetSnapshotCallback((states, count, tick) =>
        {
            // TODO: broadcast snapshot to connected clients via your transport of choice.
            // See examples/LiteNetLibExample for a complete reference implementation.
        });

        Console.WriteLine("[VelcroNet Server] Running. Press Ctrl+C to stop.");
        loop.Run(cts.Token);
        Console.WriteLine("[VelcroNet Server] Stopped.");
    }
}
