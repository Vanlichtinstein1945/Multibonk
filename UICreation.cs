using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using UnityEngine.Events;
using Il2CppTMPro;

namespace Multibonk
{
    static class UICreation
    {
        public static GameObject MultiplayerMenu;

        public static void CreateMultiplayerMenus()
        {
            var ui = GameObject.Find("UI");
            if (Helpers.ErrorIfNull(ui, "No UI game object found!"))
                return;
            var tabs = ui.transform.Find("Tabs");
            if (Helpers.ErrorIfNull(tabs, "No Tabs game object found!"))
                return;
            var menu = tabs.Find("Menu");
            if (Helpers.ErrorIfNull(menu, "No Menu game object found!"))
                return;

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

            var buttons = menu.Find("Content/Main/Buttons");
            if (Helpers.ErrorIfNull(buttons, "No Buttons game object found!"))
                return;
            var bPlay = buttons.Find("B_Play");
            if (Helpers.ErrorIfNull(bPlay, "No B_Play game object found!"))
                return;

            // Creating IP textbox
            var IPField = new GameObject("IPField");
            var ipRt = IPField.AddComponent<RectTransform>();
            var ipIf = IPField.AddComponent<TMP_InputField>();
            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(IPField.transform, false);
            ipIf.placeholder = placeholder.AddComponent<TextMeshProUGUI>();
            var textComponent = new GameObject("T_Text");
            textComponent.transform.SetParent(IPField.transform, false);
            ipIf.textComponent = textComponent.AddComponent<TextMeshProUGUI>();
            IPField.transform.SetParent(MultiplayerMenu.transform, false);
            placeholder.GetComponent<TextMeshProUGUI>().text = "Enter IP Address...";

            // Creating join button
            var joinButtonOnClick = (UnityAction)(() =>
            {
                Networking.Instance.StartClient(GameData.ServerIP);
            });
            var joinButton = Helpers.CreateButtonFromExample(bPlay.gameObject, MultiplayerMenu.transform, "B_Join", "Join", joinButtonOnClick);
            multiplayerWindow.allButtons.Add(joinButton.GetComponent<MyButton>());
            multiplayerWindow.allButtonsHashed.Add(joinButton);
            multiplayerWindow.startBtn = joinButton.GetComponent<MyButton>();

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

            // Creating host button
            var hostButtonOnClick = (UnityAction)(() =>
            {
                Networking.Instance.StartServer();
                ui.GetComponent<MainMenu>().SetWindow(lobbyMenu);
                lobbyWindow.FocusWindow();
            });
            var hostButton = Helpers.CreateButtonFromExample(bPlay.gameObject, MultiplayerMenu.transform, "B_Host", "Host", hostButtonOnClick);
            multiplayerWindow.allButtons.Add(hostButton.GetComponent<MyButton>());
            multiplayerWindow.allButtonsHashed.Add(hostButton);

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

            // Creating list to show lobby members
            // Creating character selection screen
            // Creating map selection screen
            // Creating readyup button

            // Creating start match button
            var startButtonOnClick = (UnityAction)(() =>
            {
                // FOR TESTING, SETTING DEFAULT MAP PARAMS HERE
                CharacterMenu.selectedCharacter = GameData.ECharacter;
                GameData.MapTierIndex = 0;
                GameData.MapData = Caches.MapDataCache.GetByName("MapForest");
                GameData.StageData = GameData.MapData.stages[GameData.MapTierIndex];
                GameData.ChallengeData = null;
                GameData.MusicIndex = -1;
                GameData.Seed = 42069;

                Networking.Instance.SendGameStart();
            });
            var startButton = Helpers.CreateButtonFromExample(bPlay.gameObject, lobbyMenu.transform, "B_Start", "Start", startButtonOnClick);
            lobbyWindow.allButtons.Add(startButton.GetComponent<MyButton>());
            lobbyWindow.allButtonsHashed.Add(startButton);
            lobbyWindow.startBtn = startButton.GetComponent<MyButton>();

            // Creating leave lobby button
            var leaveButtonOnClick = (UnityAction)(() =>
            {
                Networking.Instance.Stop();
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
