using Fusion;
using UnityEngine;

// Script này chỉ lo Visual: Xoay vật thể
public class ToggleRotator : NetworkBehaviour // Dùng NetworkBehaviour để có hàm Spawned và Render
{
    [Header("Dependencies")]
    // Không bắt buộc phải là NetworkToggleState, chỉ cần ai đó có IToggleState là được
    private IToggleState _stateSource; 

    [Header("Settings")]
    public Transform TargetToRotate; // Vật thể cần xoay (Cánh cửa)
    public Vector3 OffAngle = Vector3.zero;
    public Vector3 OnAngle = new Vector3(0, 90, 0);
    public float Duration = 1f;
    public AnimationCurve MotionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _currentProgress;

    public override void Spawned()
    {
        // TỰ ĐỘNG LẮP RÁP
        _stateSource = GetComponent<IToggleState>();
        
        if (_stateSource == null)
        {
            Debug.LogError("Thiếu script quản lý trạng thái (IToggleState)!");
            return;
        }

        // Cập nhật ngay lập tức khi vừa vào game (Snap)
        _currentProgress = _stateSource.IsActive ? 1f : 0f;
    }

    public override void Render()
    {
        if (_stateSource == null || TargetToRotate == null) return;

        // 1. Đọc dữ liệu từ module Logic
        float target = _stateSource.IsActive ? 1f : 0f;

        // 2. Tính toán Visual (mượt mà hóa)
        float step = Time.deltaTime / Duration;
        _currentProgress = Mathf.MoveTowards(_currentProgress, target, step);

        // 3. Xoay
        float curveValue = MotionCurve.Evaluate(_currentProgress);
        TargetToRotate.localRotation = Quaternion.Slerp(Quaternion.Euler(OffAngle), Quaternion.Euler(OnAngle), curveValue);
    }
}