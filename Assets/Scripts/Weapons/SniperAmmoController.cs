using UnityEngine;
using Jrmgx.Helpers;
using CC;
using System.Collections.Generic;
using CC.StreamPlay;
using FMODUnity;

public class SniperAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private StudioEventEmitter soundEventFireMiss = default;

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
        Physics2D.Raycast(myPosition, direction, new ContactFilter2D().NoFilter(), hits);
        foreach (RaycastHit2D hit in hits) {

            var collider = hit.collider;

            if (!ColliderIsValid(collider)) continue;
            if (PhysicLogic.CheckOutOfBounds(collider, out string _)) continue;

            ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
            if (receiveExplosion != null && !explosionHasBeenGenerated) {
                Vector2 hitPosition = hit.point;
                // Debugging.DrawLine(myPosition, hitPosition, Color.green, 10);
                SniperSFX(myPosition, hitPosition, success: true);
                NetworkRecordSnapshot(Method_SniperSFX, myPosition, hitPosition, true);
                InstantiateExplosionController(hitPosition);
                this.ExecuteInSecond(0.33f, UnlockAndDestroy);
                return;
            }
        }

        // Log.Message("SniperAmmoController", "Miss");
        Vector2 far = myPosition + (30f * direction);
        SniperSFX(myPosition, far, success: false);
        NetworkRecordSnapshot(Method_SniperSFX, myPosition, far, false);
        this.ExecuteInSecond(0.33f, UnlockAndDestroy);
    }

    [StreamPlay(Method_SniperSFX)]
    protected void SniperSFX(Vector2 pointA, Vector2 pointB, bool success)
    {
        if (IsAISimulationActive) return;
        soundEventFireMiss.Play();
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
