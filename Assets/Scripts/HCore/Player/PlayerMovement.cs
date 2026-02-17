using UnityEngine;
using Unity.Netcode; 

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController _controller;
    [SerializeField] private StarterAssetsInputs _input;

    [Header("Physical Status")]
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float currentStamina;
    [SerializeField] private float staminaRegenRate = 2f;
    [SerializeField] private float staminaDrainRate = 3f;
    [SerializeField] private float currentWeight = 1f;
    [SerializeField] private bool isExhausted;
    [SerializeField, Range(0.05f, 1f)] private float exhaustedRecoverThreshold = 0.2f;

    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float sprintSpeed = 8.0f;
    public float speedChangeRate = 12.5f; // Độ nhạy khi tăng tốc/dừng lại (Inertia)
    
    [Header("Physics")]
    public float gravity = -15.0f;
    public float jumpHeight = 1.2f;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // Các biến lưu trữ trạng thái để làm mượt
    private float _speed;
    private float _animationBlend;

    public void SetCarryWeight(float weight)
    {
        currentWeight = Mathf.Clamp(weight, 1f, 10f);
    }
    
    public override void OnNetworkSpawn()
    {
        maxStamina = Mathf.Max(0.01f, maxStamina);
        currentStamina = Mathf.Clamp(currentStamina <= 0f ? maxStamina : currentStamina, 0f, maxStamina);
        currentWeight = Mathf.Clamp(currentWeight, 1f, 10f);

        // Chỉ bật CharacterController nếu là chủ sở hữu để tránh xung đột vật lý
        if (!IsOwner)
        {
            _controller.enabled = false;
        }
    }

    private void Update()
    {
        // Quan trọng: Chỉ chủ sở hữu mới xử lý di chuyển (Client-authoritative)
        if (!IsOwner) return;

        UpdateStamina();

        ApplyGravity();
        ApplyJump();
        Move();
    }

    private void UpdateStamina()
    {
        // "Thực sự di chuyển" theo velocity (theo yêu cầu)
        float horizontalVelocityMagnitude = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        bool isActuallyMoving = horizontalVelocityMagnitude > 0.1f;
        bool wantsSprint = _input.sprint;

        if (wantsSprint && isActuallyMoving && !isExhausted)
        {
            currentStamina -= staminaDrainRate * currentWeight * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
            }

            if (isExhausted && currentStamina >= maxStamina * exhaustedRecoverThreshold)
            {
                isExhausted = false;
            }
        }
    }

    private void Move()
    {
        // 1. Xác định tốc độ mục tiêu dựa trên trạng thái Sprint + Stamina
        bool canSprint = _input.sprint && !isExhausted && currentStamina > 0f;
        float baseSpeed = canSprint ? sprintSpeed : moveSpeed;

        // Mang càng nặng đi càng chậm (không bao giờ đứng yên vì weight đã bị kẹp >= 1)
        float targetSpeed = baseSpeed / currentWeight;

        // Nếu không nhấn nút di chuyển, tốc độ mục tiêu là 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // 2. Tạo quán tính (Inertia) giống Lethal Company
        // Giúp nhân vật không dừng khựng lại ngay lập tức
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * speedChangeRate);
        }
        else
        {
            _speed = targetSpeed;
        }

        // 3. Tính toán hướng di chuyển dựa trên hướng của nhân vật (Yaw đã xoay từ chuột)
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        if (_input.move != Vector2.zero)
        {
            // Chuyển hướng input từ Local sang World Space
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

        // 4. Thực hiện di chuyển thông qua CharacterController
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void ApplyJump()
    {
        if (_controller.isGrounded)
        {
            if (_input.jump && _verticalVelocity < 0.0f)
            {
                // Công thức vật lý chuẩn: v = sqrt(h * -2 * g)
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        // Reset trạng thái jump trong input sau khi đã xử lý
        _input.jump = false;
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0.0f)
        {
            _verticalVelocity = -2f; // Giữ nhân vật dính sát mặt đất
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }
    }
}