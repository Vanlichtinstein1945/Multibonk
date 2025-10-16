using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace Multibonk.Patches
{
    [HarmonyPatch(typeof(MapGenerationController._GenerateMap_d__22), nameof(MapGenerationController._GenerateMap_d__22.MoveNext))]
    static class MapGenerationController_GenerateMap_Patch
    {
        [HarmonyPostfix]
        static void Postfix(bool __result)
        {
            if (__result != false || !GameData.IsMultiplayer) return;

            if (Config.VerboseHarmonyPatches)
                MelonLogger.Msg("[MGC_GM_Patch] GenerateMap coroutine finished. Starting host/client awaits");

            if (Networking.SteamNetworking.IsHost)
                Networking.SteamNetworking.HostBeginInitBarrier(new System.Collections.Generic.List<Networking.SteamNetworking.NetInitObject>());
            else
                Networking.SteamNetworking.ClientBeginInitBarrier();
        }
    }
}
