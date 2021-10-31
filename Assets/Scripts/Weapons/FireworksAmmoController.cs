using System.Collections;
using CC;
using CC.StreamPlay;
using FMODUnity;
using Jrmgx.Helpers;
using UnityEngine;

public class FireworksAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private GameObject rocketContainer = default;
    [SerializeField] private GameObject debrisContainer = default;
    [SerializeField] private SFXWeaponFireworksController fireworksExplosion = default;
    [SerializeField] private Rigidbody2D myRigidbody = default;
    [SerializeField] private StudioEventEmitter fireworkStartSound = default;
    [SerializeField] private StudioEventEmitter fireworkMainExplodeSound = default;
    [SerializeField] private StudioEventEmitter fireworkDebrisExplodeSound = default;
    [SerializeField] private StudioEventEmitter fireworkDownSound = default;

    // Config
    public float speed = 40f;
    public int distanceAboveMap = 30;
    public int horizontalExpansion = 2;
    public int verticalExpansion = 1;
    public float globalFactor = 1;

    // State
    private enum PhaseEnum { Launch, Explosion, Debris }
    private PhaseEnum phase = PhaseEnum.Launch;
    private Vector2 particleForce;

    #endregion

    protected override void Awake()
    {
        base.Awake();
        myRigidbody.simulated = false;
        rocketContainer.SetActive(false);
        debrisContainer.SetActive(false);
        fireworksExplosion.gameObject.SetActive(false);
    }

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        // Log.Message("FireworksAmmoController", "Init");
        base.Init(characterManager, weapon, angle, isFacingRight, power);
    }

    protected override void Start()
    {
        base.Start();
        if (!IsNetworkOwner) return;
        // Log.Message("FireworksAmmoController", "Start");
        if (phase == PhaseEnum.Launch) {
            Launch();
            NetworkRecordSnapshot(Method_FireworksLaunch);
        } else {
            Debris();
            Debris_Common();
            NetworkRecordSnapshot(Method_FireworksDebris_Common);
        }
        isActive = true;
        ScheduleToDestroy(10);
        CameraManager.Instance.RequestFollow(transform);
    }

    [StreamPlay(Method_FireworksLaunch)]
    protected void Launch()
    {
        // Log.Message("FireworksAmmoController", "Launch sequence");
        rocketContainer.SetActive(true);
        fireworkStartSound.Play();
    }

    private void Debris()
    {
        var force = particleForce * globalFactor;
        // Log.Message("FireworksAmmoController", "Debris sequence with force: " + force);
        myRigidbody.simulated = true;
        Physics2D.SyncTransforms();
        myRigidbody.AddForce(force + (Vector2.up * 10), ForceMode2D.Impulse);
        myRigidbody.AddTorque(RandomNum.Value, ForceMode2D.Impulse);
    }

    [StreamPlay(Method_FireworksDebris_Common)]
    protected void Debris_Common()
    {
        // Log.Message("FireworksAmmoController", "Debris Common");
        debrisContainer.SetActive(true);
        fireworkDebrisExplodeSound.Play();
        fireworkDownSound.Play();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!isActive) return;

        switch (phase) {
            case PhaseEnum.Launch:
                transform.position += Vector3.up * speed * Time.fixedDeltaTime;
                if (transform.position.y > GameManager.Instance.MapController.CalculatedHeight + distanceAboveMap) {
                    phase = PhaseEnum.Explosion;
                    FireworksExplode();
                }
                break;
            case PhaseEnum.Debris:
                myRigidbody.AddForce(GetWindForce() * weapon.WindMultiplier * Vector2.right);
                break;
        }
    }

    private void FireworksExplode()
    {
        // Log.Message("FireworksAmmoController", "Reached max altitude, will explode");
        phase = PhaseEnum.Explosion;

        FireworksExplode_Common();
        NetworkRecordSnapshot(Method_FireworksExplode_Common);

        StartCoroutine(WaitForInit());
        IEnumerator WaitForInit() {
            var particleForces = new Vector2[]{
                new Vector2(0.1f, 8),
                new Vector2(16, 4),
                new Vector2(-16, 4),
                new Vector2(8, 5),
                new Vector2(-8, 5),
                new Vector2(4, 6),
                new Vector2(-4, 6),
            };
            for (int i = 0; i < particleForces.Length; i++) {
                InstantiateDebris(particleForces[i]);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    [StreamPlay(Method_FireworksExplode_Common)]
    protected void FireworksExplode_Common()
    {
        // Log.Message("FireworksAmmoController", "Explode Common");
        rocketContainer.SetActive(false);
        fireworkStartSound.Stop();
        fireworkMainExplodeSound.Play();
        fireworksExplosion.gameObject.SetActive(true);
        fireworksExplosion.Init();
    }

    private void InstantiateDebris(Vector2 particleForce)
    {
        // Log.Message("FireworksAmmoController", "Instantiate Debris");
        var networkId = NetworkIdentifierNew();
        InitDebris(networkId, particleForce);
        NetworkRecordSnapshot(Method_FireworksInitDebris, networkId, particleForce);
    }

    [StreamPlay(Method_FireworksInitDebris)]
    protected void InitDebris(int networkId, Vector2 particleForce)
    {
        // Log.Message("FireworksAmmoController", "Init Debris");
        Weapon weapon = WeaponManager.Instance.GetWeapon(Weapon.AmmoEnum.Fireworks);
        FireworksAmmoController stub = Instantiate(this, transform.position, transform.rotation);
        stub.NetworkIdentifier = networkId;
        stub.phase = PhaseEnum.Debris;
        stub.particleForce = particleForce;
        // Forcing Network refresh here, as it seems that the one made in Start() will be too late
        StreamPlayPlayer.RefreshOne(stub);

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
