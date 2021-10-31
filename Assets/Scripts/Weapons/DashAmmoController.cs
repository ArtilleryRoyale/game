using CC;
using UnityEngine;
using Jrmgx.Helpers;
using DG.Tweening;
using System.Collections.Generic;

public class DashAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private CircleCollider2D circleCollider = default;
    [SerializeField] private SFXSimpleSpawnController dashFire = default;

    // State
    private int distance;
    private Vector2 direction;

    // Touched colliders
    private readonly List<Collider2D> touchedColliders = new List<Collider2D>();

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);
        AssertCharacterManagerNotNullForThisWeapon();

        direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        ScheduleToDestroy(5);

        float duration = .67f;

        var characterColliders = new List<Collider2D>();
        characterManager.Rigidbody.GetAttachedColliders(characterColliders);
        touchedColliders.AddRange(characterColliders);

        InstantiateExplosionControllerMulti(transform.position);

        isActive = true;
        distance = 25;

        // Character dash sequence
        characterManager.DashWeapon(direction, distance, duration);
        // Map dash sequence
        GameManager.Instance.MapController.OnReceiveDash(transform.position, direction, distance, 5f, duration);
        // Weapon dash sequence
        DOTween.To(
            () => transform.position,
            p => transform.position = p,
            transform.position + ((Vector3)direction * distance),
            duration
        ).OnComplete(() => {
            InstantiateExplosionControllerMulti(transform.position);
            UnlockAndDestroy();
        });
    }

    protected override void Start()
    {
        base.Start();
        dashFire.Init();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!isActive) return;

        var colliders = new Collider2D[5];
        var layers = Layer.Character | Layer.Box | Layer.Mine | Layer.Shield;
        circleCollider.OverlapCollider(layers.ContactFilter, colliders);

        if (colliders.Length > 0) {
            foreach (var collider in colliders) {
                if (collider == null) continue;
                DidCollide(collider);
            }
        }
    }

    private void DidCollide(Collider2D collider)
    {
        if (!isActive) return;
        if (touchedColliders.Contains(collider)) {
            // Log.Message("DashAmmoController", "Already collided with " + collider.name + ", skip");
            return;
        }
        touchedColliders.Add(collider);

        // Log.Message("DashAmmoController", "DidCollide with " + collider.name);
        if (!ColliderIsValid(collider)) return;
        if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
            UnlockAndDestroy();
            return;
        }

        if (collider.gameObject.layer == Layer.Shield.Index) {
            // Touched shield, will get that instance and stop it
            var shield = collider.GetComponentInParent<ShieldAmmoController>();
            if (shield) {
                shield.End();
            }
            return;
        }

        ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
        if (receiveExplosion != null) { // Multiple explosion possible
            Vector2 origin = collider.ClosestPoint(transform.position);
            InstantiateExplosionControllerMulti(origin);
            return;
        }
    }

    private void InstantiateExplosionControllerMulti(Vector2 origin)
    {
        var explosion = ExplosionManager.Instance.GetExplosion(origin);
        characterManager.CharacterMove.ExplosionImmunize(explosion.ExplosionUniqueIdentifier);
        explosion.Init(weapon.Explosion);
    }
}
