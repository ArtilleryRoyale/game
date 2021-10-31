using UnityEngine;
using Jrmgx.Helpers;
using CC;
using FMODUnity;

public class MeteorAmmoController : AmmoControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private Rigidbody2D myRigidbody = default;
    [SerializeField] private GameObject Object = default;
    [SerializeField] private SpriteRenderer meteorSpriteRenderer = default;
    [SerializeField] private SFXWeaponBazookaController meteorSmoke = default;
    [SerializeField] private StudioEventEmitter meteorSound = default;

    // State
    private Vector2 direction;

    #endregion

    protected override void Awake()
    {
        base.Awake();
        Object.SetActive(false);
        myRigidbody.bodyType = RigidbodyType2D.Static;
    }

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);
        AssertCharacterManagerNotNullForThisWeapon();

        float height = GameManager.Instance.MapController.CalculatedHeight + 20;
        float aBit = (RandomNum.Value - 0.5f) * 10;
        float bBit = (RandomNum.Value - 0.5f) * 10;

        Vector2 top = new Vector2(characterManager.MyPosition.x + aBit, height);
        Vector2 bot = new Vector2(characterManager.MyPosition.x + bBit, 0);
        direction = Basics.VectorBetween(top, bot).normalized;

        // Log.Message("MeteorAmmoController", "Init with positions: " + top + "/" + bot + " direction: " + direction);

        transform.position = top;
        transform.rotation = Quaternion.FromToRotation(Vector3.down, direction);
        Physics2D.SyncTransforms();

        isActive = true;
        ScheduleToDestroy(10); // This is in case the bullet goes wild
    }

    protected override void Start()
    {
        base.Start();
        Object.SetActive(true);
        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        if (IsAISimulationActive) return;
        meteorSmoke.Init();
        meteorSound.Play();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!isActive) return;

        myRigidbody.AddForce(direction * weapon.Speed);
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

    protected override void UnlockAndDestroy()
    {
        if (meteorSmoke != null) {
            meteorSmoke.DetachAndStop(inSeconds: 10);
            meteorSmoke = null;
        }
        base.UnlockAndDestroy();
    }
}
