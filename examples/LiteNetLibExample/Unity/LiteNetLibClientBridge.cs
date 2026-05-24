// ─────────────────────────────────────────────────────────────────────────────
// REFERENCE EXAMPLE — Unity client-side bridge using LiteNetLib
// Attach to a GameObject alongside VelcroViewManager.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using VelcroNet;
using VelcroNet.Network;

namespace VelcroNet.Examples.LiteNetLib
{
    public sealed class LiteNetLibClientBridge : MonoBehaviour, INetEventListener
    {
        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private int    _serverPort    = 7777;

        private NetManager       _client;
        private NetPeer?         _server;
        private StateInterpolator _interpolator;
        private EntityState[]    _interpolatedStates;
        private float            _serverTime;

        private void Awake()
        {
            _client             = new NetManager(this);
            _interpolator       = new StateInterpolator(renderDelaySeconds: 0.1f);
            _interpolatedStates = new EntityState[SimulationConstants.MaxBodies];
        }

        private void Start()
        {
            _client.Start();
            _client.Connect(_serverAddress, _serverPort, "velcronet");
        }

        private void Update()
        {
            _client.PollEvents();
            _serverTime += Time.deltaTime;

            // Sample the interpolated snapshot and apply it to local entities
            int count = _interpolator.Sample(_serverTime, _interpolatedStates);
            ApplySnapshot(_interpolatedStates, count);
        }

        private void OnDestroy() => _client.Stop();

        private void ApplySnapshot(EntityState[] states, int count)
        {
            var manager = VelcroViewManager.Instance;
            if (manager == null) return;

            for (int i = 0; i < count; i++)
            {
                // In a real implementation you'd update the visual transform via
                // VelcroViewManager or network-managed ghost objects here.
            }
        }

        // ─── INetEventListener ────────────────────────────────────────────────

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            uint tick  = reader.GetUInt();
            int  count = reader.GetInt();

            // Deserialize EntityState payload
            var states     = new EntityState[count]; // NOTE: allocates — preallocate in production
            int byteCount  = count * System.Runtime.InteropServices.Marshal.SizeOf<EntityState>();
            byte[] payload = reader.GetRemainingBytes();

            StateSerializer.Deserialize(payload, 0, byteCount, states);
            _interpolator.ReceiveSnapshot(tick, _serverTime, states, count);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            _server = peer;
            Debug.Log($"[VelcroNet Example] Connected to server: {peer.EndPoint}");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            _server = null;
            Debug.Log("[VelcroNet Example] Disconnected from server.");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) => request.Reject();
    }
}
