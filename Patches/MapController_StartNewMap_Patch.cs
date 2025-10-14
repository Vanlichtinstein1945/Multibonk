using HarmonyLib;
using Il2CppAssets.Scripts.Managers;
using Il2CppAssets.Scripts.Game.Other;

namespace Multibonk.Patches
{
    [HarmonyPatch(typeof(MapController), nameof(MapController.StartNewMap))]
    static class MapController_StartNewMap_Patch
    {
        [HarmonyPrefix]
        static void Prefix(RunConfig newRunConfig)
        {
            if (!Networking.Instance.IsConnected) return;

            newRunConfig.mapData = GameData.MapData;
            newRunConfig.stageData = GameData.StageData;
            newRunConfig.mapTierIndex = GameData.MapTierIndex;
            newRunConfig.challenge = GameData.ChallengeData;

            UnityEngine.Random.InitState(GameData.Seed);
        }
    }
}
