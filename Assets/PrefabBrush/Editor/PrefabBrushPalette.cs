using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabBrushPlugin
{
    [CreateAssetMenu(fileName = "PrefabBrushPalette", menuName = "Prefab Brush/Palette")]
    public sealed class PrefabBrushPalette : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Prefab to place.")]
            public GameObject prefab;

            [Tooltip("Relative chance of this prefab being picked. 1 = most frequent; 0 = rarest, but still possible.")]
            [Range(0f, 1f)] public float weight = 1f;
        }

        public const float MinWeight = 0.01f;

        [Tooltip("Prefabs the brush picks from randomly, according to their weights.")]
        public Entry[] entries = new Entry[0];

        [Tooltip("Place only on objects with the selected tag (and their child renderers).")]
        public bool useTagFilter;

        [Tooltip("Tag required on the surface object when the tag filter is enabled.")]
        public string tag = "Untagged";

        [Tooltip("Rotate each placed object by a random angle around its local Y axis.")]
        public bool randomYRotation = true;

        [Tooltip("Object orientation: 0 = always world up (trees on slopes stay vertical), 1 = local Y follows the surface normal (walls, ceilings).")]
        [Range(0f, 1f)] public float normalAlignment = 1f;

        [Tooltip("Random uniform scale multiplier range (min, max) applied on top of the prefab's own scale.")]
        public Vector2 scaleRange = Vector2.one;

        [Tooltip("Multiplier for the minimum distance between objects. 1 = their sizes just touch, above 1 = gaps, 0 = overlap allowed.")]
        [Min(0f)] public float spacing = 1f;

        [Tooltip("Radius (world units) for Shift+LMB erasing. Objects whose pivot is inside it are removed.")]
        [Min(0.01f)] public float eraseRadius = 1f;

        [NonSerialized] bool built;
        [NonSerialized] bool valid;
        [NonSerialized] float[] unitRadius;
        [NonSerialized] float[] prefabScaleXZ;
        [NonSerialized] float[] cumulative;
        [NonSerialized] float total;
        [NonSerialized] float scaleMin = 1f;
        [NonSerialized] float scaleMax = 1f;
        readonly Dictionary<GameObject, int> lookup = new Dictionary<GameObject, int>();

        public float MaxRadius { get; private set; }

        void OnValidate() => built = false;
        void OnEnable() => built = false;

        /// <summary>Rebuilds cached radii and weights if needed; returns true if the palette has a usable prefab.</summary>
        public bool Build()
        {
            if (built) return valid;
            built = true;
            int n = entries != null ? entries.Length : 0;
            unitRadius = new float[n];
            prefabScaleXZ = new float[n];
            cumulative = new float[n];
            lookup.Clear();
            total = 0f;
            MaxRadius = 0f;
            scaleMin = Mathf.Min(scaleRange.x, scaleRange.y);
            scaleMax = Mathf.Max(scaleRange.x, scaleRange.y);
            for (int i = 0; i < n; i++)
            {
                var e = entries[i];
                bool hasPrefab = e != null && e.prefab != null;
                if (hasPrefab)
                {
                    var s = e.prefab.transform.localScale;
                    unitRadius[i] = ComputeUnitRadius(e.prefab);
                    prefabScaleXZ[i] = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
                    lookup[e.prefab] = i;
                    total += Mathf.Max(e.weight, MinWeight);
                    MaxRadius = Mathf.Max(MaxRadius, unitRadius[i] * prefabScaleXZ[i] * scaleMax);
                }
                cumulative[i] = total;
            }
            valid = total > 0f;
            return valid;
        }

        /// <summary>True if the entry has a prefab assigned.</summary>
        public bool IsUsable(int i)
        {
            return built && entries != null && i >= 0 && i < entries.Length && entries[i] != null
                   && entries[i].prefab != null;
        }

        /// <summary>Picks an entry index proportionally to weights.</summary>
        public int Pick(System.Random rng)
        {
            float r = (float)(rng.NextDouble() * total);
            int lo = 0, hi = cumulative.Length - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (cumulative[mid] > r) hi = mid;
                else lo = mid + 1;
            }
            return lo;
        }

        /// <summary>Random uniform scale within the configured range.</summary>
        public float RollScale(System.Random rng)
        {
            return Mathf.Lerp(scaleMin, scaleMax, (float)rng.NextDouble());
        }

        /// <summary>Horizontal radius of an entry placed with the given extra scale.</summary>
        public float WorldRadius(int i, float scale)
        {
            return unitRadius[i] * prefabScaleXZ[i] * scale;
        }

        /// <summary>Radius in the prefab root's unscaled local space, if the prefab belongs to the palette.</summary>
        public bool TryGetUnitRadius(GameObject prefab, out float radius)
        {
            if (lookup.TryGetValue(prefab, out int i))
            {
                radius = unitRadius[i];
                return true;
            }
            radius = 0f;
            return false;
        }

        static float ComputeUnitRadius(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return 0.5f;
            var rootInv = prefab.transform.worldToLocalMatrix;
            Bounds b = default;
            bool init = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                var m = rootInv * r.transform.localToWorldMatrix;
                var lb = r.localBounds;
                var c = lb.center;
                var ex = lb.extents;
                for (int k = 0; k < 8; k++)
                {
                    var corner = m.MultiplyPoint3x4(c + new Vector3(
                        (k & 1) == 0 ? -ex.x : ex.x,
                        (k & 2) == 0 ? -ex.y : ex.y,
                        (k & 4) == 0 ? -ex.z : ex.z));
                    if (!init) { b = new Bounds(corner, Vector3.zero); init = true; }
                    else b.Encapsulate(corner);
                }
            }
            return Mathf.Max(0.05f, Mathf.Max(b.extents.x, b.extents.z));
        }
    }
}
