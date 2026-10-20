using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    [InitializeOnLoad]
    public static class LiquidTestSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/LiquidSculptingTest.unity";
        static LiquidTestSceneBuilder() { EditorApplication.delayCall += CreateIfMissing; }
        static void CreateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MovementTest.unity") != null) Build();
        }
        [MenuItem("Aquarius/Open Liquid Sculpting Test")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(); EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            MovementTestSceneBuilder.Build();
            if (!AssetDatabase.CopyAsset("Assets/Scenes/MovementTest.unity", ScenePath)) throw new InvalidOperationException("Could not copy the movement scene.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                FirstPersonMotor player = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    player = root.GetComponentInChildren<FirstPersonMotor>();
                    if (player != null) break;
                }
                if (player == null) throw new InvalidOperationException("Movement scene needs a FirstPersonMotor player.");
                var waterObject = new GameObject("Water Blob - liquid sculpting");
                waterObject.transform.position = new Vector3(0, 0.45f, -1.5f);
                var water = waterObject.AddComponent<WaterBody>();
                water.ignoredPlayer = player.transform;
                const string materialPath = "Assets/Prototype/Materials/Liquid.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.color = new Color(0.04f, 0.48f, 0.72f);
                    material.SetFloat("_Smoothness", 0.8f); material.SetFloat("_Metallic", 0.15f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                waterObject.GetComponent<MeshRenderer>().sharedMaterial = material;
                var manipulator = player.gameObject.AddComponent<WaterManipulator>();
                manipulator.water = water; manipulator.view = player.GetComponentInChildren<Camera>();
                // Runtime mesh is rebuilt in Awake. Do not serialize a transient mesh into the scene.
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Liquid sculpting ready: Aquarius > Open Liquid Sculpting Test. LMB bends; Backspace resets water.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
