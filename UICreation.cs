using Il2Cpp;
using Il2CppAssets.Scripts._Data.MapsAndStages;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Multibonk
{
    static class UICreation
    {
        public static GameObject MultiplayerMenu;

        public static void CreateMultiplayerMenus()
        {
            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "No UI game object found!")) return;
            var tabs = ui.transform.Find("Tabs");
            if (Helpers.ErrorIfNull(tabs, "No Tabs game object found!")) return;
            var menu = tabs.Find("Menu");
            if (Helpers.ErrorIfNull(menu, "No Menu game object found!")) return;
            var buttons = menu.Find("Content/Main/Buttons");
            if (Helpers.ErrorIfNull(buttons, "No Buttons game object found!")) return;
            var bPlay = buttons.Find("B_Play");
            if (Helpers.ErrorIfNull(bPlay, "No B_Play game object found!")) return;

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

            // Creating list to hold lobby objects to join

            CreateLobbyMenus();

            var lobbyMenu = tabs.Find("LobbyMenu");
            if (Helpers.ErrorIfNull(lobbyMenu, "No LobbyMenu game object found!")) return;
            var lobbyWindow = lobbyMenu.GetComponent<Window>();

            // Creating host button
            var hostButtonOnClick = (UnityAction)(() =>
            {
                ui.GetComponent<MainMenu>().SetWindow(lobbyMenu.gameObject);
                lobbyWindow.FocusWindow();
                LobbyManager.Instance.CreatePublicLobby();
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
            if (Helpers.ErrorIfNull(bUnlocks, "No B_Unlocks game object found!"))
                return;
            int bUnlocksIndex = bUnlocks.GetSiblingIndex();
            multiplayerButton.transform.SetSiblingIndex(bUnlocksIndex);
        }

        public static void CreateLobbyMenus()
        {
            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "No UI game object found!")) return;
            var tabs = ui.transform.Find("Tabs");
            if (Helpers.ErrorIfNull(ui, "No Tabs game object found!")) return;
            var menu = tabs.Find("Menu");
            if (Helpers.ErrorIfNull(ui, "No Menu game object found!")) return;
            var multiplayerWindow = tabs.Find("MultiplayerWindow")?.GetComponent<Window>();
            if (Helpers.ErrorIfNull(ui, "No MultiplayerMenu game object found!")) return;
            var buttons = menu.Find("Content/Main/Buttons");
            if (Helpers.ErrorIfNull(buttons, "No Buttons game object found!")) return;
            var bPlay = buttons.Find("B_Play");
            if (Helpers.ErrorIfNull(bPlay, "No B_Play game object found!")) return;

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

            // Creating list to show lobby members
            // Creating character selection screen
            // Creating map selection screen
            // Creating readyup button

            // Creating start match button
            var startButtonOnClick = (UnityAction)(() =>
            {
                // FOR TESTING, SETTING DEFAULT MAP/CHARACTER PARAMS HERE
                GameData.ECharacter = ECharacter.SirOofie;
                GameData.MapTierIndex = 0;
                GameData.MapData = DataManager.Instance.GetMap(EMap.Forest);
                GameData.StageData = GameData.MapData.stages[GameData.MapTierIndex];
                GameData.ChallengeData = null;
                GameData.MusicIndex = -1;
                GameData.Seed = 69420;

                LobbyManager.Instance.SetMyCharacter((int)ECharacter.SirOofie);

                LobbyManager.Instance.HostSetConfig(
                    eMap: GameData.MapData.eMap,
                    tierIndex: GameData.MapTierIndex,
                    challengeNameOrIndex: GameData.ChallengeData?.ToString(),
                    musicIndex: GameData.MusicIndex,
                    seed: GameData.Seed
                );

                LobbyManager.Instance.HostBroadcastStart();
            });
            var startButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_Start", "Start", startButtonOnClick);
            lobbyWindow.allButtons.Add(startButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(startButton);
            lobbyWindow.startBtn = startButton.GetComponent<MyButton>();

            // Creating leave lobby button
            var leaveButtonOnClick = (UnityAction)(() =>
            {
                LobbyManager.Instance.LeaveLobby();
                ui.GetComponent<MainMenu>().SetWindow(MultiplayerMenu);
                multiplayerWindow.FocusWindow();
            });
            var leaveButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_Leave", "Leave", leaveButtonOnClick);
            leaveButton.AddComponent<BackEscape>();
            lobbyWindow.allButtons.Add(leaveButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(leaveButton);
        }
    }
}
