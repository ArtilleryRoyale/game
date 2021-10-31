using System.Collections.Generic;
using CC;
using Jrmgx.Helpers;
using UnityEngine;

public class BeamWeaponController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private Transform beamTransform = default;
    [SerializeField] private SpriteRenderer spriteRenderer = default;
    [SerializeField] private Transform rotateTransform = default;
    [SerializeField] private PolygonCollider2D polygonCollider = default;

    // State
    private float angle;

    #endregion

    #region Display Logic

    public void SetAngle(float angle, bool isFacingRight)
    {
        this.angle = angle;
        rotateTransform.rotation = Quaternion.Euler(0, 0, angle * (isFacingRight ? 1f : -1f));
        bool positionValid = IsBeamPositionValid(angle, isFacingRight);
        if (!positionValid) {
            spriteRenderer.color = Color.red.WithA(0.5f);
            return;
        }
        spriteRenderer.color = Mathf.Abs(angle) < CharacterMove.MaxSlope - 3 ? Color.white : new Color(1, 1, 0, 0.5f);
    }

    #endregion

    #region Action Logic

    public bool IsBeamPositionValid(float angle, bool isFacingRight)
    {
        var results = new List<Collider2D>();
        int count = polygonCollider.OverlapCollider(Layer.Mine.ExceptThatContactFilter, results);
        return count == 0;
    }

    public void ConfirmPosition(bool isFacingRight)
    {
        GameManager.Instance.MapController.PositionBeam(transform.position, angle, isFacingRight,
            distance: beamTransform.localPosition.x * beamTransform.lossyScale.y
        );
    }

    #endregion
}
