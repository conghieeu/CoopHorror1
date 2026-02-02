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

    // Được gọi khi Player nhặt thành công (Inventory gọi)
    public void OnGrabbed()
    {
        isHeld = true;

        // 1. Tắt Vật Lý (Để không rơi khỏi tay)
        if (_rb)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.detectCollisions = false; // Tắt va chạm vật lý hoàn toàn
        }

        // 2. Tắt Collider (Để không đẩy Player khi đi)
        foreach (var col in _colliders) col.enabled = false;

        // 3. QUAN TRỌNG: Ngắt đồng bộ vị trí khi đang cầm
        // Nếu không tắt, Server sẽ cố kéo vật thể về vị trí server tính toán,
        // gây xung đột với việc "gắn vào tay" ở máy Client -> Rung lắc dữ dội.
        if (_netTransform != null)
        {
            _netTransform.InLocalSpace = true; // Chuyển sang tính toán cục bộ (hoặc disable)
        }
    }

    // Được gọi khi Player vứt đồ (Inventory gọi)
    public void OnDropped()
    {
        isHeld = false;

        // 1. Tách khỏi tay (Unparent)
        transform.SetParent(null);

        // 2. Reset Scale (Đề phòng lúc cầm bị méo, lúc vứt ra phải về 1)
        transform.localScale = Vector3.one;

        // 3. Bật lại Vật Lý
        if (_rb)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.detectCollisions = true;
        }

        // 4. Bật lại Collider
        foreach (var col in _colliders) col.enabled = true;

        // 5. Bật lại đồng bộ vị trí (Để Server đồng bộ vị trí rơi cho mọi người thấy)
        if (_netTransform != null)
        {
            _netTransform.InLocalSpace = false; // Trả về đồng bộ toàn cầu

            // Nếu là Server thì phải Teleport ngay lập tức để tránh lerp từ tay xuống đất
            if (IsServer)
            {
                _netTransform.Teleport(transform.position, transform.rotation, transform.localScale);
            }
        }

        // Đảm bảo object hiện hình (nếu trước đó bị ẩn trong túi)
        gameObject.SetActive(true);
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