using System.Collections;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Steamworks;
using Multibonk.Networking;

namespace Multibonk
{
    static class UICreation
    {
        public static GameObject MultiplayerMenu;

        private static readonly Dictionary<CSteamID, GameObject> MemberRows = new Dictionary<CSteamID, GameObject>();

        public static void CreateMultiplayerMenus()
        {
            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "[UI] No UI game object found!")) return;
            var tabs = ui.transform.Find("Tabs");
            if (Helpers.ErrorIfNull(tabs, "[UI] No Tabs game object found!")) return;
            var menu = tabs.Find("Menu");
            if (Helpers.ErrorIfNull(menu, "[UI] No Menu game object found!")) return;
            var buttons = menu.Find("Content/Main/Buttons");
            if (Helpers.ErrorIfNull(buttons, "[UI] No Buttons game object found!")) return;
            var bPlay = buttons.Find("B_Play");
            if (Helpers.ErrorIfNull(bPlay, "[UI] No B_Play game object found!")) return;

            // Creating multiplayer Window
            MultiplayerMenu = Object.Instantiate(menu.gameObject, tabs, false);
            MultiplayerMenu.name = "MultiplayerMenu";
            MultiplayerMenu.SetActive(false);
            Object.Destroy(MultiplayerMenu.GetComponent<MenuAlerts>());
            Helpers.DestroyAllChildren(MultiplayerMenu.transform);
            var mmVLG = MultiplayerMenu.AddComponent<VerticalLayoutGroup>();
            mmVLG.childControlHeight = false;
            mmVLG.childControlWidth = false;
            mmVLG.childForceExpandHeight = false;
            mmVLG.spacing = 20;
            mmVLG.childAlignment = TextAnchor.MiddleCenter;
            mmVLG.padding.top = 200;
            var multiplayerWindow = MultiplayerMenu.GetComponent<Window>();
            multiplayerWindow.isFocused = false;
            multiplayerWindow.allButtons.Clear();
            multiplayerWindow.allButtonsHashed.Clear();

            CreateLobbyMenus();

            var lobbyMenu = tabs.Find("LobbyMenu");
            if (Helpers.ErrorIfNull(lobbyMenu, "[UI] No LobbyMenu game object found!")) return;
            var lobbyWindow = lobbyMenu.GetComponent<Window>();

            // Creating host button
            var hostButtonOnClick = (UnityAction)(() =>
            {
                LobbyManager.Instance.CreatePublicLobby();
                MelonCoroutines.Start(WaitForLobbyAndOpen());
            });
            var hostButton = Helpers.CreateButtonFromExample(bPlay.gameObject, MultiplayerMenu.transform, "B_Host", "Host", hostButtonOnClick);
            multiplayerWindow.allButtons.Add(hostButton.GetComponent<MyButton>());
            multiplayerWindow.allButtonsHashed.Add(hostButton);
            multiplayerWindow.startBtn = hostButton.GetComponent<MyButton>();

            // Creating back button to open the menu Window
            var backButtonOnClick = (UnityAction)(() =>
            {
                ui.GetComponent<MainMenu>().SetWindow(menu.gameObject);
                menu.gameObject.GetComponent<Window>().FocusWindow();
            });
            var backButton = Helpers.CreateButtonFromExample(bPlay.gameObject, MultiplayerMenu.transform, "B_Back", "Back", backButtonOnClick);
            backButton.AddComponent<BackEscape>();
            multiplayerWindow.allButtons.Add(backButton.GetComponent<MyButton>());
            multiplayerWindow.allButtonsHashed.Add(backButton);

            // Creating multiplayer button to open the multiplayer Window
            var multiplayerButtonOnClick = (UnityAction)(() =>
            {
                ui.GetComponent<MainMenu>().SetWindow(MultiplayerMenu);
                multiplayerWindow.FocusWindow();
            });
            var multiplayerButton = Helpers.CreateButtonFromExample(bPlay.gameObject, buttons, "B_Multiplayer", "Multiplayer", multiplayerButtonOnClick);
            menu.gameObject.GetComponent<Window>().allButtons.Add(multiplayerButton.GetComponent<MyButton>());
            menu.gameObject.GetComponent<Window>().allButtonsHashed.Add(multiplayerButton);

            // Setting multiplayer button above unlocks button
            var bUnlocks = buttons.Find("B_Unlocks");
            if (Helpers.ErrorIfNull(bUnlocks, "[UI] No B_Unlocks game object found!"))
                return;
            int bUnlocksIndex = bUnlocks.GetSiblingIndex();
            multiplayerButton.transform.SetSiblingIndex(bUnlocksIndex);
        }

        public static void CreateLobbyMenus()
        {
            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "[UI] No UI game object found!")) return;
            var tabs = ui.transform.Find("Tabs");
            if (Helpers.ErrorIfNull(ui, "[UI] No Tabs game object found!")) return;
            var menu = tabs.Find("Menu");
            if (Helpers.ErrorIfNull(ui, "[UI] No Menu game object found!")) return;
            var multiplayerWindow = tabs.Find("MultiplayerWindow")?.GetComponent<Window>();
            if (Helpers.ErrorIfNull(ui, "[UI] No MultiplayerMenu game object found!")) return;
            var buttons = menu.Find("Content/Main/Buttons");
            if (Helpers.ErrorIfNull(buttons, "[UI] No Buttons game object found!")) return;
            var bPlay = buttons.Find("B_Play");
            if (Helpers.ErrorIfNull(bPlay, "[UI] No B_Play game object found!")) return;

            // Creating lobby Window
            var lobbyMenu = Object.Instantiate(menu.gameObject, tabs, false);
            lobbyMenu.name = "LobbyMenu";
            lobbyMenu.SetActive(false);
            Object.Destroy(lobbyMenu.GetComponent<MenuAlerts>());
            Helpers.DestroyAllChildren(lobbyMenu.transform);
            var lmVLG = lobbyMenu.AddComponent<VerticalLayoutGroup>();
            lmVLG.childControlHeight = false;
            lmVLG.childControlWidth = false;
            lmVLG.childForceExpandHeight = false;
            lmVLG.spacing = 20;
            lmVLG.childAlignment = TextAnchor.MiddleCenter;
            lmVLG.padding.top = 200;
            var lobbyWindow = lobbyMenu.GetComponent<Window>();
            lobbyWindow.isFocused = false;
            lobbyWindow.allButtons.Clear();
            lobbyWindow.allButtonsHashed.Clear();            

            // Creating character selection screen
            var chooseCharButtonOnClick = (UnityAction)(() =>
            {
                MelonLogger.Msg("Open Choose Character Screen");
            });
            var chooseCharButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_ChooseChar", "Choose Character", chooseCharButtonOnClick);
            lobbyWindow.allButtons.Add(chooseCharButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(chooseCharButton);
            lobbyWindow.startBtn = chooseCharButton.GetComponent<MyButton>();
            lobbyWindow.alwaysUseStartBtn = true;

            // Creating map selection screen
            var chooseMapButtonOnClick = (UnityAction)(() =>
            {
                MelonLogger.Msg("Open Choose Map Screen");
            });
            var chooseMapButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_ChooseMap", "Choose Map", chooseMapButtonOnClick);
            lobbyWindow.allButtons.Add(chooseMapButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(chooseMapButton);

            // Creating readyup button
            var readyButtonOnClick = (UnityAction)(() =>
            {
                GameData.IsReady = !GameData.IsReady;
                LobbyManager.Instance.SetReadyStatus(GameData.IsReady);
            });
            var readyButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_Ready", "Ready", readyButtonOnClick);
            lobbyWindow.allButtons.Add(readyButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(readyButton);

            // Creating start match button
            var startButtonOnClick = (UnityAction)(() =>
            {
                LobbyManager.Instance.HostBroadcastStart();
            });
            var startButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_Start", "Start", startButtonOnClick);
            lobbyWindow.allButtons.Add(startButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(startButton);
            lobbyWindow.startBtn = startButton.GetComponent<MyButton>();

            // Creating leave lobby button
            var leaveButtonOnClick = (UnityAction)(() =>
            {
                CloseLobbyMenu();
            });
            var leaveButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_Leave", "Leave", leaveButtonOnClick);
            leaveButton.AddComponent<BackEscape>();
            lobbyWindow.allButtons.Add(leaveButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(leaveButton);
        }

        public static IEnumerator WaitForLobbyAndOpen()
        {
            while (LobbyManager.Instance.NotInLobby())
                yield return null;

            yield return null;

            OpenLobbyMenu();
            MelonCoroutines.Start(RefreshLobby());
        }

        private static IEnumerator RefreshLobby()
        {
            var ui = GameObject.Find("UI");
            var lobbyMenu = ui.transform.Find("Tabs/LobbyMenu");
            var startButton = lobbyMenu.transform.Find("B_Start").GetComponent<Button>();
            var startButtonOverlay = startButton.transform.Find("DisabledOverlay").gameObject;

            for (;;)
            {
                if (LobbyManager.Instance == null || LobbyManager.Instance.NotInLobby())
                    yield break;

                if (LobbyManager.Instance.IsHost())
                {
                    if (LobbyManager.Instance.IsAllReady())
                        EnableButton(startButton, startButtonOverlay);
                    else
                        DisableButton(startButton, startButtonOverlay);
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        public static void EnableButton(Button button, GameObject buttonOverlay)
        {
            if (!button || !buttonOverlay) return;

            button.interactable = true;
            buttonOverlay.SetActive(false);
        }

        public static void DisableButton(Button button, GameObject buttonOverlay)
        {
            button.interactable = false;
            buttonOverlay.SetActive(true);
        }

        public static void OpenLobbyMenu()
        {
            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "[UI] No UI game object found!")) return;
            var lobbyMenu = ui.transform.Find("Tabs/LobbyMenu");
            if (Helpers.ErrorIfNull(lobbyMenu, "[UI] No LobbyMenu game object found!")) return;

            ui.GetComponent<MainMenu>().SetWindow(lobbyMenu.gameObject);
            lobbyMenu.GetComponent<Window>().FocusWindow();

            ResetLobbyMenu(lobbyMenu);
            DisableLobbyButtons(lobbyMenu.gameObject);
        }

        public static void CloseLobbyMenu()
        {
            LobbyManager.Instance.LeaveLobby();

            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "[UI] No UI game object found!")) return;
            var multiplayerMenu = ui.transform.Find("Tabs/MultiplayerMenu");
            if (Helpers.ErrorIfNull(multiplayerMenu, "[UI] No MultiplayerMenu game object found!")) return;

            ui.GetComponent<MainMenu>().SetWindow(multiplayerMenu.gameObject);
            multiplayerMenu.GetComponent<Window>().FocusWindow();
        }

        public static void ResetLobbyMenu(Transform lobbyMenu)
        {
            var startButton = lobbyMenu.transform.Find("B_Start").GetComponent<Button>();
            var startButtonOverlay = startButton.transform.Find("DisabledOverlay").gameObject;
            var readyButton = lobbyMenu.transform.Find("B_Ready").GetComponent<Button>();
            var readyButtonOverlay = readyButton.transform.Find("DisabledOverlay").gameObject;
            var chooseMapButton = lobbyMenu.transform.Find("B_ChooseMap").GetComponent<Button>();
            var chooseMapButtonOverlay = chooseMapButton.transform.Find("DisabledOverlay").gameObject;

            EnableButton(startButton, startButtonOverlay);
            EnableButton(readyButton, readyButtonOverlay);
            EnableButton(chooseMapButton, chooseMapButtonOverlay);
        }

        public static void DisableLobbyButtons(GameObject lobbyMenu)
        {
            var startButton = lobbyMenu.transform.Find("B_Start");
            if (Helpers.ErrorIfNull(startButton, "[UI] No B_Start game object found!")) return;
            var readyButton = lobbyMenu.transform.Find("B_Ready");
            if (Helpers.ErrorIfNull(readyButton, "[UI] No B_Ready game object found!")) return;
            var chooseMapButton = lobbyMenu.transform.Find("B_ChooseMap");
            if (Helpers.ErrorIfNull(chooseMapButton, "[UI] No B_ChooseMap game object found!")) return;

            if (!LobbyManager.Instance.IsHost())
            {
                var chooseMapButtonOverlay = chooseMapButton.Find("DisabledOverlay");
                if (Helpers.ErrorIfNull(chooseMapButtonOverlay, "[UI] No DisabledOverlay game object found!")) return;
                DisableButton(chooseMapButton.GetComponent<Button>(), chooseMapButtonOverlay.gameObject);

                var startButtonOverlay = startButton.Find("DisabledOverlay");
                if (Helpers.ErrorIfNull(startButtonOverlay, "[UI] No DisabledOverlay game object found!")) return;
                DisableButton(startButton.GetComponent<Button>(), startButtonOverlay.gameObject);
            }
            else
            {
                var readyButtonOverlay = readyButton.Find("DisabledOverlay");
                if (Helpers.ErrorIfNull(readyButtonOverlay, "[UI] No DisabledOverlay game object found!")) return;
                DisableButton(readyButton.GetComponent<Button>(), readyButtonOverlay.gameObject);
            }
        }
    }
}
