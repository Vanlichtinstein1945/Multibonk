using MelonLoader;
using Il2Cpp;
using UnityEngine;
using UnityEngine.Events;

[assembly: MelonInfo(typeof(Multibonk.Main), "Multibonk", "0.0.1", "Vanlichtinstein")]
[assembly: MelonGame("Ved", "Megabonk")]

namespace Multibonk
{
    public static class GameData
    {
        public static int Seed;
        public static MapData MapData;
        public static StageData StageData;
        public static int MapTierIndex;
        public static ChallengeData ChallengeData;
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
                if (!bHasCached)
                    HarvestDataFromMainMenu();

                UICreation.CreateMultiplayerMenu();
            }
        }

        private void HarvestDataFromMainMenu()
        {
            var stages = Object.FindObjectsOfType<MapEntry>(true);
            if (stages == null || stages.Length == 0)
            {
                MelonLogger.Error("No game objects of type MapEntry found!");
                return;
            }

            foreach (var stage in stages)
            {
                MapData md = stage.mapData;
                if (md == null) continue;

                Caches.MapDataCache.Put(md.name, md);
            }

            var ui = GameObject.Find("UI");
            if (ui == null)
            {
                MelonLogger.Error("No UI game object found!");
                return;
            }

            ui.GetComponent<MainMenu>().GoToCharacterSelection();
            ui.GetComponent<MainMenu>().GoToMenu();

            var characters = Object.FindObjectsOfType<MyButtonCharacter>(true);
            if (characters == null || characters.Length == 0)
            {
                MelonLogger.Error("No game objects of type MyCharacterButton found!");
                return;
            }

            foreach (var character in characters)
            {
                var cData = character.characterData;
                if (cData == null) continue;

                Caches.CharacterDataCache.Put(cData.eCharacter, cData);
            }

            bHasCached = true;
        }
    }
}
