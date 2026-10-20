using UnityEngine;

namespace Aquarius.Prototype
{
    // Small greybox-specific objective/reset helper. Constructions survive failed attempts.
    public sealed class PillarCourse : MonoBehaviour
    {
        public FirstPersonMotor player;
        public WaterStream water;
        public Transform destination;
        public Vector3 destinationSize = new Vector3(3.8f, 2.4f, 3.8f);
        public float riverResetHeight = 0.45f;
        public bool ReachedDestination { get; private set; }
        public int Falls { get; private set; }
        void Update() { CheckProgress(); }
        public void CheckProgress()
        {
            if (player == null) return;
            if (player.transform.position.y < riverResetHeight)
            {
                if (water != null) water.Release();
                player.ResetPlayer(); Falls++;
                return;
            }
            if (destination != null && new Bounds(destination.position + Vector3.up * 0.9f, destinationSize).Contains(player.transform.position))
                ReachedDestination = true;
        }
        void OnGUI()
        {
            GUI.Box(new Rect(12, 260, 660, 65), ReachedDestination ?
                "DESTINATION REACHED\nYour ice route is still here. Try another route, or melt and rebuild it." :
                "PILLAR COURSE | Reach the gold beacon\nBlue basins supply water. Build ice around the rock and across the canyon.\nRiver contact resets you to the start; your ice constructions remain.");
            if (Falls > 0) GUI.Label(new Rect(12, 330, 660, 24), "River resets: " + Falls);
        }
    }
}
