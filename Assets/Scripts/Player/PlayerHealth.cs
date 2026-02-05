using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(100);
    public bool IsDead => CurrentHealth.Value <= 0;

    // --- LỆNH CONSOLE DÀNH CHO ADMIN ---
    
    // Cú pháp gõ console: damage_id [Client_ID] [Sát_thương]
    // Ví dụ: damage_id 0 20 (Trừ 20 máu của Host/Server)
    // Ví dụ: damage_id 1 50 (Trừ 50 máu của người chơi thứ 2)
    [Command("damage_id")]
    public static void CheatDamagePlayer(ulong clientId, int damage)
    {
        // 1. Kiểm tra xem có phải Server không (Vì biến Máu chỉ Server được sửa)
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Lỗi: Bạn phải là Host/Server mới dùng được lệnh này!");
            return;
        }

        // 2. Tìm kiếm Player trong danh sách kết nối của Netcode
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            // Lấy PlayerObject (GameObject chính của người chơi đó)
            if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerHealth healthScript))
            {
                // Gọi hàm trừ máu gốc
                healthScript.TakeDamage(damage);
                Debug.Log($"<color=green>Quantum Console:</color> Đã trừ {damage} máu của Player {clientId}");
            }
            else
            {
                Debug.LogError($"Player {clientId} không có script PlayerHealth!");
            }
        }
        else
        {
            Debug.LogError($"Không tìm thấy Player nào có ID: {clientId}");
        }
    }

    // --- LOGIC GAME GỐC (GIỮ NGUYÊN) ---

    // Bạn có thể bỏ [Command] ở đây nếu không muốn nó hiện trong list gợi ý nữa
    // hoặc giữ lại để debug cục bộ nếu click vào object.
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (IsDead) return;

        CurrentHealth.Value -= damage;
        Debug.Log($"Player {OwnerClientId} took {damage} dmg. HP: {CurrentHealth.Value}");

        if (CurrentHealth.Value <= 0)
        {
            CurrentHealth.Value = 0;
            DieClientRpc();
        }
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        Debug.Log($"Player {OwnerClientId} is Dead!");
        // Logic chết...
    }
}