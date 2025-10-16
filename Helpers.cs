using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using Il2CppTMPro;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Il2Cpp;
using Steamworks;

namespace Multibonk
{
    class Helpers
    {
        public static void HandleSteamLaunchInvite()
        {
            if (SteamApps.GetLaunchCommandLine(out string cmdLine, 8192) > 0)
            {
                if (Config.VerboseSteamworks)
                    MelonLogger.Msg($"[STEAM] Launch command line: {cmdLine}");

                var parts = cmdLine.Split(' ');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Equals("+connect_lobby", System.StringComparison.OrdinalIgnoreCase)
                        && i + 1 < parts.Length)
                    {
                        if (ulong.TryParse(parts[i + 1], out var lobbyId64))
                        {
                            CSteamID lobbyID = new CSteamID(lobbyId64);
                            if (Config.VerboseSteamworks)
                                MelonLogger.Msg($"[STEAM] Detected launch invite to lobby {lobbyID}");
                            Networking.LobbyManager.PendingLobbyJoin = lobbyID;
                            Networking.LobbyManager.PendingOpenLobbyUI = true;
                        }
                    }
                }
            }
        }

        public static void SteamInit()
        {
            var steamOk = SteamAPI.Init();

            if (!steamOk)
            {
                MelonLogger.Error("[STEAM] SteamAPI failed to init!");
                return;
            }

            if (Config.VerboseSteamworks)
                MelonLogger.Msg("[STEAM] SteamAPI initialized");

            Networking.LobbyManager.Initialize();
        }

        public static bool ErrorIfNull<T>(T item, string errorMessage)
        {
            if (item == null)
            {
                MelonLogger.Error(errorMessage);
                return true;
            }
            return false;
        }

        public static void DestroyAllChildren(Transform parent)
        {
            if (parent == null) return;

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null)
                    Object.Destroy(child.gameObject);
            }
        }

        public static GameObject CreateButtonFromExample(GameObject exampleButton, Transform parent, string objectName, string buttonLabel, UnityAction onClick)
        {
            if (ErrorIfNull(exampleButton, "[HELPER] Example button was null!")) return null;

            var newButton = Object.Instantiate(exampleButton, parent, false);
            newButton.name = objectName;

            var overlayTr = newButton.transform.Find("DisabledOverlay");
            if (overlayTr) overlayTr.gameObject.SetActive(false);

            TextMeshProUGUI tmp;
            var tText = newButton.transform.Find("T_Text");
            if (tText) tmp = tText.GetComponent<TextMeshProUGUI>();
            else tmp = newButton.GetComponentInChildren<TextMeshProUGUI>(true);

            tmp.text = buttonLabel;
            var loc = tmp.GetComponent<LocalizeStringEvent>();
            if (loc) loc.enabled = false;

            var myButtonNormal = newButton.GetComponent<MyButtonNormal>();
            var btn = newButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                btn.onClick.AddListener(onClick);
                btn.onClick.AddListener((UnityAction) myButtonNormal.PlaySfx);
                btn.onClick.AddListener((UnityAction) myButtonNormal.OnClick);
            }
            btn.enabled = true;

            var selectable = newButton.GetComponent<Selectable>();
            if (selectable) selectable.OnDeselect(null);

            return newButton;
        }

        public enum AnimBits : byte
        {
            None     = 0,
            Grounded = 1 << 0,
            Jumping  = 1 << 1,
            Grinding = 1 << 2,
            Moving   = 1 << 3,
        }

        public static class AnimSync
        {
            private const string P_Grounded = "grounded";
            private const string P_Jumping = "jumping";
            private const string P_Grinding = "grinding";
            private const string P_Moving = "moving";

            public static AnimBits Build(Animator a)
            {
                if (!a) return AnimBits.None;
                AnimBits bits = AnimBits.None;
                if (a.GetBool(P_Grounded)) bits |= AnimBits.Grounded;
                if (a.GetBool(P_Jumping)) bits |= AnimBits.Jumping;
                if (a.GetBool(P_Grinding)) bits |= AnimBits.Grinding;
                if (a.GetBool(P_Moving)) bits |= AnimBits.Moving;
                return bits;
            }

            public static void Apply(Animator a, AnimBits bits)
            {
                if (!a) return;
                a.SetBool(P_Grounded, (bits & AnimBits.Grounded) != 0);
                a.SetBool(P_Jumping, (bits & AnimBits.Jumping) != 0);
                a.SetBool(P_Grinding, (bits & AnimBits.Grinding) != 0);
                a.SetBool(P_Moving, (bits & AnimBits.Moving) != 0);
            }
        }
    }
}
