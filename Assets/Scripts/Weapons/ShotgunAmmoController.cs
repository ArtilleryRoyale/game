using UnityEngine;
using Jrmgx.Helpers;
using CC;
using System.Collections.Generic;
using CC.StreamPlay;

public class ShotgunAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer = default;

    [Header("Config")]
    [SerializeField] private float distance = 5f;

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);
        ScheduleToDestroy(5);
    }

    protected override void Start()
    {
        base.Start();

        if (!IsNetworkOwner) return;

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        // Debugging.DrawRay(myPosition, direction, Color.red, 10);

        var hits = new List<RaycastHit2D>();
        Physics2D.Raycast(myPosition, direction, new ContactFilter2D().NoFilter(), hits, distance);
        foreach (RaycastHit2D hit in hits) {

            var collider = hit.collider;

            if (!ColliderIsValid(collider)) continue;
            if (PhysicLogic.CheckOutOfBounds(collider, out string _)) continue;

            ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
            if (receiveExplosion != null && !explosionHasBeenGenerated) {
                Vector2 hitPosition = hit.point;
                // Debugging.DrawLine(myPosition, hitPosition, Color.green, 10);
                ShotgunSFX(myPosition, hitPosition, success: true);
                NetworkRecordSnapshot(Method_ShotgunSFX, myPosition, hitPosition, true);
                //float damageFactor = 1f - (Basics.VectorBetween(myPosition, hitPosition).magnitude / (distance * 2f));
                // Log.Message("ShotgunAmmoController", "Hit " + collider.name + ", damage factor: 1");
                InstantiateExplosionController(hitPosition);
                this.ExecuteInSecond(0.33f, UnlockAndDestroy);
                return;
            }
        }

        // Log.Message("ShotgunAmmoController", "Miss");
        Vector2 pointB = myPosition + (distance * direction);
        ShotgunSFX(myPosition, pointB, success: false);
        NetworkRecordSnapshot(Method_ShotgunSFX, myPosition, pointB, false);
        InstantiateExplosionController(pointB, 0);
        this.ExecuteInSecond(0.33f, UnlockAndDestroy);
    }

    [StreamPlay(Method_ShotgunSFX)]
    protected void ShotgunSFX(Vector2 pointA, Vector2 pointB, bool success)
    {
        if (IsAISimulationActive) return;
        lineRenderer.SetPosition(0, pointA);
        lineRenderer.SetPosition(1, pointB);
        lineRenderer.gameObject.SetActive(true);
    }

    // Add ExplosionImmunize for the current character
    protected override void InstantiateExplosionController(Vector2 origin, float damageFactor = 1f)
    {
        if (explosionHasBeenGenerated) return;
        // Prevent multiple instantiation if hiting multiple colliders
        explosionHasBeenGenerated = true;
        if (damageFactor < 0) {
            damageFactor = 0;
        }

        var explosion = ExplosionManager.Instance.GetExplosion(origin, IsAISimulationActive);
        if (characterManager != null) {
            characterManager.CharacterMove.ExplosionImmunize(explosion.ExplosionUniqueIdentifier);
        }
        explosion.InitSimulable(weapon.Explosion, damageFactor, weapon, angle, isFacingRight, power);
    }
}
