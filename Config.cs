using System.IO;
using MelonLoader;

namespace Multibonk
{
    public class Config
    {
        public const uint APP_ID = 3405340;

        public static bool VerboseSteamworks = false;
        public static bool LogMapObjectsAndPositions = false;
        public static bool LogRunStartStats = false;
        public static bool VerboseLocalPlayer = false;
        public static bool VerboseHarmonyPatches = true;

        private static string _configPath;

        public static void Load()
        {
            string dllPath = typeof(Config).Assembly.Location;
            string dllDir = Path.GetDirectoryName(dllPath);
            _configPath = Path.Combine(dllDir ?? ".", "MultibonkConfig.txt");

            try
            {
                foreach (var line in File.ReadAllLines(_configPath))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    string[] parts = trimmed.Split('=', 2, System.StringSplitOptions.TrimEntries);
                    if (parts.Length != 2)
                        continue;

                    string key = parts[0];
                    string value = parts[1];

                    switch (key.ToLowerInvariant())
                    {
                        case "verbosesteamworks":
                            bool.TryParse(value, out VerboseSteamworks);
                            break;

                        case "logmapobjectsandpositions":
                            bool.TryParse(value, out LogMapObjectsAndPositions);
                            break;

                        case "logrunstartstats":
                            bool.TryParse(value, out LogRunStartStats);
                            break;

                        case "verboselocalplayer":
                            bool.TryParse(value, out VerboseLocalPlayer);
                            break;

                        case "verboseharmonypatches":
                            bool.TryParse(value, out VerboseHarmonyPatches);
                            break;
                    }
                }

                MelonLogger.Msg("[CONFIG] Loaded settings from MultibonkConfig.txt");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[CONFIG] Failed to load config: {ex}");
            }
        }
    }
}
