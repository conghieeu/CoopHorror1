using System.Collections.Generic;
using QFSW.QC;
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

    // Client cache (nguồn sự thật nằm ở server qua NetworkList).
    private GrabbableObject[] _inventorySlots;

    // Server authoritative inventory state (fixed-size = maxSlots).
    // NOTE: NetworkList allocates native memory and MUST be disposed.
    // Do not allocate it in a field initializer to avoid leaks on domain reload / object destroy.
    private NetworkList<NetworkObjectReference> _inventoryRefs;

    // Biến mạng đồng bộ Slot đang chọn (0, 1, 2, 3)
    private NetworkVariable<int> _currentSlotIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Input/use event state (owner-only)
    private bool _useHeld;
    private int _useHeldSlotIndex = -1;

    // Cache rebuild (client-side)
    private bool _pendingInventoryRebuild;
    private int _pendingInventoryRebuildFrame;
    private int _unresolvedRebuildRetryBudget;

    // 3rd person pose optimization
    private bool _handPoseInitialized;
    private Vector3 _lastHandPos;
    private Quaternion _lastHandRot;

    // Third-person held item visuals (local-only per client)
    private GameObject _thirdPersonHeldModel;
    private ItemData _thirdPersonHeldData;
    private GrabbableObject _observedActiveItem;

    private void Awake()
    {
        EnsureInventoryRefsAllocated();
        EnsureInventorySlotsInitialized();

        // Best-effort auto wire for remote visuals.
        // If your player uses a Humanoid avatar, this will find the right-hand bone.
        if (serverHandBone == null)
        {
            var animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                serverHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        EnsureInventoryRefsAllocated();

        // Khi biến Slot thay đổi -> Cập nhật hiển thị (Tắt đồ cũ, bật đồ mới)
        _currentSlotIndex.OnValueChanged += OnSlotChanged;

        // Khi inventory state thay đổi -> client rebuild cache + refresh visuals
        _inventoryRefs.OnListChanged += OnInventoryRefsListChanged;

        if (IsServer)
        {
            EnsureInventoryRefsInitializedServer();
        }

        EnsureInventorySlotsInitialized();
        RequestInventoryRebuild();

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

    public override void OnNetworkDespawn()
    {
        _currentSlotIndex.OnValueChanged -= OnSlotChanged;
        if (_inventoryRefs != null)
        {
            _inventoryRefs.OnListChanged -= OnInventoryRefsListChanged;
        }

        UnobserveActiveItem();
        DestroyThirdPersonHeldModel();
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        // In case Unity destroys the object without a clean network despawn.
        _currentSlotIndex.OnValueChanged -= OnSlotChanged;

        if (_inventoryRefs != null)
        {
            _inventoryRefs.OnListChanged -= OnInventoryRefsListChanged;
            _inventoryRefs.Dispose();
            _inventoryRefs = null;
        }

        UnobserveActiveItem();
        DestroyThirdPersonHeldModel();
        base.OnDestroy();
    }

    private void Update()
    {
        // Inventory cache rebuild should run on all clients (owner + non-owner)
        ProcessPendingInventoryRebuild();

        // Chỉ chủ sở hữu mới được điều khiển
        if (!IsOwner) return;

        // 1. Cuộn chuột để đổi Slot
        HandleSlotSwitching();

        // 2. Phím G: Vứt đồ
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (_inventorySlots[_currentSlotIndex.Value] != null)
            {
                if (_useHeld && _useHeldSlotIndex == _currentSlotIndex.Value)
                {
                    UseItemServerRpc(false, _useHeldSlotIndex);
                    _useHeld = false;
                    _useHeldSlotIndex = -1;
                }

                DropItemServerRpc(_currentSlotIndex.Value);
            }
        }

        // 3. Chuột Trái: Dùng đồ (gửi event, chỉ khi state đổi để tránh spam RPC)
        bool isMouseDown = Input.GetMouseButton(0);
        if (isMouseDown != _useHeld)
        {
            _useHeld = isMouseDown;
            int slotIndex = Mathf.Clamp(_currentSlotIndex.Value, 0, maxSlots - 1);
            _useHeldSlotIndex = _useHeld ? slotIndex : -1;
            UseItemServerRpc(_useHeld, slotIndex);
        }
    }

    private void LateUpdate()
    {
        // Lethal-style: world items disappear while held. Do not sync a world item to hand bones.
        // 1st-person visuals are handled by local-only viewmodels (FirstPersonHands).
    }

    // --- LOGIC GỌI TỪ RAYCAST (INTERACTION) ---

    // Hàm này được gọi từ script PlayerInteraction khi Raycast trúng đồ và bấm E
    public void GrabObject(GrabbableObject grabbable)
    {
        TryGrab(grabbable);
    }

    // Pick-up request entrypoint (client intent only; server validates)
    public void TryGrab(GrabbableObject grabbable)
    {
        Debug.Log("Client A: Tôi thử nhặt đồ.", this);
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
            Debug.Log("Client A: Gửi yêu cầu nhặt đồ lên Server.", this);
            GrabObjectServerRpc(grabbable.NetworkObject, targetSlot);
        }
        else
        {
            Debug.Log("Inventory Full! (Túi đầy rồi)");
        }
    }

    // Tráo 2 slot (ví dụ: kéo thả hotbar/UI hoặc phím tắt).
    // Lưu ý: Vì slot ảnh hưởng gameplay (Use/Drop), swap phải qua Server để tránh desync.
    public void SwapSlots(int slotA, int slotB)
    {
        if (!IsOwner) return;
        EnsureInventorySlotsInitialized();

        if (slotA == slotB) return;
        if (!IsValidSlotIndex(slotA) || !IsValidSlotIndex(slotB)) return;

        SwapSlotsServerRpc(slotA, slotB);
    }

    [ServerRpc]
    private void GrabObjectServerRpc(NetworkObjectReference itemRef, int slotIndex)
    {
        EnsureInventorySlotsInitialized();
        EnsureInventoryRefsInitializedServer();
        if (!IsValidSlotIndex(slotIndex)) return;

        if (itemRef.TryGet(out NetworkObject netObj))
        {
            var item = netObj.GetComponent<GrabbableObject>();
            if (item == null) return;

            Debug.Log("Server: Xử lý yêu cầu nhặt đồ từ Client.", this);

            // Server-authoritative: reject if already held by someone else.
            if (item.IsHeld) return;

            // 1. Cập nhật dữ liệu Server (source of truth)
            _inventoryRefs[slotIndex] = itemRef;

            // 2. Chuyển chủ sở hữu cho Client này
            netObj.ChangeOwnership(OwnerClientId);

            // 3. Server quyết định state (physics/collider). Client sẽ apply visual theo inventoryRefs.
            item.SetInventoryStateServer(true);
        }
    }

    [ServerRpc]
    private void DropItemServerRpc(int slotIndex)
    {
        EnsureInventorySlotsInitialized();
        EnsureInventoryRefsInitializedServer();
        if (!IsValidSlotIndex(slotIndex)) return;

        NetworkObjectReference itemRef = _inventoryRefs[slotIndex];
        if (itemRef.Equals(default)) return;

        if (!itemRef.TryGet(out NetworkObject netObj) || netObj == null) return;

        var item = netObj.GetComponent<GrabbableObject>();
        if (item == null) return;

        // 1. Xóa dữ liệu (source of truth)
        _inventoryRefs[slotIndex] = default;

        // 2. Trả quyền sở hữu về Server
        netObj.RemoveOwnership();
        netObj.TrySetParent((Transform)null);

        // 3. Đặt vị trí thả và bật lại vật lý
        Vector3 forward = dropPosition != null ? dropPosition.forward : transform.forward;
        Vector3 dropPos = dropPosition != null ? dropPosition.position : (transform.position + forward * 0.8f);
        item.transform.position = dropPos;
        item.SetInventoryStateServer(false);

        // 4. Ném (Server-authority physics)
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.AddForce(forward * throwForce, ForceMode.Impulse);
        }
    }

    [ServerRpc]
    private void SwitchSlotServerRpc(int newSlot)
    {
        newSlot = Mathf.Clamp(newSlot, 0, maxSlots - 1);
        _currentSlotIndex.Value = newSlot;
    }

    [ServerRpc]
    private void SwapSlotsServerRpc(int slotA, int slotB)
    {
        EnsureInventorySlotsInitialized();
        EnsureInventoryRefsInitializedServer();
        if (slotA == slotB) return;
        if (!IsValidSlotIndex(slotA) || !IsValidSlotIndex(slotB)) return;

        // Swap dữ liệu server (source of truth)
        var tmp = _inventoryRefs[slotA];
        _inventoryRefs[slotA] = _inventoryRefs[slotB];
        _inventoryRefs[slotB] = tmp;
    }

    [ServerRpc]
    private void UseItemServerRpc(bool isDown, int slotIndex, ServerRpcParams rpcParams = default)
    {
        EnsureInventoryRefsInitializedServer();

        // This NetworkBehaviour is owned by the player, but validate sender anyway.
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        slotIndex = Mathf.Clamp(slotIndex, 0, maxSlots - 1);
        if (slotIndex < 0 || slotIndex >= _inventoryRefs.Count) return;

        var itemRef = _inventoryRefs[slotIndex];
        if (itemRef.Equals(default)) return;

        if (!itemRef.TryGet(out NetworkObject netObj) || netObj == null) return;

        // Ensure the item is actually owned by this player before allowing use.
        if (netObj.OwnerClientId != OwnerClientId) return;

        UseItemClientRpc(isDown, slotIndex);
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

        // Owner: also forward to local-only viewmodel.
        if (IsOwner && localHandsVisuals != null)
        {
            localHandsVisuals.ForwardItemActivate(isDown);
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
        // If owner is holding-use and changes slot, send stop/start events.
        if (IsOwner && _useHeld)
        {
            int oldClamped = Mathf.Clamp(oldSlot, 0, maxSlots - 1);
            int newClamped = Mathf.Clamp(newSlot, 0, maxSlots - 1);
            if (oldClamped != newClamped)
            {
                UseItemServerRpc(false, oldClamped);
                UseItemServerRpc(true, newClamped);
                _useHeldSlotIndex = newClamped;
            }
        }

        // Reset 3rd person pose cache so the next LateUpdate updates immediately
        _handPoseInitialized = false;

        // Owner: update the local viewmodel based on active slot.
        if (IsOwner && localHandsVisuals != null)
        {
            EnsureInventorySlotsInitialized();
            int activeSlot = Mathf.Clamp(_currentSlotIndex.Value, 0, _inventorySlots.Length - 1);
            var activeItem = _inventorySlots[activeSlot];
            if (activeItem != null) localHandsVisuals.EquipItem(activeItem);
            else localHandsVisuals.ClearEquippedItem();
        }

        // Non-owner: update third-person held visuals for observers.
        UpdateObservedActiveItem();
        UpdateThirdPersonHeldVisuals();
    }

    private void OnInventoryRefsListChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        // Treat inventory on clients as cache; rebuild from server truth.
        _unresolvedRebuildRetryBudget = 60; // ~1s @ 60fps to resolve late-spawned objects
        RequestInventoryRebuild();
    }

    private void RequestInventoryRebuild()
    {
        _pendingInventoryRebuild = true;
        _pendingInventoryRebuildFrame = Time.frameCount;
    }

    private void ProcessPendingInventoryRebuild()
    {
        if (!_pendingInventoryRebuild) return;

        // Wait one frame so spawned objects have a chance to resolve.
        if (Time.frameCount == _pendingInventoryRebuildFrame) return;

        _pendingInventoryRebuild = false;
        RebuildInventoryCacheFromRefs();
    }

    private void RebuildInventoryCacheFromRefs()
    {
        EnsureInventorySlotsInitialized();
        EnsureInventoryRefsAllocated();

        int slotCount = _inventorySlots.Length;
        var oldItems = new HashSet<GrabbableObject>();
        var newItems = new HashSet<GrabbableObject>();
        bool hasUnresolvedRefs = false;

        for (int i = 0; i < slotCount; i++)
        {
            if (_inventorySlots[i] != null) oldItems.Add(_inventorySlots[i]);
        }

        // Resolve refs into cache
        for (int i = 0; i < slotCount; i++)
        {
            GrabbableObject resolved = null;
            if (i < _inventoryRefs.Count)
            {
                var itemRef = _inventoryRefs[i];
                if (!itemRef.Equals(default))
                {
                    if (itemRef.TryGet(out NetworkObject netObj) && netObj != null)
                    {
                        resolved = netObj.GetComponent<GrabbableObject>();
                    }
                    else
                    {
                        hasUnresolvedRefs = true;
                    }
                }
            }

            _inventorySlots[i] = resolved;
            if (resolved != null) newItems.Add(resolved);
        }

        // Apply state transitions for items actually entering/leaving the inventory.
        // Visual presentation is derived from each item's networked held state.
        // Inventory here is a cache; avoid mutating item state locally to prevent desync.

        // Owner: keep FirstPersonHands in sync.
        if (IsOwner && localHandsVisuals != null)
        {
            int activeSlot = Mathf.Clamp(_currentSlotIndex.Value, 0, slotCount - 1);
            var activeItem = _inventorySlots[activeSlot];
            if (activeItem != null) localHandsVisuals.EquipItem(activeItem);
            else localHandsVisuals.ClearEquippedItem();
        }

        UpdateObservedActiveItem();
        UpdateThirdPersonHeldVisuals();

        // Reset pose cache so remote visuals update immediately.
        _handPoseInitialized = false;

        // Late-join/spawn-order: retry rebuild a bit if some refs couldn't resolve yet.
        if (hasUnresolvedRefs && _unresolvedRebuildRetryBudget > 0)
        {
            _unresolvedRebuildRetryBudget--;
            RequestInventoryRebuild();
        }
    }

    private void UpdateObservedActiveItem()
    {
        EnsureInventorySlotsInitialized();
        int activeSlot = Mathf.Clamp(_currentSlotIndex.Value, 0, _inventorySlots.Length - 1);
        var activeItem = _inventorySlots[activeSlot];

        if (_observedActiveItem == activeItem) return;

        UnobserveActiveItem();
        _observedActiveItem = activeItem;
        if (_observedActiveItem != null)
        {
            _observedActiveItem.HeldStateChanged += OnObservedItemHeldStateChanged;
        }
    }

    private void UnobserveActiveItem()
    {
        if (_observedActiveItem != null)
        {
            _observedActiveItem.HeldStateChanged -= OnObservedItemHeldStateChanged;
            _observedActiveItem = null;
        }
    }

    private void OnObservedItemHeldStateChanged(bool held, ulong holderClientId)
    {
        UpdateThirdPersonHeldVisuals();
    }

    private void UpdateThirdPersonHeldVisuals()
    {
        // Only show 3rd-person held props to observers.
        if (IsOwner)
        {
            DestroyThirdPersonHeldModel();
            return;
        }

        EnsureInventorySlotsInitialized();

        int activeSlot = Mathf.Clamp(_currentSlotIndex.Value, 0, _inventorySlots.Length - 1);
        var activeItem = _inventorySlots[activeSlot];
        if (activeItem == null || activeItem.itemData == null)
        {
            DestroyThirdPersonHeldModel();
            return;
        }

        // Only show if this player is actually holding this item.
        if (!activeItem.IsHeld || activeItem.HeldByClientId != OwnerClientId)
        {
            DestroyThirdPersonHeldModel();
            return;
        }

        EnsureServerHandBoneResolved();
        if (serverHandBone == null)
        {
            DestroyThirdPersonHeldModel();
            return;
        }

        var data = activeItem.itemData;
        GameObject prefab = data.thirdPersonPrefab != null ? data.thirdPersonPrefab : data.spawnPrefab;
        if (prefab == null)
        {
            DestroyThirdPersonHeldModel();
            return;
        }

        if (_thirdPersonHeldModel == null || _thirdPersonHeldData != data)
        {
            DestroyThirdPersonHeldModel();
            _thirdPersonHeldData = data;
            _thirdPersonHeldModel = Instantiate(prefab);
            _thirdPersonHeldModel.name = $"TP_{prefab.name}_{OwnerClientId}";
            _thirdPersonHeldModel.transform.SetParent(serverHandBone, worldPositionStays: false);

            // Safety: TP prefab should not be networked. If it is, disable Netcode components.
            foreach (var netObj in _thirdPersonHeldModel.GetComponentsInChildren<Unity.Netcode.NetworkObject>(true))
            {
                netObj.enabled = false;
            }
            foreach (var netTransform in _thirdPersonHeldModel.GetComponentsInChildren<Unity.Netcode.Components.NetworkTransform>(true))
            {
                netTransform.enabled = false;
            }

            foreach (var rb in _thirdPersonHeldModel.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var col in _thirdPersonHeldModel.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }
        }

        // Pose relative to the hand bone.
        _thirdPersonHeldModel.transform.localPosition = data.thirdPersonPositionOffset;
        _thirdPersonHeldModel.transform.localRotation = Quaternion.Euler(data.thirdPersonRotationOffset);
        _thirdPersonHeldModel.transform.localScale = Vector3.one;
    }

    private void DestroyThirdPersonHeldModel()
    {
        if (_thirdPersonHeldModel != null)
        {
            Destroy(_thirdPersonHeldModel);
            _thirdPersonHeldModel = null;
            _thirdPersonHeldData = null;
        }
    }

    private void EnsureServerHandBoneResolved()
    {
        if (serverHandBone != null) return;
        var animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.isHuman)
        {
            serverHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
    }

    private void EnsureInventorySlotsInitialized()
    {
        if (_inventorySlots == null || _inventorySlots.Length != maxSlots)
        {
            _inventorySlots = new GrabbableObject[Mathf.Max(1, maxSlots)];
        }
    }

    private void EnsureInventoryRefsInitializedServer()
    {
        EnsureInventoryRefsAllocated();
        if (!IsServer) return;
        int desired = Mathf.Max(1, maxSlots);
        if (_inventoryRefs.Count == desired) return;

        _inventoryRefs.Clear();
        for (int i = 0; i < desired; i++)
        {
            _inventoryRefs.Add(default);
        }
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return _inventorySlots != null && slotIndex >= 0 && slotIndex < _inventorySlots.Length;
    }

    private void EnsureInventoryRefsAllocated()
    {
        if (_inventoryRefs != null) return;

        _inventoryRefs = new NetworkList<NetworkObjectReference>(
            null,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
    }

    // hàm này check xem các món đồ trong inventory, debug các món đồ
    [Command("/check_inventory", MonoTargetType.All)] // Sử dụng QFSW.QC để tạo lệnh console check inventory của name gameobject
    public void DebugInventoryContents()
    {
        EnsureInventorySlotsInitialized();
        Debug.Log($"=== Player Inventory of {gameObject.name} ===", this);
        for (int i = 0; i < _inventorySlots.Length; i++)
        {
            var item = _inventorySlots[i];
            if (item != null)
            {
                Debug.Log($"Slot {i}: {item.itemData.itemName} (NetId: {item.NetworkObject.NetworkObjectId})");
            }
            else
            {
                Debug.Log($"Slot {i}: Empty");
            }
        }
    }
}