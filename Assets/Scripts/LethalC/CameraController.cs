using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public StarterAssetsInputs input; // Kéo script của bạn vào đây
    public GameObject cinemachineTarget;
    public float sensitivity = 1.5f;

    private float _pitch;
    private float _yaw;

    private void Update()
    {
        // Chuyên gia sẽ đọc biến 'look' từ script bạn đã viết
        if (input.look.sqrMagnitude >= 0.01f)
        {
            _yaw += input.look.x * sensitivity;
            _pitch += input.look.y * sensitivity; // Trục Y thường bị ngược nên dùng dấu trừ
        }

        // Giới hạn góc nhìn dọc
        _pitch = Mathf.Clamp(_pitch, -30f, 70f);

        // Áp dụng xoay
        cinemachineTarget.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
    }
}