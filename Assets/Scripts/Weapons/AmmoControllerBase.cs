using UnityEngine;
using Jrmgx.Helpers;
using CC.StreamPlay;
using CC;
using TMPro;

abstract public class AmmoControllerBase : NetworkObject, AmmoInterface, FloatPackStreamPlay, RoundLockIdentifier
{
    #region Fields

    [Header("Prefabs")]
    [SerializeField] private TextMeshPro timerTextPrefab = default;

    [Header("References")]
    // Used when a droppable is dropped on the floor, that way it does not roll or bounce
    [SerializeField] protected PhysicsMaterial2D droppableFullFrictionMaterial = default;

    protected Weapon weapon;
    protected TextMeshPro timerText;

    public int RoundLockIdentifier { get; set; }

    // State
    public bool IsAISimulationActive { get; set; }
    protected bool explosionHasBeenGenerated;
    protected bool isDestroyed;
    protected bool isActive;
    protected float initTime;
    protected float explodeTime;
    protected float destroyTime;
    protected float angle;
    protected float power;
    protected bool isFacingRight;
    /// <summary>
    /// Note: characterManager can be null
    /// For example: comming from a box or from a stub
    /// </summary>
    protected CharacterManager characterManager;

    protected Vector2 myPosition => transform.position;

    #endregion

    protected override void Awake()
    {
        if (IsAISimulationActive) return;
        base.Awake();
        RoundLockIdentifier = RoundLock.NewRoundLockIdentifier();
    }

    public virtual void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        this.characterManager = characterManager;
        this.angle = angle;
        this.power = power;
        this.initTime = Time.fixedTime;
        this.weapon = weapon;
        this.isFacingRight = isFacingRight;

        if (IsAISimulationActive) {
            foreach (Renderer component in gameObject.GetComponentsInChildren<Renderer>()) {
                component.enabled = false;
            }
            return;
        }

        // NetworkAssertNotGuest();
        GameManager.Instance.RoundLock.LockChainReaction(this);
    }

    protected void ScheduleToDestroy(float seconds)
    {
        destroyTime = Time.fixedTime + seconds;
    }

    protected void ExplodeIn(float seconds)
    {
        // Log.Message("AmmoController", "Will explode in " + seconds + " sec");
        explodeTime = Time.fixedTime + seconds;
        if (IsAISimulationActive) return;
        if (timerTextPrefab == null) {
            Log.Error("AmmoController", "Called WeaponAmmoController::ShowTimer() but timerTextPrefab is not set");
            return;
        }
        ShowTimer();
        NetworkRecordSnapshot(Method_ShowTimer);
    }

    [StreamPlay(Method_ShowTimer)]
    protected void ShowTimer()
    {
        timerText = Instantiate(timerTextPrefab, GameManager.Instance.transform);
        UpdateTimer();
    }

    protected void UpdateTimer()
    {
        if (timerText == null) return;
        timerText.transform.position = transform.position + Vector3.one;
        timerText.text = "" + Mathf.FloorToInt(1 + explodeTime - Time.fixedTime);
    }

    protected virtual void UnlockAndDestroy()
    {
        // NetworkAssertNotGuest();
        if (isDestroyed) return;
        isDestroyed = true;

        GameManager.Instance.RoundLock.UnlockChainReaction(this);
        NetworkDestroy();
    }

    public override void NetworkDestroy()
    {
        NetworkRecordSnapshot(Method_AmmoDestroy_Common);
        AmmoDestroy_Common();
    }

    [StreamPlay(Method_AmmoDestroy_Common)]
    protected void AmmoDestroy_Common()
    {
        if (timerText != null) {
            Destroy(timerText.gameObject);
        }
        Destroy(gameObject);
    }

    protected virtual void InstantiateExplosionController(Vector2 origin, float damageFactor = 1f)
    {
        if (explosionHasBeenGenerated) return;
        // Log.Message("AmmoController", "Instantiate Explosion");
        // Prevent multiple instantiation if hiting multiple colliders
        explosionHasBeenGenerated = true;
        if (damageFactor < 0) {
            damageFactor = 0;
        }

#if CC_EXTRA_CARE
try {
#endif
        ExplosionManager.Instance
            .GetExplosion(origin, IsAISimulationActive)
            .InitSimulable(weapon.Explosion, damageFactor, weapon, angle, isFacingRight, power)
        ;
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    #region Stream Play

    protected virtual void FixedUpdate()
    {
        if (explodeTime > 0 && Time.fixedTime > explodeTime) {
            InstantiateExplosionController(origin: transform.position);
            UnlockAndDestroy();
        } else if (destroyTime > 0 && Time.fixedTime > destroyTime) {
            UnlockAndDestroy();
        }

        if (!isActive) return;
        // Debugging.DrawPoint(transform.position, Color.blue, IsAISimulationActive ? 10f : 3f);
        if (IsAISimulationActive) return;
        if (timerText != null) {
            UpdateTimer();
        }

        RecordFixedUpdate();
    }

    protected void RecordFixedUpdate()
    {
        if (GameManager.Instance == null) return;
        if (timerText) {
            NetworkRecordFloatPack(
                // Base ammo
                transform.position, transform.rotation,
                // Timer Text related
                (Vector2)timerText.transform.position, 1 + explodeTime - Time.fixedTime);
        } else {
            NetworkRecordFloatPack(transform.position, transform.rotation);
        }
    }

    public void OnFloatPack(FloatPack floatPack)
    {
        transform.position = floatPack.NextVector();
        transform.rotation = floatPack.NextQuaternion();
        if (timerText != null) {
            timerText.transform.position = floatPack.NextVector();
            timerText.text = "" + Mathf.FloorToInt(floatPack.NextFloat());
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// A collider is considered valid if:
    /// it is not a trigger collider -or-
    /// it is a trigger collider of some sort (for now BoxControllerBase)
    /// </summary>
    protected bool ColliderIsValid(Collider2D collider)
    {
        if (collider == null) {
            Log.Error("AmmoController", "Collider is null at AmmoControllerBase::CheckIfValidTrigger()");
            return false;
        }

        if (!collider.isTrigger) {
            return true;
        }

        if (collider.GetComponentIncludingParent<BoxControllerBase>() != null) {
            return true;
        }

        return false;
    }

    protected float GetWindForce()
    {
        return GameManager.Instance.RoundControllerInstance.WindForce;
    }

    protected void AssertCharacterManagerNotNullForThisWeapon()
    {
#if CC_DEBUG
        if (characterManager == null) throw new System.Exception("CharacterManager can not be null for this weapon");
#endif
    }

    #endregion
}
