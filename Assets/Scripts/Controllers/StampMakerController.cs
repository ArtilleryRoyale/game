using Jrmgx.Helpers;
using UnityEngine;

public class StampMakerController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer = default;
    [SerializeField] private SpriteMask spriteMask = default;
    [SerializeField] private Camera makerCamera = default;
    [SerializeField] private RenderTexture renderTexture = default;

    public Sprite ShieldStampedStampSprite(
        Sprite startSprite,
        Vector2 distanceFromStampToShield,
        float radius,
        float maskRatio
    )
    {
        spriteRenderer.sprite = startSprite;
        spriteRenderer.transform.localScale = Vector3.one * radius;

        makerCamera.orthographicSize = 1.28f * radius;

        spriteMask.transform.localScale = Vector3.one * ShieldAmmoController.SHIELD_SIZE * maskRatio;

        spriteMask.transform.localPosition = distanceFromStampToShield + (Vector2.up * ShieldAmmoController.SHIELD_OFFSET);

        Texture2D texture = Basics.RenderTextureTexture(makerCamera);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
