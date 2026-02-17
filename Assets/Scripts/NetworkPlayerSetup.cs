using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

// Script này chỉ chuyên lo việc BẬT/TẮT hiển thị khi Spawn
public class NetworkPlayerSetup : NetworkBehaviour
{
    
    // Unity event 
    [SerializeField] private UnityEvent onLocalPlayerSpawned;
    [SerializeField] private UnityEvent onRemotePlayerSpawned;

    void Start()
    {
        if (IsOwner)
        {
            // Kích hoạt các thành phần chỉ dành cho Local Player
            onLocalPlayerSpawned?.Invoke();
        }
        else
        {
            // Kích hoạt các thành phần chỉ dành cho Remote Player
            onRemotePlayerSpawned?.Invoke();
        }
    }
}