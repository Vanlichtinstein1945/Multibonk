using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Il2Cpp;
using MelonLoader;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Multibonk.Networking
{
    public static class SteamNetworking
    {
        public const int Port = 1;

        private static HSteamListenSocket _listen;
        private static Dictionary<HSteamNetConnection, CSteamID> _peers = new Dictionary<HSteamNetConnection, CSteamID>();
        private static HSteamNetConnection _hostConn;

        private static Callback<SteamNetConnectionStatusChangedCallback_t> _statusChange;

        public static bool IsHost;
        public static CSteamID HostID;
        public static CSteamID SelfID => SteamUser.GetSteamID();

        private static bool _barrierActive;
        private static int _expectedInitCount;
        private static List<NetInitObject> _pendingInit = null;

        private static HashSet<ulong> _awaitingReady = new HashSet<ulong>();
        private const int INIT_OBJECTS_PER_CHUNK = 36;

        private enum Msg : byte {
            Snapshot = 1,
            InitHeader = 2,
            InitChunk = 3,
            ClientReady = 4,
            StartGame = 5,
            AbortInit = 6,
            PlayerLeft = 7
        }

        private class RemoteReplica
        {
            public GameObject replica;
            public Animator animator;
            public Transform posTrans;
            public Transform rotTrans;
            public Helpers.AnimBits lastAnim;
            public Vector3 lastPos;
            public Vector3 lastEuler;
        }

        private static Dictionary<CSteamID, RemoteReplica> _replicas = new Dictionary<CSteamID, RemoteReplica>();

        private static Animator _localAnimator;
        private static Transform _localPosTrans;
        private static Transform _localRotTrans;

        private static float _sendAccum;

        public static void HostBeginInitBarrier(List<NetInitObject> objects)
        {
            if (!IsHost) return;
            _barrierActive = true;

            Time.timeScale = 0f;

            var lobby = LobbyManager.Instance?.LobbyID ?? CSteamID.Nil;
            _awaitingReady.Clear();
            if (lobby != CSteamID.Nil)
            {
                var owner = SteamMatchmaking.GetLobbyOwner(lobby);
                int n = SteamMatchmaking.GetNumLobbyMembers(lobby);
                for (int i = 0; i < n; i++)
                {
                    var id = SteamMatchmaking.GetLobbyOwner(lobby);
                    if (id == owner) continue;
                    _awaitingReady.Add(id.m_SteamID);
                }
            }

            if (_awaitingReady.Count == 0)
            {
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg("[NET] No clients to wait on, starting game");
                HostBroadcastStart();
                return;
            }

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[NET] Pausing game until clients are ready");

            HostSendInit(objects);
        }

        public static void ClientBeginInitBarrier()
        {
            if (IsHost) return;
            _barrierActive = true;
            Time.timeScale = 0f;
        }

        public struct NetInitObject
        {
            public int prefabKey;
            public float px, py, pz;
            public float rx, ry, rz;
            public float sx, sy, sz;
        }

        private static void HostSendInit(List<NetInitObject> objs)
        {
            using (var ms = new MemoryStream(1 + 4))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)Msg.InitHeader);
                bw.Write(objs.Count);
                BroadcastUnreliable(ms.GetBuffer(), (int)ms.Length);
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg("[NET] Sent InitHeader packet");
            }

            int idx = 0;
            var buf = new MemoryStream(4096);
            var bw2 = new BinaryWriter(buf);
            while (idx < objs.Count)
            {
                buf.SetLength(0);
                bw2.Write((byte)Msg.InitChunk);
                int take = System.Math.Min(INIT_OBJECTS_PER_CHUNK, objs.Count - idx);
                bw2.Write(take);

                for (int i = 0; i < take; i++)
                {
                    var o = objs[idx];
                    bw2.Write(o.prefabKey);
                    bw2.Write(o.px); bw2.Write(o.py); bw2.Write(o.pz);
                    bw2.Write(o.rx); bw2.Write(o.ry); bw2.Write(o.rz);
                    bw2.Write(o.sx); bw2.Write(o.sy); bw2.Write(o.sz);
                }

                BroadcastUnreliable(buf.GetBuffer(), (int)buf.Length);
                idx += take;
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[NET] Sent InitChunk packet with {take} objects");
            }

            MelonCoroutines.Start(HostInitTimeoutWatchdog(10f));
        }

        private static System.Collections.IEnumerator HostInitTimeoutWatchdog(float seconds)
        {
            float t = seconds;
            while (_barrierActive && _awaitingReady.Count > 0 && t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_barrierActive) yield break;

            if (Config.VerboseSteamworks)
                MelonLogger.Warning($"[NET] Init barrier timeout; missing {_awaitingReady.Count} clients");
            HostBroadcastAbort();
        }

        private static void HostBroadcastStart()
        {
            using (var ms = new MemoryStream(1))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)Msg.StartGame);
                BroadcastUnreliable(ms.GetBuffer(), (int)ms.Length);
            }
            _barrierActive = false;
            _awaitingReady.Clear();
            Time.timeScale = 1f;
            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[NET] Broadcasting to start match");
        }

        private static void HostBroadcastAbort()
        {
            using (var ms = new MemoryStream(1))
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)Msg.AbortInit);
                BroadcastUnreliable(ms.GetBuffer(), (int)ms.Length);
            }
            _barrierActive = false;
            _awaitingReady.Clear();
            Time.timeScale = 1f;
            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[NET] Aborting match init sync");
        }

        public static void BindLocal(Animator anim, Transform tf1, Transform tf2)
        {
            _localAnimator = anim; _localPosTrans = tf1; _localRotTrans = tf2;
        }

        public static void BindRemote(CSteamID id, GameObject obj, Animator anim, Transform tf1, Transform tf2)
        {
            _replicas[id] = new RemoteReplica { replica = obj, animator = anim, posTrans = tf1, rotTrans = tf2 };
        }

        public static void Init(bool isHost, CSteamID hostID)
        {
            GameData.IsMultiplayer = true;
            IsHost = isHost;
            HostID = hostID;

            _statusChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnStatus);

            if (IsHost)
            {
                _listen = SteamNetworkingSockets.CreateListenSocketP2P(Port, 0, null);
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[NET] Host listening on P2P port {Port}, socket={_listen.m_HSteamListenSocket}");
            }
            else
            {
                var hostIdent = new SteamNetworkingIdentity();
                hostIdent.SetSteamID(HostID);
                _hostConn = SteamNetworkingSockets.ConnectP2P(ref hostIdent, Port, 0, null);
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[NET] Client connecting to host {HostID} on port {Port} (conn={_hostConn.m_HSteamNetConnection})");
            }
        }

        public static void Shutdown()
        {
            GameData.IsMultiplayer = false;
            if (IsHost)
            {
                foreach (var kv in _peers)
                    SteamNetworkingSockets.CloseConnection(kv.Key, 0, "Host shutdown", false);
                if (_listen.m_HSteamListenSocket != 0)
                    SteamNetworkingSockets.CloseListenSocket(_listen);
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg("[NET] Closed server");
            }
            else if (_hostConn.m_HSteamNetConnection != 0)
            {
                SteamNetworkingSockets.CloseConnection(_hostConn, 0, "Client shutdown", false);
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg("[NET] Host closed server");
            }
            _peers.Clear();
            _hostConn = default;
            _listen = default;
        }

        public static void Pump()
        {
            PumpReceive();

            if (_peers.Count != 0 || Config.VerboseLocalPlayer)
            {
                const float sendHz = 20f;
                _sendAccum += Time.deltaTime;
                if (_sendAccum >= 1f / sendHz)
                {
                    _sendAccum = 0f;
                    TickSend();
                }
            }
        }

        private static void TickSend()
        {
            if (_localPosTrans == null || _localRotTrans == null) return;

            var pos = _localPosTrans.position;
            var euler = _localRotTrans.eulerAngles;
            var bits = Helpers.AnimSync.Build(_localAnimator);

            short qx = (short)Mathf.Round(pos.x * 100f);
            short qy = (short)Mathf.Round(pos.y * 100f);
            short qz = (short)Mathf.Round(pos.z * 100f);
            short rx = (short)Mathf.Round(euler.x * 100f);
            short ry = (short)Mathf.Round(euler.y * 100f);
            short rz = (short)Mathf.Round(euler.z * 100f);

            using var ms = new MemoryStream(32);
            using var bw = new BinaryWriter(ms);
            bw.Write((byte)Msg.Snapshot);
            bw.Write(SelfID.m_SteamID);
            bw.Write(qx); bw.Write(qy); bw.Write(qz);
            bw.Write(rx); bw.Write(ry); bw.Write(rz);
            bw.Write((byte)bits);

            var buf = ms.ToArray();

            if (Config.VerboseLocalPlayer)
            {
                MelonLogger.Msg($"[NET] Sending local player pos: x={pos.x} y={pos.y} z={pos.z}");
                MelonLogger.Msg($"[NET] Sending local player rot: x={euler.x} y={euler.y} z={euler.z}");
                MelonLogger.Msg($"[NET] Sending local player anim: {bits}");
            }

            if (_peers.Count == 0) return;
            if (IsHost) BroadcastUnreliable(buf, buf.Length);
            else SendToHostUnreliable(buf, buf.Length);
        }

        private static void PumpReceive()
        {
            const int MAX = 32;
            var ptrs = new System.IntPtr[MAX];

            if (IsHost)
            {
                foreach (var conn in _peers.Keys)
                {
                    int n;
                    while ((n = SteamNetworkingSockets.ReceiveMessagesOnConnection(conn, ptrs, MAX)) > 0)
                        for (int i = 0; i < n; i++) HandleMsgPtr(conn, ptrs[i]);
                }
            }
            else
            {
                if (_hostConn.m_HSteamNetConnection == 0) return;
                int n;
                while ((n = SteamNetworkingSockets.ReceiveMessagesOnConnection(_hostConn, ptrs, MAX)) > 0)
                    for (int i = 0; i < n; i++) HandleMsgPtr(_hostConn, ptrs[i]);
            }
        }

        private static void HandleMsgPtr(HSteamNetConnection from, System.IntPtr pMsg)
        {
            if (pMsg == System.IntPtr.Zero) return;

            var msg = (SteamNetworkingMessage_t)Marshal.PtrToStructure(pMsg, typeof(SteamNetworkingMessage_t));

            try
            {
                byte[] data = null;
                int len = msg.m_cbSize;
                if (len > 0 && msg.m_pData != System.IntPtr.Zero)
                {
                    data = new byte[len];
                    Marshal.Copy(msg.m_pData, data, 0, len);
                }

                HandlePayload(from, data, len);
            }
            finally
            {
                SteamNetworkingMessage_t.Release(pMsg);
            }
        }

        private static void HandlePayload(HSteamNetConnection from, byte[] buf, int len)
        {
            if (buf == null || len <= 0) return;

            using var ms = new MemoryStream(buf, 0, len, writable: false);
            using var br = new BinaryReader(ms);

            var type = (Msg)br.ReadByte();
            switch (type)
            {
                case Msg.Snapshot:
                    HandlePlayerUpdate(from, buf, len, br);
                    break;

                case Msg.InitHeader:
                    HandleInitHeader(br);
                    break;

                case Msg.InitChunk:
                    HandleInitChunk(br);
                    break;

                case Msg.StartGame:
                    if (IsHost) break;
                    _barrierActive = false;
                    _expectedInitCount = 0;
                    _pendingInit = null;
                    Time.timeScale = 1f;
                    if (Config.VerboseSteamworks)
                        MelonLogger.Msg("[NET] Starting game");
                    break;

                case Msg.AbortInit:
                    if (IsHost) break;
                    _barrierActive = false;
                    _expectedInitCount = 0;
                    _pendingInit = null;
                    Time.timeScale = 1f;
                    if (Config.VerboseSteamworks)
                        MelonLogger.Warning("[NET] Host aborted init sync");
                    break;

                case Msg.ClientReady:
                    if (!IsHost) break;
                    var id = _peers[from];
                    _awaitingReady.Remove(id.m_SteamID);
                    if (_barrierActive && _awaitingReady.Count == 0)
                        HostBroadcastStart();
                    break;

                case Msg.PlayerLeft:
                    ulong id64 = br.ReadUInt64();
                    var id2 = new CSteamID(id64);
                    if (_replicas.TryGetValue(id2, out var replica) && replica.replica)
                    {
                        Object.Destroy(replica.replica);
                    }
                    _replicas.Remove(id2);
                    break;
            }            
        }

        private static void HandlePlayerUpdate(HSteamNetConnection from, byte[] buf, int len, BinaryReader br)
        {
            ulong who = br.ReadUInt64();
            short qx = br.ReadInt16(), qy = br.ReadInt16(), qz = br.ReadInt16();
            short rx = br.ReadInt16(), ry = br.ReadInt16(), rz = br.ReadInt16();
            var bits = (Helpers.AnimBits)br.ReadByte();

            var id = new CSteamID(who);

            if (IsHost)
                foreach (var kv in _peers)
                    if (kv.Key.m_HSteamNetConnection != from.m_HSteamNetConnection)
                    {
                        if (Config.VerboseSteamworks)
                            MelonLogger.Msg($"[NET] Rebroadcasting player update from {_peers[from]}");
                        SendUnreliable(kv.Key, buf, len);
                    }

            EnsureRemoteReplica(id);
            ApplySnapshot(id, qx, qy, qz, rx, ry, rz, bits);
        }

        private static void HandleInitHeader(BinaryReader br)
        {
            if (IsHost) return;
            _expectedInitCount = br.ReadInt32();
            _pendingInit = new List<NetInitObject>(_expectedInitCount);
            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[NET] Received InitGame header from host. Expecting to receive {_expectedInitCount} objects");
        }

        private static void HandleInitChunk(BinaryReader br)
        {
            if (IsHost) return;
            int n = br.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                NetInitObject o;
                o.prefabKey = br.ReadInt32();
                o.px = br.ReadSingle(); o.py = br.ReadSingle(); o.pz = br.ReadSingle();
                o.rx = br.ReadSingle(); o.ry = br.ReadSingle(); o.rz = br.ReadSingle();
                o.sx = br.ReadSingle(); o.sy = br.ReadSingle(); o.sz = br.ReadSingle();
                _pendingInit.Add(o);
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[NET] Received object {_pendingInit.Count}'s data from host: prefabKey={o.prefabKey} px={o.px} py={o.py} pz={o.pz} rx={o.rx} ry={o.ry} rz={o.rz} sx={o.sx} sy={o.sy} sz={o.sz}");
            }

            if (_pendingInit.Count >= _expectedInitCount)
            {
                ClientApplyInit(_pendingInit);
                ClientSendReady();
            }
        }

        private static void ClientApplyInit(List<NetInitObject> objs)
        {
            var active = SceneManager.GetActiveScene();
            for (int i = 0; i < objs.Count; i++)
            {
                var o = objs[i];
                var prefab = PrefabFromKey(o.prefabKey);
                if (prefab == null) continue;

                var go = Object.Instantiate(prefab);
                var tf = go.transform;
                tf.position = new Vector3(o.px, o.py, o.pz);
                tf.eulerAngles = new Vector3(o.rx, o.ry, o.rz);
                tf.localScale = new Vector3(o.sx, o.sy, o.sz);

                if (go.scene != active)
                    SceneManager.MoveGameObjectToScene(go, active);
            }
            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[NET] Created {objs.Count} objects from host");
        }

        private static GameObject PrefabFromKey(int key)
        {
            return null;
        }

        private static void ClientSendReady()
        {
            var ms = new MemoryStream(1);
            var bw = new BinaryWriter(ms);
            bw.Write((byte)Msg.ClientReady);

            if (_hostConn.m_HSteamNetConnection != 0)
                SendUnreliable(_hostConn, ms.GetBuffer(), (int)ms.Length);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[NET] Sent client ready packet to host");
        }

        private static void EnsureRemoteReplica(CSteamID id)
        {
            if (_replicas.ContainsKey(id) || id == SelfID) return;

            var lobby = LobbyManager.Instance?.LobbyID ?? CSteamID.Nil;
            int eInt = 0;
            var s = SteamMatchmaking.GetLobbyMemberData(lobby, id, LobbyManager.Keys.Char);
            int.TryParse(s, out eInt);
            var who = (ECharacter)eInt;

            var cd = DataManager.Instance.GetCharacterData(who);
            if (cd == null || cd.prefab == null)
            {
                MelonLogger.Error("$[NET] No prefab for {who} (member {id})!");
                return;
            }

            var root = new GameObject($"Remote_{id.m_SteamID}");
            var go = Object.Instantiate(cd.prefab, root.transform, false);
            var anim = go.GetComponent<Animator>();

            var active = SceneManager.GetActiveScene();
            if (root.scene != active)
                SceneManager.MoveGameObjectToScene(root, active);

            _replicas[id] = new RemoteReplica { replica = root, animator = anim, posTrans = root.transform, rotTrans = go.transform };

            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[NET] Created and bound remote player for {id}");
        }

        private static void SendUnreliable(HSteamNetConnection conn, byte[] buf, int len)
        {
            if (conn.m_HSteamNetConnection == 0 || buf == null || len <= 0) return;
            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                System.IntPtr ptr = handle.AddrOfPinnedObject();
                SteamNetworkingSockets.SendMessageToConnection(conn, ptr, (uint)len, 0, out long _);
            }
            finally { handle.Free(); }
        }

        private static void BroadcastUnreliable(byte[] buf, int len)
        {
            foreach (var kv in _peers) SendUnreliable(kv.Key, buf, len);
        }

        private static void SendToHostUnreliable(byte[] buf, int len)
        {
            if (_hostConn.m_HSteamNetConnection != 0) SendUnreliable(_hostConn, buf, len);
        }

        private static void ApplySnapshot(CSteamID id, short qx, short qy, short qz,
                                          short rx, short ry, short rz, Helpers.AnimBits bits)
        {
            if (!_replicas.TryGetValue(id, out var rep) || rep.posTrans == null || rep.rotTrans == null) return;

            var pos = new Vector3(qx / 100f, qy / 100f, qz / 100f);
            var euler = new Vector3(rx / 100f, ry / 100f, rz / 100f);

            rep.posTrans.position = pos;
            rep.rotTrans.eulerAngles = euler;
            Helpers.AnimSync.Apply(rep.animator, bits);

            rep.lastPos = pos;
            rep.lastEuler = euler;
            rep.lastAnim = bits;

            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[NET] Received snapshot for {id}: posX={qx} posY={qy} posZ={qz} rotX={rx} rotY={ry} rotZ={rz} animBits={bits}");
        }

        private static void OnConnStatus(SteamNetConnectionStatusChangedCallback_t cb)
        {
            var info = cb.m_info;

            switch (cb.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    if (IsHost && cb.m_hConn.m_HSteamNetConnection != 0)
                    {
                        var id = info.m_identityRemote.GetSteamID();
                        if (!IsLobbyMember(id))
                        {
                            SteamNetworkingSockets.CloseConnection(cb.m_hConn, 0, "Not in lobby", false);
                            return;
                        }
                        SteamNetworkingSockets.AcceptConnection(cb.m_hConn);
                        SteamNetworkingSockets.SetConnectionName(cb.m_hConn, id.m_SteamID.ToString());
                        _peers[cb.m_hConn] = id;
                        if (Config.VerboseSteamworks)
                            MelonLogger.Msg($"[NET] Accepted connection from {id}");
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    if (Config.VerboseSteamworks)
                        MelonLogger.Msg(IsHost
                            ? $"[NET] Peer connected: {_peers[cb.m_hConn]}"
                            : $"[NET] Connected to host {HostID}");
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    if (IsHost)
                    {
                        if (_peers.TryGetValue(cb.m_hConn, out var steamID))
                        {
                            if (_replicas.TryGetValue(steamID, out var replica) && replica.replica)
                            {
                                Object.Destroy(replica.replica);
                            }
                            _replicas.Remove(steamID);
                            _peers.Remove(cb.m_hConn);
                            HostBroadcastPlayerLeft(steamID);
                            if (Config.VerboseSteamworks)
                                MelonLogger.Msg($"[NET] Peer {steamID} disconnected; destroyed renderer.");
                        }
                    }
                    else
                    {
                        if (Config.VerboseSteamworks)
                            MelonLogger.Msg("[NET] Host disconnected, returning to lobby");
                        _barrierActive = false;
                        _expectedInitCount = 0;
                        _pendingInit.Clear();
                        Time.timeScale = 1f;
                        if (SceneManager.GetActiveScene().name != "MainMenu")
                            SceneManager.LoadScene("MainMenu");
                        else
                        {
                            LobbyManager.Instance.LeaveLobby();

                            var ui = GameObject.Find("UI");
                            if (Helpers.ErrorIfNull(ui, "[NET] No UI game object found!")) return;
                            var lobbyMenu = ui.transform.Find("Tabs/LobbyMenu");
                            if (Helpers.ErrorIfNull(lobbyMenu, "[NET] No LobbyMenu game object found!")) return;

                            ui.GetComponent<MainMenu>().SetWindow(lobbyMenu.gameObject);
                            lobbyMenu.GetComponent<Window>().FocusWindow();
                        }
                    }
                        break;
            }
        }

        public static void HostBroadcastPlayerLeft(CSteamID steamID)
        {
            using var ms = new MemoryStream(1 + 8);
            using var bw = new BinaryWriter(ms);
            bw.Write((byte)Msg.PlayerLeft);
            bw.Write(steamID.m_SteamID);
            BroadcastUnreliable(ms.GetBuffer(), (int)ms.Length);
        }

        private static bool IsLobbyMember(CSteamID id)
        {
            var lobby = LobbyManager.Instance?.LobbyID ?? CSteamID.Nil;
            if (lobby == CSteamID.Nil) return false;

            for (int i = 0, n = SteamMatchmaking.GetNumLobbyMembers(lobby); i < n; i++)
                if (SteamMatchmaking.GetLobbyMemberByIndex(lobby, i) == id) return true;
            return false;
        }
    }
}
