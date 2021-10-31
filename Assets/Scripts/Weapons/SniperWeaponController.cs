using UnityEngine;
using Jrmgx.Helpers;
using CC;
using System.Collections.Generic;

public class SniperWeaponController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer = default;

    // Config
    private float defaultLength = 200f;
    private float counterScale = 0f;

    #endregion

    private void Awake()
    {
        counterScale = 1f / transform.localScale.x;
    }

    #region Display Logic

    public void SetAngle(float angle, bool isFacingRight)
    {
        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);

        var hits = new List<RaycastHit2D>();
        Physics2D.Raycast(lineRenderer.transform.position, direction, new ContactFilter2D { useTriggers = false }, hits);
        foreach (RaycastHit2D hit in hits) {

            var collider = hit.collider;

            if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
                continue;
            }

            ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
            if (receiveExplosion != null) {
                Vector2 hitPosition = hit.point;
                // Debugging.DrawLine(lineRenderer.transform.position, hitPosition, Color.cyan, 2);
                var distance = Basics.VectorBetween(lineRenderer.transform.position, hitPosition).magnitude * counterScale;
                // Log.Message("SniperController", "Hit " + collider.name + ", distance: " + distance);
                // I don't really know why the distance is too big but it works quite well with 88%
                lineRenderer.SetPosition(1, new Vector2(distance * 0.88f, 0));
                return;
            }
        }

        lineRenderer.SetPosition(1, new Vector2(defaultLength * counterScale, 0));
    }

    #endregion
}
