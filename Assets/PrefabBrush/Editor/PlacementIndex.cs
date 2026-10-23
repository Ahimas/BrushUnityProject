using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PrefabBrushPlugin
{
    internal sealed class PlacementIndex
    {
        struct Item
        {
            public GameObject go;
            public Vector3 pos;
            public float radius;
        }

        static readonly System.Predicate<GameObject> IsDead = g => !g;

        readonly List<Item> items = new List<Item>();
        readonly Dictionary<Vector3Int, List<int>> cells = new Dictionary<Vector3Int, List<int>>();
        readonly List<GameObject> ignore = new List<GameObject>();
        readonly List<Renderer> rendererBuffer = new List<Renderer>();
        readonly List<int> scratch = new List<int>();
        GameObject[] ignoreArray = System.Array.Empty<GameObject>();
        bool ignoreDirty;
        float cellSize = 1f;
        float maxRadius;

        public bool Dirty = true;

        /// <summary>Renderer objects of all placed instances, to be skipped by surface picking.</summary>
        public GameObject[] IgnoreArray
        {
            get
            {
                if (ignoreDirty)
                {
                    ignore.RemoveAll(IsDead);
                    ignoreArray = ignore.ToArray();
                    ignoreDirty = false;
                }
                return ignoreArray;
            }
        }

        /// <summary>Rebuilds the index from all PrefabBrush containers in loaded scenes.</summary>
        public void Rebuild(PrefabBrushPalette palette)
        {
            Dirty = false;
            items.Clear();
            cells.Clear();
            ignore.Clear();
            ignoreDirty = true;
            maxRadius = 0f;
            cellSize = Mathf.Max(0.5f, palette.MaxRadius * 2f);
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t.name != PrefabBrushTool.ContainerName) continue;
                for (int c = 0; c < t.childCount; c++)
                {
                    var go = t.GetChild(c).gameObject;
                    Add(go, ExistingRadius(palette, go));
                }
            }
        }

        /// <summary>Registers an instance with its horizontal radius.</summary>
        public void Add(GameObject go, float radius)
        {
            var pos = go.transform.position;
            int idx = items.Count;
            items.Add(new Item { go = go, pos = pos, radius = radius });
            var key = Cell(pos);
            if (!cells.TryGetValue(key, out var list))
            {
                list = new List<int>(4);
                cells[key] = list;
            }
            list.Add(idx);
            if (radius > maxRadius) maxRadius = radius;
            go.GetComponentsInChildren(true, rendererBuffer);
            for (int i = 0; i < rendererBuffer.Count; i++) ignore.Add(rendererBuffer[i].gameObject);
            ignoreDirty = true;
        }

        /// <summary>True if a sphere at pos with the given radius does not overlap any registered instance.</summary>
        public bool IsFree(Vector3 pos, float radius, float spacing)
        {
            Collect(pos, (radius + maxRadius) * spacing);
            for (int i = 0; i < scratch.Count; i++)
            {
                var it = items[scratch[i]];
                float lim = (radius + it.radius) * spacing;
                if ((it.pos - pos).sqrMagnitude < lim * lim) return false;
            }
            return true;
        }

        /// <summary>Collects indices of live instances whose pivot lies within radius of pos.</summary>
        public void Query(Vector3 pos, float radius, List<int> result)
        {
            result.Clear();
            Collect(pos, radius);
            float sq = radius * radius;
            for (int i = 0; i < scratch.Count; i++)
                if ((items[scratch[i]].pos - pos).sqrMagnitude <= sq) result.Add(scratch[i]);
        }

        public GameObject GetObject(int index) => items[index].go;

        /// <summary>Marks an instance as removed.</summary>
        public void Remove(int index) => items[index] = default;

        void Collect(Vector3 pos, float reach)
        {
            scratch.Clear();
            int n = Mathf.CeilToInt(reach / cellSize);
            long span = 2L * n + 1;
            if (span * span * span > items.Count)
            {
                for (int i = 0; i < items.Count; i++)
                    if (items[i].go) scratch.Add(i);
                return;
            }
            var c = Cell(pos);
            for (int x = -n; x <= n; x++)
                for (int y = -n; y <= n; y++)
                    for (int z = -n; z <= n; z++)
                    {
                        if (!cells.TryGetValue(new Vector3Int(c.x + x, c.y + y, c.z + z), out var list)) continue;
                        for (int i = 0; i < list.Count; i++)
                            if (items[list[i]].go) scratch.Add(list[i]);
                    }
        }

        Vector3Int Cell(Vector3 p)
        {
            return new Vector3Int(
                Mathf.FloorToInt(p.x / cellSize),
                Mathf.FloorToInt(p.y / cellSize),
                Mathf.FloorToInt(p.z / cellSize));
        }

        float ExistingRadius(PrefabBrushPalette palette, GameObject go)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            var s = go.transform.lossyScale;
            if (src != null && palette.TryGetUnitRadius(src, out float r))
                return r * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
            go.GetComponentsInChildren(true, rendererBuffer);
            if (rendererBuffer.Count == 0) return 0.5f;
            Bounds b = rendererBuffer[0].bounds;
            for (int i = 1; i < rendererBuffer.Count; i++) b.Encapsulate(rendererBuffer[i].bounds);
            return Mathf.Max(0.05f, Mathf.Max(b.extents.x, b.extents.z));
        }
    }
}
