using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
    /// <summary>
    /// Selects a random icon to be assigned to the specified Image component.
    /// </summary>
    public class RandomIconSelector : MonoBehaviour
    {
        public Sprite[] icons;
        public Image image;

        private void OnEnable()
        {
            SetIcon();
        }

        public void SetIcon()
        {
            image.sprite = icons[Random.Range(0, icons.Length)];
        }
    }
}