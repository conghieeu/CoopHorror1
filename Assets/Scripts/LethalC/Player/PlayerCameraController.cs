using UnityEngine;
using Unity.Netcode;
using StarterAssets; // Giả sử bạn dùng namespace này cho Input

public class PlayerCameraController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private StarterAssetsInputs _input;
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private Camera _mainCamera;

    [Header("Settings")]
    public float sensitivity = 0.2f;
    public float smoothSpeed = 20f;
    public float topClamp = 85f;
    public float bottomClamp = -85f;
    
    // Cài đặt tối ưu mạng (Giống Lethal Company)
    [Header("Network Settings")]
    [SerializeField] private float _networkSendThreshold = 2f; // Chỉ gửi khi xoay lệch quá 2 độ

    // Biến Local (Dùng cho Owner)
    private float _targetPitch;
    private float _targetYaw;

    // Biến Network (Dùng để nhận dữ liệu từ người khác)
    private float _networkPitch;
    private float _networkYaw;

    // Biến lưu trạng thái gửi cuối cùng (Để so sánh)
    private float _lastSentYaw;
    private float _lastSentPitch;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _mainCamera.enabled = true;
            _mainCamera.gameObject.tag = "MainCamera";
            
            // Khởi tạo góc ban đầu
            _targetYaw = transform.rotation.eulerAngles.y;
            _lastSentYaw = _targetYaw;
        }
        else
        {
            _mainCamera.enabled = false;
            if (_mainCamera.TryGetComponent<AudioListener>(out var listener)) listener.enabled = false;
        }
    }

    private void LateUpdate()
    {
        // TÁCH BIỆT LOGIC
        if (IsOwner)
        {
            HandleOwnerRotation();
        }
        else
        {
            HandleClientRotation();
        }
    }

    // --- PHẦN 1: LOGIC CHO CHỦ SỞ HỮU (OWNER) ---
    private void HandleOwnerRotation()
    {
        // 1. Tính toán Input
        if (_input.look.sqrMagnitude >= 0.01f)
        {
            _targetYaw += _input.look.x * sensitivity;
            _targetPitch += _input.look.y * sensitivity;
        }

        // 2. Clamp (Kẹp góc)
        _targetPitch = Mathf.Clamp(_targetPitch, bottomClamp, topClamp);

        // 3. Xoay máy mình NGAY LẬP TỨC (Prediction - Không chờ Server)
        transform.rotation = Quaternion.Euler(0f, _targetYaw, 0f);
        _cameraHolder.localRotation = Quaternion.Euler(_targetPitch, 0f, 0f);

        // 4. KIỂM TRA NGƯỠNG (Threshold Check) - Tối ưu băng thông
        // Nếu góc xoay thay đổi quá 2 độ so với lần gửi cuối cùng thì mới gửi
        if (Mathf.Abs(_targetYaw - _lastSentYaw) > _networkSendThreshold || 
            Mathf.Abs(_targetPitch - _lastSentPitch) > _networkSendThreshold)
        {
            UpdateRotationServerRpc(_targetPitch, _targetYaw);
            
            _lastSentYaw = _targetYaw;
            _lastSentPitch = _targetPitch;
        }
    }

    // --- PHẦN 2: LOGIC CHO NGƯỜI CHƠI KHÁC (CLIENT) ---
    private void HandleClientRotation()
    {
        // Dùng LerpAngle để đuổi theo giá trị _networkYaw nhận được từ Server
        // Điều này giúp che giấu việc gói tin bị gửi chậm (Do tối ưu băng thông ở trên)
        float smoothYaw = Mathf.LerpAngle(transform.rotation.eulerAngles.y, _networkYaw, Time.deltaTime * smoothSpeed);
        float smoothPitch = Mathf.LerpAngle(_cameraHolder.localEulerAngles.x, _networkPitch, Time.deltaTime * smoothSpeed);

        transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
        _cameraHolder.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);
    }

    // --- PHẦN 3: GỬI NHẬN DỮ LIỆU ---
    [ServerRpc]
    private void UpdateRotationServerRpc(float pitch, float yaw)
    {
        // Server nhận được -> Gửi ngay cho tất cả các máy con khác
        UpdateRotationClientRpc(pitch, yaw);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(float pitch, float yaw)
    {
        // Nếu là Owner thì bỏ qua (vì Owner đã tự xoay ở HandleOwnerRotation rồi)
        if (IsOwner) return;

        // Cập nhật biến đích để HandleClientRotation nó Lerp theo
        _networkPitch = pitch;
        _networkYaw = yaw;
    }
}