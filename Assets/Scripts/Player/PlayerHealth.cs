using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100;
    
    // Biến mạng: Server quản lý, Client chỉ đọc
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(100);

    public bool IsDead => CurrentHealth.Value <= 0;

    // Hàm nhận sát thương (Gọi từ Quái vật hoặc Môi trường)
    public void TakeDamage(int damage)
    {
        if (!IsServer) return; // Chỉ Server mới được trừ máu

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
        Debug.Log("YOU DIED!");
        
        // 1. Tắt điều khiển
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerInteraction>().enabled = false;

        // 2. Rơi hết đồ ra đất (Inventory Drop All)
        // GetComponent<PlayerInventory>().DropAllItems();

        // 3. Spawn xác chết (Ragdoll)
        // Instantiate(ragdollPrefab, transform.position, transform.rotation);

        // 4. Chuyển Camera sang chế độ Spectate (Theo dõi người khác)
    }
}