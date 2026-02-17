using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ReNamplayerOnSpawn : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (IsOwner)
        {
            gameObject.name = "LocalPlayer " + OwnerClientId;
        }
        else
        {
            gameObject.name = "RemotePlayer " + OwnerClientId;
        }   
    }
}
