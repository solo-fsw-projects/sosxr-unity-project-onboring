using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;

namespace OverfortGames.SmartEditorSelection
{
    public class OverlayPopupWindow : PopupWindowBase
    {
        const float borderWidth = 1;

        protected virtual void OnEnable()
        {
            rootVisualElement.style.borderLeftWidth = borderWidth;
            rootVisualElement.style.borderTopWidth = borderWidth;
            rootVisualElement.style.borderRightWidth = borderWidth;
            rootVisualElement.style.borderBottomWidth = borderWidth;

            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.44f, 0.44f, 0.44f, 1f) : new Color(0.51f, 0.51f, 0.51f);
            rootVisualElement.style.borderLeftColor = borderColor;
            rootVisualElement.style.borderTopColor = borderColor;
            rootVisualElement.style.borderRightColor = borderColor;
            rootVisualElement.style.borderBottomColor = borderColor;
        }
    }

    public abstract class PopupWindowBase : EditorWindow
    {
        private static double s_LastClosedTime;
        private static Rect s_LastActivatorRect;

        static bool ShouldShowWindow(Rect activatorRect)
        {
            const double kJustClickedTime = 0.2;
            bool justClosed = (EditorApplication.timeSinceStartup - s_LastClosedTime) < kJustClickedTime;
            if (!justClosed || activatorRect != s_LastActivatorRect)
            {
                s_LastActivatorRect = activatorRect;
                return true;
            }
            return false;
        }

        public static T Show<T>(VisualElement trigger, Vector2 size) where T : EditorWindow
        {
            return Show<T>(GUIUtility.GUIToScreenRect(trigger.worldBound), size);
        }

        public static T Show<T>(Rect activatorRect, Vector2 size) where T : EditorWindow
        {
            var windows = Resources.FindObjectsOfTypeAll<T>();

            if (windows.Any())
            {
                foreach (var window in windows)
                    window.Close();
                return default;
            }

            if (ShouldShowWindow(activatorRect))
            {
                var popup = CreateInstance<T>();

                popup.hideFlags = HideFlags.DontSave;
                popup.ShowAsDropDown(activatorRect, size);
                return popup;
            }

            return default;
        }
    }
}
