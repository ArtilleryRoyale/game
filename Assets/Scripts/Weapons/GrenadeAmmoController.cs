using UnityEngine;
using FMODUnity;
using CC;
using CC.StreamPlay;

public class GrenadeAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private Rigidbody2D myRigidbody = default;

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventGrenadeBounce = default;

    #endregion

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);

        var direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        // Log.Message("GrenadeAmmoController", "Init with direction: " + direction + " and power: " + power);

        if (direction.y <= -0.85) { // dropped on the floor, no bounce, full friction, no rotation
            // Log.Message("GrenadeAmmoController", "Full friction");
            myRigidbody.sharedMaterial = droppableFullFrictionMaterial;
            myRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        myRigidbody.AddForce(power * direction * weapon.Speed, ForceMode2D.Impulse);
        myRigidbody.AddTorque(2f, ForceMode2D.Impulse);
        isActive = true;
    }

    protected override void Start()
    {
        base.Start();
        if (!IsNetworkOwner) return;
        ExplodeIn(weapon.ExplodeInSeconds);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;
        // Log.Message("GrenadeAmmoController", "OnCollisionEnter2D");
        Collider2D collider = collision.collider;

        if (PhysicLogic.CheckOutOfBounds(collider, out string _)) {
            UnlockAndDestroy();
            return;
        }

        if (IsAISimulationActive) return; // No sound wanted

        // Calculate intensity
        float intensity = Mathf.Clamp01((myRigidbody.velocity.magnitude - 1f) / 15f);
        if (intensity < 0.05f) return;
        if (soundEventGrenadeBounce.Params.Length < 2) {
            Log.Critical("GrenadeAmmoController", "soundEventGrenadeBounce does not have its two params");
            return;
        }

        bool isGroundedConrete = collider.CompareTag(Config.TagTypePlatform);
        isGroundedConrete = isGroundedConrete || collider.CompareTag(Config.TagTypeStatue);

        GrenadeSound(intensity, isGroundedConrete);
        NetworkRecordSnapshot(Method_GrenadeSound, intensity, isGroundedConrete);
    }

    [StreamPlay(Method_GrenadeSound)]
    protected void GrenadeSound(float intensity, bool isGroundedConrete)
    {
        ParamRef intensityParam = soundEventGrenadeBounce.Params[0];
        intensityParam.Value = intensity;

        ParamRef soilTypeParam = soundEventGrenadeBounce.Params[1];
        soilTypeParam.Value = isGroundedConrete ? 1 : 0;

        soundEventGrenadeBounce.Play();
    }
}
