using Unity.Netcode;
using UnityEngine;

public class ChestInventory : NetworkBehaviour
{
    [Header("Settings")]
    public int maxSlots = 8;

    [Header("Optional")]
    [Tooltip("Where stored items will be teleported to (kept out of world interaction).")]
    [SerializeField] private Transform storageAnchor;

    private GrabbableObject[] _slots;

    private void Awake()
    {
        EnsureSlotsInitialized();
    }

    public override void OnNetworkSpawn()
    {
        EnsureSlotsInitialized();
    }

    public int SlotCount
    {
        get
        {
            EnsureSlotsInitialized();
            return _slots.Length;
        }
    }

    public bool IsValidSlotIndex(int slotIndex)
    {
        EnsureSlotsInitialized();
        return slotIndex >= 0 && slotIndex < _slots.Length;
    }

    // Client/UI read
    public GrabbableObject GetItem(int slotIndex)
    {
        EnsureSlotsInitialized();
        if (!IsValidSlotIndex(slotIndex)) return null;
        return _slots[slotIndex];
    }

    // Server-side take (remove)
    public bool ServerTryTakeAt(int slotIndex, out GrabbableObject item)
    {
        EnsureSlotsInitialized();
        item = null;

        if (!IsServer) return false;
        if (!IsValidSlotIndex(slotIndex)) return false;

        item = _slots[slotIndex];
        if (item == null) return false;

        _slots[slotIndex] = null;
        SetSlotClientRpc(slotIndex, default);
        return true;
    }

    // Server-side set (store)
    public bool ServerTrySetAt(int slotIndex, GrabbableObject item)
    {
        EnsureSlotsInitialized();
        if (!IsServer) return false;
        if (!IsValidSlotIndex(slotIndex)) return false;

        _slots[slotIndex] = item;

        if (item != null)
        {
            var netObj = item.NetworkObject;
            if (netObj != null)
            {
                // Chest giữ item dưới quyền server
                netObj.RemoveOwnership();
            }

            // Disable physics/colliders to keep it out of the world.
            item.OnGrabbed();

            // Teleport to storage anchor / chest position to avoid interaction in the scene.
            Vector3 targetPos = storageAnchor != null ? storageAnchor.position : transform.position;
            Quaternion targetRot = storageAnchor != null ? storageAnchor.rotation : transform.rotation;
            item.transform.SetPositionAndRotation(targetPos, targetRot);
        }

        NetworkObjectReference itemRef = item != null ? item.NetworkObject : default;
        SetSlotClientRpc(slotIndex, itemRef);
        return true;
    }

    [ClientRpc]
    public void SetSlotClientRpc(int slotIndex, NetworkObjectReference itemRef)
    {
        EnsureSlotsInitialized();
        if (!IsValidSlotIndex(slotIndex)) return;

        if (itemRef.TryGet(out NetworkObject netObj) && netObj != null)
        {
            _slots[slotIndex] = netObj.GetComponent<GrabbableObject>();
        }
        else
        {
            _slots[slotIndex] = null;
        }
    }

    private void EnsureSlotsInitialized()
    {
        if (_slots == null || _slots.Length != maxSlots)
        {
            _slots = new GrabbableObject[Mathf.Max(1, maxSlots)];
        }
    }
}
