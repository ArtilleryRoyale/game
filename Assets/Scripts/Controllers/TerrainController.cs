using UnityEngine;
using UnityEngine.U2D;

public class TerrainController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteShape baseSpriteShapeProfile = default;
    [SerializeField] private SpriteShape destructedSpriteShapeProfile = default;
    [SerializeField] public SpriteShapeController frontTerrain = default;
    [SerializeField] public SpriteShapeController backTerrain = default;
    [SerializeField] private SpriteShapeRenderer backRenderer = default;
    [SerializeField] public PolygonCollider2D polygonCollider = default;

    [Header("Styles")]
    [SerializeField] private SpriteShape fireBaseProfile = default;
    [SerializeField] private SpriteShape fireDestructedProfile = default;
    private int fireFillPixelsPerUnit = 40;
    [SerializeField] private SpriteShape iceBaseProfile = default;
    [SerializeField] private SpriteShape iceDestructedProfile = default;
    private int iceFillPixelsPerUnit = 60;
    [SerializeField] private SpriteShape jungleBaseProfile = default;
    [SerializeField] private SpriteShape jungleDestructedProfile = default;
    private int jungleFillPixelsPerUnit = 30;
    [SerializeField] private SpriteShape mossyBaseProfile = default;
    [SerializeField] private SpriteShape mossyDestructedProfile = default;
    private int mossyFillPixelsPerUnit = 10;
    [SerializeField] private SpriteShape rocksBaseProfile = default;
    [SerializeField] private SpriteShape rocksDestructedProfile = default;
    private int rocksFillPixelsPerUnit = 60;

    // State
    private MapController.MapStyle Style = MapController.MapStyle.Jungle;

    private void Awake()
    {
        backRenderer.color = Config.ColorExplosionBorder;
    }

    public void SetStyle(MapController.MapStyle style)
    {
        // Log.Message("TerrainController", "Set stye to: " + style);
        Style = style;
        switch (style) {
            case MapController.MapStyle.Rocks:
                frontTerrain.spriteShape = rocksBaseProfile;
                backTerrain.spriteShape = rocksBaseProfile;
                frontTerrain.fillPixelsPerUnit = rocksFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = rocksFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Fire:
                frontTerrain.spriteShape = fireBaseProfile;
                backTerrain.spriteShape = fireBaseProfile;
                frontTerrain.fillPixelsPerUnit = fireFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = fireFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Ice:
                frontTerrain.spriteShape = iceBaseProfile;
                backTerrain.spriteShape = iceBaseProfile;
                frontTerrain.fillPixelsPerUnit = iceFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = iceFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Mossy:
                frontTerrain.spriteShape = mossyBaseProfile;
                backTerrain.spriteShape = mossyBaseProfile;
                frontTerrain.fillPixelsPerUnit = mossyFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = mossyFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Jungle:
            default:
                frontTerrain.spriteShape = jungleBaseProfile;
                backTerrain.spriteShape = jungleBaseProfile;
                frontTerrain.fillPixelsPerUnit = jungleFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = jungleFillPixelsPerUnit;
                break;
        }
    }

    public void SetDuplicate(MapController.MapStyle style)
    {
        // Log.Message("TerrainController", "Set duplicate to: " + style);
        Style = style;
        switch (Style) {
            case MapController.MapStyle.Rocks:
                frontTerrain.spriteShape = rocksDestructedProfile;
                backTerrain.spriteShape = rocksDestructedProfile;
                frontTerrain.fillPixelsPerUnit = rocksFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = rocksFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Fire:
                frontTerrain.spriteShape = fireDestructedProfile;
                backTerrain.spriteShape = fireDestructedProfile;
                frontTerrain.fillPixelsPerUnit = fireFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = fireFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Ice:
                frontTerrain.spriteShape = iceDestructedProfile;
                backTerrain.spriteShape = iceDestructedProfile;
                frontTerrain.fillPixelsPerUnit = iceFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = iceFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Mossy:
                frontTerrain.spriteShape = mossyDestructedProfile;
                backTerrain.spriteShape = mossyDestructedProfile;
                frontTerrain.fillPixelsPerUnit = mossyFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = mossyFillPixelsPerUnit;
                break;
            case MapController.MapStyle.Jungle:
            default:
                frontTerrain.spriteShape = jungleDestructedProfile;
                backTerrain.spriteShape = jungleDestructedProfile;
                frontTerrain.fillPixelsPerUnit = jungleFillPixelsPerUnit;
                backTerrain.fillPixelsPerUnit = jungleFillPixelsPerUnit;
                break;
        }
    }
}
