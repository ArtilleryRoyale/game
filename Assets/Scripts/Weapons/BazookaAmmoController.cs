using Jrmgx.Helpers;
using UnityEngine;
using CC;

public class BazookaAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private Rigidbody2D myRigidbody = default;
    [SerializeField] private SFXWeaponBazookaController bazookaSmoke = default;

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);

        // Log.Message("BazookaAmmoController", "Init with angle: " + angle + " and power: " + power);

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        myRigidbody.AddForce(power * direction * weapon.Speed, ForceMode2D.Impulse);
        isActive = true;

        ScheduleToDestroy(10); // This is in case the bullet goes wild
    }

    protected override void Start()
    {
        base.Start();
        if (IsAISimulationActive) return;
        bazookaSmoke.Init();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!isActive) return;

        myRigidbody.AddForce(GetWindForce() * weapon.WindMultiplier * Vector2.right);

        if (IsAISimulationActive) return;

        transform.rotation = Quaternion.FromToRotation(Vector3.right, myRigidbody.velocity.normalized);
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        // Log.Message("BazookaAmmoController", "OnCollisionEnter2D");
        Collider2D collider = collision.collider;

        TryExplode(collider);

        if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
            UnlockAndDestroy();
            return;
        }
    }

    protected void OnTriggerEnter2D(Collider2D collider)
    {
        if (!isActive) return;
        // Log.Message("BazookaAmmoController", "OnTriggerEnter2D");
        if (!ColliderIsValid(collider)) return;
        TryExplode(collider);
    }

    protected void TryExplode(Collider2D collider)
    {
        ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
        if (receiveExplosion != null && !explosionHasBeenGenerated) {
            Vector2 origin = collider.ClosestPoint(transform.position);
            InstantiateExplosionController(origin);
            UnlockAndDestroy();
        }
    }

    protected override void UnlockAndDestroy()
    {
        if (bazookaSmoke != null) {
            bazookaSmoke.DetachAndStop(inSeconds: 2);
            bazookaSmoke = null;
        }
        base.UnlockAndDestroy();
    }
}
