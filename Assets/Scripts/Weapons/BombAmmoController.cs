using UnityEngine;
using Jrmgx.Helpers;
using CC;
using FMODUnity;

public class BombAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private Rigidbody2D myRigidbody = default;
    [SerializeField] private SpriteRenderer spriteRenderer = default;
    [SerializeField] private Sprite bombActivatedSprite = default;

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventBombBounce = default; // TODO prio 1 use this

    // State
    bool willExplodeTriggered;

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);

        if (direction.y <= -0.85) { // dropped on the floor, no bounce, full friction, no rotation
            // Log.Message("GrenadeAmmoController", "Full friction");
            myRigidbody.sharedMaterial = droppableFullFrictionMaterial;
            myRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        myRigidbody.AddForce(power * direction * weapon.Speed, ForceMode2D.Impulse);
        isActive = true;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        // Log.Message("BombAmmoController", "OnCollisionEnter2D");
        Collider2D collider = collision.collider;

        if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
            UnlockAndDestroy();
            return;
        }

        // TODO prio 1 Sound
        // Use the code from grenade logic controller (move it up in baseWeapon?)

        ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
        if (receiveExplosion != null && !explosionHasBeenGenerated && !willExplodeTriggered) {
            willExplodeTriggered = true;
            spriteRenderer.sprite = bombActivatedSprite;
            ExplodeIn(weapon.ExplodeInSeconds);
        }
    }
}
