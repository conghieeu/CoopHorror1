using UnityEngine;

/// <summary>
/// Manages first-person hand visuals for the local player.
/// This is a placeholder that should be implemented with your actual hand model logic.
/// </summary>
public class FirstPersonHands : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handTransform;
    
    private GrabbableObject _currentItem;
    private GameObject _viewModel;

    /// <summary>
    /// Equip an item - show it in first person view
    /// </summary>
    public virtual void EquipItem(GrabbableObject item)
    {
        ClearEquippedItem();
        _currentItem = item;
        // TODO: Instantiate first-person viewmodel for the item
    }

    /// <summary>
    /// Clear the equipped item visual
    /// </summary>
    public virtual void ClearEquippedItem()
    {
        _currentItem = null;
        if (_viewModel != null)
        {
            Destroy(_viewModel);
            _viewModel = null;
        }
    }

    /// <summary>
    /// Forward item activation (use) to the visual
    /// </summary>
    public virtual void ForwardItemActivate(bool isDown)
    {
        // TODO: Play use animation on the first-person viewmodel
    }

    /// <summary>
    /// Show hands with the currently held item
    /// </summary>
    public virtual void ShowItem(GrabbableObject item)
    {
        EquipItem(item);
    }

    /// <summary>
    /// Hide hands/item visuals
    /// </summary>
    public virtual void HideItem()
    {
        ClearEquippedItem();
    }

    /// <summary>
    /// Update the held item visual
    /// </summary>
    public virtual void UpdateHeldItem(GrabbableObject item)
    {
        if (_currentItem != item)
        {
            EquipItem(item);
        }
    }
}
