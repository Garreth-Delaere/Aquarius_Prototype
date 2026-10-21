using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype.Editor
{
    public static class WaterReadabilityValidation
    {
        [MenuItem("Aquarius/Validate Ice Anchoring")]
        public static void Run()
        {
            Scene previous=SceneManager.GetActiveScene();
            if(!Application.isBatchMode && string.IsNullOrEmpty(previous.path)) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene); string report=Path.GetFullPath(Path.Combine(Application.dataPath,"../WaterReadabilityValidation.txt"));
            try
            {
                var iceWater=Create(new Vector3(2100,3,2000)); Stretch(iceWater,new Vector3(2100,3,2000)); Require(iceWater.FreezeHeld(),"Anchoring fixture must freeze.");
                var ice=iceWater.FrozenStructures[0]; ice.CheckAnchoring(); Require(!ice.IsAnchored,"Suspended ice must remain free to hover.");
                Bounds bounds=ice.Surface.Collision.bounds;
                var floor=Box(bounds.center+Vector3.up*(bounds.min.y-bounds.center.y-0.25f),new Vector3(bounds.size.x+1,0.5f,bounds.size.z+1));
                Physics.SyncTransforms(); ice.CheckAnchoring(); Require(ice.IsAnchored,"Floor contact must anchor ice. bounds="+bounds+" floor="+floor.GetComponent<Collider>().bounds);
                Vector3 fixedPosition=ice.transform.position; ice.SimulateHover(0.7f); Require(ice.transform.position==fixedPosition,"Anchored ice must stay completely still.");
                UnityEngine.Object.DestroyImmediate(floor); Physics.SyncTransforms(); ice.CheckAnchoring(); Require(!ice.IsAnchored,"Removing support must free the ice.");
                ice.SimulateHover(0.1f); Require(Vector3.Distance(ice.transform.position,fixedPosition)<0.02f,"Resuming hover must not jump."); Physics.SyncTransforms();
                bounds=ice.Surface.Collision.bounds;
                var wall=Box(new Vector3(bounds.max.x+0.25f,bounds.center.y,bounds.center.z),new Vector3(0.5f,bounds.size.y+2,bounds.size.z+1));
                Physics.SyncTransforms(); ice.CheckAnchoring(); Require(ice.IsAnchored,"Side wall contact must anchor ice too.");
                UnityEngine.Object.DestroyImmediate(wall); Physics.SyncTransforms(); ice.CheckAnchoring();
                var player=new GameObject("Contact should ignore player").AddComponent<CharacterController>(); player.transform.position=bounds.center;
                Physics.SyncTransforms(); ice.CheckAnchoring(); Require(!ice.IsAnchored,"Player contact must not anchor floating ice."); UnityEngine.Object.DestroyImmediate(player.gameObject);
                iceWater.BeginSource(new Vector3(2100,3,2005)); Step(iceWater,180); Stretch(iceWater,new Vector3(2100,3,2005)); Require(iceWater.FreezeHeld(),"Neighbour ice must freeze.");
                var neighbour=iceWater.FrozenStructures[1]; Bounds nb=neighbour.Surface.Collision.bounds;
                var endWall=Box(new Vector3(nb.center.x,nb.center.y,nb.max.z+0.25f),new Vector3(nb.size.x+1,nb.size.y+2,0.5f));
                Physics.SyncTransforms(); neighbour.CheckAnchoring(); ice.CheckAnchoring();
                Require(neighbour.IsAnchored && ice.IsAnchored,"A connection through other grounded ice must anchor the chain.");
                UnityEngine.Object.DestroyImmediate(endWall); Physics.SyncTransforms(); ice.CheckAnchoring(); Require(!ice.IsAnchored,"An unsupported ice cycle must not anchor itself.");
                var curve=Create(new Vector3(2200,3,2000));
                for(int i=0;i<135;i++) { float a=(i+1)/135f*Mathf.PI*0.5f; curve.SetTarget(new Vector3(2200+8*(1-Mathf.Cos(a)),3,2000+8*Mathf.Sin(a)),1); curve.SimulateStep(1f/90); }
                Require(curve.FreezeHeld(),"Curved bounds fixture must freeze."); var curved=curve.FrozenStructures[0]; Bounds cb=curved.Surface.Collision.bounds;
                Vector3 empty=new Vector3(cb.min.x+0.1f,cb.center.y,cb.max.z-0.1f);
                Require(!curved.Surface.Collision.Raycast(new Ray(empty+Vector3.up*2,Vector3.down),out _,4),"Fixture corner must be empty space, not water geometry.");
                Box(empty,Vector3.one*0.08f); Physics.SyncTransforms(); curved.CheckAnchoring(); Require(!curved.IsAnchored,"Nearby geometry inside an empty part of the bounds must not anchor the ice.");
                string pass="PASS: floor/wall anchoring, player exclusion, support removal without snap, grounded ice chains, no self-anchoring cycles, curved-bounds false-positive rejection.";
                File.WriteAllText(report,pass); Debug.Log(pass);
            }
            catch(Exception e) { File.WriteAllText(report,"FAIL: "+e); Debug.LogException(e); }
            finally { if(previous.IsValid()&&previous.isLoaded) SceneManager.SetActiveScene(previous); if(SceneManager.sceneCount>1) EditorSceneManager.CloseScene(scene,true); }
        }
        static WaterStream Create(Vector3 origin) { var w=new GameObject("Readability test water").AddComponent<WaterStream>(); w.Initialize(); w.wobbleAmplitude=0; w.BeginSource(origin); Step(w,180); return w; }
        static void Stretch(WaterStream w,Vector3 origin) { for(int i=0;i<135;i++) { w.SetTarget(origin+Vector3.forward*12*(i+1)/135,0.7f); w.SimulateStep(1f/90); } }
        static GameObject Box(Vector3 p,Vector3 size) { var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.position=p; go.transform.localScale=size; return go; }
        static void Step(WaterStream w,int n) { for(int i=0;i<n;i++) w.SimulateStep(1f/90); }
        static void Require(bool ok,string why) { if(!ok) throw new InvalidOperationException(why); }
        public static void RunAll() { Run(); WaterFeelValidation.RunAll(); }
    }
}
