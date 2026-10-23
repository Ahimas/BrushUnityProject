using UnityEditor;

namespace PrefabBrushPlugin
{
    [CustomEditor(typeof(PrefabBrushPalette))]
    public sealed class PrefabBrushPaletteEditor : Editor
    {
        /// <summary>Draws palette settings with a tag dropdown for the tag filter.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("entries"), true);
            var useTag = serializedObject.FindProperty("useTagFilter");
            EditorGUILayout.PropertyField(useTag);
            if (useTag.boolValue)
            {
                var tag = serializedObject.FindProperty("tag");
                EditorGUI.indentLevel++;
                tag.stringValue = EditorGUILayout.TagField("Tag", tag.stringValue);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomYRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("normalAlignment"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("eraseRadius"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
