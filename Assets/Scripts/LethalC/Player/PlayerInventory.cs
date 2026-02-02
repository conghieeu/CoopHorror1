using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private FirstPersonHands localHandsVisuals; // Kéo script FirstPersonHands vào đây
    [SerializeField] private Transform serverHandBone;           // Vị trí tay trên model nhân vật (để người khác nhìn)
    [SerializeField] private Transform dropPosition;             // Vị trí đồ rơi ra (thường là trước ngực)

    [Header("Settings")]
    public int maxSlots = 4;
    public float throwForce = 15f; 

    // Mảng lưu trữ các món đồ (Server và Owner cần biết)
    private GrabbableObject[] _inventorySlots;
    
    // Biến mạng đồng bộ Slot đang chọn (0, 1, 2, 3)
    private NetworkVariable<int> _currentSlotIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        EnsureInventorySlotsInitialized();
    }

    public override void OnNetworkSpawn()
    {
        // Khi biến Slot thay đổi -> Cập nhật hiển thị (Tắt đồ cũ, bật đồ mới)
        _currentSlotIndex.OnValueChanged += OnSlotChanged;

        EnsureInventorySlotsInitialized();

        // Owner cần tham chiếu FirstPersonHands để hiển thị đồ ở tay 1st person.
        // Nếu prefab không kéo sẵn trong Inspector, thử tự tìm để tránh NullReferenceException.
        if (IsOwner && localHandsVisuals == null)
        {
            localHandsVisuals = GetComponentInChildren<FirstPersonHands>(true);
            if (localHandsVisuals == null)
            {
                Debug.LogError($"[{nameof(PlayerInventory)}] Missing reference: {nameof(localHandsVisuals)}. Please assign it in Inspector or ensure a {nameof(FirstPersonHands)} exists under this player.", this);
            }
        }
    }

    private void Update()
    {
        // Chỉ chủ sở hữu mới được điều khiển
        if (!IsOwner) return;

        // 1. Cuộn chuột để đổi Slot
        HandleSlotSwitching();

        // 2. Phím G: Vứt đồ
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (_inventorySlots[_currentSlotIndex.Value] != null)
            {
                DropItemServerRpc(_currentSlotIndex.Value);
            }
        }

        // 3. Chuột Trái: Dùng đồ (Bật đèn, đánh xẻng)
        if (Input.GetMouseButtonDown(0)) UseItemServerRpc(true);
        if (Input.GetMouseButtonUp(0)) UseItemServerRpc(false);
    }

    private void LateUpdate()
    {
        // Đồng bộ pose hiển thị cho người khác (3rd person) mà không parenting NetworkObject vào bone.
        if (!IsSpawned) return;

        EnsureInventorySlotsInitialized();
        int activeSlot = Mathf.Clamp(_currentSlotIndex.Value, 0, _inventorySlots.Length - 1);
        var item = _inventorySlots[activeSlot];
        if (item == null) return;

        // Owner: FirstPersonHands sẽ tự xử lý pose (LateUpdate) khi EquipItem.
        if (IsOwner) return;

        if (serverHandBone != null)
        {
            item.transform.localScale = Vector3.one;
            item.transform.SetPositionAndRotation(serverHandBone.position, serverHandBone.rotation);
        }
    }

    // --- LOGIC GỌI TỪ RAYCAST (INTERACTION) ---

    // Hàm này được gọi từ script PlayerInteraction khi Raycast trúng đồ và bấm E
    public void GrabObject(GrabbableObject grabbable)
    {
        if (!IsOwner) return;
        if (grabbable == null || grabbable.NetworkObject == null) return;
        EnsureInventorySlotsInitialized();

        // Tìm ô trống thích hợp
        int targetSlot = -1;

        // Nếu tay đang rỗng -> Nhặt vào tay luôn
        if (_inventorySlots[_currentSlotIndex.Value] == null)
        {
            targetSlot = _currentSlotIndex.Value;
        }
        else // Nếu tay bận -> Tìm ô khác
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (_inventorySlots[i] == null)
                {
                    targetSlot = i;
                    break;
                }
            }
        }

        if (targetSlot != -1)
        {
            // Nếu nhặt vào ô khác -> Tự chuyển slot sang ô đó
            if (targetSlot != _currentSlotIndex.Value)
            {
                SwitchSlotServerRpc(targetSlot);
            }

            // Gửi yêu cầu nhặt lên Server
            GrabObjectServerRpc(grabbable.NetworkObject, targetSlot);
        }
        else
        {
            Debug.Log("Inventory Full! (Túi đầy rồi)");
        }
    }

    // --- SERVER RPCS (XỬ LÝ DỮ LIỆU & QUYỀN) ---

    [ServerRpc]
    private void GrabObjectServerRpc(NetworkObjectReference itemRef, int slotIndex)
    {
        EnsureInventorySlotsInitialized();
        if (!IsValidSlotIndex(slotIndex)) return;

        if (itemRef.TryGet(out NetworkObject netObj))
        {
            var item = netObj.GetComponent<GrabbableObject>();
            if (item == null) return;

            // 1. Cập nhật dữ liệu Server
            _inventorySlots[slotIndex] = item;

            // 2. Chuyển chủ sở hữu cho Client này
            netObj.ChangeOwnership(OwnerClientId);

            // 3. Gắn tạm vào Player (để ko bị trôi) - Quan trọng: Ép kiểu Transform để tránh lỗi CS0121
            // Không parent NetworkObject vào bone/anchor không có NetworkObject (sẽ throw InvalidParentException).
            // Pose hiển thị sẽ được xử lý client-side (FirstPersonHands / LateUpdate).

            // 4. Báo cho tất cả Client biết để hiển thị
            GrabObjectClientRpc(itemRef, slotIndex);
        }
    }

    [ServerRpc]
    private void DropItemServerRpc(int slotIndex)
    {
        EnsureInventorySlotsInitialized();
        if (!IsValidSlotIndex(slotIndex)) return;
        if (_inventorySlots[slotIndex] == null) return;

        GrabbableObject item = _inventorySlots[slotIndex];
        NetworkObject netObj = item.NetworkObject;

        // 1. Xóa dữ liệu
        _inventorySlots[slotIndex] = null;

        // 2. Trả quyền sở hữu về Server (hoặc xóa Owner)
        netObj.RemoveOwnership();
        netObj.TrySetParent((Transform)null); // Tách khỏi cha

        // 3. Báo cho Client để hiển thị vứt ra
        Vector3 forward = dropPosition != null ? dropPosition.forward : transform.forward;
        DropItemClientRpc(netObj, forward * throwForce);
    }

    [ServerRpc]
    private void SwitchSlotServerRpc(int newSlot)
    {
        _currentSlotIndex.Value = newSlot;
    }

    [ServerRpc]
    private void UseItemServerRpc(bool isDown)
    {
        // Server làm trung gian báo cho mọi người
        UseItemClientRpc(isDown, _currentSlotIndex.Value);
    }

    // --- CLIENT RPCS (XỬ LÝ HIỂN THỊ) ---

    [ClientRpc]
    private void GrabObjectClientRpc(NetworkObjectReference itemRef, int slotIndex)
    {
        EnsureInventorySlotsInitialized();
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning($"[{nameof(PlayerInventory)}] Invalid slotIndex={slotIndex} in {nameof(GrabObjectClientRpc)} (maxSlots={maxSlots}, slotsLen={_inventorySlots?.Length}).", this);
            return;
        }

        if (itemRef.TryGet(out NetworkObject netObj))
        {
            var item = netObj.GetComponent<GrabbableObject>();
            if (item == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInventory)}] NetworkObject '{netObj.name}' is missing {nameof(GrabbableObject)}.", netObj);
                return;
            }
            
            // Cập nhật mảng ở Client (để đồng bộ)
            _inventorySlots[slotIndex] = item;

            // Tắt vật lý (Gọi hàm bên GrabbableObject)
            item.OnGrabbed();

            // XỬ LÝ HIỂN THỊ "ẢO THUẬT"
            if (IsOwner)
            {
                // Nếu là mình: Giao cho script FirstPersonHands lo (gắn vào Camera, chỉnh Offset...)
                if (localHandsVisuals != null)
                {
                    localHandsVisuals.EquipItem(item);
                }
                else
                {
                    Debug.LogError($"[{nameof(PlayerInventory)}] {nameof(localHandsVisuals)} is null on Owner. Item will be grabbed but not equipped visually.", this);
                }
            }
            else
            {
                // Nếu là người khác: Gắn vào tay nhân vật 3D
                if (serverHandBone != null)
                {
                    item.transform.localScale = Vector3.one;
                    item.transform.SetPositionAndRotation(serverHandBone.position, serverHandBone.rotation);
                }
            }

            // Ẩn hiện đúng slot
            RefreshSlotVisibility();
        }
        else
        {
            Debug.LogWarning($"[{nameof(PlayerInventory)}] Failed to resolve itemRef in {nameof(GrabObjectClientRpc)}. Object may not be spawned yet.", this);
        }
    }

    [ClientRpc]
    private void DropItemClientRpc(NetworkObjectReference itemRef, Vector3 throwVelocity)
    {
        EnsureInventorySlotsInitialized();

        if (itemRef.TryGet(out NetworkObject netObj))
        {
            var item = netObj.GetComponent<GrabbableObject>();
            if (item == null) return;

            // Xóa khỏi mảng Client
            for (int i = 0; i < maxSlots; i++)
            {
                if (_inventorySlots[i] == item) _inventorySlots[i] = null;
            }

            // Bật lại vật lý
            item.OnDropped();

            // Nếu là mình thì gỡ khỏi tay ảo
            if (IsOwner && localHandsVisuals != null) localHandsVisuals.UnequipItem(item);

            // Server đẩy lực ném (Server Authority Physics)
            if (IsServer)
            {
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.AddForce(throwVelocity, ForceMode.Impulse);
                }
            }
        }
    }

    [ClientRpc]
    private void UseItemClientRpc(bool isDown, int slotIndex)
    {
        EnsureInventorySlotsInitialized();
        if (!IsValidSlotIndex(slotIndex)) return;

        // Kích hoạt món đồ ở slot tương ứng
        var item = _inventorySlots[slotIndex];
        if (item != null)
        {
            item.ItemActivate(isDown);
        }
    }

    // --- HELPER FUNCTIONS ---

    private void HandleSlotSwitching()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int newSlot = _currentSlotIndex.Value + (scroll > 0 ? 1 : -1);
            
            // Loop slot (0-3)
            if (newSlot > maxSlots - 1) newSlot = 0;
            if (newSlot < 0) newSlot = maxSlots - 1;

            if (newSlot != _currentSlotIndex.Value) SwitchSlotServerRpc(newSlot);
        }

        // Phím số
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchSlotServerRpc(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchSlotServerRpc(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchSlotServerRpc(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchSlotServerRpc(3);
    }

    private void OnSlotChanged(int oldSlot, int newSlot)
    {
        RefreshSlotVisibility();
    }

    private void RefreshSlotVisibility()
    {
        EnsureInventorySlotsInitialized();
        int slotCount = _inventorySlots.Length;
        int activeSlot = Mathf.Clamp(_currentSlotIndex.Value, 0, slotCount - 1);

        for (int i = 0; i < slotCount; i++)
        {
            if (_inventorySlots[i] != null)
            {
                // Chỉ hiện món đồ ở Slot hiện tại
                _inventorySlots[i].gameObject.SetActive(i == activeSlot);
            }
        }
    }

    private void EnsureInventorySlotsInitialized()
    {
        if (_inventorySlots == null || _inventorySlots.Length != maxSlots)
        {
            _inventorySlots = new GrabbableObject[Mathf.Max(1, maxSlots)];
        }
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return _inventorySlots != null && slotIndex >= 0 && slotIndex < _inventorySlots.Length;
    }
}