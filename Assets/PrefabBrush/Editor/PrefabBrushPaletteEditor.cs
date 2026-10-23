using UnityEditor;
using UnityEngine;

namespace PrefabBrushPlugin
{
    [CustomEditor(typeof(PrefabBrushPalette))]
    public sealed class PrefabBrushPaletteEditor : Editor
    {
        /// <summary>Draws palette settings with a tag dropdown; new entries start with weight 1.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var entries = serializedObject.FindProperty("entries");
            int oldSize = entries.arraySize;
            EditorGUILayout.PropertyField(entries, true);
            for (int i = oldSize; i < entries.arraySize; i++)
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue = 1f;
            var useTag = serializedObject.FindProperty("useTagFilter");
            EditorGUILayout.PropertyField(useTag);
            if (useTag.boolValue)
            {
                var tag = serializedObject.FindProperty("tag");
                EditorGUI.indentLevel++;
                tag.stringValue = EditorGUILayout.TagField(new GUIContent("Tag", "Tag required on the surface object when the tag filter is enabled."), tag.stringValue);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("randomYRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("normalAlignment"));
            DrawScaleRange(serializedObject.FindProperty("scaleRange"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("eraseRadius"));
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Draws min/max scale as numeric fields around a two-handle slider.</summary>
        static void DrawScaleRange(SerializedProperty property)
        {
            var label = new GUIContent("Scale Min-Max", "Random uniform scale multiplier range (min, max) applied on top of the prefab's own scale.");
            var v = property.vector2Value;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            v.x = EditorGUILayout.FloatField(v.x, GUILayout.Width(40f));
            EditorGUILayout.MinMaxSlider(ref v.x, ref v.y, 0.1f, 3f);
            v.y = EditorGUILayout.FloatField(v.y, GUILayout.Width(40f));
            EditorGUILayout.EndHorizontal();
            if (!EditorGUI.EndChangeCheck()) return;
            v.x = Mathf.Max(0.01f, v.x);
            v.y = Mathf.Max(v.x, v.y);
            property.vector2Value = v;
        }
    }
}
