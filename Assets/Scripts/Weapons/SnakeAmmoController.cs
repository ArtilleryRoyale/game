using UnityEngine;
using Jrmgx.Helpers;
using CC;

public class SnakeAmmoController : AmmoControllerBase
{
    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);

        ScheduleToDestroy(2);
        // Debug.DrawRay(transform.position, direction, Color.green, 10);

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction);
        foreach (RaycastHit2D hit in hits) {

            var collider = hit.collider;

            if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
                UnlockAndDestroy();
                return;
            }

            ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
            if (receiveExplosion != null && !explosionHasBeenGenerated) {
                Vector2 origin = hit.point;
                InstantiateExplosionController(
                    origin,
                    damageFactor: 1f - (Basics.VectorBetween(transform.position, origin).magnitude / 100f)
                );
                UnlockAndDestroy();
                return;
            }
        }
    }
}
