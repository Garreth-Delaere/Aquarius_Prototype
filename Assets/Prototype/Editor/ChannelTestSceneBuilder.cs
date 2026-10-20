using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    [InitializeOnLoad]
    public static class ChannelTestSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/ChannelWaterTest.unity";
        static ChannelTestSceneBuilder() { EditorApplication.delayCall += CreateIfMissing; }
        static void CreateIfMissing()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                Build();
        }
        [MenuItem("Aquarius/Open Channel Water Test")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(); EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            MovementTestSceneBuilder.Build();
            if (!AssetDatabase.CopyAsset("Assets/Scenes/MovementTest.unity", ScenePath)) throw new InvalidOperationException("Could not create channel test scene.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                FirstPersonMotor player = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    var candidate = root.GetComponentInChildren<FirstPersonMotor>(); if (candidate != null) player = candidate;
                    if (root.name == "Floor") UnityEngine.Object.DestroyImmediate(root);
                }
                if (player == null) throw new InvalidOperationException("No player in movement test scene.");
                var grey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Grey.mat");
                var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/ChannelWater.mat");
                if (waterMaterial == null)
                {
                    waterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    waterMaterial.color = new Color(0.035f, 0.48f, 0.8f);
                    waterMaterial.SetFloat("_Smoothness", 0.45f);
                    AssetDatabase.CreateAsset(waterMaterial, "Assets/Prototype/Materials/ChannelWater.mat");
                }
                // Replace the floor with slabs around a genuine shallow trench along the left side.
                Box("Main floor", new Vector3(2.25f, -0.25f, 4), new Vector3(17.5f, 0.5f, 24), grey);
                Box("Outer channel bank", new Vector3(-9.75f, -0.25f, 4), new Vector3(2.5f, 0.5f, 24), grey);
                Box("Channel bed", new Vector3(-7.5f, -0.68f, 4), new Vector3(2f, 0.2f, 24), grey);
                Box("Channel end south", new Vector3(-7.5f, -0.25f, -7.5f), new Vector3(2f, 0.5f, 1), grey);
                Box("Channel end north", new Vector3(-7.5f, -0.25f, 15.5f), new Vector3(2f, 0.5f, 1), grey);
                var sourceObject = new GameObject("Infinite water source");
                sourceObject.transform.position = new Vector3(-7.5f, -0.18f, 4);
                var source = sourceObject.AddComponent<WaterSource>();
                GameObject surface = Box("Channel water surface", new Vector3(-7.5f, -0.2f, 4), new Vector3(1.8f, 0.04f, 22), waterMaterial);
                UnityEngine.Object.DestroyImmediate(surface.GetComponent<Collider>());
                surface.transform.SetParent(sourceObject.transform, true);
                var waterObject = new GameObject("Drawn water - persistent droplets");
                var stream = waterObject.AddComponent<WaterStream>(); stream.ignoredPlayer = player.transform;
                waterObject.GetComponent<WaterDropletRenderer>().material = waterMaterial;
                var manipulator = player.gameObject.AddComponent<StreamManipulator>();
                manipulator.water = stream; manipulator.source = source; manipulator.view = player.GetComponentInChildren<Camera>();
                player.transform.SetPositionAndRotation(new Vector3(-4.5f, 0.05f, -4), Quaternion.Euler(0, -45, 0));
                var sign = new GameObject("Channel instructions").AddComponent<TextMesh>();
                sign.text = "DRAW FROM CHANNEL\nHOLD LMB / RELEASE TO DROP"; sign.anchor = TextAnchor.MiddleCenter;
                sign.characterSize = 0.09f; sign.fontSize = 48;
                sign.transform.position = new Vector3(-7.5f, 1f, -1.3f);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Channel water test ready: Aquarius > Open Channel Water Test. Released droplets stay where they land.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
    }
}
