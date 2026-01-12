using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionImpostor
{   
    /// <summary>
    /// Contains data about the map.  There is both a main game map as well as a pregame map to reference spawn points
    /// </summary>
    public class MapData : MonoBehaviour
    {
        // Where we would set maps, and then reference the colliders when changing them from the pre-game menu
        public Collider wallCollider;
        public GameObject hull;
        public Transform[] spawns;

        public void SetWallColliders(bool state)
        {
            wallCollider.enabled = state;
        }

        public Vector3 GetSpawnPosition(int index)
        {
            return spawns[index].position;
        }
    }
}