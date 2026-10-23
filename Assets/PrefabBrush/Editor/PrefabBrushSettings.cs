using UnityEditor;

namespace PrefabBrushPlugin
{
    internal static class PrefabBrushSettings
    {
        static string Key => "PrefabBrush.Palette." + PlayerSettings.productGUID;

        static PrefabBrushPalette palette;

        /// <summary>Active palette, persisted per project via its asset GUID.</summary>
        public static PrefabBrushPalette Palette
        {
            get
            {
                if (palette == null)
                {
                    var path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(Key, ""));
                    if (!string.IsNullOrEmpty(path)) palette = AssetDatabase.LoadAssetAtPath<PrefabBrushPalette>(path);
                }
                return palette;
            }
            set
            {
                palette = value;
                var path = value != null ? AssetDatabase.GetAssetPath(value) : "";
                EditorPrefs.SetString(Key, AssetDatabase.AssetPathToGUID(path));
            }
        }
    }
}
