using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class FirstPersonHands : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handAnchor; // GameObject con của Camera (Vị trí đặt tay)
    
    [Header("Sway Settings")]
    public float swayAmount = 0.02f;
    public float maxSway = 0.06f;
    public float smoothSway = 5f;

    private Vector3 _initialLocalPos;

    private GameObject _equippedViewModel;
    private MonoBehaviour _equippedViewModelUsable;
    private ItemData _equippedData;

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
        if (_equippedViewModel == null) return;
        if (handAnchor == null) return;

        ApplyPose(_equippedViewModel.transform, _equippedData);
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

        EquipItemData(item.itemData);
    }
    
    public void ClearEquippedItem()
    {
        _equippedData = null;

        if (_equippedViewModel != null)
        {
            Destroy(_equippedViewModel);
            _equippedViewModel = null;
            _equippedViewModelUsable = null;
        }
    }

    public void EquipItemData(ItemData itemData)
    {
        ClearEquippedItem();
        if (itemData == null) return;
        if (handAnchor == null) return;

        _equippedData = itemData;

        GameObject prefab = itemData.firstPersonPrefab != null ? itemData.firstPersonPrefab : itemData.spawnPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[{nameof(FirstPersonHands)}] No prefab assigned for item '{itemData.itemName}'.", this);
            return;
        }

        // Strongly recommended: firstPersonPrefab should NOT contain NetworkObject/NetworkTransform.
        // If it does (or we had to fallback to spawnPrefab), disable those components on the local instance.

        _equippedViewModel = Instantiate(prefab);
        _equippedViewModel.name = $"FP_{prefab.name}";
        _equippedViewModel.transform.SetParent(handAnchor, worldPositionStays: false);

        foreach (var netObj in _equippedViewModel.GetComponentsInChildren<NetworkObject>(true))
        {
            netObj.enabled = false;
        }
        foreach (var netTransform in _equippedViewModel.GetComponentsInChildren<NetworkTransform>(true))
        {
            netTransform.enabled = false;
        }

        // Disable physics/colliders on the local viewmodel.
        foreach (var rb in _equippedViewModel.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (var col in _equippedViewModel.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        // Render in 1st person layer if present.
        SetLayerRecursively(_equippedViewModel, LayerMask.NameToLayer("FirstPersonObjects"));

        // Prefer a dedicated local-only interface. Fall back to GrabbableObject for older prefabs.
        _equippedViewModelUsable = _equippedViewModel.GetComponentInChildren<MonoBehaviour>(true);
        var usables = _equippedViewModel.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < usables.Length; i++)
        {
            if (usables[i] is IFirstPersonViewModelUsable)
            {
                _equippedViewModelUsable = usables[i];
                break;
            }
        }

        ApplyPose(_equippedViewModel.transform, _equippedData);
    }

    public void ForwardItemActivate(bool isDown)
    {
        if (_equippedViewModelUsable == null) return;
        if (_equippedViewModelUsable is IFirstPersonViewModelUsable usable)
        {
            usable.OnUseChanged(isDown);
            return;
        }

        // Backward compatibility: allow viewmodel prefabs that still ship with a GrabbableObject-derived script.
        if (_equippedViewModelUsable is GrabbableObject legacy)
        {
            legacy.ItemActivate(isDown);
        }
    }

    private void ApplyPose(Transform target, ItemData data)
    {
        if (target == null || handAnchor == null) return;

        // Reset Scale (đề phòng model bị méo). Lưu ý: scale chịu ảnh hưởng hierarchy hiện tại.
        target.localScale = Vector3.one;

        if (data != null)
        {
            target.localPosition = data.positionOffset;
            target.localRotation = Quaternion.Euler(data.rotationOffset);
        }
        else
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
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

// Local-only first-person item behaviour (viewmodel). Not networked.
// Implement this on first-person prefabs to react to use input.
public interface IFirstPersonViewModelUsable
{
    void OnUseChanged(bool isDown);
}