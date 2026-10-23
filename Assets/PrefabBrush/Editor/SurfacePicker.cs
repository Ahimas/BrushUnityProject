using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PrefabBrushPlugin
{
    internal static class SurfacePicker
    {
        delegate bool RayMeshFn(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit);

        const int MaxAttempts = 4;

        static readonly RayMeshFn IntersectRayMesh = CreateIntersect();
        static readonly List<GameObject> Skipped = new List<GameObject>(MaxAttempts);
        static Mesh bakeMesh;

        /// <summary>Finds the first visible mesh surface under the GUI point, without requiring colliders.</summary>
        public static bool TryPick(Vector2 guiPoint, GameObject[] ignore, GameObject[] filter, out RaycastHit hit, out GameObject surface)
        {
            hit = default;
            surface = null;
            var ray = HandleUtility.GUIPointToWorldRay(guiPoint);
            Skipped.Clear();
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var go = HandleUtility.PickGameObject(guiPoint, false, Merge(ignore), filter);
                if (go == null) return false;
                if (TryIntersect(ray, go, out hit))
                {
                    surface = go;
                    return true;
                }
                Skipped.Add(go);
            }
            return false;
        }

        static GameObject[] Merge(GameObject[] ignore)
        {
            if (Skipped.Count == 0) return ignore;
            var merged = new GameObject[ignore.Length + Skipped.Count];
            ignore.CopyTo(merged, 0);
            Skipped.CopyTo(merged, ignore.Length);
            return merged;
        }

        static bool TryIntersect(Ray ray, GameObject go, out RaycastHit hit)
        {
            var t = go.transform;
            if (go.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                return IntersectRayMesh(ray, mf.sharedMesh, t.localToWorldMatrix, out hit);

            if (go.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                if (bakeMesh == null) bakeMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                smr.BakeMesh(bakeMesh, true);
                return IntersectRayMesh(ray, bakeMesh, Matrix4x4.TRS(t.position, t.rotation, Vector3.one), out hit);
            }

            hit = default;
            return false;
        }

        static RayMeshFn CreateIntersect()
        {
            var method = typeof(HandleUtility).GetMethod(
                "IntersectRayMesh",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Ray), typeof(Mesh), typeof(Matrix4x4), typeof(RaycastHit).MakeByRefType() },
                null);
            return (RayMeshFn)Delegate.CreateDelegate(typeof(RayMeshFn), method);
        }
    }
}
