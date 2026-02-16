using UnityEngine;

// Stub types for HDRP compatibility (project uses URP)
// These provide empty implementations so code that references HDRP types can compile

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Stub for HDRP LocalVolumetricFog. Does nothing in URP.
    /// </summary>
    [System.Serializable]
    public struct LocalVolumetricFogArtistParameters
    {
        public float meanFreePath;
        public float blendingDistance;
    }

    public class LocalVolumetricFog : MonoBehaviour
    {
        public LocalVolumetricFogArtistParameters parameters = new LocalVolumetricFogArtistParameters
        {
            meanFreePath = 10f,
            blendingDistance = 0f
        };

        public float meanFreePath
        {
            get => parameters.meanFreePath;
            set => parameters.meanFreePath = value;
        }
        public float blendingDistance
        {
            get => parameters.blendingDistance;
            set => parameters.blendingDistance = value;
        }
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// Stub for HDRP HDAdditionalLightData. Does nothing in URP.
    /// </summary>
    public class HDAdditionalLightData : MonoBehaviour
    {
        public float lightDimmer = 1f;
        public float intensity = 1f;
        public float volumetricDimmer = 1f;
    }

    /// <summary>
    /// Stub for HDRP DecalProjector. Provides the API surface used by spray paint and blood decals.
    /// </summary>
    public class DecalProjector : MonoBehaviour
    {
        public Material material;
        public float fadeFactor = 1f;
        public float drawDistance = 1000f;
        public float size = 1f;
        public DecalLayerEnum decalLayerMask = DecalLayerEnum.DecalLayerDefault;
    }

    /// <summary>
    /// Stub for HDRP DecalLayerEnum flags.
    /// </summary>
    [System.Flags]
    public enum DecalLayerEnum
    {
        Nothing = 0,
        DecalLayerDefault = 1,
        DecalLayer1 = 2,
        DecalLayer2 = 4,
        DecalLayer3 = 8,
        DecalLayer4 = 16,
        DecalLayer5 = 32,
        DecalLayer6 = 64,
        DecalLayer7 = 128,
        Everything = 255
    }
}

namespace UnityEngine.Rendering.Universal
{
    // URP stub for DepthOfField if not already provided by URP
}

// Stub for post-processing types used in settings
public class DepthOfField : UnityEngine.Rendering.VolumeComponent
{
    public UnityEngine.Rendering.MinFloatParameter nearFocusEnd = new UnityEngine.Rendering.MinFloatParameter(0.2f, 0f);
}

public class LiftGammaGain : UnityEngine.Rendering.VolumeComponent
{
    public UnityEngine.Rendering.Vector4Parameter gamma = new UnityEngine.Rendering.Vector4Parameter(Vector4.zero);
}