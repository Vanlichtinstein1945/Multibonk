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
            if (ErrorIfNull(exampleButton, "[HELPER] Example button was null!"))
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
