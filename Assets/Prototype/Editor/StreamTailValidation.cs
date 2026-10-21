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
            Scene previous=SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) { Debug.LogWarning("Save the scene first."); return; }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../StreamTailValidation.txt"));
            try
            {
                Vector3 origin=new Vector3(1000,3,1000);
                var slow=Create(origin); DrawLine(slow,origin,12,1080);
                var fast=Create(origin+Vector3.right*25); DrawLine(fast,origin+Vector3.right*25,12,135);
                float slowSpan=Span(slow),fastSpan=Span(fast);
                Require(slowSpan<1.8f && fastSpan>3 && fastSpan>slowSpan*2,"Identical 12m paths must depend on actual motion speed: slow="+slowSpan+" fast="+fastSpan);
                foreach(var d in slow.Droplets) Require(d.position.z>origin.z+9,"A compact slowly carried shape must leave its source.");
                Require(fast.Droplets.Count==fast.dropletsPerDraw && Mathf.Approximately(fast.dropletRadius,0.075f),"Extraction count and particle size must stay unchanged.");
                var snapshot=new Vector3[fast.Droplets.Count]; for(int i=0;i<snapshot.Length;i++) snapshot[i]=fast.Droplets[i].position;
                Require(fast.FreezeHeld(),"Fast-drawn stream must freeze: "+fast.FreezeMessage);
                var ice=fast.FrozenStructures[0]; var vertices=ice.Surface.Collision.sharedMesh.vertices;
                for(int i=0;i<snapshot.Length;i++) Require(ice.Snapshot[i].position==snapshot[i],"Freeze must preserve actual moving water positions.");
                var bounds=ice.Surface.Collision.bounds;
                var cc=new GameObject("Gesture platform walker").AddComponent<CharacterController>(); cc.height=1.75f; cc.radius=0.3f;
                cc.center=Vector3.up*0.875f; cc.stepOffset=0.2f; cc.slopeLimit=55; cc.skinWidth=0.025f;
                Require(ice.Surface.Collision.Raycast(new Ray(new Vector3(bounds.center.x,bounds.max.y+1,bounds.min.z+0.5f),Vector3.down),out var top,3),"Gesture platform needs a solid top.");
                cc.transform.position=top.point+Vector3.up*0.03f; Physics.SyncTransforms(); float start=cc.transform.position.z;
                for(int i=0;i<75;i++) cc.Move(new Vector3(0,-4,2)/60f);
                Require(cc.transform.position.z>start+2 && cc.transform.position.y>bounds.min.y-0.3f,"A platform frozen during motion must support walking.");
                UnityEngine.Object.DestroyImmediate(cc.gameObject); Step(fast,360);
                var fixedVertices=ice.Surface.Collision.sharedMesh.vertices;
                for(int i=0;i<vertices.Length;i++) Require(vertices[i]==fixedVertices[i],"Frozen platforms must never keep gathering.");
                var gather=Create(origin+Vector3.right*50); DrawLine(gather,origin+Vector3.right*50,12,135);
                float before=Span(gather); var first=gather.Droplets[0].position; Step(gather,1);
                Require(Vector3.Distance(first,gather.Droplets[0].position)<0.15f,"Stopping must not snap particles.");
                Step(gather,179); float afterTwo=Span(gather); Require(afterTwo<before*0.75f,"Stationary held water must gather meaningfully within two seconds: "+before+" -> "+afterTwo);
                Step(gather,270); Require(Span(gather)<1.8f,"Gathering must settle into a short resting trail.");
                var curve=Create(origin+Vector3.right*75); Vector3 curveOrigin=origin+Vector3.right*75;
                for(int i=0;i<180;i++)
                {
                    float angle=(i+1)/180f*Mathf.PI*0.75f;
                    curve.SetTarget(curveOrigin+new Vector3(8*(1-Mathf.Cos(angle)),0,8*Mathf.Sin(angle)),0.7f); curve.SimulateStep(1f/90);
                }
                Vector3 a=curve.Droplets[0].position,b=curve.Droplets[curve.Droplets.Count-1].position;
                Vector3 direction=(b-a).normalized; float bend=0;
                foreach(var d in curve.Droplets) bend=Mathf.Max(bend,Vector3.Cross(d.position-a,direction).magnitude);
                Require(bend>0.4f,"Fast curved gestures must leave a curved wake: bend="+bend);
                Require(curve.FreezeHeld(),"Curved moving water must still freeze.");
                fast.BeginSource(origin+new Vector3(25,0,20)); Step(fast,180);
                Require(fast.FreezeHeld() && ice.IsSolid && fast.FrozenCount==2,"A fresh structure must preserve earlier ice.");
                Require(fast.Melt(ice) && fast.FrozenCount==1 && fast.Droplets.Count==snapshot.Length,"Targeted melting must restore original drops and preserve other ice.");
                Step(fast,90); Require(fast.Droplets[0].position.y<snapshot[0].y-1,"Melted water must fall normally.");
                Require(fast.BeginPickup(0),"Dropped water must remain re-grabbable.");
                string success="PASS: same path at slow/fast speeds ("+slowSpan.ToString("F2")+"m / "+fastSpan.ToString("F2")+"m physical span), unchanged thin particles/extraction, moving tail, smooth gathering ("+before.ToString("F2")+"m -> "+afterTwo.ToString("F2")+"m after 2s), curved fast gesture (bend="+bend.ToString("F2")+"m), exact freeze snapshots, real CharacterController traversal, frozen geometry stays still, independent structures, targeted melt, gravity and re-grab.";
                File.WriteAllText(report,success); Debug.Log(success);
            }
            catch(Exception e) { File.WriteAllText(report,"FAIL: "+e); Debug.LogException(e); }
            finally { if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if(SceneManager.sceneCount>1) EditorSceneManager.CloseScene(scene,true); }
        }
        static WaterStream Create(Vector3 origin)
        {
            var w=new GameObject("Gesture water").AddComponent<WaterStream>(); w.Initialize(); w.wobbleAmplitude=0; w.BeginSource(origin); Step(w,180); return w;
        }
        static void DrawLine(WaterStream w,Vector3 origin,float distance,int steps)
        {
            // Same swipe-strength input: geometry must respond to measured world motion speed.
            for(int i=0;i<steps;i++) { w.SetTarget(origin+Vector3.forward*distance*(i+1)/steps,0.7f); w.SimulateStep(1f/90); }
        }
        static float Span(WaterStream w)
        {
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            foreach(var d in w.Droplets) { min=Mathf.Min(min,d.position.z); max=Mathf.Max(max,d.position.z); } return max-min;
        }
        public static void RunAll() { Run(); WaterTactileValidation.RunDepth(); WaterTactileValidation.RunMotion(); StreamValidation.Run(); PillarCourseValidation.Run(); MovementValidation.Run(); }
        static void Step(WaterStream w,int n) { for(int i=0;i<n;i++) w.SimulateStep(1f/90); }
        static void Require(bool value,string why) { if(!value) throw new InvalidOperationException(why); }
    }
}
