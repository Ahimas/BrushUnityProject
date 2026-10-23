using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrefabBrushPlugin
{
    [EditorTool("Prefab Brush")]
    public sealed class PrefabBrushTool : EditorTool
    {
        public const string ContainerName = "PrefabBrush_Placed";
        const string UndoName = "Prefab Brush";
        const float DragThresholdSqr = 16f;

        internal static readonly PlacementIndex Index = new PlacementIndex();

        readonly System.Random rng = new System.Random();
        readonly List<int> queryBuffer = new List<int>();
        readonly List<Renderer> rendererBuffer = new List<Renderer>();
        GUIContent icon;
        GameObject[] filter;
        bool filterHasNoTargets;
        string filterKey;
        int pendingEntry = -1;
        float pendingScale = 1f;
        bool hoverValid;
        RaycastHit hover;
        Vector2 lastMouse;
        int undoGroup;

        public override GUIContent toolbarIcon
        {
            get
            {
                if (icon == null) icon = new GUIContent(EditorGUIUtility.IconContent("Grid.PaintTool").image, "Prefab Brush");
                return icon;
            }
        }

        [MenuItem("Tools/Prefab Brush")]
        [Shortcut("Prefab Brush/Toggle", KeyCode.B, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void Toggle()
        {
            if (ToolManager.activeToolType == typeof(PrefabBrushTool)) ToolManager.RestorePreviousTool();
            else ToolManager.SetActiveTool<PrefabBrushTool>();
        }

        /// <summary>Subscribes to scene events and shows the settings overlay.</summary>
        public override void OnActivated()
        {
            Index.Dirty = true;
            filterKey = null;
            Undo.undoRedoPerformed += MarkDirty;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            SetOverlayVisible(true);
        }

        /// <summary>Unsubscribes from scene events and hides the settings overlay.</summary>
        public override void OnWillBeDeactivated()
        {
            Undo.undoRedoPerformed -= MarkDirty;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            SetOverlayVisible(false);
        }

        static void MarkDirty() => Index.Dirty = true;
        static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Index.Dirty = true;

        static void SetOverlayVisible(bool visible)
        {
            foreach (SceneView sv in SceneView.sceneViews)
                if (sv.TryGetOverlay(PrefabBrushOverlay.OverlayId, out var overlay)) overlay.displayed = visible;
        }

        /// <summary>Handles brush input and preview in the Scene View.</summary>
        public override void OnToolGUI(EditorWindow window)
        {
            var palette = PrefabBrushSettings.Palette;
            if (palette == null || !palette.Build()) return;

            var e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        ToolManager.RestorePreviousTool();
                        e.Use();
                    }
                    break;

                case EventType.MouseMove:
                    if ((e.mousePosition - lastMouse).sqrMagnitude >= 4f)
                    {
                        lastMouse = e.mousePosition;
                        UpdateHover(palette, e.mousePosition);
                        window.Repaint();
                    }
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt && HandleUtility.nearestControl == id)
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                        BeginStroke(palette);
                        lastMouse = e.mousePosition;
                        Apply(palette, e.mousePosition, e.shift);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        e.Use();
                        if ((e.mousePosition - lastMouse).sqrMagnitude >= DragThresholdSqr)
                        {
                            lastMouse = e.mousePosition;
                            Apply(palette, e.mousePosition, e.shift);
                        }
                        window.Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                        Undo.CollapseUndoOperations(undoGroup);
                    }
                    break;

                case EventType.Repaint:
                    DrawPreview(palette, e.shift);
                    break;
            }
        }

        void BeginStroke(PrefabBrushPalette palette)
        {
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            RefreshFilter(palette);
            EnsureIndex(palette);
        }

        void EnsureIndex(PrefabBrushPalette palette)
        {
            if (Index.Dirty) Index.Rebuild(palette);
        }

        /// <summary>Caches renderer objects carrying the palette tag (including their children).</summary>
        void RefreshFilter(PrefabBrushPalette palette)
        {
            string key = palette.useTagFilter ? palette.tag : null;
            if (key == filterKey && (key == null || filter != null)) return;
            filterKey = key;
            filter = null;
            filterHasNoTargets = false;
            if (key == null) return;

            GameObject[] tagged;
            try { tagged = GameObject.FindGameObjectsWithTag(key); }
            catch (UnityException) { tagged = System.Array.Empty<GameObject>(); }

            var list = new List<GameObject>();
            for (int i = 0; i < tagged.Length; i++)
            {
                tagged[i].GetComponentsInChildren(true, rendererBuffer);
                for (int r = 0; r < rendererBuffer.Count; r++) list.Add(rendererBuffer[r].gameObject);
            }
            filter = list.ToArray();
            filterHasNoTargets = filter.Length == 0;
        }

        bool Pick(PrefabBrushPalette palette, Vector2 mouse, out GameObject surface)
        {
            surface = null;
            RefreshFilter(palette);
            EnsureIndex(palette);
            if (filterHasNoTargets) return false;
            return SurfacePicker.TryPick(mouse, Index.IgnoreArray, filter, out hover, out surface);
        }

        void UpdateHover(PrefabBrushPalette palette, Vector2 mouse)
        {
            hoverValid = Pick(palette, mouse, out _);
            if (!palette.IsUsable(pendingEntry)) Roll(palette);
        }

        void Roll(PrefabBrushPalette palette)
        {
            pendingEntry = palette.Pick(rng);
            pendingScale = palette.RollScale(rng);
        }

        void Apply(PrefabBrushPalette palette, Vector2 mouse, bool erase)
        {
            hoverValid = Pick(palette, mouse, out var surface);
            if (!hoverValid) return;
            if (erase) Erase(palette, hover.point);
            else Place(palette, surface);
        }

        /// <summary>Places the pending prefab at the hover point if it does not overlap other brush objects.</summary>
        void Place(PrefabBrushPalette palette, GameObject surface)
        {
            if (!palette.IsUsable(pendingEntry)) Roll(palette);
            float radius = palette.WorldRadius(pendingEntry, pendingScale);
            if (!Index.IsFree(hover.point, radius, palette.spacing)) return;

            var prefab = palette.entries[pendingEntry].prefab;
            var up = Vector3.Slerp(Vector3.up, hover.normal, palette.normalAlignment);
            var rot = Quaternion.FromToRotation(Vector3.up, up);
            if (palette.randomYRotation)
                rot *= Quaternion.AngleAxis((float)rng.NextDouble() * 360f, Vector3.up);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, surface.scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            var t = go.transform;
            t.SetPositionAndRotation(hover.point, rot * prefab.transform.rotation);
            t.localScale = prefab.transform.localScale * pendingScale;
            Undo.SetTransformParent(t, GetContainer(surface.transform), UndoName);

            Index.Add(go, radius);
            Roll(palette);
        }

        /// <summary>Destroys brush objects whose pivot is within the erase radius.</summary>
        void Erase(PrefabBrushPalette palette, Vector3 point)
        {
            Index.Query(point, palette.eraseRadius, queryBuffer);
            for (int i = 0; i < queryBuffer.Count; i++)
            {
                var go = Index.GetObject(queryBuffer[i]);
                if (go != null) Undo.DestroyObjectImmediate(go);
                Index.Remove(queryBuffer[i]);
            }
        }

        /// <summary>Returns the PrefabBrush_Placed child of the surface, creating it if missing.</summary>
        static Transform GetContainer(Transform surface)
        {
            var existing = surface.Find(ContainerName);
            if (existing != null) return existing;

            var go = new GameObject(ContainerName);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            var t = go.transform;
            Undo.SetTransformParent(t, surface, UndoName);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        void DrawPreview(PrefabBrushPalette palette, bool erase)
        {
            if (!hoverValid || !palette.IsUsable(pendingEntry)) return;
            float radius = erase ? palette.eraseRadius : palette.WorldRadius(pendingEntry, pendingScale);
            bool free = erase || Index.IsFree(hover.point, radius, palette.spacing);
            Handles.color = erase ? Color.red : free ? Color.green : new Color(1f, 0.5f, 0f);
            Handles.DrawWireDisc(hover.point, hover.normal, radius);
            Handles.DrawLine(hover.point, hover.point + hover.normal * radius);
        }
    }
}
