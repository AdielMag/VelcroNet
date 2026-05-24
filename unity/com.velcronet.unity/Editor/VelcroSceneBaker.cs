using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;
using VelcroNet.Server; // BakedEntityDef, BakedFixtureDef, MapData

namespace VelcroNet.Editor
{
    /// <summary>
    /// Scans the scene for VelcroRigidbody + collider components and exports a
    /// MapData JSON file that the headless server can load without Unity installed.
    /// </summary>
    public static class VelcroSceneBaker
    {
        [MenuItem("VelcroNet/Bake Scene to JSON")]
        public static void BakeScene()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string defaultPath = Path.Combine("Assets", "StreamingAssets", $"{sceneName}.json");
            string savePath = EditorUtility.SaveFilePanel(
                "Save Baked Map", "Assets/StreamingAssets", sceneName, "json");

            if (string.IsNullOrEmpty(savePath)) return;

            MapData map = BuildMapData(sceneName);

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(map, MapSerializerContext.Default.MapData);
            File.WriteAllText(savePath, json);

            // Refresh Asset Database if saved inside the project
            if (savePath.StartsWith(Application.dataPath))
            {
                string relative = "Assets" + savePath[Application.dataPath.Length..];
                AssetDatabase.ImportAsset(relative);
            }

            Debug.Log($"[VelcroNet] Baked {map.Entities.Length} entities to: {savePath}");
            EditorUtility.DisplayDialog("VelcroNet Baker",
                $"Baked {map.Entities.Length} entities.\n\nSaved to:\n{savePath}", "OK");
        }

        private static MapData BuildMapData(string sceneName)
        {
#if UNITY_2022_1_OR_NEWER
            var rigidbodies = UnityEngine.Object.FindObjectsByType<VelcroRigidbody>(FindObjectsSortMode.None);
#else
            var rigidbodies = UnityEngine.Object.FindObjectsOfType<VelcroRigidbody>();
#endif
            var entities = new List<BakedEntityDef>(rigidbodies.Length);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                VelcroRigidbody rb = rigidbodies[i];
                Transform       tf = rb.transform;

                var entity = new BakedEntityDef
                {
                    EntityId       = i,
                    BodyType       = rb.BodyType,
                    PositionX      = tf.position.x / SimulationConstants.PixelsPerMeter,
                    PositionY      = tf.position.y / SimulationConstants.PixelsPerMeter,
                    Angle          = MathExtensions.ToSimAngle(tf.eulerAngles.z),
                    LinearDamping  = rb.LinearDamping,
                    AngularDamping = rb.AngularDamping,
                    GravityScale   = rb.GravityScale,
                    FixedRotation  = rb.FixedRotation,
                    Constraints    = rb.Constraints,
                    Fixtures       = BuildFixtures(rb),
                };
                entities.Add(entity);
            }

            return new MapData { MapName = sceneName, Entities = entities.ToArray() };
        }

        private static BakedFixtureDef[] BuildFixtures(VelcroRigidbody rb)
        {
            var result = new List<BakedFixtureDef>();

            foreach (var box in rb.GetComponents<VelcroBoxCollider>())
            {
                var so = new SerializedObject(box);
                result.Add(new BakedFixtureDef
                {
                    Shape         = BakedFixtureShape.Box,
                    Width         = so.FindProperty("_size").vector2Value.x  / SimulationConstants.PixelsPerMeter,
                    Height        = so.FindProperty("_size").vector2Value.y  / SimulationConstants.PixelsPerMeter,
                    OffsetX       = so.FindProperty("_offset").vector2Value.x / SimulationConstants.PixelsPerMeter,
                    OffsetY       = so.FindProperty("_offset").vector2Value.y / SimulationConstants.PixelsPerMeter,
                    IsSensor      = so.FindProperty("_isTrigger").boolValue,
                    Layer         = so.FindProperty("_layer").intValue,
                    CollisionMask = so.FindProperty("_collisionMask").intValue,
                    Density       = ReadMaterialDensity(so),
                    Friction      = ReadMaterialFriction(so),
                    Restitution   = ReadMaterialRestitution(so),
                });
            }

            foreach (var circle in rb.GetComponents<VelcroCircleCollider>())
            {
                var so = new SerializedObject(circle);
                result.Add(new BakedFixtureDef
                {
                    Shape         = BakedFixtureShape.Circle,
                    Radius        = so.FindProperty("_radius").floatValue / SimulationConstants.PixelsPerMeter,
                    OffsetX       = so.FindProperty("_offset").vector2Value.x / SimulationConstants.PixelsPerMeter,
                    OffsetY       = so.FindProperty("_offset").vector2Value.y / SimulationConstants.PixelsPerMeter,
                    IsSensor      = so.FindProperty("_isTrigger").boolValue,
                    Layer         = so.FindProperty("_layer").intValue,
                    CollisionMask = so.FindProperty("_collisionMask").intValue,
                    Density       = ReadMaterialDensity(so),
                    Friction      = ReadMaterialFriction(so),
                    Restitution   = ReadMaterialRestitution(so),
                });
            }

            foreach (var poly in rb.GetComponents<VelcroPolygonCollider>())
            {
                var so   = new SerializedObject(poly);
                var verts = so.FindProperty("_vertices");
                var xs   = new float[verts.arraySize];
                var ys   = new float[verts.arraySize];
                for (int v = 0; v < verts.arraySize; v++)
                {
                    Vector2 pt = verts.GetArrayElementAtIndex(v).vector2Value;
                    xs[v] = pt.x / SimulationConstants.PixelsPerMeter;
                    ys[v] = pt.y / SimulationConstants.PixelsPerMeter;
                }
                result.Add(new BakedFixtureDef
                {
                    Shape         = BakedFixtureShape.Polygon,
                    VerticesX     = xs,
                    VerticesY     = ys,
                    IsSensor      = so.FindProperty("_isTrigger").boolValue,
                    Layer         = so.FindProperty("_layer").intValue,
                    CollisionMask = so.FindProperty("_collisionMask").intValue,
                    Density       = ReadMaterialDensity(so),
                    Friction      = ReadMaterialFriction(so),
                    Restitution   = ReadMaterialRestitution(so),
                });
            }

            return result.ToArray();
        }

        private static float ReadMaterialDensity(SerializedObject so)
        {
            var mat = so.FindProperty("_material").objectReferenceValue as VelcroPhysicsMaterial;
            return mat != null ? mat.Density : 1f;
        }

        private static float ReadMaterialFriction(SerializedObject so)
        {
            var mat = so.FindProperty("_material").objectReferenceValue as VelcroPhysicsMaterial;
            return mat != null ? mat.Friction : 0.2f;
        }

        private static float ReadMaterialRestitution(SerializedObject so)
        {
            var mat = so.FindProperty("_material").objectReferenceValue as VelcroPhysicsMaterial;
            return mat != null ? mat.Restitution : 0f;
        }
    }
}
