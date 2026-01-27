using Unity.Netcode;
using UnityEngine;

// Interface này giữ nguyên (hoặc tự định nghĩa nếu chưa có)
public interface IToggleState 
{ 
    bool IsActive { get; } 
}

public class ToggleRotator : NetworkBehaviour 
{
    [Header("Dependencies")]
    private IToggleState _stateSource; 

    [Header("Settings")]
    public Transform TargetToRotate; 
    public Vector3 OffAngle = Vector3.zero;
    public Vector3 OnAngle = new Vector3(0, 90, 0);
    public float Duration = 1f;
    public AnimationCurve MotionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _currentProgress;

    // Thay thế Spawned()
    public override void OnNetworkSpawn()
    {
        _stateSource = GetComponentInParent<IToggleState>();
        
        if (_stateSource == null)
        {
            Debug.LogError("Thiếu script quản lý trạng thái (IToggleState)!");
            return;
        }

        // Snap trạng thái ban đầu để tránh lerp từ 0 khi mới vào
        _currentProgress = _stateSource.IsActive ? 1f : 0f;
    }

    // Thay thế Render() bằng Update() của Unity
    private void Update()
    {
        // Chỉ chạy logic visual ở Client (Server dedicate không cần render, nhưng Host thì cần)
        // Nếu là Dedicated Server thì return, nếu là Host/Client thì chạy.
        if (IsServer && !IsHost) return; 

        if (_stateSource == null || TargetToRotate == null) return;

        // 1. Đọc dữ liệu (State vẫn được sync qua NetworkVariable ở script khác)
        float target = _stateSource.IsActive ? 1f : 0f;

        // 2. Tính toán Visual
        float step = Time.deltaTime / Duration;
        _currentProgress = Mathf.MoveTowards(_currentProgress, target, step);

        // 3. Xoay
        float curveValue = MotionCurve.Evaluate(_currentProgress);
        TargetToRotate.localRotation = Quaternion.Slerp(Quaternion.Euler(OffAngle), Quaternion.Euler(OnAngle), curveValue);
    }
}