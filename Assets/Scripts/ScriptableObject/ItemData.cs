using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "LethalCompany/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName = "Scrap Metal"; // Tên món đồ
    public int baseValue = 10; // Giá cơ bản khi bán
    public float weight = 1.05f; // 1.0 = nhẹ, càng cao càng nặng (làm chậm player)
    public bool isTwoHanded = false; // Có phải đồ 2 tay không (như cái Cầu chì to)
    
    [Header("Prefabs")]
    public GameObject spawnPrefab; // Prefab ném ra đất, dùng cho trường hợp mua, spawn ra từ List...
    public GameObject firstPersonPrefab; // Prefab hiển thị trên tay 1st person (local-only). Nếu null sẽ fallback spawnPrefab.
    public Vector3 positionOffset; // Vị trí ướm vào tay cho vừa
    public Vector3 rotationOffset; // Góc xoay ướm vào tay cho vừa
}