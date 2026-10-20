using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    [InitializeOnLoad]
    public static class FreezeTestSceneBuilder
    {
        const string Path = "Assets/Scenes/FreezeTraversalTest.unity";
        static FreezeTestSceneBuilder() { EditorApplication.delayCall += CreateIfMissing; }
        static void CreateIfMissing()
        {
            if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && AssetDatabase.LoadAssetAtPath<SceneAsset>(Path) == null) Build();
        }
        [MenuItem("Aquarius/Open Freeze Traversal Test")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(); EditorSceneManager.OpenScene(Path);
        }
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Path) != null) return;
            ChannelTestSceneBuilder.Build();
            if (!AssetDatabase.CopyAsset("Assets/Scenes/ChannelWaterTest.unity", Path)) throw new System.InvalidOperationException("Could not copy channel scene.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive); SceneManager.SetActiveScene(scene);
            try
            {
                WaterStream water = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.TryGetComponent<WaterStream>(out var found)) water = found;
                    if (root.name == "Main floor" || root.name.StartsWith("Low step") || root.name.StartsWith("Jump block") || root.name.StartsWith("Unreachable ledge") || root.name.StartsWith("Gentle ramp") || root.name.StartsWith("Steep ramp") ||
                        (root.GetComponent<TextMesh>() != null && root.name != "Channel instructions")) Object.DestroyImmediate(root);
                }
                var grey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Grey.mat");
                var green = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Gentle.mat");
                const string materialPath = "Assets/Prototype/Materials/Ice.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.65f, 0.9f, 1f) };
                    material.SetFloat("_Smoothness", 0.6f); AssetDatabase.CreateAsset(material, materialPath);
                }
                var ice = water.GetComponent<WaterIceSurface>(); if (ice == null) ice = water.gameObject.AddComponent<WaterIceSurface>();
                ice.iceMaterial = material;
                Box("Starting deck", new Vector3(2.25f, -0.25f, -2.5f), new Vector3(17.5f, 0.5f, 11), grey);
                Box("Far deck", new Vector3(2.25f, -0.25f, 10.5f), new Vector3(17.5f, 0.5f, 11), grey);
                Box("Ramp ledge - 0.8 m", new Vector3(-3, 0.4f, 1.5f), new Vector3(2.4f, 0.8f, 2), green);
                Box("Bridge landing - 0.8 m", new Vector3(-3, 0.4f, 6), new Vector3(2.4f, 0.8f, 2), green);
                Box("Elevated descent - 2 m", new Vector3(-3, 1, 10), new Vector3(2.4f, 2, 2), grey);
                Label("1. MAKE A RAMP", new Vector3(-3, 1.5f, 2));
                Label("2. FREEZE A BRIDGE", new Vector3(-3, 1.5f, 6.7f));
                Label("3. CLIMB / MAKE A DESCENT", new Vector3(-3, 2.8f, 10.7f));
                EditorSceneManager.SaveScene(scene, Path);
                Debug.Log("Freeze traversal ready: Aquarius > Open Freeze Traversal Test. F freezes held water; aim at ice + F melts that structure.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        static void Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
        }
        static void Label(string text, Vector3 position)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.085f; label.fontSize = 48; label.transform.position = position;
        }
    }
}

