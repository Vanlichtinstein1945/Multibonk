using System.Collections.Generic;
using Il2Cpp;

namespace Multibonk.Caches
{
    static class MapDataCache
    {
        public static readonly Dictionary<string, MapData> MapDataPairs = new Dictionary<string, MapData>();

        public static void Put(string name, MapData mapData)
        {
            if (mapData)
                if (!string.IsNullOrEmpty(name))
                    MapDataPairs[name] = mapData;
        }

        public static MapData GetByName(string name) =>
            MapDataPairs.TryGetValue(name, out var md) ? md : null;
    }
}
