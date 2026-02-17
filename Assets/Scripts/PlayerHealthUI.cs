using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hieu.Core.Singletons;

public class PlayerHealthUI : Singleton<PlayerHealthUI>
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private Image _damageVignette;

    [Header("Settings")]
    [SerializeField] private float _maxVignetteAlpha = 0.75f;

    private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (_damageVignette != null)
        {
            var c = _damageVignette.color;
            c.a = 0f;
            _damageVignette.color = c;
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Initialize(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealthUI.Initialize: playerHealth is null");
            return;
        }

        if (_healthText == null || _damageVignette == null)
        {
            Debug.LogError("PlayerHealthUI: Missing references. Assign Health Text and Damage Vignette in the Canvas.");
            return;
        }

        if (_playerHealth == playerHealth)
        {
            UpdateUI(_playerHealth.CurrentHealth.Value);
            return;
        }

        Unbind();

        _playerHealth = playerHealth;
        _playerHealth.CurrentHealth.OnValueChanged += HandleHealthChanged;

        UpdateUI(_playerHealth.CurrentHealth.Value);
    }

    public void Unbind()
    {
        if (_playerHealth != null)
        {
            _playerHealth.CurrentHealth.OnValueChanged -= HandleHealthChanged;
            _playerHealth = null;
        }
    }

    private void HandleHealthChanged(int previousValue, int newValue)
    {
        UpdateUI(newValue);
    }

    private void UpdateUI(int healthValue)
    {
        if (_playerHealth == null) return;

        int maxHealth = Mathf.Max(1, _playerHealth.MaxHealth);
        int clampedHealth = Mathf.Clamp(healthValue, 0, maxHealth);

        _healthText.text = $"HP: {clampedHealth}";

        float normalized = clampedHealth / (float)maxHealth;
        float targetAlpha = (1f - normalized) * _maxVignetteAlpha;

        var c = _damageVignette.color;
        c.a = targetAlpha;
        _damageVignette.color = c;
    }
}
