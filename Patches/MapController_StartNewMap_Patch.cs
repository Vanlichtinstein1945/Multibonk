using HarmonyLib;
using Il2CppAssets.Scripts.Managers;
using Il2CppAssets.Scripts.Game.Other;
using System;
using MelonLoader;

namespace Multibonk.Patches
{
    [HarmonyPatch(typeof(MapController), nameof(MapController.StartNewMap))]
    static class MapController_StartNewMap_Patch
    {
        [HarmonyPrefix]
        static void Prefix(RunConfig newRunConfig)
        {
            try
            {
                newRunConfig.mapData = GameData.MapData;
                newRunConfig.stageData = GameData.StageData;
                newRunConfig.mapTierIndex = GameData.MapTierIndex;
                newRunConfig.challenge = GameData.ChallengeData;

                UnityEngine.Random.InitState(GameData.Seed);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Ran into an error setting RunConfig data: {ex}");
            }
        }
    }
}
