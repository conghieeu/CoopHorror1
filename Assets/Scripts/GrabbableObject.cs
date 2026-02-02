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
    public bool isHeld = false;
    public bool isHoarded = false; // Dành cho Enemy sau này

    [Header("Usage")]
    public bool isBeingUsed = false; // Biến kiểm tra xem đang bật hay tắt
    public int scrapValue = 0; // Giá trị tiền khi vứt ra đất

    // --- CÁC COMPONENT BẮT BUỘC ---
    private Rigidbody _rb;
    private NetworkObject _netObj;
    private Collider[] _colliders;
    private NetworkTransform _netTransform; // Component đồng bộ vị trí của Unity
    

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _netObj = GetComponent<NetworkObject>();
        _colliders = GetComponentsInChildren<Collider>();
        _netTransform = GetComponent<NetworkTransform>();

        // Setup mặc định khi vừa sinh ra
        if (itemData != null)
        {
            // Có thể setup layer mặc định ở đây nếu cần
            // gameObject.layer = LayerMask.NameToLayer("Grabbable");
        }
    }

    // --- PHẦN 1: LOGIC TƯƠNG TÁC (INTERFACE) ---

    public string GetInteractText()
    {
        // Nếu đang bị người khác cầm thì không hiện chữ nhặt
        return isHeld ? "" : $"Nhặt {itemData.itemName}";
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

        ApplyInventoryVisualState(inInventory);

        if (inInventory)
        {
            ApplyInventoryPhysicsHeldServer();
        }
        else
        {
            ApplyInventoryPhysicsDroppedServer();
        }
    }

    public void ApplyInventoryVisualState(bool inInventory)
    {
        isHeld = inInventory;

        // Important: clients also need to adjust NetworkTransform behavior to avoid fighting hand-follow visuals.
        if (_netTransform != null)
        {
            _netTransform.InLocalSpace = inInventory;
        }

        if (!inInventory)
        {
            // Visual reset when leaving inventory/hand
            transform.SetParent(null);
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }
    }

    // Được gọi khi Player nhặt thành công (Inventory gọi)
    public void OnGrabbed()
    {
        ApplyInventoryVisualState(true);

        // Server applies physics/collider state. Clients should avoid side effects.
        if (IsServer)
        {
            ApplyInventoryPhysicsHeldServer();
        }
    }

    // Được gọi khi Player vứt đồ (Inventory gọi)
    public void OnDropped()
    {
        ApplyInventoryVisualState(false);

        // Server applies physics/collider state. Clients should avoid side effects.
        if (IsServer)
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
            _netTransform.Teleport(transform.position, transform.rotation, transform.localScale);
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