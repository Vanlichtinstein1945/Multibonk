using MelonLoader;
using Il2Cpp;
using Il2CppAssets.Scripts.Game.Other;
using UnityEngine.SceneManagement;
using UnityEngine;

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
    }

    public static class Config
    {
        public static bool VerboseSteamworks = true;
        public static bool LogMapObjectsAndPositions = false;
        public static bool LogRunStartStats = true;
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
            LobbyManager.Instance.Update();
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
                    MelonLogger.Msg("Printing Objects...");
                    foreach (var obj in objects)
                    {
                        MelonLogger.Msg($"obj: {obj.name} | pos: {obj.transform.position}");
                    }
                }
            }
        }

        private void GrabRunConfig()
        {
            var msui = Object.FindObjectOfType<MapSelectionUi>(true);
            if (Helpers.ErrorIfNull(msui, "No game object of type MapSelectionUi found!")) return;
            GameData.RunConfig = msui.runConfig;
            bCached = true;
        }
    }
}
