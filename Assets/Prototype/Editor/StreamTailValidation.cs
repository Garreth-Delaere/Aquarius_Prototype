using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class StreamTailValidation
    {
        [MenuItem("Aquarius/Validate Following Stream Tail")]
        public static void Run()
        {
            Scene previous = SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the scene first."); return; }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../StreamTailValidation.txt"));
            try
            {
                var w=new GameObject("Following stream validation").AddComponent<WaterStream>(); w.Initialize(); w.wobbleAmplitude=0;
                Vector3 origin=new Vector3(1000,3,1000); w.BeginSource(origin);
                for(int i=0;i<240;i++)
                {
                    float angle=i/239f*Mathf.PI*0.5f;
                    w.SetTarget(origin+new Vector3(4*(1-Mathf.Cos(angle)),0,4*Mathf.Sin(angle)),0.8f); w.SimulateStep(1f/90);
                }
                Step(w,90);
                Vector3 first=w.Droplets[0].position,last=w.Droplets[w.Droplets.Count-1].position;
                Vector3 line=(last-first).normalized; float bend=0;
                foreach(var d in w.Droplets) bend=Mathf.Max(bend,Vector3.Cross(d.position-first,line).magnitude);
                Require(bend>0.35f,"A curved gesture must retain a curved trail, rather than a straight tether: bend="+bend);
                Require(w.Droplets.Count==w.dropletsPerDraw && Mathf.Approximately(w.dropletRadius,0.075f),"Original particle count and thin particle size must remain intact.");
                w.ResetWater(); w.BeginSource(origin);
                for(int i=0;i<900;i++) { w.SetTarget(origin+Vector3.forward*i*0.05f,0.5f); w.SimulateStep(1f/90); }
                Step(w,90);
                foreach(var d in w.Droplets) Require(d.position.z>origin.z+27,"The entire stream, including its tail, must leave the source behind.");
                Require(Vector3.Distance(w.SmoothedTarget,origin+Vector3.forward*44.95f)<0.02f,"Head must keep following beyond the old source reach.");
                Require(Vector3.Distance(w.Droplets[0].position,w.Droplets[w.Droplets.Count-1].position)<w.maximumStreamLength+0.5f,"Following stream must remain bounded.");
                var saved=new Vector3[w.Droplets.Count]; for(int i=0;i<saved.Length;i++) saved[i]=w.Droplets[i].position;
                Require(w.FreezeHeld(),"Following stream must freeze: "+w.FreezeMessage);
                var a=w.FrozenStructures[0]; for(int i=0;i<saved.Length;i++) Require(a.Snapshot[i].position==saved[i],"Freeze must snapshot actual curved stream positions.");
                var bounds=a.Surface.Collision.bounds; var cc=new GameObject("Stream walker").AddComponent<CharacterController>();
                cc.height=1.75f; cc.radius=0.3f; cc.center=Vector3.up*0.875f; cc.stepOffset=0.2f; cc.slopeLimit=55; cc.skinWidth=0.025f;
                Require(a.Surface.Collision.Raycast(new Ray(new Vector3(bounds.center.x,bounds.max.y+1,bounds.min.z+0.5f),Vector3.down),out var top,3),"Frozen stream needs a walking surface.");
                cc.transform.position=top.point+Vector3.up*0.03f; Physics.SyncTransforms(); float start=cc.transform.position.z;
                for(int i=0;i<90;i++) cc.Move(new Vector3(0,-4,2)/60f);
                Require(cc.transform.position.z>start+2 && cc.transform.position.y>bounds.min.y-0.3f,"Original ice surface must still support walking.");
                UnityEngine.Object.DestroyImmediate(cc.gameObject);
                w.BeginSource(origin+Vector3.right*8); w.SetTarget(origin+new Vector3(8,0,4),0.5f); Step(w,270);
                Require(w.FreezeHeld() && a.IsSolid && w.FrozenCount==2,"Additional independent structures must preserve previous ice.");
                Require(w.Melt(a) && w.FrozenCount==1 && w.Droplets.Count==saved.Length,"Targeted melt must restore the stream without affecting other ice.");
                w.Release(); Step(w,90); Require(w.Droplets[0].position.y<saved[0].y-1,"Released/melted water must fall.");
                Require(w.BeginPickup(0),"Released water must remain re-grabbable.");
                string success="PASS: original thin particles, curved gesture trail (bend="+bend.ToString("F2")+"m), entire stream transported over 40m, freely following head, bounded rolling tail, exact physical freeze snapshots, actual CharacterController ice traversal, independent structures, targeted melt, gravity and re-grab.";
                File.WriteAllText(report,success); Debug.Log(success);
            }
            catch(Exception e) { File.WriteAllText(report,"FAIL: "+e); Debug.LogException(e); }
            finally { if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if(SceneManager.sceneCount>1) EditorSceneManager.CloseScene(scene,true); }
        }
        public static void RunAll() { Run(); WaterTactileValidation.RunDepth(); WaterTactileValidation.RunMotion(); StreamValidation.Run(); PillarCourseValidation.Run(); MovementValidation.Run(); }
        static void Step(WaterStream w,int n) { for(int i=0;i<n;i++) w.SimulateStep(1f/90); }
        static void Require(bool value,string why) { if(!value) throw new InvalidOperationException(why); }
    }
}
