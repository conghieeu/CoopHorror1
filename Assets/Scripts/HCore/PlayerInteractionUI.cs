using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nhớ Import TMP Essentials
using Hieu.Core.Singletons;

public class PlayerInteractionUI : Singleton<PlayerInteractionUI>
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI _interactionText;
    [SerializeField] private Image _crosshairImage;
    [SerializeField] private Image _progressCircle; // (Tùy chọn) Vòng tròn xoay khi giữ E

    [Header("Settings")]
    [SerializeField] private Color _defaultColor = new Color(1, 1, 1, 0.5f); // Trắng mờ
    [SerializeField] private Color _highlightColor = Color.green; // Xanh khi trúng
    [SerializeField] private float _highlightScale = 1.2f; // Phóng to 1.2 lần

    private void Start()
    {
        // Reset trạng thái ban đầu
        UpdateInteractionUI(false);
    }

    // Hàm này được Player gọi mỗi khung hình
    public void UpdateInteractionUI(bool isInteractable, string text = "", float holdProgress = 0f)
    {
        // 1. Xử lý Crosshair (Màu sắc & Kích thước)
        if (isInteractable)
        {
            _crosshairImage.color = _highlightColor;
            _crosshairImage.transform.localScale = Vector3.one * _highlightScale;
        }
        else
        {
            _crosshairImage.color = _defaultColor;
            _crosshairImage.transform.localScale = Vector3.one;
        }

        // 2. Xử lý Text
        if (_interactionText != null)
        {
            if (string.IsNullOrEmpty(text))
            {
                _interactionText.text = "";
                _interactionText.gameObject.SetActive(false);
            }
            else
            {
                _interactionText.text = text + " [E]";
                _interactionText.gameObject.SetActive(true);
            }
        }

        // 3. Xử lý Vòng tròn tiến độ (Nếu có)
        if (_progressCircle != null)
        {
            _progressCircle.fillAmount = holdProgress;
            _progressCircle.gameObject.SetActive(holdProgress > 0);
        }
    }
}