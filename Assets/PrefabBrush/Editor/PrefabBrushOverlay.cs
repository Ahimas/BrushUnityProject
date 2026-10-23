using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;

namespace PrefabBrushPlugin
{
    [Overlay(typeof(SceneView), OverlayId, "Prefab Brush")]
    public sealed class PrefabBrushOverlay : IMGUIOverlay
    {
        public const string OverlayId = "prefab-brush-settings";

        Editor paletteEditor;

        public PrefabBrushOverlay()
        {
            minSize = new Vector2(280f, 60f);
        }

        public override void OnCreated()
        {
            displayed = ToolManager.activeToolType == typeof(PrefabBrushTool);
        }

        public override void OnWillBeDestroyed()
        {
            if (paletteEditor != null) Object.DestroyImmediate(paletteEditor);
        }

        /// <summary>Draws the palette selector and palette settings.</summary>
        public override void OnGUI()
        {
            var palette = PrefabBrushSettings.Palette;
            EditorGUI.BeginChangeCheck();
            var picked = (PrefabBrushPalette)EditorGUILayout.ObjectField(palette, typeof(PrefabBrushPalette), false);
            if (EditorGUI.EndChangeCheck()) PrefabBrushSettings.Palette = palette = picked;

            if (palette == null)
            {
                if (GUILayout.Button("New Palette")) CreatePalette();
                return;
            }

            Editor.CreateCachedEditor(palette, null, ref paletteEditor);
            paletteEditor.OnInspectorGUI();
            EditorGUILayout.HelpBox("LMB: place   Shift+LMB: erase   Esc: exit", MessageType.None);
        }

        static void CreatePalette()
        {
            var path = EditorUtility.SaveFilePanelInProject("New Palette", "PrefabBrushPalette", "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            var asset = ScriptableObject.CreateInstance<PrefabBrushPalette>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            PrefabBrushSettings.Palette = asset;
        }
    }
}
