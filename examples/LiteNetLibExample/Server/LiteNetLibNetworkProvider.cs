// ─────────────────────────────────────────────────────────────────────────────
// REFERENCE EXAMPLE — not part of the core AetherNet package
//
// Shows how to implement INetworkStateProvider with LiteNetLib.
// Add package: dotnet add package LiteNetLib
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using AetherNet;
using AetherNet.Network;

namespace AetherNet.Examples.LiteNetLib
{
    /// <summary>
    /// Broadcasts a full physics snapshot to all connected peers after each tick.
    /// Swap this class out for any other transport by implementing INetworkStateProvider.
    /// </summary>
    public sealed class LiteNetLibNetworkProvider : INetworkStateProvider, INetEventListener
    {
        private readonly NetManager    _server;
        private readonly NetDataWriter _writer;
        private readonly byte[]        _sendBuffer;

        public LiteNetLibNetworkProvider(int port = 7777)
        {
            _server     = new NetManager(this);
            _writer     = new NetDataWriter();
            _sendBuffer = new byte[SimulationConstants.MaxBodies * 40 + 32]; // rough upper bound
            _server.Start(port);
            Console.WriteLine($"[AetherNet Example] Listening on port {port}");
        }

        public void PollEvents() => _server.PollEvents();

        // ─── INetworkStateProvider ────────────────────────────────────────────

        void INetworkStateProvider.OnTickComplete(uint tick, EntityState[] states, int count)
        {
            // Header: tick (uint) + count (int)
            _writer.Reset();
            _writer.Put(tick);
            _writer.Put(count);

            // Payload: raw EntityState bytes
            int byteCount = StateSerializer.Serialize(states, count, _sendBuffer, 0);
            _writer.Put(_sendBuffer, 0, byteCount);

            _server.SendToAll(_writer, DeliveryMethod.Unreliable);
        }

        void INetworkStateProvider.ApplySnapshot(uint tick, EntityState[] states, int count)
        {
            // Server is authoritative — no need to apply snapshots from clients here.
            // Override in a client-only or relay implementation.
        }

        // ─── INetEventListener ────────────────────────────────────────────────

        public void OnPeerConnected(NetPeer peer)
            => Console.WriteLine($"[AetherNet Example] Client connected: {peer.EndPoint}");

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
            => Console.WriteLine($"[AetherNet Example] Client disconnected: {peer.EndPoint} ({info.Reason})");

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("aethernet");
    }
}
