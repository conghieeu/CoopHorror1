using UnityEngine;

/// <summary>
/// ScriptableObject that defines item properties.
/// This is used by PlayerInventory to access item data like display name,
/// third-person prefab, and hand offsets.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "Item";
    public string itemDescription = "";
    public Sprite itemIcon;
    
    [Header("Prefabs")]
    public GameObject spawnPrefab;
    public GameObject thirdPersonPrefab;
    
    [Header("Third Person Pose Offsets")]
    public Vector3 thirdPersonPositionOffset;
    public Vector3 thirdPersonRotationOffset;
    
    [Header("First Person Pose Offsets")]
    public Vector3 firstPersonPositionOffset;
    public Vector3 firstPersonRotationOffset;
}
