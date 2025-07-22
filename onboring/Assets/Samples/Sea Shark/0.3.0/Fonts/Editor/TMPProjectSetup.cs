using SOSXR.EnhancedLogger;
using TMPro;
using UnityEditor;
using UnityEngine;


namespace SOSXR.SeaShark.Editor
{
    public static class TMPProjectSetup
    {
        private static readonly string _defaultFontAssetPath = "Fonts & Materials/Maple Mono/Maple Mono";
        private static TMP_FontAsset _defaultFontAsset;
        private static Font _defaultFont;


        [MenuItem("SOSXR/Setup/Setup TMP Default Font")]
        public static void SetDefaultTMPFont()
        {
            if (!GetDefaultFontAsset())
            {
                return;
            }

            if (TMP_Settings.defaultFontAsset == _defaultFontAsset)
            {
                Log.Static("TMP default font asset is already set to ", _defaultFontAssetPath);

                return;
            }

            Undo.RecordObject(TMP_Settings.instance, "Change TMP Default Font");
            TMP_Settings.defaultFontAsset = _defaultFontAsset;
            EditorUtility.SetDirty(TMP_Settings.instance);

            AssetDatabase.SaveAssets();

            Debug.Log("TMP default font asset set.");
        }


        private static bool GetDefaultFontAsset()
        {
            _defaultFontAsset = Resources.Load(_defaultFontAssetPath) as TMP_FontAsset;

            if (_defaultFontAsset == null)
            {
                Log.Static($"Font asset {_defaultFontAssetPath} not found at specified path.");

                return false;
            }

            _defaultFont = _defaultFontAsset.sourceFontFile;

            return true;
        }
    }
}