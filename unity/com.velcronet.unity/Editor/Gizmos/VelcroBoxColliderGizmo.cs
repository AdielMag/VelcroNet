using UnityEditor;
using UnityEngine;

namespace VelcroNet.Editor
{
    [CustomEditor(typeof(VelcroBoxCollider))]
    [CanEditMultipleObjects]
    public sealed class VelcroBoxColliderGizmo : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var col = (VelcroBoxCollider)target;
            var so  = new SerializedObject(col);

            Vector2 size      = so.FindProperty("_size").vector2Value;
            Vector2 offset    = so.FindProperty("_offset").vector2Value;
            bool    isTrigger = so.FindProperty("_isTrigger").boolValue;

            Handles.color = isTrigger ? new Color(0.4f, 0.7f, 1f, 0.9f) : new Color(0.1f, 0.9f, 0.1f, 0.9f);

            Transform tf     = col.transform;
            Vector3   center = tf.TransformPoint(new Vector3(offset.x, offset.y, 0f));

            // Draw four edges of the box in scene space
            Vector3 halfX = tf.TransformVector(new Vector3(size.x * 0.5f, 0f, 0f));
            Vector3 halfY = tf.TransformVector(new Vector3(0f, size.y * 0.5f, 0f));

            Vector3 tl = center - halfX + halfY;
            Vector3 tr = center + halfX + halfY;
            Vector3 br = center + halfX - halfY;
            Vector3 bl = center - halfX - halfY;

            Handles.DrawLine(tl, tr);
            Handles.DrawLine(tr, br);
            Handles.DrawLine(br, bl);
            Handles.DrawLine(bl, tl);
        }
    }
}
