using UnityEngine;

public class FirstPersonHands : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handAnchor; // GameObject con của Camera (Vị trí đặt tay)
    
    [Header("Sway Settings")]
    public float swayAmount = 0.02f;
    public float maxSway = 0.06f;
    public float smoothSway = 5f;

    private Vector3 _initialLocalPos;

    private GrabbableObject _equippedItem;

    private void Start()
    {
        if(handAnchor != null)
            _initialLocalPos = handAnchor.localPosition;
    }

    private void Update()
    {
        // Hiệu ứng lắc tay khi di chuyển chuột (Weapon Sway)
        if (handAnchor != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * swayAmount;
            float mouseY = Input.GetAxis("Mouse Y") * swayAmount;

            mouseX = Mathf.Clamp(mouseX, -maxSway, maxSway);
            mouseY = Mathf.Clamp(mouseY, -maxSway, maxSway);

            Vector3 targetPos = new Vector3(_initialLocalPos.x - mouseX, _initialLocalPos.y - mouseY, _initialLocalPos.z);
            handAnchor.localPosition = Vector3.Lerp(handAnchor.localPosition, targetPos, Time.deltaTime * smoothSway);
        }
    }

    private void LateUpdate()
    {
        if (_equippedItem == null) return;
        if (handAnchor == null) return;

        ApplyPose(_equippedItem);
    }

    // --- HÀM QUAN TRỌNG: GẮN ĐỒ VÀO TAY ---
    public void EquipItem(GrabbableObject item)
    {
        if (item == null) return;
        if (handAnchor == null)
        {
            Debug.LogError($"[{nameof(FirstPersonHands)}] Missing reference: {nameof(handAnchor)}. Cannot equip item visually.", this);
            return;
        }

        _equippedItem = item;

        // Không được parent NetworkObject vào non-NetworkObject parent (Netcode sẽ throw InvalidParentException).
        // Chỉ "follow" tay bằng cách set world pose.
        ApplyPose(item);

        // 4. Đổi Layer sang "FirstPersonObjects" (Để camera render đẹp, không bị xuyên tường)
        // Lưu ý: Bạn cần tạo Layer này trong Unity Editor trước
        SetLayerRecursively(item.gameObject, LayerMask.NameToLayer("FirstPersonObjects")); 
    }

    public void UnequipItem(GrabbableObject item)
    {
        if (item == null) return;

        if (_equippedItem == item) _equippedItem = null;
        
        // Trả về Layer mặc định (Default) để người khác nhìn thấy bình thường
        SetLayerRecursively(item.gameObject, LayerMask.NameToLayer("Default"));
    }
    
    public void ClearEquippedItem()
    {
        if (_equippedItem != null)
        {
            UnequipItem(_equippedItem);
            _equippedItem = null;
        }
    }

    private void ApplyPose(GrabbableObject item)
    {
        if (item == null || handAnchor == null) return;

        // Reset Scale (đề phòng model bị méo). Lưu ý: scale chịu ảnh hưởng hierarchy hiện tại.
        item.transform.localScale = Vector3.one;

        if (item.itemData != null)
        {
            Vector3 worldPos = handAnchor.TransformPoint(item.itemData.positionOffset);
            Quaternion worldRot = handAnchor.rotation * Quaternion.Euler(item.itemData.rotationOffset);
            item.transform.SetPositionAndRotation(worldPos, worldRot);
        }
        else
        {
            item.transform.SetPositionAndRotation(handAnchor.position, handAnchor.rotation);
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        // Nếu layer không tồn tại (chưa tạo) thì bỏ qua để tránh lỗi
        if (layer == -1) return; 

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}