using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class MovementValidation
    {
        [MenuItem("Aquarius/Validate Player Movement")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the current scene first."); return; }
            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            string report = Path.GetFullPath(Path.Combine(Application.dataPath, "../MovementValidation.txt"));
            try
            {
                Vector3 origin = new Vector3(1000, 0, 1000);
                Box(origin + new Vector3(0, -0.25f, 5), new Vector3(30, 0.5f, 30));
                Physics.SyncTransforms();
                var motor = Player(origin + Vector3.up * 0.03f);
                for (int i = 0; i < 15; i++) motor.SimulateMovement(Vector2.zero, false, 1f / 60f);
                float floor = motor.transform.position.y;
                motor.SimulateMovement(Vector2.zero, true, 1f / 60f);
                for (int i = 0; i < 8; i++) motor.SimulateMovement(Vector2.zero, false, 1f / 60f);
                float z = motor.transform.position.z;
                float apex = motor.transform.position.y;
                // Begin steering only after takeoff; a second jump request must be ignored.
                for (int i = 0; i < 12; i++)
                {
                    motor.SimulateMovement(Vector2.up, i == 3, 1f / 60f);
                    apex = Mathf.Max(apex, motor.transform.position.y);
                }
                Require(motor.transform.position.z - z > 0.3f, "A stationary jump must gain meaningful movement when a direction is pressed in midair.");
                float previousZ = motor.transform.position.z;
                for (int i = 0; i < 11; i++)
                {
                    motor.SimulateMovement(Vector2.down, false, 1f / 60f);
                    apex = Mathf.Max(apex, motor.transform.position.y);
                }
                previousZ = motor.transform.position.z;
                motor.SimulateMovement(Vector2.down, false, 1f / 60f);
                Require(motor.transform.position.z < previousZ && motor.transform.position.y > floor + 0.1f, "Midair steering should allow reversing direction before landing.");
                Require(apex - floor > 0.85f && apex - floor < 1.15f, "Jump height should remain near 1 m with no double jump.");
                UnityEngine.Object.DestroyImmediate(motor.gameObject);
                Ramp(origin + new Vector3(4, 0, 6), 50);
                Physics.SyncTransforms();
                motor = Player(origin + new Vector3(4, 0.03f, 3));
                for (int i = 0; i < 480; i++)
                {
                    motor.SimulateMovement(Vector2.up, false, 1f / 60f);
                    if (motor.transform.position.z > origin.z + 8.5f) break;
                }
                Require(motor.transform.position.y > 4.3f && motor.transform.position.z > origin.z + 8,
                    "Player should climb the 50-degree ramp without jumping: " + (motor.transform.position - origin));
                UnityEngine.Object.DestroyImmediate(motor.gameObject);
                Ramp(origin + new Vector3(-5, 0, 6), 65); Physics.SyncTransforms();
                motor = Player(origin + new Vector3(-5, Mathf.Sin(65 * Mathf.Deg2Rad) * 3 + 0.06f, 6));
                for (int i = 0; i < 6; i++) motor.SimulateMovement(Vector2.zero, false, 1f / 60f);
                float before = motor.transform.position.y;
                motor.SimulateMovement(Vector2.left, true, 1f / 60f);
                for (int i = 0; i < 6; i++) motor.SimulateMovement(Vector2.left, false, 1f / 60f);
                Require(motor.transform.position.y > before + 0.2f, "A supported steep surface must allow jumping away.");
                string success = "PASS: stationary jump with delayed midair movement, air direction reversal, 1 m jump without double jumping, 50-degree slope ascent, jump escape from 65-degree support.";
                File.WriteAllText(report, success); Debug.Log(success);
            }
            catch (Exception e) { File.WriteAllText(report, "FAIL: " + e); Debug.LogException(e); }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(temporary, true);
            }
        }
        static FirstPersonMotor Player(Vector3 position)
        {
            var obj = new GameObject("Movement validation player"); obj.transform.position = position;
            var capsule = obj.AddComponent<CharacterController>(); capsule.height = 1.75f; capsule.radius = 0.3f;
            capsule.center = Vector3.up * 0.875f; capsule.skinWidth = 0.025f; capsule.minMoveDistance = 0;
            Physics.SyncTransforms(); return obj.AddComponent<FirstPersonMotor>();
        }
        static void Ramp(Vector3 centre, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            var ramp = Box(centre + Vector3.up * (3 * Mathf.Sin(radians) - 0.125f * Mathf.Cos(radians)), new Vector3(3, 0.25f, 6));
            ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);
            float top = 6 * Mathf.Sin(radians);
            Box(centre + new Vector3(0, top / 2, 3 * Mathf.Cos(radians) + 1.4f), new Vector3(3, top, 3));
        }
        static GameObject Box(Vector3 position, Vector3 size) { var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.transform.position = position; obj.transform.localScale = size; return obj; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}



