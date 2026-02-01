using UnityEngine;

public interface IInteractable
{
    // Chữ hiện lên màn hình (VD: "Mở cửa", "Sửa máy")
    string GetInteractText();

    // Hành động khi tương tác thành công
    void Interact();

    // --- Phần mở rộng cho tính năng HOLD (Giữ phím) ---
    // Vật này có cần giữ phím không? (Mặc định là false)
    bool IsHoldable() => false; 

    // Cần giữ trong bao lâu? (Mặc định 0 giây)
    float GetHoldDuration() => 0f;
}