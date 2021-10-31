using System.Collections;
using System.Collections.Generic;
using CC.StreamPlay;
using CC;
using FMODUnity;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Jrmgx.Helpers;

public class MapItemMineController : NetworkObject, RoundLockIdentifier, FloatPackStreamPlay, PositionableInterface, ExplosionReceiver
{
    #region Fields

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer = default;
    [SerializeField] private Sprite mineActivatedSprite = default;
    [SerializeField] private Rigidbody2D myRigidbody = default;
    [SerializeField] private PolygonCollider2D characterDetectionCollider = default;

    [Header("Config")]
    [SerializeField] private Explosion explosion = default;
    private const float ExplodeIn = 2f;

    [Header("SFX")]
    [SerializeField] private StudioEventEmitter soundEventActivated = default;
    [SerializeField] private GameObject particlesContainer = default;

    [Header("Predefined")]
    [SerializeField] private bool isPredefined = false;

    // State
    public int RoundLockIdentifier { get; set; }
    private bool isReceivingExternalForce;
    private bool mineActivated;
    private bool isValid;
    private int restTick;
    private readonly List<int> ReceivedExplosionsIdentifiers = new List<int>();

    // StreamPlay
    private Vector2 previousPosition = Vector2.zero;

    private Vector2 myPosition => transform.position;

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();
        RoundLockIdentifier = RoundLock.NewRoundLockIdentifier();
        myRigidbody.bodyType = RigidbodyType2D.Static;

        Physics2D.SyncTransforms();

        spriteRenderer.enabled = false;
        myRigidbody.simulated = false;
        characterDetectionCollider.enabled = false;
    }

    protected override void Start()
    {
        base.Start();
        if (isPredefined) {
            SetPosition(transform.position);
        }
    }

    #endregion

    #region ExplosionReceiver

    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        if (ReceivedExplosionsIdentifiers.Contains(explosion.ExplosionUniqueIdentifier)) return;
        ReceivedExplosionsIdentifiers.Add(explosion.ExplosionUniqueIdentifier);
        ReceivedExternalForce(force);
    }

    public void ReceivedExternalForce(Vector2 withImpulse)
    {
        // NetworkAssertNotGuest();
        // Log.Message("MineController", "Character received force for bounce: " + withImpulse);
        isReceivingExternalForce = true;
        GameManager.Instance.RoundLock.LockPhysicsMove(this);

        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        myRigidbody.velocity = Vector2.zero;
        myRigidbody.AddForce(withImpulse, ForceMode2D.Impulse);

        NetworkCameraRequestFollow();
        NetworkRecordSnapshot(Method_CameraFollowMine);
    }

    #endregion

    #region Main

    private void CheckForCharacter()
    {
        if (!isValid || mineActivated || !IsNetworkOwner) return;
        var results = new List<Collider2D>();
        if (characterDetectionCollider.OverlapCollider(Layer.Character.ContactFilter, results) > 0) {
            StartCoroutine(StartMine());
        }
    }

    private IEnumerator StartMine()
    {
        // NetworkAssertNotGuest();
        GameManager.Instance.RoundLock.LockChainReaction(this);
        ActivateMine();
        NetworkRecordSnapshot(Method_ActivateMine);
        // We can not know who will inflict damages to who has the mine can play its part
        VoiceManager.Instance.SayNothingThisTurn();
        yield return new WaitForSeconds(ExplodeIn);
        try {
            InstantiateExplosionController(transform.position);
            UnlockAndDestroy();
        } catch (System.Exception e) {
            Log.Critical("MineController", "Exception in StartMine " + e);
        }
    }

    [StreamPlay(Method_ActivateMine)]
    protected void ActivateMine()
    {
        mineActivated = true;
        spriteRenderer.sprite = mineActivatedSprite;
        soundEventActivated.Play();
        particlesContainer.SetActive(true);
    }

    private void InstantiateExplosionController(Vector2 origin)
    {
        ExplosionManager.Instance.GetExplosion(origin).Init(explosion);
    }

    private void UnlockAndDestroy()
    {
        GameManager.Instance.RoundLock.UnlockChainReaction(this);
        GameManager.Instance.RoundLock.UnlockPhysicsMove(this);
        NetworkDestroy();
    }

    #endregion

    #region Physics2D

    public void OnCollisionEnter2D(Collision2D collision)
    {
        // Log.Message("MineController", "OnCollisionEnter2D");
        if (PhysicLogic.CheckOutOfBounds(collision.collider, out string boundTag)) {
            UnlockAndDestroy();
        }
    }

    private void FixedUpdate()
    {
        if (!IsNetworkOwner) return;

        CheckForCharacter();
        TryToRest();
        RecordFixedUpdate();
    }

    private void TryToRest()
    {
        if (isReceivingExternalForce) {
            if (myRigidbody.velocity.magnitude < 2f) {
                restTick++;
                if (restTick > Config.RestTickNeeded) {
                    isReceivingExternalForce = false;
                    DidRest();
                    restTick = 0;
                }
            } else {
                restTick = 0;
            }
        }
    }

    private void DidRest()
    {
        // NetworkAssertNotGuest();
        // Log.Message("MineController", "Rigidbody settled down, sleeping");

        myRigidbody.velocity = Vector2.zero;
        myRigidbody.Sleep();
        myRigidbody.bodyType = RigidbodyType2D.Static;

        GameManager.Instance.RoundLock.UnlockPhysicsMove(this);
    }

    #endregion

    #region Stream Play

    private void RecordFixedUpdate()
    {
        if (!IsNetworkOwner) return;
        if (myPosition != previousPosition) {
            previousPosition = myPosition;
            NetworkRecordFloatPack(myPosition);
        }
    }

    public void OnFloatPack(FloatPack floatPack)
    {
        transform.position = floatPack.NextVector();
    }

    #endregion

    #region PositionableInterface

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        previousPosition = myPosition;

        spriteRenderer.enabled = true;
        myRigidbody.simulated = true;
        characterDetectionCollider.enabled = true;

        Physics2D.SyncTransforms();

        if (isPredefined) {
            OnMapControllerIsValid();
        } else {
#if CC_EXTRA_CARE
try {
#endif
            OnMapReady().Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    private async UniTask OnMapReady()
    {
        await UniTask.WaitUntil(() => GameManager.Instance.MapController.IsReady).CancelOnDestroy(this);
        OnMapControllerIsValid();
    }

    private void OnMapControllerIsValid()
    {
        isValid = true;
    }

    #endregion

    #region Camera

    [StreamPlay(Method_CameraFollowMine)]
    protected void NetworkCameraRequestFollow()
    {
        CameraManager.Instance.RequestFollow(transform);
    }

    #endregion
}
