using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

// Script này chỉ chuyên lo việc BẬT/TẮT hiển thị khi Spawn
public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("--- Local Player Components ---")]
    [Tooltip("Camera chính của người chơi")]
    [SerializeField] private Camera gameplayCamera;
    
    [Tooltip("Tai nghe của người chơi (Để không bị lỗi 2 AudioListener)")]
    [SerializeField] private AudioListener audioListener;
    
    [Tooltip("Cánh tay cầm súng/đèn (Chỉ hiện ở góc nhìn thứ 1)")]
    [SerializeField] private GameObject firstPersonArms;

    [Header("--- Body Visibility Settings ---")]
    [Tooltip("Toàn bộ Model nhân vật (Body, Head...)")]
    [SerializeField] private Renderer[] playerMeshes;

    [Header("--- Remote Cleanup ---")]
    [Tooltip("Những script cần TẮT nếu đây không phải là mình (VD: PlayerController, Interaction)")]
    [SerializeField] private Behaviour[] scriptsToDisableOnRemote;

    // Hàm này chạy TỰ ĐỘNG ngay khi nhân vật xuất hiện trên mạng
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
    }

    private void SetupLocalPlayer()
    {
        // 1. BẬT Camera và Tai nghe cho mình
        if(gameplayCamera) gameplayCamera.gameObject.SetActive(true);
        if(audioListener) audioListener.enabled = true;

        // 2. BẬT tay góc nhìn thứ 1
        if(firstPersonArms) firstPersonArms.SetActive(true);

        // 3. ẨN thân mình đi (chỉ để đổ bóng) để camera không nhìn xuyên qua bụng
        foreach (var mesh in playerMeshes)
        {
            mesh.shadowCastingMode = ShadowCastingMode.ShadowsOnly; 
        }
        
        // 4. Đổi Layer sang "LocalPlayer" (nếu cần thiết để Camera không render dính)
        // SetLayerRecursively(gameObject, LayerMask.NameToLayer("LocalPlayer"));
    }

    private void SetupRemotePlayer()
    {
        // 1. TẮT Camera và Tai nghe của thằng bạn
        if(gameplayCamera) gameplayCamera.gameObject.SetActive(false);
        if(audioListener) audioListener.enabled = false;

        // 2. TẮT tay góc nhìn thứ 1 (vì mình nhìn nó ở góc nhìn thứ 3)
        if(firstPersonArms) firstPersonArms.SetActive(false);

        // 3. HIỆN thân xác nó lên để mình nhìn thấy
        foreach (var mesh in playerMeshes)
        {
            mesh.shadowCastingMode = ShadowCastingMode.On; 
        }

        // 4. TẮT các script điều khiển của nó trên máy mình
        // Để tránh việc máy mình nhận input WASD lại làm con của nó di chuyển
        foreach (var script in scriptsToDisableOnRemote)
        {
            script.enabled = false;
        }
    }
}