using UnityEngine;
using System;

public class MapBeamController : MapItemControllerBase, PositionableInterface
{
    [SerializeField] public PolygonCollider2D polygonCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer spriteRendererBackground;

    public float Angle { get; private set; }

    /// See MapItemControllerBase Start() method for more info
    protected override void Start()
    {
        if (!IsNetwork) return;
#if CC_DEBUG
        if (NetworkIdentifier == 0) {
            throw new Exception("NetworkIdentifier is not defined for: " + name);
        }
#endif
        StreamPlayPlayer.Refresh();
    }

    /// This is used in training for beam overlay
    private void OnEnable()
    {
        if (!isPredefined) return;
        SetPosition(transform.position);
        isPredefined = false;
    }

    public void Sort(int currentSortingLayer)
    {
        spriteRenderer.sortingOrder = currentSortingLayer - 1;
        spriteRendererBackground.sortingOrder = -currentSortingLayer;
    }

    public void SetAngle(float angle)
    {
        Angle = angle;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
