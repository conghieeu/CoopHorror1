using Unity.Netcode;
using Unity.Netcode.Components; // Cần cái này để gọi NetworkTransform
using UnityEngine;

// Kế thừa NetworkBehaviour để đồng bộ
// Kế thừa IInteractable để hiện chữ "Press E..."
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : NetworkBehaviour, IInteractable
{
    [Header("Data")]
    public ItemData itemData; // File dữ liệu (Tên, Cân nặng, Prefab...)

    [Header("State (Debug)")]
    [SerializeField] private bool heldDebug;
    [SerializeField] private ulong heldByClientIdDebug;
    public bool isHoarded = false; // Dành cho Enemy sau này

    // Networked held state (single source of truth, server authoritative)
    private readonly NetworkVariable<bool> _isHeldNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<ulong> _heldByClientIdNet = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsHeld => IsSpawned ? _isHeldNet.Value : heldDebug;
    public ulong HeldByClientId => IsSpawned ? _heldByClientIdNet.Value : heldByClientIdDebug;

    public delegate void HeldStateChangedHandler(bool held, ulong holderClientId);
    public event HeldStateChangedHandler HeldStateChanged;

    [Header("Usage")]
    public bool isBeingUsed = false; // Biến kiểm tra xem đang bật hay tắt
    public int scrapValue = 0; // Giá trị tiền khi vứt ra đất

    // --- CÁC COMPONENT BẮT BUỘC ---
    private Rigidbody _rb;
    private NetworkObject _netObj;
    private Collider[] _colliders;
    private NetworkTransform _netTransform; // Component đồng bộ vị trí của Unity
    private Renderer[] _renderers;
    

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _netObj = GetComponent<NetworkObject>();
        _colliders = GetComponentsInChildren<Collider>();
        _netTransform = GetComponent<NetworkTransform>();
        _renderers = GetComponentsInChildren<Renderer>(true);

        // Setup mặc định khi vừa sinh ra
        if (itemData != null)
        {
            // Có thể setup layer mặc định ở đây nếu cần
            // gameObject.layer = LayerMask.NameToLayer("Grabbable");
        }
    }

    public override void OnNetworkSpawn()
    {
        _isHeldNet.OnValueChanged += OnHeldNetChanged;
        _heldByClientIdNet.OnValueChanged += OnHeldByClientIdNetChanged;

        ApplyHeldPresentation(_isHeldNet.Value, _heldByClientIdNet.Value);
        HeldStateChanged?.Invoke(_isHeldNet.Value, _heldByClientIdNet.Value);
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        _isHeldNet.OnValueChanged -= OnHeldNetChanged;
        _heldByClientIdNet.OnValueChanged -= OnHeldByClientIdNetChanged;
        base.OnNetworkDespawn();
    }

    // --- PHẦN 1: LOGIC TƯƠNG TÁC (INTERFACE) ---

    public string GetInteractText()
    {
        // Nếu đang bị người khác cầm thì không hiện chữ nhặt
        bool held = IsHeld;
        return held ? "" : $"Nhặt {itemData.itemName}";
    }

    public void Interact()
    {
        // Hàm này để trống hoặc chỉ xử lý logic phụ.
        // LÝ DO: Logic "Nhặt" chính thức nằm ở PlayerInteraction và PlayerInventory.
        // Khi Raycast trúng vật này, Player sẽ tự gọi inventory.GrabObject(this).
        // Ta không gọi ngược lại từ đây để tránh vòng lặp code (Circular Dependency).
    }

    // --- PHẦN 2: LOGIC CẦM / THẢ (GỌI TỪ INVENTORY) ---

    // Server là source of truth cho physics/collider; client chỉ apply visual/sync state.
    public void SetInventoryStateServer(bool inInventory)
    {
        if (!IsServer) return;

        // Inventory/held state is authoritative on server; clients react via NetworkVariables.
        SetHeldStateServer(inInventory, inInventory ? NetworkObject.OwnerClientId : 0);

        if (inInventory)
        {
            ApplyInventoryPhysicsHeldServer();
        }
        else
        {
            ApplyInventoryPhysicsDroppedServer();
        }
    }

    private void ApplyInventoryPhysicsHeldServer()
    {
        // 1. Tắt Vật Lý (Để không rơi khỏi tay)
        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.detectCollisions = false;
        }

        // 2. Tắt Collider (Để không đẩy Player khi đi)
        foreach (var col in _colliders) col.enabled = false;
    }

    private void ApplyInventoryPhysicsDroppedServer()
    {
        // 1. Bật lại Vật Lý
        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
        }

        // 2. Bật lại Collider
        foreach (var col in _colliders) col.enabled = true;

        // 3. Teleport ngay lập tức để tránh lerp từ tay xuống đất
        if (_netTransform != null)
        {
            // Ensure the component is enabled so Teleport is applied & replicated.
            if (!_netTransform.enabled) _netTransform.enabled = true;
            _netTransform.Teleport(transform.position, transform.rotation, transform.localScale);
        }
    }

    private void SetHeldStateServer(bool held, ulong holderClientId)
    {
        if (!IsServer) return;

        if (held)
        {
            _heldByClientIdNet.Value = holderClientId;
            _isHeldNet.Value = true;
        }
        else
        {
            _isHeldNet.Value = false;
            _heldByClientIdNet.Value = 0;
        }
    }

    private void OnHeldNetChanged(bool previous, bool current)
    {
        Debug.Log($"OnHeldNetChanged: {previous} -> {current}, HeldByClientId: {_heldByClientIdNet.Value}", this);
        ApplyHeldPresentation(current, _heldByClientIdNet.Value);
        HeldStateChanged?.Invoke(current, _heldByClientIdNet.Value);
    }

    private void OnHeldByClientIdNetChanged(ulong previous, ulong current)
    {
        // Holder can be set before/after held; re-apply presentation for safety.
        ApplyHeldPresentation(_isHeldNet.Value, current);
        HeldStateChanged?.Invoke(_isHeldNet.Value, current);
    }

    private void ApplyHeldPresentation(bool held, ulong holderClientId)
    {
        // Debug-only mirrors for inspector (not a source of truth).
        heldDebug = held;
        heldByClientIdDebug = holderClientId;

        // Keep NetworkTransform enabled even while held.
        // World items are hidden while held (lethal-style), and the server still needs to
        // replicate Teleport/transform updates reliably to observers when dropping/throwing.

        // Lethal-style: world item disappears for everyone while held.
        bool shouldHide = held;

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null) _renderers[i].enabled = !shouldHide;
            }
        }

        // Unity does not replicate Collider.enabled across the network.
        // To match lethal-style disappearance (as-if SetActive(false)), mirror collider state locally.
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null) _colliders[i].enabled = !shouldHide;
            }
        }

    }

    // --- PHẦN 3: LOGIC DÙNG ĐỒ (VIRTUAL METHODS) ---

    // Hàm ảo: Đèn pin, Súng, Xẻng sẽ ghi đè hàm này
    public virtual void ItemActivate(bool used, bool buttonDown = true)
    {
        isBeingUsed = used;
        // Code cụ thể nằm ở class con (FlashlightItem...)
    }

    // Hàm ảo: Dành cho logic phụ (Chuột phải / R)
    public virtual void ItemInteractSecondary()
    {
        // Ví dụ: Bật chế độ quét của máy radar
    }

    // Hàm đồng bộ giá trị tiền (Scrap Value)
    public void SetScrapValue(int value)
    {
        scrapValue = value;
        // Logic hiện UI tiền lên vật thể (Canvas World Space)
    }
}