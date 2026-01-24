using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ToggleMove : NetworkBehaviour // Dùng NetworkBehaviour để có hàm Spawned và Render
{
    [Header("Dependencies")]
    // Không bắt buộc phải là NetworkToggleState, chỉ cần ai đó có IToggleState là được
    private IToggleState _stateSource; 

    [Header("Settings")]
    public Transform TargetToMove; // Vật thể cần di chuyển
    public Vector3 OffsetPosition = new Vector3(0, 2, 0); // Khoảng cách di chuyển khi bật
    public float Duration = 1f;
    public AnimationCurve MotionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _initialPosition; // Lưu vị trí ban đầu
    private float _currentProgress;

    public override void Spawned()
    {
        // TỰ ĐỘNG LẮP RÁP
        _stateSource = GetComponentInParent<IToggleState>();
        
        if (_stateSource == null)
        {
            Debug.LogError("Thiếu script quản lý trạng thái (IToggleState)!");
            return;
        }

        // Lưu vị trí ban đầu
        if (TargetToMove != null)
        {
            _initialPosition = TargetToMove.localPosition;
        }

        // Cập nhật ngay lập tức khi vừa vào game (Snap)
        _currentProgress = _stateSource.IsActive ? 1f : 0f;
    }

    public override void Render()
    {
        if (_stateSource == null || TargetToMove == null) return;

        // 1. Đọc dữ liệu từ module Logic
        float target = _stateSource.IsActive ? 1f : 0f;

        // 2. Tính toán Visual (mượt mà hóa)
        float step = Time.deltaTime / Duration;
        _currentProgress = Mathf.MoveTowards(_currentProgress, target, step);

        // 3. Di chuyển
        float curveValue = MotionCurve.Evaluate(_currentProgress);
        Vector3 targetPosition = _initialPosition + OffsetPosition;
        TargetToMove.localPosition = Vector3.Lerp(_initialPosition, targetPosition, curveValue);
    }
}
