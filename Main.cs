using MelonLoader;
using Il2Cpp;
using Il2CppAssets.Scripts.Game.Other;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using Steamworks;

[assembly: MelonInfo(typeof(Multibonk.Main), "Multibonk", "0.0.1", "Vanlichtinstein")]
[assembly: MelonGame("Ved", "Megabonk")]

namespace Multibonk
{
    public static class GameData
    {
        public static ECharacter ECharacter;
        public static MapData MapData;
        public static StageData StageData;
        public static int MapTierIndex;
        public static ChallengeData ChallengeData;
        public static int MusicIndex;
        public static int Seed;
        public static RunConfig RunConfig;
        public static bool IsMultiplayer = false;
    }

    public static class Config
    {
        public static bool VerboseSteamworks = true;
        public static bool LogMapObjectsAndPositions = false;
        public static bool LogRunStartStats = false;
        public static bool VerboseLocalPlayer = false;
    }

    public class Main : MelonMod
    {
        private bool bCached = false;

        public override void OnInitializeMelon()
        {
            SteamManager.SteamInit();
        }

        public override void OnUpdate()
        {
            LobbyManager.Instance?.Update();
            SteamNetworking.Pump();
        }

        public override void OnDeinitializeMelon()
        {
            LobbyManager.Shutdown();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                LobbyManager.Instance?.LeaveLobby();
                UICreation.CreateMultiplayerMenus();

                if (!bCached)
                    GrabRunConfig();
            }
            else if (sceneName == "GeneratedMap")
            {
                if (Config.LogMapObjectsAndPositions)
                {
                    var objects = SceneManager.GetActiveScene().GetRootGameObjects();
                    MelonLogger.Msg("[MAIN] Printing Objects...");
                    foreach (var obj in objects)
                    {
                        MelonLogger.Msg($"obj: {obj.name} | pos: {obj.transform.position}");
                    }
                }

                if (GameData.IsMultiplayer)
                    MelonCoroutines.Start(WaitAndBindLocal());
            }
        }

        private void GrabRunConfig()
        {
            var msui = Object.FindObjectOfType<MapSelectionUi>(true);
            if (Helpers.ErrorIfNull(msui, "[MAIN] No game object of type MapSelectionUi found!")) return;
            GameData.RunConfig = msui.runConfig;
            bCached = true;
        }

        private IEnumerator WaitAndBindLocal()
        {
            yield return new WaitForSeconds(1f);

            var player = Object.FindObjectOfType<PlayerRenderer>(true);
            if (Helpers.ErrorIfNull(player, "[MAIN] No game object of type PlayerRenderer found!")) yield break;
            var anim = player.GetComponentInChildren<Animator>();
            if (Helpers.ErrorIfNull(anim, "[MAIN] No game object of type Animator found!")) yield break;
            var tf = player.transform.root;

            SteamNetworking.BindLocal(anim, tf, anim.transform);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[MAIN] Bound local player to SteamNetworking.");

            var lobby = LobbyManager.Instance?.LobbyID ?? CSteamID.Nil;
            if (lobby == CSteamID.Nil) yield break;

            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            for (int i = 0; i < count; i++)
            {
                var id = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (id == SteamUser.GetSteamID()) continue;

                var s = SteamMatchmaking.GetLobbyMemberData(lobby, id, "char");
                int.TryParse(s, out var eInt);
                var who = (ECharacter)eInt;

                var charData = DataManager.Instance.GetCharacterData(who);
                if (charData == null || charData.prefab == null)
                {
                    MelonLogger.Error($"[MAIN] Missing prefab for {who}");
                    continue;
                }

                var root = new GameObject("$Remote_{id.m_SteamID}");
                if (root == null) continue;
                var go = Object.Instantiate(charData.prefab, root.transform, false);
                if (go == null) continue;

                var rAnim = go.GetComponent<Animator>();
                var rTf = go.transform;

                SteamNetworking.BindRemote(id, root, rAnim, root.transform, rTf);
            }
        }
    }
}
