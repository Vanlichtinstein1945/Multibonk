using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace Multibonk.Patches
{
    [HarmonyPatch(typeof(MapEntry), nameof(MapEntry.OnMapSelected))]
    class MapEntry_OnMapSelected_Patch
    {
        [HarmonyPostfix]
        static void Postfix(MapEntry __instance, MapData arg2)
        {
            if (__instance != null && __instance._mapData_k__BackingField == arg2)
            {
                MelonLogger.Msg(arg2.eMap);
            }
        }
    }
}
