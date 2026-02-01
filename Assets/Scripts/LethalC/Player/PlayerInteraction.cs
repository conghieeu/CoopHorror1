using UnityEngine;
using Unity.Netcode;
using StarterAssets;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _interactionDistance = 5f; // Độ dài tia
    [SerializeField] private LayerMask _interactableLayer;    // Layer của vật thể tương tác
    
    [Header("References")]
    [SerializeField] private Camera _playerCamera;
    
    private RaycastHit _hit;
    private bool _hasHit;

    private void Update()
    {
        if (!IsOwner) return;

        // Bắn tia từ giữa màn hình
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Kiểm tra va chạm mỗi khung hình để vẽ Gizmos và xử lý Input
        _hasHit = Physics.Raycast(ray, out _hit, _interactionDistance, _interactableLayer);

        // Nhấn E để kiểm tra tên vật thể
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_hasHit)
            {
                // In ra tên vật thể va chạm
                Debug.Log("<color=green>Đã tương tác với: </color>" + _hit.collider.gameObject.name);
                
                // Nếu muốn chuyên nghiệp như Lethal Company, bạn có thể gọi Interface ở đây
                var interactable = _hit.collider.GetComponentInParent<IInteractable>();
                interactable?.Interact();
            }
            else
            {
                Debug.Log("<color=yellow>Không có vật thể nào trong tầm với.</color>");
            }
        }
    }

    // Vẽ tia Debug trong Editor
    private void OnDrawGizmos()
    {
        if (_playerCamera == null) return;

        // Thiết lập tia xuất phát từ giữa Camera
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Gizmos.color = Color.green;

        if (_hasHit)
        {
            // Nếu va chạm: Vẽ tia từ camera đến điểm va chạm
            Gizmos.DrawLine(ray.origin, _hit.point);
            // Vẽ một khối cầu nhỏ tại điểm va chạm cho dễ nhìn
            Gizmos.DrawWireSphere(_hit.point, 0.1f);
        }
        else
        {
            // Nếu không va chạm: Vẽ tia đủ độ dài đã set
            Gizmos.DrawRay(ray.origin, ray.direction * _interactionDistance);
        }
    }
}