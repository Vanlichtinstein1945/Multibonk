using System.Collections.Generic;
using Il2Cpp;
using Il2CppAssets.Scripts._Data.MapsAndStages;
using Il2CppAssets.Scripts.Managers;
using MelonLoader;
using Steamworks;

namespace Multibonk
{
    public class SteamManager
    {
        public static void SteamInit()
        {
            var steamOk = SteamAPI.Init();
            if (!steamOk)
            {
                MelonLogger.Error("SteamAPI failed to init!");
                return;
            }
            if (Config.VerboseSteamworks)
                MelonLogger.Msg("SteamAPI initialized");
            LobbyManager.Initialize();
        }
    }

    public class LobbyManager
    {
        public static LobbyManager Instance;

        public const int MaxMembers = 4;

        public CSteamID LobbyID = CSteamID.Nil;

        private static class Keys
        {
            public const string Name = "name";
            public const string Ver = "version";
            public const string Mode = "mode";

            public const string Map = "cfg.map";
            public const string Tier = "cfg.tier";
            public const string Chall = "cfg.challenge";
            public const string Music = "cfg.music";
            public const string Seed = "cfg.seed";
            public const string Rev = "cfg.rev";
            public const string Start = "cfg.start";

            public const string Char = "char";
        }

        public CSteamID OwnerID => (LobbyID != CSteamID.Nil) ? SteamMatchmaking.GetLobbyOwner(LobbyID) : CSteamID.Nil;

        private Callback<LobbyCreated_t> _cbLobbyCreated;
        private Callback<LobbyEnter_t> _cbLobbyEnter;
        private Callback<LobbyChatUpdate_t> _cbLobbyChatUpdate;
        private Callback<GameLobbyJoinRequested_t> _cbGameLobbyJoinRequested;
        private Callback<LobbyDataUpdate_t> _cbLobbyDataUpdate;

        private CallResult<LobbyMatchList_t> _crLobbyMatchList;

        private string _lastStartToken;

        public static void Initialize()
        {
            if (Instance != null)
                Shutdown();

            Instance = new LobbyManager();
            Instance.SetupCallbacks();

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("LobbyManager initialized");
        }

        public static void Shutdown()
        {
            Instance?.Cleanup();
            Instance = null;

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("LobbyManager cleared");
        }

        private void Cleanup()
        {
            _cbLobbyCreated?.Dispose();
            _cbLobbyEnter?.Dispose();
            _cbLobbyChatUpdate?.Dispose();
            _cbGameLobbyJoinRequested?.Dispose();
            _cbLobbyDataUpdate?.Dispose();
            _crLobbyMatchList?.Dispose();
            _crLobbyMatchList = null;

            LeaveLobby();
        }

        private void SetupCallbacks()
        {
            _cbLobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _cbLobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            _cbLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            _cbGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            _cbLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);

            _crLobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
        }

        public void Update()
        {
            SteamAPI.RunCallbacks();
        }

        public void CreatePublicLobby()
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MaxMembers);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("Creating steam lobby");
        }

        private void OnLobbyCreated(LobbyCreated_t cb)
        {
            if (cb.m_eResult != EResult.k_EResultOK)
            {
                MelonLogger.Warning("Lobby create failed: " + cb.m_eResult);
                return;
            }

            LobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("Lobby created: " + LobbyID);

            SteamMatchmaking.SetLobbyJoinable(LobbyID, true);
            SteamMatchmaking.SetLobbyData(LobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");
            SteamMatchmaking.SetLobbyData(LobbyID, "version", "1");
            SteamMatchmaking.SetLobbyData(LobbyID, "mode", "coop");

            SteamFriends.SetRichPresence("status", "In Lobby");

            SteamFriends.ActivateGameOverlayInviteDialog(LobbyID);
        }

        public void RequestLobbyList(string modeFilter = null)
        {
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);
            if (!string.IsNullOrEmpty(modeFilter))
            {
                SteamMatchmaking.AddRequestLobbyListStringFilter("mode", modeFilter, ELobbyComparison.k_ELobbyComparisonEqual);
            }

            SteamAPICall_t h = SteamMatchmaking.RequestLobbyList();
            _crLobbyMatchList.Set(h);
        }

        private void OnLobbyMatchList(LobbyMatchList_t cb, bool bIOFailure)
        {
            if (bIOFailure)
            {
                MelonLogger.Warning("Lobby list IO failure");
                return;
            }

            int count = (int)cb.m_nLobbiesMatching;
            MelonLogger.Msg("Found lobbies: " + count);

            for (int i = 0; i < count; i++)
            {
                CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
                SteamMatchmaking.RequestLobbyData(id);
            }
        }

        public void JoinLobby(CSteamID lobbyID)
        {
            SteamMatchmaking.JoinLobby(lobbyID);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"Joining lobby: {lobbyID}");
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
        {
            MelonLogger.Msg("Invite/Join requested for lobby: " + cb.m_steamIDLobby);
            JoinLobby(cb.m_steamIDLobby);
        }

        private void OnLobbyEnter(LobbyEnter_t cb)
        {
            LobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("Entered lobby: " + LobbyID);

            SteamMatchmaking.SetLobbyMemberData(LobbyID, "ready", "0");

            CSteamID owner = SteamMatchmaking.GetLobbyOwner(LobbyID);
            bool isHost = owner == SteamUser.GetSteamID();

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("IsHost: " + isHost);
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t cb)
        {
            MelonLogger.Msg("Lobby chat update: " + cb.m_ulSteamIDLobby);
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t cb)
        {
            var lobby = new CSteamID(cb.m_ulSteamIDLobby);

            if (cb.m_ulSteamIDLobby == cb.m_ulSteamIDMember)
            {
                var start = SteamMatchmaking.GetLobbyData(lobby, Keys.Start);
                if (!string.IsNullOrEmpty(start) && start != _lastStartToken)
                {
                    _lastStartToken = start;

                    if (!TryResolveConfigFromLobby(out var err))
                    {
                        MelonLogger.Error($"[LOBBY] Could not resolve config: {err}");
                        return;
                    }

                    bool isHost = SteamUser.GetSteamID() == OwnerID;

                    SteamNetworking.Init(isHost, OwnerID);

                    MapController.StartNewMap(GameData.RunConfig);
                }

                if (Config.VerboseSteamworks)
                {
                    string name = SteamMatchmaking.GetLobbyData(lobby, Keys.Name);
                    string ver = SteamMatchmaking.GetLobbyData(lobby, Keys.Ver);
                    string mode = SteamMatchmaking.GetLobbyData(lobby, Keys.Mode);
                    MelonLogger.Msg($"Lobby data: name=\"{name}\" ver={ver} mode={mode}");
                }
            }
            else
            {
                var member = new CSteamID(cb.m_ulSteamIDMember);
                string ready = SteamMatchmaking.GetLobbyMemberData(lobby, member, "ready");
                string charStr = SteamMatchmaking.GetLobbyMemberData(lobby, member, Keys.Char);

                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"Member {member} ready={ready} char={charStr}");
            }
        }

        public void LeaveLobby()
        {
            if (LobbyID != CSteamID.Nil)
            {
                SteamMatchmaking.LeaveLobby(LobbyID);
                SteamFriends.SetRichPresence("status", "");

                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"Left lobby: {LobbyID}");

                LobbyID = CSteamID.Nil;
            }
        }

        public void HostSetConfig(EMap eMap, int tierIndex, string challengeNameOrIndex, int musicIndex, int seed)
        {
            if (SteamUser.GetSteamID() != OwnerID) { MelonLogger.Warning("HostSetConfig called by non-owner"); return; }
            if (LobbyID == CSteamID.Nil) return;

            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Map, ((int)eMap).ToString());
            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Tier, tierIndex.ToString());
            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Chall, challengeNameOrIndex);
            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Music, musicIndex.ToString());
            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Seed, seed.ToString());

            int rev = int.TryParse(SteamMatchmaking.GetLobbyData(LobbyID, Keys.Rev), out var r) ? r + 1 : 1;
            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Rev, rev.ToString());

            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[LOBBY] Pushed config: map={eMap} tier={tierIndex} chall='{challengeNameOrIndex ?? "null"}' music={musicIndex} seed={seed} rev={rev}");
        }

        public void SetMyCharacter(int eCharacterInt)
        {
            if (LobbyID == CSteamID.Nil) return;
            SteamMatchmaking.SetLobbyMemberData(LobbyID, Keys.Char, eCharacterInt.ToString());
            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[LOBBY] Set my character={eCharacterInt}");
        }

        public void HostBroadcastStart()
        {
            if (SteamUser.GetSteamID() != OwnerID) { MelonLogger.Warning("HostBroadcastStart called by non-owner"); return; }
            if (LobbyID == CSteamID.Nil) return;

            SteamMatchmaking.SetLobbyJoinable(LobbyID, false);

            var token = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            SteamMatchmaking.SetLobbyData(LobbyID, Keys.Start, token);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[LOBBY] Start token={token}");
        }

        private bool TryResolveConfigFromLobby(out string error)
        {
            error = null;
            if (LobbyID == CSteamID.Nil) { error = "No lobby"; return false; }

            if (!int.TryParse(SteamMatchmaking.GetLobbyData(LobbyID, Keys.Map), out var eMapInt)) { error = "map missing"; return false; }
            if (!int.TryParse(SteamMatchmaking.GetLobbyData(LobbyID, Keys.Tier), out var tierIdx)) { error = "tier missing"; return false; }
            var challStr = SteamMatchmaking.GetLobbyData(LobbyID, Keys.Chall);
            int.TryParse(SteamMatchmaking.GetLobbyData(LobbyID, Keys.Music), out var musicIdx);
            int.TryParse(SteamMatchmaking.GetLobbyData(LobbyID, Keys.Seed), out var seed);

            var mapData = DataManager.Instance.GetMap((EMap)eMapInt);
            if (mapData == null) { error = "mapData null"; return false; }
            var stageData = mapData.stages[tierIdx];

            ChallengeData challenge = null;
            if (!string.IsNullOrEmpty(challStr))
            {
                challenge = null;
            }

            GameData.MapData = mapData;
            GameData.StageData = stageData;
            GameData.MapTierIndex = tierIdx;
            GameData.ChallengeData = challenge;
            GameData.MusicIndex = musicIdx;
            GameData.Seed = seed;

            return true;
        }
    }

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

        public static void Init(bool isHost, CSteamID hostID)
        {
            IsHost = isHost;
            HostID = hostID;

            _statusChange = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnStatus);

            if (IsHost)
            {
                _listen = SteamNetworkingSockets.CreateListenSocketP2P(Port, 0, null);
                MelonLogger.Msg($"[NET] Host listening on P2P port {Port}, socket={_listen.m_HSteamListenSocket}");
            }
            else
            {
                var hostIdent = new SteamNetworkingIdentity();
                hostIdent.SetSteamID(HostID);
                _hostConn = SteamNetworkingSockets.ConnectP2P(ref hostIdent, Port, 0, null);
                MelonLogger.Msg($"[NET] Client connecting to host {HostID} on port {Port} (conn={_hostConn.m_HSteamNetConnection})");
            }
        }

        public static void Shutdown()
        {
            if (IsHost)
            {
                foreach (var kv in _peers)
                    SteamNetworkingSockets.CloseConnection(kv.Key, 0, "Host shutdown", false);
                if (_listen.m_HSteamListenSocket != 0)
                    SteamNetworkingSockets.CloseListenSocket(_listen);
            }
            else if (_hostConn.m_HSteamNetConnection != 0)
            {
                SteamNetworkingSockets.CloseConnection(_hostConn, 0, "Client shutdown", false);
            }
            _peers.Clear();
            _hostConn = default;
            _listen = default;
        }

        public static void Pump()
        {

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
                        MelonLogger.Msg($"[NET] Host accepted {id}");
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    MelonLogger.Msg(IsHost
                        ? $"[NET] Peer connected: {_peers[cb.m_hConn]}"
                        : $"[NET] Connected to host {HostID}");
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    if (IsHost) _peers.Remove(cb.m_hConn);
                    break;
            }
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
