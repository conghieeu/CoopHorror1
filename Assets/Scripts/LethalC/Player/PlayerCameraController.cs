using UnityEngine;
using Unity.Netcode;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private StarterAssetsInputs _input;
    [SerializeField] private Transform _cameraHolder; // Kéo object chứa Camera vào đây
    [SerializeField] private Camera _mainCamera;      // Camera nằm trong Prefab

    [Header("Settings")]
    public float sensitivity = 0.2f;
    public float smoothSpeed = 20f;
    public float topClamp = 85f;
    public float bottomClamp = -85f;

    private float _targetPitch;
    private float _targetYaw;
    private float _currentPitch;
    private float _currentYaw;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _mainCamera.enabled = true;
            _mainCamera.gameObject.tag = "MainCamera";
            _targetYaw = transform.rotation.eulerAngles.y;
        }
        else
        {
            _mainCamera.enabled = false;
            // Tắt AudioListener của người chơi khác
            if (_mainCamera.TryGetComponent<AudioListener>(out var listener)) listener.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        // 1. Lấy input thô từ script StarterAssetsInputs
        _targetYaw += _input.look.x * sensitivity;
        _targetPitch += _input.look.y * sensitivity;

        // 2. Clamp góc dọc
        _targetPitch = Mathf.Clamp(_targetPitch, bottomClamp, topClamp);

        // 3. Làm mượt (Smoothing) giống Zeekerss làm trong Lethal Company
        _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * smoothSpeed);
        _currentPitch = Mathf.LerpAngle(_currentPitch, _targetPitch, Time.deltaTime * smoothSpeed);

        // 4. Áp dụng xoay cho Local
        transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f); // Xoay thân nhân vật
        _cameraHolder.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f); // Xoay đầu/camera

        // 5. Đồng bộ hóa qua mạng (Chỉ gửi Yaw để tiết kiệm băng thông)
        UpdateRotationServerRpc(_currentYaw);
    }

    [ServerRpc]
    private void UpdateRotationServerRpc(float yaw)
    {
        UpdateRotationClientRpc(yaw);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(float yaw)
    {
        if (IsOwner) return;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}