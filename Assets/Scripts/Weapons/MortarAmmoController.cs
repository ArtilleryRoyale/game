using UnityEngine;
using Jrmgx.Helpers;
using CC;
using CC.StreamPlay;

public class MortarAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private Rigidbody2D myRigidbody = default;

    // State
    private bool isMainStub = true;

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        // Log.Message("MortarAmmoController", "Init with direction: " + direction + " and power: " + power);

        myRigidbody.AddForce(direction * weapon.Speed, ForceMode2D.Impulse);
        isActive = true;

        ScheduleToDestroy(10); // This is in case the bullet goes wild
    }

    protected override void Start()
    {
        base.Start();
        if (!IsNetworkOwner) return;
        if (isMainStub) {
            InstantiateStub(angle + 0.3f);
            InstantiateStub(angle - 0.3f);
        }
    }

    private void InstantiateStub(float angle)
    {
        var networkId = NetworkIdentifierNew();
        InitStub(networkId, angle, isFacingRight, power);
        NetworkRecordSnapshot(Method_MortarInitStub, networkId, angle, isFacingRight, power);
    }

    [StreamPlay(Method_MortarInitStub)]
    protected void InitStub(int networkId, float angle, bool isFacingRight, float power)
    {
        Weapon weapon = WeaponManager.Instance.GetWeapon(Weapon.AmmoEnum.Mortar);
        MortarAmmoController stub = Instantiate(this, transform.position, transform.rotation);
        stub.NetworkIdentifier = networkId;
        stub.isMainStub = false;

        if (!IsNetworkOwner) return;
        stub.Init(characterManager: null, weapon, angle, isFacingRight, power);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        Collider2D collider = collision.collider;

        ExplosionReceiver receiveExplosion = collider.GetComponentIncludingParent<ExplosionReceiver>();
        if (receiveExplosion != null && !explosionHasBeenGenerated) {
            Vector2 origin = collider.ClosestPoint(transform.position);
            InstantiateExplosionController(origin);
            UnlockAndDestroy();
            return;
        }

        if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
            UnlockAndDestroy();
            return;
        }
    }
}
