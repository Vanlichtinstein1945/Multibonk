using HarmonyLib;
using Il2CppAssets.Scripts.Managers;
using Il2CppAssets.Scripts.Game.Other;
using Il2Cpp;
using Il2CppUtility;
using MelonLoader;

namespace Multibonk.Patches
{
    [HarmonyPatch(typeof(MapController), nameof(MapController.StartNewMap))]
    static class MapController_StartNewMap_Patch
    {
        [HarmonyPrefix]
        static void Prefix(RunConfig newRunConfig)
        {
            if (!GameData.IsMultiplayer) return;

            newRunConfig.mapData = GameData.MapData;
            newRunConfig.stageData = GameData.StageData;
            newRunConfig.mapTierIndex = GameData.MapTierIndex;
            newRunConfig.challenge = GameData.ChallengeData;
            newRunConfig.musicTrackIndex = GameData.MusicIndex;

            CharacterMenu.selectedCharacter = GameData.ECharacter;

            UnityEngine.Random.InitState(GameData.Seed);
            MapGenerator.seed = GameData.Seed;
            MyRandom.random = new ConsistentRandom(GameData.Seed);

            if (Config.LogRunStartStats)
                MelonLogger.Msg($"[SNM Patch] Starting run mapData={GameData.MapData.eMap} stageData={GameData.StageData.name} mapTierIndex={GameData.MapTierIndex} challengeData={GameData.ChallengeData?.name} seed={GameData.Seed} selectedCharacter={GameData.ECharacter}");
        }
    }
}
