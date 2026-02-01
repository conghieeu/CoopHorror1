using UnityEngine;
using Unity.Netcode;
using StarterAssets;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _interactionDistance = 5f;
    [SerializeField] private LayerMask _interactableLayer;

    [Header("References")]
    [SerializeField] private Camera _playerCamera;

    // Biến lưu trạng thái để vẽ Gizmos
    private RaycastHit _hit;
    private bool _hasHit;

    // Biến logic
    private IInteractable _currentInteractable;
    private float _currentHoldTimer = 0f;

    private void Update()
    {
        // 1. Chỉ chủ sở hữu mới được chạy logic này
        if (!IsOwner) return;

        // 2. Bắn tia tìm vật thể và Cập nhật UI
        HandleRaycastAndUI();

        // 3. Xử lý bấm nút (Nếu đang nhìn thấy vật thể)
        HandleInput();
    }

    private void HandleRaycastAndUI()
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // CHỈ BẮN RAYCAST 1 LẦN DUY NHẤT MỖI KHUNG HÌNH (Tối ưu hiệu năng)
        _hasHit = Physics.Raycast(ray, out _hit, _interactionDistance, _interactableLayer);

        if (_hasHit)
        {
            // Thử lấy Interface từ vật thể va chạm
            // Dùng TryGetComponent sẽ nhanh và gọn hơn
            if (_hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                // -- TÌM THẤY --
                _currentInteractable = interactable;

                // Tính toán % đã giữ phím (để hiển thị vòng tròn xoay xoay)
                float progress = 0f;
                if (interactable.IsHoldable() && interactable.GetHoldDuration() > 0)
                {
                    progress = _currentHoldTimer / interactable.GetHoldDuration();
                }

                // Cập nhật UI: Hiện chữ, Đổi màu tâm, Hiện thanh loading
                if (PlayerInteractionUI.Instance != null)
                {
                    PlayerInteractionUI.Instance.UpdateInteractionUI(true, interactable.GetInteractText(), progress);
                }
                return; // Kết thúc hàm tại đây để không chạy xuống phần Reset bên dưới
            }
        }

        // -- KHÔNG TÌM THẤY GÌ --
        _currentInteractable = null;
        _currentHoldTimer = 0f; // Reset thời gian giữ nếu nhìn ra chỗ khác

        // Tắt UI
        if (PlayerInteractionUI.Instance != null)
        {
            PlayerInteractionUI.Instance.UpdateInteractionUI(false, null, 0f);
        }
    }

    private void HandleInput()
    {
        // Nếu không nhìn vào cái gì thì không cho bấm E
        if (_currentInteractable == null) return;

        // 1. Logic cho vật phẩm cần GIỮ (Hold) - Ví dụ: Kéo van, cứu người
        if (_currentInteractable.IsHoldable())
        {
            if (Input.GetKey(KeyCode.E))
            {
                _currentHoldTimer += Time.deltaTime;

                // Nếu giữ đủ lâu
                if (_currentHoldTimer >= _currentInteractable.GetHoldDuration())
                {
                    _currentInteractable.Interact();
                    _currentHoldTimer = 0f; // Reset sau khi xong để tránh spam
                }
            }
            else
            {
                // Nếu thả tay ra giữa chừng -> Reset về 0
                _currentHoldTimer = 0f;
            }
        }
        // 2. Logic cho vật phẩm BẤM PHÁT ĂN NGAY (Instant) - Ví dụ: Mở cửa, Bật đèn
        else
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                _currentInteractable.Interact();
            }
        }
    }

    // Vẽ tia Debug trong Editor để dễ căn chỉnh
    private void OnDrawGizmos()
    {
        if (_playerCamera == null) return;

        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Gizmos.color = _hasHit ? Color.green : Color.red; // Xanh nếu trúng, Đỏ nếu trượt

        if (_hasHit)
        {
            Gizmos.DrawLine(ray.origin, _hit.point);
            Gizmos.DrawWireSphere(_hit.point, 0.1f);
        }
        else
        {
            Gizmos.DrawRay(ray.origin, ray.direction * _interactionDistance);
        }
    }
}