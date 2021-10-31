using UnityEngine;
using FMODUnity;
using CC;

public class StickyGrenadeAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private Rigidbody2D myRigidbody = default;

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventGrenadeStick = default; // TODO prio 2 make and use it

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        // Log.Message("StickyGrenadeAmmoController", "Init with direction: " + direction + " and power: " + power);

        myRigidbody.AddForce(power * direction * weapon.Speed, ForceMode2D.Impulse);
        myRigidbody.AddTorque(1f, ForceMode2D.Impulse);
        isActive = true;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;

        // Log.Message("StickyGrenadeAmmoController", "OnCollisionEnter2D");
        Collider2D collider = collision.collider;

        CharacterManager colliderCharacter = collider.GetComponentInParent<CharacterManager>();
        if (colliderCharacter != null && colliderCharacter == characterManager) {
            // Log.Message("StickyGrenadeAmmo", "Prevent sticking to my self");
            return;
        }

        // TODO prio 7 we could make the bounds limit visible with some dashed line and make objects
        // going out of bound explode for a better consistency
        if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
            UnlockAndDestroy();
            return;
        }

        // Stop rigidbody
        myRigidbody.velocity = Vector3.zero;
        myRigidbody.angularVelocity = 0f;
        myRigidbody.bodyType = RigidbodyType2D.Static;

        isActive = false; // This does not prevent explosion and timer to run
        explodeTime = Time.fixedTime + 2;

        if (IsAISimulationActive) return; // No sound wanted

        // TODO prio 2 sound is not in sync in the network, see Grenade
        // soundEventGrenadeStick.Play();
    }
}
