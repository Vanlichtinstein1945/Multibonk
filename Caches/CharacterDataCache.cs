using System.Collections.Generic;
using Il2Cpp;

namespace Multibonk.Caches
{
    static class CharacterDataCache
    {
        public static readonly Dictionary<ECharacter, CharacterData> CharacterDataPairs = new Dictionary<ECharacter, CharacterData>();

        public static void Put(ECharacter eCharacter, CharacterData cData)
        {
            if (cData)
                CharacterDataPairs[eCharacter] = cData;
        }

        public static CharacterData GetByName(ECharacter eCharacter) =>
            CharacterDataPairs.TryGetValue(eCharacter, out var cd) ? cd : null;
    }
}
