using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using MelonLoader;
using System.Net.Sockets;

namespace Multibonk
{
    public enum NetMsg : byte
    {
        Default = 0
    }

    class Networking : INetEventListener
    {
        public static Networking Instance;
        public NetManager Manager;
        public NetPeer ServerPeer;
        public bool IsServer;
        public NetDataWriter Writer = new NetDataWriter();

        public bool StartServer(int port = 25565)
        {
            IsServer = true;
            Manager = new NetManager(this) { AutoRecycle = true, IPv6Enabled = false };
            Manager.Start(port);
            MelonLogger.Msg($"Server listening on port {port}");
            return true;
        }

        public bool StartClient(string host, int port = 25565)
        {
            IsServer = false;
            Manager = new NetManager(this) { AutoRecycle = true, IPv6Enabled = false };
            Manager.Start();
            Manager.Connect(host, port, "Multibonk");
            MelonLogger.Msg($"Connecting to {host}:{port}");
            return true;
        }

        public void Stop()
        {
            Manager?.Stop();
            Manager = null;
            ServerPeer = null;
        }

        public void Update() => Manager?.PollEvents();

        public void OnPeerConnected(NetPeer peer)
        {
            if (!IsServer)
            {
                ServerPeer = peer;
                return;
            }
            MelonLogger.Msg($"Peer connected: {peer.Address}");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            MelonLogger.Msg($"Peer disconnected: {info.Reason}");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // For checking ping
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            var tag = (NetMsg)reader.GetByte();

            switch (tag)
            {
                default:
                    MelonLogger.Warning($"Unknown NetMsg: {tag}");
                    break;
            }

            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => reader.Recycle();

        public void OnNetworkError(IPEndPoint remoteEndPoint, SocketError socketError) =>
            MelonLogger.Warning($"Network error: {socketError}");

        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey("Multibonk");
    }
}
