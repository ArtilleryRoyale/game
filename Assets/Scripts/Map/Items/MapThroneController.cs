using UnityEngine;
using System;

public class MapThroneController : MapItemControllerBase, PositionableInterface
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRendererThrone;
    [SerializeField] private SpriteRenderer spriteRendererThroneBackground;
    [SerializeField] private SpriteRenderer spriteRendererArmrest;
    [SerializeField] private Sprite spriteThroneRed;

    public bool IsPlayerOne => hasSymmetry;

    public void SetPlayerOneThrone(bool status) => SetSymmetry(status);

    public override void SetSymmetry(bool status)
    {
        base.SetSymmetry(status);
        if (status) {
            spriteRendererThrone.sprite = spriteThroneRed;
            spriteRendererThroneBackground.sprite = spriteThroneRed;
        }
    }

    public void KingIsCaptured()
    {
        // Log.Message("MapThroneController", "This throne is captured: IsPlayerOne? " + IsPlayerOne);
        spriteRendererThrone.sortingLayerName = "Map";
        spriteRendererThrone.sortingOrder = 72;
        spriteRendererArmrest.enabled = false;
    }
}
