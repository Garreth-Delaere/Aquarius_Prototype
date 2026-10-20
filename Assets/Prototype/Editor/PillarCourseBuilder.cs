using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    [InitializeOnLoad]
    public static class PillarCourseBuilder
    {
        public const string ScenePath = "Assets/Scenes/PillarCourse.unity";
        static PillarCourseBuilder() { EditorApplication.delayCall += CreateIfMissing; }
        static void CreateIfMissing()
        {
            if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Build();
        }
        [MenuItem("Aquarius/Open Pillar Course")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(); EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/FreezeTraversalTest.unity") == null) FreezeTestSceneBuilder.Build();
            Scene previous = SceneManager.GetActiveScene();
            if (!AssetDatabase.CopyAsset("Assets/Scenes/FreezeTraversalTest.unity", ScenePath)) throw new InvalidOperationException("Could not copy the sandbox scene.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                FirstPersonMotor player = null; WaterStream water = null;
                foreach (var obj in scene.GetRootGameObjects())
                {
                    if (obj.TryGetComponent<FirstPersonMotor>(out var p)) player = p;
                    else if (obj.TryGetComponent<WaterStream>(out var w)) water = w;
                    else if (obj.GetComponent<Light>() == null && obj.name != "Global Volume") UnityEngine.Object.DestroyImmediate(obj);
                }
                if (player == null || water == null) throw new InvalidOperationException("The sandbox must contain a player and water stream.");
                Material wood = Material("CourseWood", new Color(0.34f, 0.22f, 0.12f));
                Material deck = Material("CourseDeck", new Color(0.58f, 0.40f, 0.23f));
                Material rock = Material("CourseRock", new Color(0.28f, 0.31f, 0.34f));
                Material blue = Material("CourseWater", new Color(0.025f, 0.38f, 0.57f));
                Material basin = Material("CourseBasin", new Color(0.38f, 0.44f, 0.46f));
                Material gold = Material("CourseDestination", new Color(1f, 0.7f, 0.12f));
                Material green = Material("CourseGoalDeck", new Color(0.25f, 0.53f, 0.37f));
                Vector3[] tops = { new Vector3(0,8,0), new Vector3(5,10,9), new Vector3(-2,9,17), new Vector3(4,12,26) };
                float[] widths = { 5f, 4.8f, 4.8f, 5f };
                var sources = new WaterSource[4];
                for (int i = 0; i < tops.Length; i++)
                {
                    Vector3 top = tops[i]; float width = widths[i];
                    Box("Pillar " + (i + 1), new Vector3(top.x, top.y * 0.5f - 0.2f, top.z), new Vector3(width * 0.62f, top.y + 0.4f, width * 0.62f), wood);
                    Box("Pillar " + (i + 1) + " deck", top - Vector3.up * 0.16f, new Vector3(width, 0.32f, width), i == 3 ? green : deck);
                    // A shallow raised trough, clear of the central walking/arrival area.
                    Vector3 centre = top + new Vector3(-width * 0.27f, 0.04f, width * 0.21f);
                    var supply = new GameObject("Pillar " + (i + 1) + " water basin"); supply.transform.position = centre + Vector3.up * 0.06f;
                    sources[i] = supply.AddComponent<WaterSource>(); sources[i].surfaceSize = new Vector2(1.25f, 1.25f);
                    Box("Basin base " + (i + 1), centre, new Vector3(1.5f, 0.08f, 1.5f), basin);
                    Box("Basin side west " + (i + 1), centre + new Vector3(-0.7f, 0.05f, 0), new Vector3(0.12f, 0.18f, 1.5f), basin);
                    Box("Basin side east " + (i + 1), centre + new Vector3(0.7f, 0.05f, 0), new Vector3(0.12f, 0.18f, 1.5f), basin);
                    Box("Basin side south " + (i + 1), centre + new Vector3(0, 0.05f, -0.7f), new Vector3(1.5f, 0.18f, 0.12f), basin);
                    Box("Basin side north " + (i + 1), centre + new Vector3(0, 0.05f, 0.7f), new Vector3(1.5f, 0.18f, 0.12f), basin);
                    GameObject surface = Box("Basin surface " + (i + 1), supply.transform.position - Vector3.up * 0.015f, new Vector3(1.25f, 0.03f, 1.25f), blue);
                    UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>()); surface.transform.SetParent(supply.transform, true);
                    Label(i == 0 ? "START" : i == 3 ? "DESTINATION" : "WATER", top + new Vector3(0.6f, 1.2f, width * 0.3f));
                }
                Box("River - unsafe below", new Vector3(1, -0.12f, 12), new Vector3(32, 0.2f, 50), blue);
                Box("Canyon west wall", new Vector3(-15.5f, 6, 12), new Vector3(2, 27, 50), rock);
                Box("Canyon east wall", new Vector3(17.5f, 6, 12), new Vector3(2, 27, 50), rock);
                Box("Route obstruction", new Vector3(2.6f, 6.2f, 5), new Vector3(3.6f, 13, 2.6f), rock);
                // Beacon offset toward the left of the goal deck remains visible around the rock.
                Vector3 beaconBase = tops[3] + new Vector3(-1.4f, 0, 0);
                Box("Destination beacon mast", beaconBase + Vector3.up * 2.4f, new Vector3(0.22f, 4.8f, 0.22f), gold);
                Box("Destination beacon", beaconBase + Vector3.up * 5, new Vector3(1.4f, 0.7f, 1.4f), gold);
                player.transform.SetPositionAndRotation(new Vector3(-0.65f, 8.05f, -1.1f), Quaternion.Euler(0, 8, 0));
                player.resetBelowY = -8; // Course reset happens just above the river; keep sandbox fallback.
                var interaction = player.GetComponent<StreamManipulator>();
                interaction.source = sources[0]; interaction.additionalSources = new[] { sources[1], sources[2], sources[3] };
                var objective = new GameObject("Pillar course objective").AddComponent<PillarCourse>();
                objective.player = player; objective.water = water;
                var destination = new GameObject("Destination arrival area").transform; destination.position = tops[3]; objective.destination = destination;
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = new Color(0.40f, 0.47f, 0.52f); RenderSettings.fogStartDistance = 38; RenderSettings.fogEndDistance = 100;
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Pillar course ready: Aquarius > Open Pillar Course. Reach the gold beacon using your own persistent ice route.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        static Material Material(string name, Color colour)
        {
            string path = "Assets/Prototype/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = colour }; material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        static void Label(string text, Vector3 position)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.12f; label.fontSize = 48; label.transform.position = position;
        }
    }
}

