using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
    /// <summary>
    /// Class that handles the movement of the background element.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ScrollingRawImage : MonoBehaviour
    {
        RawImage rawImage;
        public float xSpeed, ySpeed;
        private float xVal, yVal;

        void Start()
        {
            rawImage = GetComponent<RawImage>();
        }

        void Update()
        {
            xVal += Time.deltaTime * xSpeed;
            yVal += Time.deltaTime * ySpeed;
            rawImage.uvRect = new Rect(xVal, yVal, rawImage.uvRect.width, rawImage.uvRect.height);
        }
    }
}