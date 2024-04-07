using UnityEngine;

namespace FoW
{
    class FogOfWarLegacyManager : FogOfWarPostProcessManager
    {
        Material _material;
        RenderTexture _source = null;
        RenderTexture _destination = null;

        public FogOfWarLegacyManager()
        {
            _material = new Material(FogOfWarUtils.FindShader("Hidden/FogOfWarLegacy"));
            _material.name = "FogOfWarLegacy";
        }

        public void Setup(RenderTexture source, RenderTexture destination)
        {
            _source = source;
            _destination = destination;
        }

        protected override void SetTexture(int id, Texture value) { _material.SetTexture(id, value); }
        protected override void SetVector(int id, Vector4 value) { _material.SetVector(id, value); }
        protected override void SetColor(int id, Color value) { _material.SetColor(id, value); }
        protected override void SetFloat(int id, float value) { _material.SetFloat(id, value); }
        protected override void SetMatrix(int id, Matrix4x4 value) { _material.SetMatrix(id, value); }
        protected override void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
                _material.EnableKeyword(keyword);
            else
                _material.DisableKeyword(keyword);
        }

        protected override void GetTargetSize(out int width, out int height, out int depth)
        {
            width = _source.width;
            height = _source.height;
            depth = _source.depth;
        }

        protected override void BlitToScreen()
        {
#if !UNITY_2021_1_OR_NEWER
            if (_destination != null)
                _destination.MarkRestoreExpected();
#endif
            Graphics.Blit(_source, _destination, _material);
        }
    }

    [AddComponentMenu("FogOfWar/FogOfWarLegacy")]
    public class FogOfWarLegacy : MonoBehaviour
    {
        [Tooltip("The team index that will be displayed. This should be the same index specified on the corresponding FogOfWarTeam component.")]
        public int team = 0;
        [Tooltip("If a pixel is infinitely far away, should it be fogged?")]
        public bool fogFarPlane = true;
        [Range(0.0f, 1.0f), Tooltip("Should areas outside of the map be fogged?")]
        public float outsideFogStrength = 1;
        [Tooltip("The minimum height that fog can appear.")]
        public float minFogHeight = float.NegativeInfinity;
        [Tooltip("The maximum height that fog can appear.")]
        public float maxFogHeight = float.PositiveInfinity;
        [Tooltip("The visual style of the fog.")]
        public FogOfWarStyle style = FogOfWarStyle.Linear;

        [Header("Color")]
        [Tooltip("The color of the fog. When using clear fog, the alpha value will determine how transparent the fogged area will be (you usually want the alpha to be zero).")]
        public Color fogColor = Color.black;
        [Range(0, 1), Tooltip("How visible the partial fog areas should be.")]
        public float partialFogAmount = 0.5f;
        [Tooltip("The texture applied to the fog.")]
        public Texture2D fogColorTexture = null;
        [EnableIf(nameof(fogColorTexture), EnableIfComparison.NotEqual, new object[] { null }), Tooltip("If true, the texture will be applied in screen space. If false, it will be applied along the fog plane.")]
        public bool fogTextureScreenSpace = false;
        [EnableIf(nameof(fogColorTexture), EnableIfComparison.NotEqual, new object[] { null }), Tooltip("The uniform scale applied to the fogColorTexture.")]
        public float fogColorTextureScale = 1;
        [EnableIf(nameof(fogColorTexture), EnableIfComparison.NotEqual, new object[] { null }), Tooltip("The height at which the fogColorTexture will be at. Only applicable when fogTextureScreenSpace is false.")]
        public float fogColorTextureHeight = 0;

        // core stuff
        FogOfWarLegacyManager _postProcess = null;
        Camera _camera;

        void Awake()
        {
            _postProcess = new FogOfWarLegacyManager();
        }

        void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode |= DepthTextureMode.Depth;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            _postProcess.Setup(source, destination);
            _postProcess.team = team;
            _postProcess.camera = _camera;
            _postProcess.fogFarPlane = fogFarPlane;
            _postProcess.outsideFogStrength = outsideFogStrength;
            _postProcess.fogHeightMin = minFogHeight;
            _postProcess.fogHeightMax = maxFogHeight;
            _postProcess.style = style;
            _postProcess.fogColor = fogColor;
            _postProcess.partialFogAmount = partialFogAmount;
            _postProcess.fogColorTexture = fogColorTexture;
            _postProcess.fogTextureScreenSpace = fogTextureScreenSpace;
            _postProcess.fogColorTextureScale = fogColorTextureScale;
            _postProcess.fogColorTextureHeight = fogColorTextureHeight;
            _postProcess.Render();
        }
    }
}
