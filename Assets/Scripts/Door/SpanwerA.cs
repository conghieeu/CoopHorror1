using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;

public class SpanwerA : MonoBehaviour
{
    // spawn object ra ở server
    public NetworkObject objectToSpawn;
    // object spawn
    public NetworkObject spawnedObject;

    // hàm spawn networjk object
    [Command("/spawn-object-A")]
    public void SpawnObject()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject spawnedObject = Instantiate(objectToSpawn, new Vector3(0, 1, 0), Quaternion.identity);
            spawnedObject.Spawn();

            this.spawnedObject = spawnedObject;
        }
    }

    // hàm hủy object trên server
    [Command("/despawn-object-A")]
    public void DespawnObject()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            this.spawnedObject?.Despawn();
        }
    }
}
