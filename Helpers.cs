using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using Il2CppTMPro;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Il2Cpp;

namespace Multibonk
{
    class Helpers
    {
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
            if (ErrorIfNull(exampleButton, "Example button was null!"))
                return null;

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
    }
}
