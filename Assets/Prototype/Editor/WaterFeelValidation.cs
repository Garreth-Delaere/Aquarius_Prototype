using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class WaterFeelValidation
    {
        [MenuItem("Aquarius/Validate Ice Hover")]
        public static void RunHover()
        {
            Scene previous=SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the scene first."); return; }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene); var priorMode=Physics.simulationMode; Physics.simulationMode=SimulationMode.Script;
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../WaterHoverValidation.txt"));
            try
            {
                var w=new GameObject("Hover water").AddComponent<WaterStream>(); w.Initialize(); w.wobbleAmplitude=0;
                Vector3 origin=new Vector3(2000,3,2000); w.BeginSource(origin); Step(w,180);
                for(int i=0;i<135;i++) { w.SetTarget(origin+Vector3.forward*12*(i+1)/135,0.7f); w.SimulateStep(1f/90); }
                Require(w.FreezeHeld(),"Hover test stream must freeze."); var ice=w.FrozenStructures[0]; var mesh=ice.Surface.Collision.sharedMesh;
                var original=ice.Surface.Collision.bounds; float low=original.center.y,high=low;
                w.BeginSource(origin+Vector3.forward*5); Step(w,180);
                for(int i=0;i<135;i++) { w.SetTarget(origin+Vector3.forward*(5+12*(i+1)/135f),0.7f); w.SimulateStep(1f/90); }
                Require(w.FreezeHeld(),"Second live-sculpted hover platform must freeze.");
                var second=w.FrozenStructures[1]; second.SimulateHover(0.8f); Physics.SyncTransforms(); Physics.Simulate(1f/60);
                var cc=new GameObject("Hover walker").AddComponent<CharacterController>(); cc.height=1.75f; cc.radius=0.3f; cc.center=Vector3.up*0.875f; cc.skinWidth=0.025f; cc.stepOffset=0.2f; cc.slopeLimit=55;
                Require(ice.Surface.Collision.Raycast(new Ray(new Vector3(original.center.x,original.max.y+1,original.min.z+0.5f),Vector3.down),out var top,3),"Hover needs a walkable top.");
                cc.transform.position=top.point+Vector3.up*0.03f; Physics.SyncTransforms(); float start=cc.transform.position.z;
                for(int i=0;i<360;i++)
                {
                    ice.SimulateHover(1f/60); second.SimulateHover(1f/60); Physics.SyncTransforms(); Physics.Simulate(1f/60);
                    float y=ice.Surface.Collision.bounds.center.y; low=Mathf.Min(low,y); high=Mathf.Max(high,y);
                    cc.Move(new Vector3(0,-2,i<330?1.5f:0)/60);
                    Require(cc.transform.position.y>original.min.y-0.2f,"Player must remain supported throughout the hover cycle.");
                }
                Require(high-low>0.05f && high-low<0.065f,"Ice should move a few centimetres, not drift: "+(high-low));
                Require(cc.transform.position.z>original.max.z+1,"Player must cross the live-sculpted bobbing join: position="+cc.transform.position+" A="+original+" B="+second.Surface.Collision.bounds);
                Require(ice.Surface.Collision.sharedMesh==mesh,"Hover must not rebuild the frozen mesh.");
                Require(ice.Surface.Collision.GetComponent<MeshFilter>().sharedMesh==mesh,"Render and collision must share the same moving geometry.");
                UnityEngine.Object.DestroyImmediate(cc.gameObject);
                ice.SimulateHover(0.6f); Physics.SyncTransforms(); Physics.Simulate(1f/60); Physics.SyncTransforms(); Vector3 offset=ice.Displacement;
                var saved=ice.Snapshot[0].position; Require(w.Melt(ice) && second.IsSolid,"Bobbing ice must melt independently.");
                Require(Vector3.Distance(w.Droplets[0].position,saved+offset)<0.001f,"Melt must restore water at the current bobbed position.");
                string pass="PASS: physical ice bobbing peak-to-peak="+(high-low).ToString("F3")+"m, bounded motion, unchanged shared mesh, real CharacterController crosses two live-sculpted platforms with different hover phases using 0.2m step height, support over multiple cycles, melt restores current displaced position and preserves the other ice.";
                File.WriteAllText(report,pass); Debug.Log(pass);
            }
            catch(Exception e) { File.WriteAllText(report,"FAIL: "+e); Debug.LogException(e); }
            finally { Physics.simulationMode=priorMode; if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if(SceneManager.sceneCount>1) EditorSceneManager.CloseScene(scene,true); }
        }
        [MenuItem("Aquarius/Validate Round Resting Water")]
        public static void RunCompact()
        {
            Scene previous=SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../WaterCompactValidation.txt"));
            try
            {
                var w=new GameObject("Round water").AddComponent<WaterStream>(); w.Initialize();
                Vector3 origin=new Vector3(3000,3,3000); w.BeginSource(origin); w.SetTarget(origin,0); Step(w,540);
                Bounds bounds=new Bounds(w.Droplets[0].position,Vector3.zero);
                foreach(var d in w.Droplets) bounds.Encapsulate(d.position);
                Vector3 size=bounds.size+Vector3.one*w.dropletRadius*2;
                float max=Mathf.Max(size.x,Mathf.Max(size.y,size.z)),min=Mathf.Min(size.x,Mathf.Min(size.y,size.z));
                Require(max<0.75f && min>0.35f && max/min<1.45f,"Resting water must be small and approximately spherical: "+size);
                Require(w.Droplets.Count==w.dropletsPerDraw && Mathf.Approximately(w.dropletRadius,0.075f),"Rounding must preserve droplet count and size.");
                for(int i=0;i<135;i++) { w.SetTarget(origin+Vector3.forward*12*(i+1)/135,0.7f); w.SimulateStep(1f/90); }
                float low=float.PositiveInfinity,high=float.NegativeInfinity;
                foreach(var d in w.Droplets) { low=Mathf.Min(low,d.position.z); high=Mathf.Max(high,d.position.z); }
                Require(high-low>3,"The rounded resting shape must still stretch into a stream.");
                Require(w.FreezeHeld(),"A stretched rounded mass must still freeze.");
                string pass="PASS: resting particle envelope="+size+"m, approximately spherical, unchanged particle size/count, fast gesture stretches beyond 3m, freezing preserved.";
                File.WriteAllText(report,pass); Debug.Log(pass);
            }
            catch(Exception e) { File.WriteAllText(report,"FAIL: "+e); Debug.LogException(e); }
            finally { if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if(SceneManager.sceneCount>1) EditorSceneManager.CloseScene(scene,true); }
        }
        [MenuItem("Aquarius/Validate Fast Water Tracking")]
        public static void RunTracking()
        {
            Scene previous=SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene); string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../WaterTrackingValidation.txt"));
            try
            {
                float baseline=TrackingError(1,new Vector3(4000,3,4000),out float oldSpan);
                float improved=TrackingError(3,new Vector3(4050,3,4000),out float newSpan);
                Require(improved<baseline*0.65f && improved<1.8f,"Fast guidance must keep the visible head closer to the target: "+baseline+" -> "+improved);
                Require(newSpan>3,"Closer head following must not remove fast gesture stretching.");
                float slowOld=SlowError(1,new Vector3(4100,3,4000)),slowNew=SlowError(3,new Vector3(4150,3,4000));
                Require(Mathf.Abs(slowOld-slowNew)<0.03f,"Slow guidance must preserve its original resistance.");
                string pass="PASS: fast visible-head error="+baseline.ToString("F2")+"m -> "+improved.ToString("F2")+"m, stretched span="+newSpan.ToString("F2")+"m, slow guidance resistance unchanged.";
                File.WriteAllText(report,pass); Debug.Log(pass);
            }
            catch(Exception e) { File.WriteAllText(report,"FAIL: "+e); Debug.LogException(e); }
            finally { if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if(SceneManager.sceneCount>1) EditorSceneManager.CloseScene(scene,true); }
        }
        static float TrackingError(float boost,Vector3 origin,out float span)
        {
            var w=new GameObject("Tracking water").AddComponent<WaterStream>(); w.Initialize(); w.fastFollowBoost=boost; w.wobbleAmplitude=0;
            w.BeginSource(origin); Step(w,180); float error=0; int samples=0;
            for(int i=0;i<180;i++)
            {
                Vector3 target=origin+Vector3.forward*8*(i+1)/90f; w.SetTarget(target,1); w.SimulateStep(1f/90);
                if(i>60) { Vector3 head=Vector3.zero; for(int j=0;j<5;j++) head+=w.Droplets[j].position; error+=Vector3.Distance(head/5,target); samples++; }
            }
            float low=float.PositiveInfinity,high=float.NegativeInfinity;
            foreach(var d in w.Droplets) { low=Mathf.Min(low,d.position.z); high=Mathf.Max(high,d.position.z); }
            span=high-low; return error/samples;
        }
        static float SlowError(float boost,Vector3 origin)
        {
            var w=new GameObject("Slow tracking water").AddComponent<WaterStream>(); w.Initialize(); w.fastFollowBoost=boost; w.wobbleAmplitude=0;
            w.BeginSource(origin); Step(w,180); Vector3 target=origin;
            for(int i=0;i<180;i++) { target=origin+Vector3.forward*(i+1)/90f; w.SetTarget(target,0); w.SimulateStep(1f/90); }
            return Vector3.Distance(w.Droplets[0].position,target);
        }
        public static void RunCollisionChecks() { RunHover(); FreezeValidation.Run(); }
        public static void RunAll()
        {
            RunHover(); RunCompact(); RunTracking(); StreamTailValidation.RunAll(); FreezeValidation.Run();
        }
        static void Step(WaterStream w,int n) { for(int i=0;i<n;i++) w.SimulateStep(1f/90); }
        static void Require(bool ok,string why) { if(!ok) throw new InvalidOperationException(why); }
    }
}
