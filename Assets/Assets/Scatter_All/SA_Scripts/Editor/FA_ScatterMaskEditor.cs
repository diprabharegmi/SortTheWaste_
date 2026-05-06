#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Bukyja.ScatterAll
{
    [CustomEditor(typeof(ScatterMask))]
    public class FA_ScatterMaskEditor : Editor
    {
        private ScatterMask mask;
        private Vector3? previewPoint = null;
        private LayerMask snapLayerMask;

        private bool isEditingEnabled = true;

        private void OnEnable()
        {
            mask = (ScatterMask)target;
            snapLayerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label("ScatterMask Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Instructions:\n" +
                 "1) By default, you can move each vertex using the transformation handles.\n" +
                 "2) Hold down CTRL (without SHIFT) to hide the transformation handles.\n" +
                 "   Instead, you can hover over the edges to see a magenta preview sphere.\n" +
                 "3) Click the left mouse button while holding down CTRL to add a new vertex.\n" +
                 "4) Every MeshPainterID object within the polygon is deactivated in real-time.\n",
                MessageType.Info
            );

            isEditingEnabled = GUILayout.Toggle(isEditingEnabled, "Enable Editing");

            if (isEditingEnabled)
            {
                GUILayout.Label("Editing is enabled. You can modify the mask.");
            }
            else
            {
                GUILayout.Label("Editing is disabled. You can move the mask without interference.");
            }
        }

        private void OnSceneGUI()
        {
            if (!isEditingEnabled)
                return;

            Event e = Event.current;

            if (e.shift)
            {
                HandlePivotMovement(e);
            }

            bool isCtrlHeld = e.control && !e.shift;

            if (isCtrlHeld)
            {
                HandleAddVertex(e);
            }
            else
            {
                if (previewPoint.HasValue)
                {
                    previewPoint = null;
                    Repaint();
                }
            }

            if (!isCtrlHeld && !e.shift)
            {
                DrawVertexHandles();
            }

            if (previewPoint.HasValue)
            {
                Handles.color = Color.magenta;
                float size = HandleUtility.GetHandleSize(previewPoint.Value) * 0.1f;
                Handles.SphereHandleCap(0, previewPoint.Value, Quaternion.identity, size, EventType.Repaint);
            }

            if (GUI.changed)
                Repaint();
        }

        private void DrawVertexHandles()
        {
            for (int i = 0; i < mask.points.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 worldPos = mask.transform.TransformPoint(mask.points[i]);
                Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(mask, "Move Vertex");
                    mask.points[i] = mask.transform.InverseTransformPoint(newWorldPos);
                    EditorUtility.SetDirty(mask);
                }

                Handles.color = Color.green;
                float sphereSize = HandleUtility.GetHandleSize(worldPos) * 0.07f;
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, sphereSize, EventType.Repaint);
            }
        }

        private void HandleAddVertex(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane plane = new Plane(mask.transform.up, mask.transform.position);
            if (!plane.Raycast(ray, out float dist))
            {
                previewPoint = null;
                return;
            }

            Vector3 clickPos = ray.GetPoint(dist);

            int n = mask.points.Count;
            int nearestEdge = -1;
            float nearestDist = float.MaxValue;
            Vector3 bestProj = Vector3.zero;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector3 A = mask.transform.TransformPoint(mask.points[i]);
                Vector3 B = mask.transform.TransformPoint(mask.points[j]);
                Vector3 proj = ProjectOnSegment(clickPos, A, B, out float d);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearestEdge = i;
                    bestProj = proj;
                }
            }

            Debug.Log($"Nearest Edge Index: {nearestEdge}, Distance: {nearestDist}, Projection Point: {bestProj}");

            previewPoint = bestProj;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (nearestEdge == -1)
                {
                    Debug.LogWarning("No edge found to add a vertex.");
                    return;
                }

                int nextIndex = (nearestEdge + 1) % n;
                Vector3 localV = mask.transform.InverseTransformPoint(bestProj);

                Undo.RecordObject(mask, "Add Vertex");
                mask.points.Insert(nextIndex, localV);

                RecenterPivotInline();

                previewPoint = null;
                e.Use();
            }

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                previewPoint = bestProj;
                HandleUtility.Repaint();
            }
        }

        private void RecenterPivotInline()
        {
            if (mask.points.Count == 0) return;

            Vector3 avg = Vector3.zero;
            for (int i = 0; i < mask.points.Count; i++)
                avg += mask.points[i];
            avg /= mask.points.Count;

            Vector3 oldWorldCenter = mask.transform.TransformPoint(avg);

            for (int i = 0; i < mask.points.Count; i++)
                mask.points[i] -= avg;

            Vector3 newWorldCenter = mask.transform.TransformPoint(Vector3.zero);

            Vector3 shift = oldWorldCenter - newWorldCenter;
            Undo.RecordObject(mask.transform, "Recenter Pivot");
            mask.transform.position += shift;
            EditorUtility.SetDirty(mask.transform);
        }

        private void HandlePivotMovement(Event e)
        {
            Vector3 pivotWorldPos = mask.transform.position;
            EditorGUI.BeginChangeCheck();
            Vector3 newPivotWorldPos = Handles.PositionHandle(pivotWorldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, snapLayerMask))
                {
                    newPivotWorldPos = hit.point;
                }
                else
                {
                    Debug.LogWarning("Raycast did not hit any collider.");
                }

                Undo.RecordObject(mask.transform, "Move Pivot");
                mask.transform.position = newPivotWorldPos;
                EditorUtility.SetDirty(mask.transform);
            }

            Handles.color = Color.blue;
            float sphereSize = HandleUtility.GetHandleSize(pivotWorldPos) * 0.1f;
            Handles.SphereHandleCap(0, pivotWorldPos, Quaternion.identity, sphereSize, EventType.Repaint);
        }

        private Vector3 ProjectOnSegment(Vector3 p, Vector3 a, Vector3 b, out float outDist)
        {
            Vector3 ap = p - a;
            Vector3 ab = b - a;
            float abLengthSquared = ab.sqrMagnitude;

            if (abLengthSquared == 0)
            {
                outDist = Vector3.Distance(p, a);
                return a;
            }

            float t = Vector3.Dot(ap, ab) / abLengthSquared;
            t = Mathf.Clamp01(t);

            Vector3 proj = a + ab * t;
            outDist = Vector3.Distance(p, proj);
            return proj;
        }
    }
}
#endif