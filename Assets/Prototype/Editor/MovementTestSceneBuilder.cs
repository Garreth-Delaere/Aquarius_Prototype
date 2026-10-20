using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    [InitializeOnLoad]
    public static class MovementTestSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/MovementTest.unity";
        static MovementTestSceneBuilder() { EditorApplication.delayCall += CreateIfMissing; }
        static void CreateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            Build();
        }
        [MenuItem("Aquarius/Open Movement Test")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Build();
            EditorSceneManager.OpenScene(ScenePath);
        }
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                Material grey = Material("Grey", new Color(0.45f, 0.48f, 0.5f));
                Material green = Material("Gentle", new Color(0.35f, 0.55f, 0.42f));
                Material red = Material("Steep", new Color(0.65f, 0.38f, 0.32f));
                Box("Floor", new Vector3(0, -0.25f, 4), new Vector3(22, 0.5f, 24), grey);
                Box("Back wall", new Vector3(0, 1.5f, -8), new Vector3(22, 3, 0.3f), grey);
                Box("Left wall", new Vector3(-11, 1.5f, 4), new Vector3(0.3f, 3, 24), grey);
                Box("Low step - 0.10 m", new Vector3(-2, 0.05f, 1), new Vector3(2, 0.1f, 2), green);
                Box("Jump block - 0.22 m", new Vector3(1, 0.11f, 1), new Vector3(2, 0.22f, 2), grey);
                Box("Unreachable ledge - 1 m", new Vector3(5, 0.5f, 2), new Vector3(3, 1, 3), red);
                Ramp("Gentle ramp - 20 degrees", -4, 20, green);
                Ramp("Steep ramp - 50 degrees", 3, 50, red);
                Label("LOW STEP", new Vector3(-2, 0.8f, 2));
                Label("SMALL JUMP", new Vector3(1, 0.8f, 2));
                Label("1 m LEDGE", new Vector3(5, 1.7f, 3));
                Label("20 DEG - WALK", new Vector3(-4, 2.6f, 10));
                Label("50 DEG - WALK", new Vector3(3, 5.2f, 10));
                var light = new GameObject("Directional Light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.4f; light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
                var player = new GameObject("Player");
                player.transform.position = new Vector3(0, 0.05f, -4);
                var controller = player.AddComponent<CharacterController>();
                controller.height = 1.75f; controller.radius = 0.3f; controller.center = new Vector3(0, 0.875f, 0);
                controller.skinWidth = 0.025f; controller.minMoveDistance = 0; controller.stepOffset = 0.2f; controller.slopeLimit = 55;
                var camera = new GameObject("First Person Camera").AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.transform.SetParent(player.transform, false);
                camera.transform.localPosition = new Vector3(0, 1.6f, 0);
                camera.nearClipPlane = 0.05f; camera.fieldOfView = 75;
                camera.gameObject.AddComponent<AudioListener>();
                player.AddComponent<FirstPersonLook>().cameraPivot = camera.transform;
                player.AddComponent<FirstPersonMotor>().inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("Movement test ready. Use Aquarius > Open Movement Test, then Play.");
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        static Material Material(string name, Color color)
        {
            const string folder = "Assets/Prototype/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prototype", "Materials");
            string path = folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color; material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        static void Ramp(string name, float x, float angle, Material material)
        {
            const float length = 6f, thickness = 0.25f;
            float radians = angle * Mathf.Deg2Rad;
            var ramp = Box(name, new Vector3(x, length * Mathf.Sin(radians) / 2f - thickness * Mathf.Cos(radians) / 2f, 8), new Vector3(2.8f, thickness, length), material);
            ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);
            float top = length * Mathf.Sin(radians);
            Box(name + " landing", new Vector3(x, top / 2f, 8 + length * Mathf.Cos(radians) / 2f + 1.4f), new Vector3(2.8f, top, 3), material);
        }
        static void Label(string text, Vector3 position)
        {
            var label = new GameObject(text).AddComponent<TextMesh>();
            label.text = text; label.transform.position = position;
            label.anchor = TextAnchor.MiddleCenter; label.characterSize = 0.12f; label.fontSize = 48; label.color = Color.white;
        }
    }
}



