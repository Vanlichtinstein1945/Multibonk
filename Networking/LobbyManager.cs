using Steamworks;
using Il2Cpp;
using Il2CppAssets.Scripts._Data.MapsAndStages;
using Il2CppAssets.Scripts.Managers;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace Multibonk.Networking
{
    public class LobbyManager
    {
        public static LobbyManager Instance;

        public const int MaxMembers = 4;

        internal static CSteamID PendingLobbyJoin = CSteamID.Nil;
        internal static bool PendingOpenLobbyUI = false;

        public static bool HasPendingJoin => PendingLobbyJoin != CSteamID.Nil;

        public CSteamID LobbyID = CSteamID.Nil;

        public static class Keys
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

            public const string Ready = "ready";
        }

        public CSteamID OwnerID => LobbyID != CSteamID.Nil ? SteamMatchmaking.GetLobbyOwner(LobbyID) : CSteamID.Nil;

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
                MelonLogger.Msg("[LOBBY] LobbyManager initialized");
        }

        public static void Shutdown()
        {
            Instance?.Cleanup();
            Instance = null;

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[LOBBY] LobbyManager cleared");

            SteamNetworking.Shutdown();
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

        public bool IsHost() => SteamUser.GetSteamID() == OwnerID;
        public bool NotInLobby() => LobbyID == CSteamID.Nil;

        public void CreatePublicLobby()
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, MaxMembers);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[LOBBY] Creating steam lobby");
        }

        private void OnLobbyCreated(LobbyCreated_t cb)
        {
            if (cb.m_eResult != EResult.k_EResultOK)
            {
                MelonLogger.Warning("[LOBBY] Lobby create failed: " + cb.m_eResult);
                return;
            }

            LobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[LOBBY] Lobby created: " + LobbyID);

            SteamMatchmaking.SetLobbyJoinable(LobbyID, true);
            SteamMatchmaking.SetLobbyData(LobbyID, "name", SteamFriends.GetPersonaName() + "'s Lobby");
            SteamMatchmaking.SetLobbyData(LobbyID, "version", "0.0.1");
            SteamMatchmaking.SetLobbyData(LobbyID, "mode", "coop");

            // Set default lobby data
            GameData.ECharacter = ECharacter.SirOofie;
            GameData.MapTierIndex = 0;
            GameData.MapData = DataManager.Instance.GetMap(EMap.Forest);
            GameData.StageData = GameData.MapData.stages[GameData.MapTierIndex];
            GameData.ChallengeData = null;
            GameData.MusicIndex = -1;
            GameData.Seed = 42069;

            Instance.SetMyCharacter((int)ECharacter.SirOofie);

            Instance.HostSetConfig(
                eMap: GameData.MapData.eMap,
                tierIndex: GameData.MapTierIndex,
                challengeNameOrIndex: GameData.ChallengeData?.ToString(),
                musicIndex: GameData.MusicIndex,
                seed: GameData.Seed
            );

            SteamFriends.SetRichPresence("status", "In Lobby");
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
                MelonLogger.Warning("[LOBBY] Lobby list IO failure");
                return;
            }

            int count = (int)cb.m_nLobbiesMatching;
            MelonLogger.Msg("[LOBBY] Found lobbies: " + count);

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
                MelonLogger.Msg($"[LOBBY] Joining lobby: {lobbyID}");
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
        {
            MelonLogger.Msg("[LOBBY] Invite/Join requested for lobby: " + cb.m_steamIDLobby);

            PendingLobbyJoin = cb.m_steamIDLobby;
            PendingOpenLobbyUI = true;

            if (SceneManager.GetActiveScene().name != "MainMenu")
                SceneManager.LoadScene("MainMenu");
            else
            {
                Instance.LeaveLobby();
                TryConsumePendingJoinOnMainMenu();
            }
        }

        internal static void TryConsumePendingJoinOnMainMenu()
        {
            if (!HasPendingJoin) return;

            Instance.JoinLobby(PendingLobbyJoin);

            if (PendingOpenLobbyUI)
                MelonCoroutines.Start(UICreation.WaitForLobbyAndOpen());

            PendingLobbyJoin = CSteamID.Nil;
            PendingOpenLobbyUI = false;
        }

        private void OnLobbyEnter(LobbyEnter_t cb)
        {
            LobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[LOBBY] Entered lobby: " + LobbyID);

            SteamMatchmaking.SetLobbyMemberData(LobbyID, Keys.Ready, false.ToString());

            CSteamID owner = SteamMatchmaking.GetLobbyOwner(LobbyID);
            var isHost = IsHost();

            if (isHost && !TryResolveConfigFromLobby(out var error))
            {
                MelonLogger.Warning("[LOBBY] Failed to pull lobby config: " + error);
                return;
            }

            if (Config.VerboseSteamworks)
            {
                string name = SteamMatchmaking.GetLobbyData(LobbyID, Keys.Name);
                string ver = SteamMatchmaking.GetLobbyData(LobbyID, Keys.Ver);
                string mode = SteamMatchmaking.GetLobbyData(LobbyID, Keys.Mode);
                MelonLogger.Msg($"[LOBBY] Lobby data: ishost={isHost} lobbyName=\"{name}\" ver={ver} mode={mode}");
            }
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t cb)
        {
            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[LOBBY] Lobby chat update: " + cb.m_ulSteamIDLobby);
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
            }
            else
            {
                var member = new CSteamID(cb.m_ulSteamIDMember);
                string ready = SteamMatchmaking.GetLobbyMemberData(lobby, member, "ready");
                string charStr = SteamMatchmaking.GetLobbyMemberData(lobby, member, Keys.Char);

                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[LOBBY] Member {member} ready={ready} char='{charStr}'");
            }
        }

        public void LeaveLobby()
        {
            if (Instance != null && LobbyID != CSteamID.Nil)
            {
                SteamMatchmaking.LeaveLobby(LobbyID);
                SteamFriends.SetRichPresence("status", "");

                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[LOBBY] Left lobby: {LobbyID}");

                LobbyID = CSteamID.Nil;

                SteamNetworking.Shutdown();
            }
        }

        public void HostSetConfig(EMap eMap, int tierIndex, string challengeNameOrIndex, int musicIndex, int seed)
        {
            if (SteamUser.GetSteamID() != OwnerID) { MelonLogger.Warning("[LOBBY] HostSetConfig called by non-owner"); return; }
            if (LobbyID == CSteamID.Nil) { MelonLogger.Warning("[LOBBY] HostSetConfig called while not in a lobby"); return; }

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
                MelonLogger.Msg($"[LOBBY] Set my character='{eCharacterInt}'");
        }

        public void SetReadyStatus(bool status)
        {
            if (LobbyID == CSteamID.Nil) return;
            SteamMatchmaking.SetLobbyMemberData(LobbyID, Keys.Ready, status.ToString());
            if (Config.VerboseSteamworks)
                MelonLogger.Msg($"[LOBBY] Set ready status to {status}");
        }

        public bool IsAllReady()
        {
            int num = SteamMatchmaking.GetNumLobbyMembers(LobbyID);
            for (int i = 0; i < num; i++)
            {
                var id = SteamMatchmaking.GetLobbyMemberByIndex(LobbyID, i);
                if (id == OwnerID) continue;

                if (SteamMatchmaking.GetLobbyMemberData(LobbyID, id, Keys.Ready) == false.ToString())
                    return false;
            }

            return true;
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
}
