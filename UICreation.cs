using UnityEngine;
using Il2Cpp;
using UnityEngine.Events;

namespace Multibonk
{
    static class UICreation
    {
        public static GameObject MultiplayerMenu;

        public static void CreateMultiplayerMenu()
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

            MultiplayerMenu = Object.Instantiate(menu.gameObject, tabs, false);
            MultiplayerMenu.name = "MultiplayerMenu";
            MultiplayerMenu.SetActive(false);
            Object.Destroy(MultiplayerMenu.GetComponent<MenuAlerts>());
            Helpers.DestroyAllChildren(MultiplayerMenu.transform);
            var multiplayerWindow = MultiplayerMenu.GetComponent<Window>();
            multiplayerWindow.enabled = false;
            multiplayerWindow.isFocused = false;

            var buttons = menu.Find("Content/Main/Buttons");
            if (Helpers.ErrorIfNull(buttons, "No Buttons game object found!"))
                return;
            var bPlay = buttons.Find("B_Play");
            if (Helpers.ErrorIfNull(bPlay, "No B_Play game object found!"))
                return;

            var backButtonOnClick = (UnityAction)(() =>
            {
                ui.GetComponent<MainMenu>().SetWindow(menu.gameObject);
                multiplayerWindow.enabled = false;
                menu.gameObject.GetComponent<Window>().FocusWindow();
            });
            var backButton = Helpers.CreateButtonFromExample(bPlay.gameObject, MultiplayerMenu.transform, "B_Back", "Back", backButtonOnClick);
            backButton.AddComponent<BackEscape>();

            multiplayerWindow.allButtons.Clear();
            multiplayerWindow.allButtons.Add(backButton.GetComponent<MyButton>());
            multiplayerWindow.allButtonsHashed.Clear();
            multiplayerWindow.allButtonsHashed.Add(backButton);
            multiplayerWindow.startBtn = backButton.GetComponent<MyButton>();

            var multiplayerButtonOnClick = (UnityAction)(() =>
            {
                ui.GetComponent<MainMenu>().SetWindow(MultiplayerMenu);
                multiplayerWindow.enabled = true;
                multiplayerWindow.FocusWindow();
            });
            var multiplayerButton = Helpers.CreateButtonFromExample(bPlay.gameObject, buttons, "B_Multiplayer", "Multiplayer", multiplayerButtonOnClick);
            menu.gameObject.GetComponent<Window>().allButtons.Add(multiplayerButton.GetComponent<MyButton>());
            menu.gameObject.GetComponent<Window>().allButtonsHashed.Add(multiplayerButton);

            var bUnlocks = buttons.Find("B_Unlocks");
            if (Helpers.ErrorIfNull(bUnlocks, "No B_Unlocks game object found!"))
                return;

            int bUnlocksIndex = bUnlocks.GetSiblingIndex();
            multiplayerButton.transform.SetSiblingIndex(bUnlocksIndex);
        }
    }
}
