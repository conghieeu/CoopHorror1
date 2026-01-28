using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class SimpleToggleState : NetworkBehaviour, IToggleState
{
    // Biến mạng đồng bộ bool
    private readonly NetworkVariable<bool> _isActive = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Triển khai interface để ToggleRotator đọc được
    public bool IsActive => _isActive.Value;

    // Hàm gọi để đổi trạng thái (Chỉ gọi từ Server)
    [Command("/toggle-door")]
    public void Toggle()
    {
        if (IsServer)
        {
            _isActive.Value = !_isActive.Value;
        }
    }
}