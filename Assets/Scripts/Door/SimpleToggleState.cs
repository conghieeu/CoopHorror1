using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class SimpleToggleState : NetworkBehaviour, IInteractable
{
    // Biến mạng đồng bộ bool
    private readonly NetworkVariable<bool> _isActive = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Triển khai interface để ToggleRotator đọc được
    public bool IsActive => _isActive.Value;

    public string GetInteractText()
    {
        return IsActive ? "Đóng cửa" : "Mở cửa";
    }

    [Command("/toggle-door")]
    public void Interact()
    {
        Debug.Log("<color=green>Thực hiện: </color>" + (IsActive ? "Đóng cửa" : "Mở cửa"));
        ToggleServerRpc();
    }

    // Hàm gọi để đổi trạng thái (Chỉ gọi từ Server)
    [ServerRpc(RequireOwnership = false)]
    public void ToggleServerRpc()
    {
        _isActive.Value = !_isActive.Value;
    }
}