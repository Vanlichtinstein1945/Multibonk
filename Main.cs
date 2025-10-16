using MelonLoader;
using Il2Cpp;
using Il2CppAssets.Scripts.Game.Other;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using Il2CppSteamworks;
using Multibonk.Networking;

[assembly: MelonInfo(typeof(Multibonk.Main), "Multibonk", "0.0.1", "Vanlichtinstein")]
[assembly: MelonGame("Ved", "Megabonk")]

// TODO: Look into why steam launch options printed empty when '+connect_lobby 1' was passed
// TODO: Create a list object to hold public lobbies to click and join off of
// TODO: Create a list object to show current lobby members in lobby ui
// TODO: Look into copying and creating a character choice menu in lobby ui
// TODO: Look into copying and creating a map choice menu for host in lobby ui
// TODO: Find a way to get references to map genned prefabs
// TODO: Find a way to grab all genned objects from map and pass to HostBeginInitBarrier
// TODO: Change spawning remote players to using a copy of the player with .SetCharacter(CharacterData) called
// TODO: Look into syncing enemies
// TODO: Look into causing enemies to attract to all, not just local player
// TODO: Look into syncing weapon attacks
// TODO: Look into syncing XP and currency
// TODO: Look into syncing interactables like chests and shrines being used
// TODO: Look into forcing players to stick together when taking portal to next area
// TODO: Determine how to react when host or a client dies
// TODO: Determine how to react if host or a client takes the portal to end game after final boss

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
        public static bool IsReady = false;
    }

    public class Main : MelonMod
    {
        private bool bCached = false;

        public override void OnInitializeMelon()
        {
            Config.Load();
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(Config.APP_ID)))
            {
                Application.Quit();
                return;
            }
            Helpers.SteamInit();
            Helpers.HandleSteamLaunchInvite();
        }

        public override void OnUpdate()
        {
            LobbyManager.Instance?.Update();
            Networking.SteamNetworking.Pump();
        }

        public override void OnDeinitializeMelon()
        {
            LobbyManager.Shutdown();
        }

        public override void OnApplicationQuit()
        {
            LobbyManager.Shutdown();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                if (!bCached)
                    GrabRunConfig();

                LobbyManager.Instance.LeaveLobby();
                UICreation.CreateMultiplayerMenus();
                LobbyManager.TryConsumePendingJoinOnMainMenu();
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

            Networking.SteamNetworking.BindLocal(anim, tf, anim.transform);

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[MAIN] Bound local player to SteamNetworking.");
        }
    }
}
