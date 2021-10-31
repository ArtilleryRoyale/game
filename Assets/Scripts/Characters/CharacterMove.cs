using UnityEngine;
using FMODUnity;
using Jrmgx.Helpers;
using CC;
using CC.StreamPlay;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class CharacterMove : NetworkObject, RoundLockIdentifier, FloatPackStreamPlay, ExplosionReceiver
{
    #region Fields

    public const int MaxSlope = 65;
    public float HighJumpFactor = 1.8f;

    public const int MOVE_ACTION_CONTINUE = 0;
    public const int MOVE_ACTION_LEFT = -1;
    public const int MOVE_ACTION_RIGHT = 1;

    [Header("References")]
    [SerializeField] private ParticleSystem jumpParticleSystem = default;
    [SerializeField] private CharacterWeapon characterWeapon = default;
    [SerializeField] private Animator animator = default;
    // When the character got some external force and will bounce (not in the case it's falling)
    [SerializeField] private PhysicsMaterial2D physicsMaterialExternalForceBounce = default;
    // When the character is falling and hit a valid slope
    [SerializeField] private PhysicsMaterial2D physicsMaterialFallMaxFriction = default;
    // When the character is resting and hit a valid slope
    [SerializeField] private PhysicsMaterial2D physicsMaterialRestMaxFriction = default;
    // When the character is resting and hit a non valid slope
    [SerializeField] private PhysicsMaterial2D physicsMaterialRestNoFriction = default;
    // When the player control the character and they are moving
    [SerializeField] private PhysicsMaterial2D physicsMaterialWhenMovingNoFriction = default;
    // When the character does not move (default)
    [SerializeField] private PhysicsMaterial2D physicsMaterialWhenStaticMaxFriction = default;

    // Parent, siblings and delegate
    private CharacterManager CharacterManager;

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventJump = default;
    [SerializeField] private StudioEventEmitter soundEventJumpDouble = default;
    [SerializeField] private StudioEventEmitter soundEventStepDefault = default;
    [SerializeField] private StudioEventEmitter soundEventStepConcrete = default;
    [SerializeField] private StudioEventEmitter soundEventLandDefault = default;
    [SerializeField] private StudioEventEmitter soundEventLandConcrete = default;
    [SerializeField] private StudioEventEmitter soundEventBounce = default; // Bounce

    [SerializeField] public StudioEventEmitter soundEventHurt = default; // OnReceiveExplosion and Animator::EndRagdoll
    [SerializeField] private StudioEventEmitter soundEventFall = default;

    private Rigidbody2D myRigidbody => CharacterManager.Rigidbody;
    private PhysicsMaterial2D myRigidbody_sharedMaterial {
        set {
#if CC_DEBUG
            if (myRigidbody.sharedMaterial == value) return;
#endif
            // Log.Message("CharacterMove", "Updated material for " + name + " to " + value.name);
            myRigidbody.sharedMaterial = value;
        }
        get => myRigidbody.sharedMaterial;
    }

    // Public state
    public int RoundLockIdentifier { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFacingRight { get; private set; } = true;
    public bool IsGrounded { get; private set; } = true;
    public bool IsReceivingExternalForce { get; private set; }
    public bool IsMoving {
        get {
            // NetworkAssertNotGuest();
            return
                controllerWantsToMove != MOVE_ACTION_CONTINUE ||
                controllerWantsToJump ||
                myRigidbody.velocity != Vector2.zero;
        }
    }
    // State
    Vector2 slope = Vector2.zero;
    // Note: isValidSlope supperseeds IsGrounded (as the condition is similar but harder to be true)
    private bool isValidSlope = false;
    private bool isGroundedConcrete;
    private int jumpCount;
    private float lastGroundedY;
    private float startFallPosition;
    private Debouncer soundBounceDebouncer;
    // Rest state
    private bool isAskingForRest;
    private Vector2 tryToRestPreviousPosition;
    private int restTick = 0;
    private int restStuckTick = 0;
    private int restLoop = 0;
    // Move controller
    private int controllerWantsToMove;
    private bool controllerWantsToJump;

    // Explosion
    private readonly List<int> ReceivedExplosionsIdentifiers = new List<int>();

    // StreamPlay
    private Vector2 networkVelocity;
    private Vector2 previousPosition = Vector2.zero;
    private Vector2 previousVelocity = Vector2.zero;

    private Vector2 myVelocity => myRigidbody.velocity;
    private Vector2 myPosition {
        get {
            if (CharacterManager.CharacterAnimator.IsRagdollActive) {
                return new Vector2(
                    transform.position.x,
                    transform.position.y
                    + (CharacterManager.MyHeight / 2f)
                    - (CharacterAnimator.RAGDOLL_COLLIDER_SIZE.y / 2f)
                );
            }
            return transform.position;
        }
    }

    // Config
    private const float groundCheckDistance = 0.5f; // This can not be greater otherwise it will prevent a correct jump
    private const float slopeCheckDistance = 1f;

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();
        RoundLockIdentifier = RoundLock.NewRoundLockIdentifier();
        CharacterManager = GetComponent<CharacterManager>();
        soundBounceDebouncer = new Debouncer(this, 0.2f);
    }

    protected override void Start()
    {
        NetworkIdentifier = NetworkObject.NetworkIdentifierFrom(CharacterManager.NetworkIdentifier + "_move");
        base.Start();
        StreamPlayPlayer.Refresh();
    }

    #endregion

    #region Public

    public void Activate()
    {
        startFallPosition = 0f;
        lastGroundedY = myPosition.y;
        myRigidbody_sharedMaterial = physicsMaterialWhenStaticMaxFriction;
        AnimationSpeedFactor(CharacterManager.Character.AnimationFactor);
        JumpStop();
        MoveStop();

        IsActive = true;
        myRigidbody.bodyType = RigidbodyType2D.Dynamic;

        restTick = 0;
        restStuckTick = 0;
        restLoop = 0;
    }

    public void Desactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        if (CharacterManager.IsDead) return;
        AskRest();
    }

    [StreamPlay(Method_Flip)]
    public void Flip() // Used in Player Controller
    {
        IsFacingRight = !IsFacingRight;
        // Invert player
        var s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
        // Invert health text
        CharacterManager.CharacterUserInterface.CounterFlip();
    }

    public void Dashed(Vector2 directionAndDistance)
    {
        ReceivedExternalForce(directionAndDistance);
    }

    #endregion

    #region Move

    public void JumpAction()
    {
        if (CharacterManager.CharacterWeapon.IsWeaponFiring) return;
        characterWeapon.HideWeapon();
        controllerWantsToJump = true;
    }

    private void JumpStop()
    {
        controllerWantsToJump = false;
    }

    public void MoveAction(int horizontal)
    {
        controllerWantsToMove = horizontal;
    }

    public void MoveStop()
    {
        controllerWantsToMove = 0;
    }

    public void AIMoveForceStop()
    {
        controllerWantsToMove = 0;
        controllerWantsToJump = false;
        myRigidbody.velocity = Vector2.zero;
    }

    #endregion

    #region Sounds and Animations

    private void Update()
    {
        Vector2 currentVelocity = Vector2.zero;
        bool currentIsGrounded = true;

        if (IsNetworkOwner) {
            if (IsActive) {
                currentVelocity = myRigidbody.velocity;
                currentIsGrounded = IsGrounded;
            }
        } else {
            currentVelocity = networkVelocity;
            currentIsGrounded = IsGrounded;
            networkVelocity = Vector2.zero;
        }

        animator.SetFloat(CharacterAnimator.ANIMATOR_HORIZONTAL_SPEED, Mathf.Abs(currentVelocity.x));
        animator.SetFloat(CharacterAnimator.ANIMATOR_VERTICAL_SPEED, currentVelocity.y);
        animator.SetBool(CharacterAnimator.ANIMATOR_IS_GROUNDED, currentIsGrounded);
        animator.SetBool(CharacterAnimator.ANIMATOR_IS_KING_TO_CAPTURE, CharacterManager.IsKingToCapture);
    }

    /// <summary>
    /// This comes from animation events (via AnimatorController)
    /// </summary>
    public void PlayFootstep()
    {
        if (isGroundedConcrete) {
            soundEventStepConcrete.Play();
        } else {
            soundEventStepDefault.Play();
        }
    }

    private void AnimationSpeedFactor(float f)
    {
        animator.SetFloat(CharacterAnimator.ANIMATOR_SPEED_RUN, f);
        animator.SetFloat(CharacterAnimator.ANIMATOR_SPEED_JUMP, f);
        animator.SetFloat(CharacterAnimator.ANIMATOR_SPEED_IDLE, f);
        animator.SetFloat(CharacterAnimator.ANIMATOR_SPEED_FALLING, f);
    }

    [StreamPlay(Method_ShowJump)]
    protected void ShowJump(bool isDouble)
    {
        if (isDouble) {
            soundEventJumpDouble.Play();
        } else {
            soundEventJump.Play();
        }
        AnimatorTriggerJump();
        ParticlesFeet();
    }

    [StreamPlay(Method_ShowTouchDown)]
    protected void ShowTouchDown(bool isConcrete)
    {
        if (isConcrete) {
            soundEventLandConcrete.Play();
        } else {
            soundEventLandDefault.Play();
        }
        ParticlesFeet();
    }

    private void AnimatorTriggerJump()
    {
        animator.SetTrigger(CharacterAnimator.ANIMATOR_JUMP);
    }

    private void ParticlesFeet()
    {
        var particles = Instantiate<ParticleSystem>(jumpParticleSystem, transform.parent, worldPositionStays: true);
        particles.Play();
        this.ExecuteInSecond(2f, () => Destroy(particles.gameObject));
    }

    #endregion

    #region Stream Play

    private void RecordFixedUpdate()
    {
        if (myPosition != previousPosition || myRigidbody.velocity != previousVelocity) {
            previousPosition = myPosition;
            previousVelocity = myRigidbody.velocity;
            NetworkRecordFloatPack(myPosition, myRigidbody.velocity, IsGrounded, isGroundedConcrete);
        }
    }

    public void OnFloatPack(FloatPack floatPack)
    {
        transform.position = floatPack.NextVector();
        networkVelocity = floatPack.NextVector();
        IsGrounded = floatPack.NextBool();
        isGroundedConcrete = floatPack.NextBool();

        if (networkVelocity != Vector2.zero) {
            characterWeapon.HideWeapon();
        }
    }

    #endregion

    #region Events

    private void StartFall(float from)
    {
        if (!CharacterManager.IsInited) return;
        if (CharacterManager.PreventFall) return;
        // NetworkAssertNotGuest();
        if (startFallPosition > 0) return; // Already falling

        // Log.Message("CharacterMove", "Start Fall: " + from);
        // Debugging.DrawCircle(myPosition, 1f, Color.red, 10f);

        // Desactivate first and then set the fall Y height
        IsActive = false;
        ReceivedExternalForce(myRigidbody.velocity, isFalling: true);
        startFallPosition = from;

        FallSound();
        NetworkRecordSnapshot(Method_FallSound);
    }

    [StreamPlay(Method_FallSound)]
    protected void FallSound()
    {
        soundEventFall.Play();
    }

    #endregion

    #region Explosion

    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        // If this character is into a Shield, prevent damage
        if (ShieldAmmoController.IntoShield(CharacterManager)) {
            // Log.Message("CharacterMove", "Character protected by shield");
            return;
        }
        if (damages <= 0) return;
        if (ReceivedExplosionsIdentifiers.Contains(explosion.ExplosionUniqueIdentifier)) return;
        ReceivedExplosionsIdentifiers.Add(explosion.ExplosionUniqueIdentifier);
        CharacterManager.AddDamages(damages);
        ReceivedExternalForce(force);
        soundEventHurt.Play(); // TODO prio 2 sync
    }

    public void ExplosionImmunize(int explosionIdentifier)
    {
        ReceivedExplosionsIdentifiers.Add(explosionIdentifier);
    }

    #endregion

    #region Physics

    private void ReceivedExternalForce(Vector2 withImpulse, bool isFalling = false)
    {
        // NetworkAssertNotGuest();

        // Log.Message("CharacterMove", "Character received external force: " + withImpulse);

        IsReceivingExternalForce = true;
        isAskingForRest = true;
        GameManager.Instance.RoundLock.LockPhysicsMove(this);

        if (withImpulse != Vector2.zero) {
            CharacterManager.CharacterAnimator.StartRagdoll(withImpulse);
        }

        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        myRigidbody_sharedMaterial = isFalling ? physicsMaterialFallMaxFriction : physicsMaterialExternalForceBounce;
        myRigidbody.velocity = Vector2.zero;
        myRigidbody.AddForce(withImpulse * Config.GlobalForceFactor, ForceMode2D.Impulse);

        // Debugging.DrawLine(myPosition, myPosition + withImpulse * Config.GlobalForceFactor, Color.red, 15f);

        NetworkCameraRequestFollow();
        NetworkRecordSnapshot(Method_CameraFollowCharacter);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        // Log.Message("CharacterMove", "OnCollisionEnter2D: " + collision.collider?.name);
        if (PhysicLogic.CheckOutOfBounds(collision.collider, out string boundTag)) {
            // Log.Message("CharacterMove", "OutOfBounds tag: " + boundTag);
            CharacterManager.DeadOutOfBounds(boundTag);
            return;
        }

        if (startFallPosition > 0) {
            CharacterManager.EventFall(startFallPosition - myPosition.y);
            startFallPosition = 0;
            return;
        }

        if (isAskingForRest) {
            // When asking for rest we also check if the slope is valid
            // if not we switch to the "no friction" version of the material
            UpdateSlopeInfo();
            if (isValidSlope) {
                // Log.Message("CharacterMove", "OnCollisionEnter2D: Update physics material to rest with friction");
                myRigidbody_sharedMaterial = physicsMaterialRestMaxFriction;
            } else {
                // Log.Message("CharacterMove", "OnCollisionEnter2D: Update physics material to rest without friction");
                myRigidbody_sharedMaterial = physicsMaterialRestNoFriction;
            }
        }

        if (IsReceivingExternalForce && !soundBounceDebouncer.NeedDebounce()) {
            soundBounceDebouncer.Debounce();
            soundEventBounce.Play(); // TODO prio 2 sync
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null) return;
        if (!IsNetworkOwner) return;

        if (isAskingForRest) {
            UpdateIsGrounded();
            CheckForFall();
            TryRest();
            RecordFixedUpdate();
            return;
        }

        if (!IsActive) return;
        if (CharacterManager.CharacterWeapon.IsWeaponFiring) return;

        UpdateIsGrounded();
        CheckForFall();

        // CheckForFall() will desactivate (if falling has started this fixedUpdate) so we need to check again IsActive
        if (!IsActive) return;

        // Debugging.DrawText(myPosition + Vector2.up * 6, IsGrounded ? "IsGrounded" : "NOT Grounded", Color.cyan, 0, 0.3f);

        UpdateSlopeInfo();

        // Material
        if (isValidSlope && controllerWantsToMove == 0) {
            // Debugging.DrawText(myPosition + Vector2.up * 4, "Material Max Friction", Color.cyan, 0, 0.3f);
            myRigidbody_sharedMaterial = physicsMaterialWhenStaticMaxFriction;
        } else {
            // Debugging.DrawText(myPosition + Vector2.up * 4, "Material No Friction", Color.cyan, 0, 0.3f);
            myRigidbody_sharedMaterial = physicsMaterialWhenMovingNoFriction;
        }

        // Hide weapon
        if (controllerWantsToMove != 0) {
            characterWeapon.HideWeapon();
            if (!CharacterManager.IsAIPlaying || CharacterManager.State != CharacterManager.StateEnum.Retreat) {
                NetworkCameraRequestFollow();
                NetworkRecordSnapshot(Method_CameraFollowCharacter);
            }
        }

        // Flip character
        if (controllerWantsToMove > 0) {
            if (!IsFacingRight) {
                Flip();
                NetworkRecordSnapshot(Method_Flip);
            }
        } else if (controllerWantsToMove < 0) {
            if (IsFacingRight) {
                Flip();
                NetworkRecordSnapshot(Method_Flip);
            }
        }

        bool hasMovedThisFrame = false;

        // Move horizontal
        if (!isValidSlope && IsGrounded) {
            // Slope is too steep, cancel the move
            // Debugging.DrawText(myPosition + Vector2.up * 3, "Not valid slope too steep", Color.cyan, 0, 0.3f);
        } else if (isValidSlope && IsGrounded) {
            // Can walk on slope, use slope info to calculate the new velocity
            // Debugging.DrawText(myPosition + Vector2.up * 3, "Valid slope", Color.cyan, 0, 0.3f);
            hasMovedThisFrame = true;
            myRigidbody.velocity = new Vector2(
                controllerWantsToMove * CharacterManager.Character.MoveSpeed * -slope.x,
                controllerWantsToMove * CharacterManager.Character.MoveSpeed * -slope.y
            );
        } else if (Physics2D.Raycast(myPosition + Vector2.up * 2f, IsFacingRight ? Vector2.right : Vector2.left, 2f, Layer.Terrain.Mask)) {
            // If front hit something, cancel the move
            // This is to prevent slope sliding
            // Debugging.DrawText(myPosition + Vector2.up * 3, "Not valid slope hit front", Color.cyan, 0, 0.3f);
        } else {
            // Otherwise let the Physics2D do its job for the Y axis
            // Debugging.DrawText(myPosition + Vector2.up * 3, "Not valid slope", Color.cyan, 0, 0.3f);
            hasMovedThisFrame = true;
            myRigidbody.velocity = new Vector2(
                controllerWantsToMove * CharacterManager.Character.MoveSpeed,
                myRigidbody.velocity.y
            );
        }

        // Move vertical
        bool canJump = (isValidSlope || !IsGrounded) && jumpCount < 2;
        // Debugging.DrawText(myPosition + Vector2.up * 5, canJump ? "Can jump" : "Can not jump", Color.cyan, 0, 0.3f);
        if (controllerWantsToJump) {
            controllerWantsToJump = false;
            if (canJump) {
                // Sound & animation
                if (jumpCount == 0) {
                    ShowJump(isDouble: false);
                    NetworkRecordSnapshot(Method_ShowJump, false);
                } else {
                    ShowJump(isDouble: true);
                    NetworkRecordSnapshot(Method_ShowJump, true);
                }

                // Remove current velocity for this frame and add the jump force
                myRigidbody.velocity = Vector2.zero;
                hasMovedThisFrame = true;
                if (CharacterManager.HighJumpAllowed) {
                    myRigidbody.AddForce(
                        new Vector2(0, CharacterManager.Character.MoveJumpHeight * HighJumpFactor),
                        ForceMode2D.Impulse
                    );
                } else {
                    myRigidbody.AddForce(new Vector2(0, CharacterManager.Character.MoveJumpHeight), ForceMode2D.Impulse);
                }

                IsGrounded = false;
                jumpCount++;
            }
        }

        if (hasMovedThisFrame) {
            if (CharacterManager.Shield != null) {
                CharacterManager.Shield.End();
            }
        }

        RecordFixedUpdate();
    }

    /// Define IsGrounded
    private void UpdateIsGrounded()
    {
        bool wasGrounded = IsGrounded;
        bool isGroundedInFrame = false;

        Collider2D collider = Physics2D.OverlapCircle(myPosition, groundCheckDistance, Layer.Terrain.Mask);
        if (collider) {
            // Debugging.DrawCircle(myPosition, groundCheckDistance, Color.cyan);
            isGroundedInFrame = true;
            jumpCount = 0;
            lastGroundedY = myPosition.y;

            if (!wasGrounded) {
                isGroundedConcrete = collider.CompareTag(Config.TagTypePlatform) || collider.CompareTag(Config.TagTypeStatue);
                ShowTouchDown(isGroundedConcrete);
                NetworkRecordSnapshot(Method_ShowTouchDown, isGroundedConcrete);
            }
        } else {
            // Debugging.DrawCircle(myPosition, groundCheckDistance, Color.red);
        }

        IsGrounded = isGroundedInFrame;
    }

    private void CheckForFall()
    {
        if (CharacterManager.PreventFall) return;
        if (lastGroundedY - myPosition.y > Config.FallDistance) {
            StartFall(myPosition.y);
        }
    }

    /// Define isValidSlope and slope
    private void UpdateSlopeInfo()
    {
        isValidSlope = false;
        slope = Vector2.zero;

        RaycastHit2D slopeHit;
        // Check front slope
        if (slopeHit = Physics2D.Raycast(myPosition, IsFacingRight ? Vector2.right : Vector2.left, slopeCheckDistance, Layer.Terrain.Mask)) {
            float slopeAngle = Vector2.Angle(slopeHit.normal, Vector2.up);
            slope = Vector2.Perpendicular(slopeHit.normal).normalized;
            isValidSlope = slopeAngle <= MaxSlope;
            // Log.Message("CharacterMove", "Update Slope Info for " + name + ": Hit front at " + slopeAngle, IsActive);
            // Debugging.DrawText(myPosition + Vector2.up * 2, "Hit front " + slopeAngle, Color.cyan, 0, 0.3f);
            // Debugging.DrawRay(slopeHit.point, slopeHit.normal, Color.green);
            // Debugging.DrawRay(slopeHit.point, slope, Color.red);
        }
        // Check back slope
        if (!isValidSlope && (slopeHit = Physics2D.Raycast(myPosition, IsFacingRight ? Vector2.left : Vector2.right, slopeCheckDistance, Layer.Terrain.Mask))) {
            float slopeAngle = Vector2.Angle(slopeHit.normal, Vector2.up);
            slope = Vector2.Perpendicular(slopeHit.normal).normalized;
            isValidSlope = slopeAngle <= MaxSlope;
            // Log.Message("CharacterMove", "Update Slope Info for " + name + ": Hit back at " + slopeAngle, IsActive);
            // Debugging.DrawText(myPosition + Vector2.up * 2, "Hit back " + slopeAngle, Color.cyan, 0, 0.3f);
            // Debugging.DrawRay(slopeHit.point, slopeHit.normal, Color.green);
            // Debugging.DrawRay(slopeHit.point, slope, Color.red);
        }
        // Check down slope
        if (!isValidSlope && (slopeHit = Physics2D.Raycast(myPosition, Vector2.down, slopeCheckDistance, Layer.Terrain.Mask))) {
            float slopeAngle = Vector2.Angle(slopeHit.normal, Vector2.up);
            slope = Vector2.Perpendicular(slopeHit.normal).normalized;
            isValidSlope = slopeAngle <= MaxSlope;
            // Log.Message("CharacterMove", "Update Slope Info for " + name + ": Hit down at " + slopeAngle, IsActive);
            // Debugging.DrawText(myPosition + Vector2.up * 2, "Hit down " + slopeAngle, Color.cyan, 0, 0.3f);
            // Debugging.DrawRay(slopeHit.point, slopeHit.normal, Color.green);
            // Debugging.DrawRay(slopeHit.point, slope, Color.red);
        }
#if CC_DEBUG
        if (!isValidSlope) {
            // Log.Message("CharacterMove", "Update Slope Info for " + name + ": Not a valid slope", IsActive);
        }
#endif
    }

    #endregion

    #region Resting

    private void AskRest()
    {
        // NetworkAssertNotGuest();

        // Log.Message("CharacterMove", "Ask rest");
        UpdateIsGrounded();
        if (IsGrounded) {
            // Log.Message("CharacterMove", "Already grounded, did rest");
            DidRest();
            return;
        }

        isAskingForRest = true;
        GameManager.Instance.RoundLock.LockPhysicsMove(this);

        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        // TODO this code is duplicated! re-factorise
        // When asking for rest we also check if the slope is valid
        // if not we switch to the "no friction" version of the material
        UpdateSlopeInfo();
        if (isValidSlope) {
            // Log.Message("CharacterMove", "Ask For Rest: Update physics material to rest with friction");
            myRigidbody_sharedMaterial = physicsMaterialRestMaxFriction;
        } else {
            // Log.Message("CharacterMove", "Ask For Rest: Update physics material to rest without friction");
            myRigidbody_sharedMaterial = physicsMaterialRestNoFriction;
        }
    }

    private void TryRest()
    {
        // NetworkAssertNotGuest();

        if (!isAskingForRest) return;
        // Log.Message("CharacterMove", "Try rest tick: " + restTick + " stuck: " + restStuckTick);

        if (Vector2.Distance(tryToRestPreviousPosition, myPosition) < Config.RestMaxMagnitude) {
            restTick++;
            if (restTick > Config.RestTickNeeded) {
                UpdateIsGrounded();
                UpdateSlopeInfo();
                if (IsGrounded && isValidSlope) { // All good
                    isAskingForRest = false;
                    IsReceivingExternalForce = false;
                    restTick = 0;
                    restStuckTick = 0;
#if CC_EXTRA_CARE
try {
#endif
                    WillRest().Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
                } else {
                    if (CharacterManager.CharacterAnimator.PreventStuck()) {
                        // Reset tries and restart the whole resting process
                        restTick = 0;
                        restStuckTick = 0;
                        return;
                    }
                    Log.Error("CharacterMove", "Character is stuck while trying to rest");
                    restStuckTick++;
                    if (restStuckTick > Config.RestTickNeeded) {
                        Log.Error("CharacterMove", "Character was stuck, force DidRest()");
                        isAskingForRest = false;
                        IsReceivingExternalForce = false;
                        restTick = 0;
                        restStuckTick = 0;
#if CC_EXTRA_CARE
try {
#endif
                        WillRest().Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
                    }
                }
            }
        } else {
            restTick = 0;
            restStuckTick = 0;
        }

        tryToRestPreviousPosition = myPosition;
    }

    private async UniTask WillRest()
    {
        // NetworkAssertNotGuest();

        // Log.Message("CharacterMove", "Will rest");
#if CC_EXTRA_CARE
try {
#endif
        await CharacterManager.CharacterAnimator.WillEndRagdoll(CharacterManager.IsDeadOrWillDie).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        // Because we waitted a bit it's possible to be in a isAskingForRest state again
        if (isAskingForRest) return;
        DidRest();
    }

    private void DidRest()
    {
        // NetworkAssertNotGuest();

        // Log.Message("CharacterMove", "Did rest");
        UpdateIsGrounded();
        UpdateSlopeInfo();
        if ((!IsGrounded || !isValidSlope) && restLoop < 3) {
            Log.Error("CharacterMove", "Did rest was not in a valid position, go for a round again: " + restLoop);
            restLoop++;
            AskRest();
            return;
        }

        if (!CharacterManager.IsDeadOrWillDie) {
            // Log.Message("CharacterMove", "DidRest => animator.enabled = true for: " + name);
            animator.enabled = true;
        }

        myRigidbody_sharedMaterial = physicsMaterialWhenStaticMaxFriction;
        myRigidbody.velocity = Vector2.zero;
        myRigidbody.Sleep();
        myRigidbody.bodyType = RigidbodyType2D.Static;

        GameManager.Instance.RoundLock.UnlockPhysicsMove(this);
    }

    #endregion

    #region Force Transfer

    // Transfer some force from other character when expulsed
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.layer != Layer.TransferForce.Index) return;
        var other = collider.gameObject.GetComponentInParent<CharacterMove>();
        if (other == null) return;
        if (IsReceivingExternalForce) return;
        if (other.IsReceivingExternalForce) {
            // Log.Message("CharacterMove", "Transfering force from Character to another");
            CharacterManager.AddDamages(0); // Only to trigger the end of this turn if needed
            ReceivedExternalForce(other.myVelocity * 0.67f);
        }
    }

    #endregion

    #region Camera

    [StreamPlay(Method_CameraFollowCharacter)]
    public void NetworkCameraRequestFollow()
    {
        CameraManager.Instance.RequestFollow(transform);
    }

    #endregion
}
