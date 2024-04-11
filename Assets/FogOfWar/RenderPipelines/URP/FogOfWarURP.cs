using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FoW
{
    [System.Serializable]
    public sealed class FogOfWarStyleParameter : VolumeParameter<FogOfWarStyle>
    {
        public FogOfWarStyleParameter(FogOfWarStyle value, bool overrideState = false) : base(value, overrideState) { }
    }

    [System.Serializable, VolumeComponentMenu("FogOfWarURP")]
    public sealed class FogOfWarURP : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("The team index that will be displayed. This should be the same index specified on the corresponding FogOfWarTeam component.")]
        public IntParameter team = new IntParameter(0);
        [Tooltip("If a pixel is infinitely far away, should it be fogged?")]
        public BoolParameter fogFarPlane = new BoolParameter(true);
        [Range(0.0f, 1.0f), Tooltip("Should areas outside of the map be fogged?")]
        public FloatParameter outsideFogStrength = new FloatParameter(1f);
        [Tooltip("The minimum height that fog can appear.")]
        public FloatParameter minFogHeight = new FloatParameter(-100000);
        [Tooltip("The maximum height that fog can appear.")]
        public FloatParameter maxFogHeight = new FloatParameter(100000);
        [Tooltip("The visual style of the fog.")]
        public FogOfWarStyleParameter style = new FogOfWarStyleParameter(FogOfWarStyle.Linear);

        [Header("Color")]
        [Tooltip("The color of the fog. When using clear fog, the alpha value will determine how transparent the fogged area will be (you usually want the alpha to be zero).")]
        public ColorParameter fogColor = new ColorParameter(Color.clear);
        [Range(0, 1), Tooltip("How visible the partial fog areas should be.")]
        public FloatParameter partialFogAmount = new FloatParameter(0.5f);
        [Tooltip("The texture applied to the fog.")]
        public TextureParameter fogColorTexture = new TextureParameter(null);
        [Tooltip("If true, the texture will be applied in screen space. If false, it will be applied along the fog plane.")]
        public BoolParameter fogTextureScreenSpace = new BoolParameter(false);
        [Tooltip("The uniform scale applied to the fogColorTexture.")]
        public FloatParameter fogColorTextureScale = new FloatParameter(1);
        [Tooltip("The height at which the fogColorTexture will be at. Only applicable when fogTextureScreenSpace is false.")]
        public FloatParameter fogColorTextureHeight = new FloatParameter(0);
    
        public bool IsActive() => Application.isPlaying && fogColor.value.a > 0.001f;

        public bool IsTileCompatible() => false;
    }
}
