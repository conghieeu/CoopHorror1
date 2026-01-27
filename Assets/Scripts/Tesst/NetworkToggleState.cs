using Fusion;
using QFSW.QC;
using UnityEngine;

// Script này chỉ lo Logic: Mở hay Đóng
public class NetworkToggleState : NetworkBehaviour, IToggleState
{
    [Networked] public bool IsOn { get; set; }

    // Thực thi Interface để các module khác đọc được
    public bool IsActive => IsOn;

    public override void Spawned()
    {
        // Có thể thêm logic khởi tạo nếu cần
    }

    // Hàm gọi từ UI hoặc Raycast
    [ContextMenu("Toggle Door")]
    [Command("ToggleDoor", "Toggles the state of the object")]
    public void Toggle()
    {
        RPC_SetState(!IsOn);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetState(bool state)
    {
        IsOn = state;
    }
}