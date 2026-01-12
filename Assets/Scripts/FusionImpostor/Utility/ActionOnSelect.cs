using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FusionImpostor
{
    /// <summary>
    /// Helper behaviour for assigning events when a UI element is selected or deselected.
    /// </summary>
    public class ActionOnSelect : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public UnityEvent onSelect;
        public UnityEvent onDeselect;

        private void Awake()
        {
            if(TryGetComponent(out HoverOverSelectable h))
            {
                onSelect.AddListener(h.SetHoveredScale);
                onDeselect.AddListener(h.GetDefaultScale);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            onDeselect.Invoke();
        }

        public void OnSelect(BaseEventData eventData)
        {
            onSelect.Invoke();
        }
    }
}