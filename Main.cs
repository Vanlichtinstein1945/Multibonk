using MelonLoader;
using Il2Cpp;
using Il2CppAssets.Scripts.Game.Other;
using UnityEngine;

[assembly: MelonInfo(typeof(Multibonk.Main), "Multibonk", "0.0.1", "Vanlichtinstein")]
[assembly: MelonGame("Ved", "Megabonk")]

namespace Multibonk
{
    public static class GameData
    {
        // FOR TESTING, SETTING DEFAULT ECHARACTER
        public static ECharacter ECharacter = ECharacter.SirOofie;
        public static MapData MapData;
        public static StageData StageData;
        public static int MapTierIndex;
        public static ChallengeData ChallengeData;
        public static int MusicIndex;
        public static int Seed;
        public static string ServerIP = "127.0.0.1";
        public static RunConfig runConfig;
    }

    public class Main : MelonMod
    {
        private bool bHasCached = false;

        public override void OnInitializeMelon()
        {
            Networking.Instance = new Networking();
        }

        public override void OnUpdate()
        {
            Networking.Instance.Update();
        }

        public override void OnApplicationQuit()
        {
            Networking.Instance.Stop();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu")
            {
                if (Networking.Instance.IsConnected)
                    Networking.Instance.Stop();

                UICreation.CreateMultiplayerMenus();

                if (!bHasCached)
                    HarvestDataFromMainMenu();
            }
        }

        private void HarvestDataFromMainMenu()
        {
            var mapSelectors = Object.FindObjectOfType<MapSelectionUi>(true);
            if (Helpers.ErrorIfNull(mapSelectors, "No game object of type MapSelectionUi found!"))
                return;
            GameData.runConfig = mapSelectors.runConfig;

            bHasCached = true;
        }
    }
}
