using System.Collections.Generic;
using UnityEngine;

namespace Bukyja.ScatterAll
{
    [ExecuteInEditMode]
    public class ScatterMask : MonoBehaviour
    {
        [HideInInspector]
        public List<Vector3> points = new List<Vector3>();

        private HashSet<GameObject> affectedMeshes = new HashSet<GameObject>();

        private void Reset()
        {
            if (points.Count == 0)
            {
                points.Add(new Vector3(-5, 0, -5));
                points.Add(new Vector3(-5, 0, 5));
                points.Add(new Vector3(5, 0, 5));
                points.Add(new Vector3(5, 0, -5));
            }
        }

        private void OnEnable()
        {
            RestoreMeshes(); 
        }

        private void OnDrawGizmos()
        {
            if (points == null || points.Count < 2)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                Vector3 wA = transform.TransformPoint(points[i]);
                Vector3 wB = transform.TransformPoint(points[next]);
                Gizmos.DrawLine(wA, wB);
            }

            if (!Application.isPlaying)
            {
                UpdateAffectedMeshes();
            }
        }

        private void OnDisable()
        {
            RestoreMeshes(); 
        }

        private void OnDestroy()
        {
            RestoreMeshes();
        }

        private void UpdateAffectedMeshes()
        {
            RestoreMeshes(); // Ripristina prima di aggiornare

            MeshPainterID[] allMeshIDs = FindObjectsOfType<MeshPainterID>(true);
            foreach (var mpid in allMeshIDs)
            {
                if (mpid == null) continue;

                bool inside = IsInsidePolygon(mpid.transform.position);
                if (inside)
                {
                    if (!affectedMeshes.Contains(mpid.gameObject))
                    {
                        affectedMeshes.Add(mpid.gameObject);
                        mpid.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void RestoreMeshes()
        {
            foreach (var mesh in affectedMeshes)
            {
                if (mesh != null)
                    mesh.SetActive(true);
            }

            affectedMeshes.Clear();
        }

        private bool IsInsidePolygon(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            bool inside = false;
            int n = points.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 pi = new Vector2(points[i].x, points[i].z);
                Vector2 pj = new Vector2(points[j].x, points[j].z);
                Vector2 pt = new Vector2(localPos.x, localPos.z);

                bool intersect = ((pi.y > pt.y) != (pj.y > pt.y)) &&
                                 (pt.x < (pj.x - pi.x) * (pt.y - pi.y) / (pj.y - pi.y) + pi.x);
                if (intersect)
                    inside = !inside;
            }
            return inside;
        }
    }
}
