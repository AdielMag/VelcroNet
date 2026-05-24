using UnityEditor;
using UnityEngine;

namespace AetherNet.Editor
{
    [CustomEditor(typeof(AetherCircleCollider))]
    [CanEditMultipleObjects]
    public sealed class AetherCircleColliderGizmo : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var col = (AetherCircleCollider)target;
            var so  = new SerializedObject(col);

            float   radius    = so.FindProperty("_radius").floatValue;
            Vector2 offset    = so.FindProperty("_offset").vector2Value;
            bool    isTrigger = so.FindProperty("_isTrigger").boolValue;

            Handles.color = isTrigger ? new Color(0.4f, 0.7f, 1f, 0.9f) : new Color(0.1f, 0.9f, 0.1f, 0.9f);

            Transform tf     = col.transform;
            Vector3   center = tf.TransformPoint(new Vector3(offset.x, offset.y, 0f));
            float     scale  = tf.lossyScale.x; // assume uniform scale

            Handles.DrawWireDisc(center, Vector3.forward, radius * scale);
        }
    }
}
