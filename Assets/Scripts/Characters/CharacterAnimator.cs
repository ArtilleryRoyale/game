using System;
using System.Collections.Generic;
using System.Linq;
using CC.StreamPlay;
using CC;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Jrmgx.Helpers;
using UnityEngine;

public class CharacterAnimator : NetworkObject, FloatPackStreamPlay
{
    #region Fields

    public const string ANIMATOR_HORIZONTAL_SPEED = "horizontalSpeed";
    public const string ANIMATOR_VERTICAL_SPEED = "verticalSpeed";
    public const string ANIMATOR_IS_GROUNDED = "isGrounded";
    public const string ANIMATOR_IS_KING_TO_CAPTURE = "isKingToCapture";
    public const string ANIMATOR_JUMP = "jump";
    public const string ANIMATOR_SPEED_RUN = "speedRun";
    public const string ANIMATOR_SPEED_JUMP = "speedJump";
    public const string ANIMATOR_SPEED_IDLE = "speedIdle";
    public const string ANIMATOR_SPEED_FALLING = "speedFalling";

    public static Vector2 RAGDOLL_COLLIDER_SIZE = Vector2.one;
    private const int BEND_RESET = int.MinValue;

    [Header("External References")]
    [SerializeField] private CharacterManager characterManager = default;
    [SerializeField] private FixedJoint2D fixedJoint = default;

    [Header("References")]
    [SerializeField] protected Animator animator = default;
    //public Animator Animator => animator;

    [Header("IK")]
    [SerializeField] private GameObject IKs = default;
    [SerializeField] private Transform headIKCenterTransform = default;
    [SerializeField] private Transform leftArmIKTransform = default;
    [SerializeField] private Transform rightArmIKTransform = default;
    [SerializeField] private Transform leftHandIKTransform = default;
    [SerializeField] private Transform rightHandIKTransform = default;
    [SerializeField] private Transform leftLegIKTransform = default;
    [SerializeField] private Transform rightLegIKTransform = default;
    // No need they are children of Leg
    // [SerializeField] private Transform leftFeetIKTransform = default;
    // [SerializeField] private Transform rightFeetIKTransform = default;

    [Header("Ragdoll")]
    [SerializeField] private CapsuleCollider2D characterCollider = default;
    [SerializeField] private Rigidbody2D mainRigidBody = default;
    [SerializeField] private Rigidbody2D[] rigidbodies = default;
    [SerializeField] private Transform[] bones = default;
    private StudioEventEmitter soundEventHurt => characterManager.CharacterMove.soundEventHurt;

    [Header("Menu Config")]
    [SerializeField] private bool isPredefined;
    [SerializeField] private bool runOutOfNowhere;

    // State
    public bool IsRagdollActive { get; protected set; }
    private List<Vector2> bonesPostitions = new List<Vector2>();
    private List<Quaternion> bonesRotations = new List<Quaternion>();
    private Vector2 characterColliderSize;

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();
        headIKCenterTransform.gameObject.SetActive(false);
        leftArmIKTransform.gameObject.SetActive(false);
        rightArmIKTransform.gameObject.SetActive(false);
        leftLegIKTransform.gameObject.SetActive(false);
        rightLegIKTransform.gameObject.SetActive(false);

        ResetRagdoll();
    }

    private void ResetRagdoll()
    {
        if (characterCollider != null) { // Can be null in the main menu
            characterColliderSize = characterCollider.size;

            foreach (var rb in rigidbodies) {
                rb.simulated = false;
            }

            fixedJoint.enabled = false;
        }

        IKs.SetActive(true);
        SetAnimatorActive(true);
    }

    protected override void Start()
    {
         // For menu only
        if (runOutOfNowhere) {
            animator.SetFloat(ANIMATOR_HORIZONTAL_SPEED, 1);
        }

        if (isPredefined) {
            return;
        }
        // End menu

        NetworkIdentifier = NetworkObject.NetworkIdentifierFrom(characterManager.NetworkIdentifier + "_anim");
        base.Start();
        StreamPlayPlayer.Refresh();
    }

    #endregion

    #region Animation Events

    /// <summary>
    /// This comes from animation events
    /// </summary>
    public void PlayFootstep()
    {
        if (characterManager == null) {
#if CC_DEBUG
            // Debug.LogWarning("CharacterAnimator: Play Footstep not bindded");
#endif
            return;
        }
        characterManager.CharacterMove.PlayFootstep();
    }

    #endregion

    #region Ragdoll

    public void StartRagdoll(Vector2 force)
    {
        if (!IsRagdollActive) {
            // Save tansforms
            foreach (var t in bones) {
                bonesPostitions.Add(t.localPosition);
                bonesRotations.Add(t.localRotation);
            }
        }

        // Log.Message("Ragdoll", "StartRagdoll for: " + name);
        characterCollider.size = RAGDOLL_COLLIDER_SIZE;

        InitRagdoll();
        NetworkRecordSnapshot(Method_InitRagdoll);

        mainRigidBody.AddForce(force, ForceMode2D.Impulse);
    }

    [StreamPlay(Method_InitRagdoll)]
    protected void InitRagdoll()
    {
        SetAnimatorActive(false);
        IKs.SetActive(false);
        IsRagdollActive = true;

        foreach (var rb in rigidbodies) {
            rb.simulated = true;
        }

        fixedJoint.enabled = true;
    }

    // This appends when the force was so strong that a part of the character's body
    // pass through the terrain and is on the other side of the terrain collider
    // and basically gets stuck inside
    public bool PreventStuck()
    {
        if (!IsRagdollActive) return false;

        bool didSomething = false;
        foreach (var rb in rigidbodies) {
            RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.up, 0.01f, Layer.Terrain.Mask);
            if (hit) {
                Log.Error("Ragdoll", rb.name + " was inside " + hit.collider.name);
                rb.simulated = false;
                didSomething = true;
            }
        }

        return didSomething;
    }

    public async UniTask WillEndRagdoll(bool isDeadOrWillDie)
    {
        if (!IsRagdollActive) return;
        // Log.Message("Ragdoll", "WillEndRagdoll for: " + name + " dead this turn? " + isDeadOrWillDie);

        await UniTask.Delay(Config.DelayEndRagdoll).CancelOnDestroy(this);
        if (characterManager.CharacterMove.IsReceivingExternalForce) {
            // Log.Message("Ragdoll", "Has been hit again while ending ragdoll: canceling the ending (WillEndRagdoll 1)");
            return;
        }

        if (!isDeadOrWillDie) {
#if CC_EXTRA_CARE
try {
#endif
            soundEventHurt.Play(); // TODO prio 2 sync
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
#if CC_EXTRA_CARE
try {
#endif
            // Restore transforms
            await LerpBackRagdoll(.333f).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        if (characterManager.CharacterMove.IsReceivingExternalForce) {
            // Log.Message("Ragdoll", "Has been hit again while ending ragdoll: canceling the ending (WillEndRagdoll 2)");
            return;
        }

        DidEndRagdoll(isDeadOrWillDie);
        NetworkRecordSnapshot(Method_DidEndRagdoll, isDeadOrWillDie);
    }

    // TODO prio 5 this could be more advanced by separating the animation of the legs and arms
    private async UniTask LerpBackRagdoll(float duration)
    {
        float time = 0;
        List<Vector2> currentPositions = bones.Select(t => (Vector2) t.localPosition).ToList();
        List<Quaternion> currentRotations = bones.Select(t => t.localRotation).ToList();
        Vector2 characterColliderCurrentSize = characterCollider.size;

        while (time < duration) {

            if (characterManager.CharacterMove.IsReceivingExternalForce) {
                // Log.Message("Ragdoll", "Has been hit again while ending ragdoll: canceling the ending (LerpBackRagdoll 1)");
                return;
            }

            float delta = time / duration;

            for (int index = 0, max = bones.Length; index < max; index++) {
                Transform t = bones[index].transform;
                t.localPosition = Vector2.Lerp(currentPositions[index], bonesPostitions[index], delta);
                t.localRotation = Quaternion.Lerp(currentRotations[index], bonesRotations[index], delta);
            }

            characterCollider.size = Vector2.Lerp(characterColliderCurrentSize, characterColliderSize, delta);

            time += Time.deltaTime;
            await UniTask.WaitForEndOfFrame(this.GetCancellationTokenOnDestroy());
        }

        if (characterManager.CharacterMove.IsReceivingExternalForce) {
            // Log.Message("Ragdoll", "Has been hit again while ending ragdoll: canceling the ending (LerpBackRagdoll 2)");
            return;
        }

        for (int index = 0, max = bones.Length; index < max; index++) {
            Transform t = bones[index].transform;
            t.localPosition = bonesPostitions[index];
            t.localRotation = bonesRotations[index];
        }

        characterCollider.size = characterColliderSize;

        await UniTask.WaitForEndOfFrame(this.GetCancellationTokenOnDestroy());
    }

    [StreamPlay(Method_DidEndRagdoll)]
    private void DidEndRagdoll(bool isDeadOrWillDie)
    {
        // Log.Message("Ragdoll", "DidEndRagdoll for: " + name + " dead this turn? " + isDeadOrWillDie);
        IsRagdollActive = false;

        foreach (var rb in rigidbodies) {
            rb.simulated = false;
        }

        fixedJoint.enabled = false;

        if (!isDeadOrWillDie) {
            IKs.SetActive(true);
            SetAnimatorActive(true);
        }
    }

    #endregion

    #region Stream Play

    private void FixedUpdate()
    {
        if (!IsNetworkOwner) return;
        RecordFixedUpdate();
    }

    private void RecordFixedUpdate()
    {
        if (!IsRagdollActive) return;
        var data = new object[bones.Length * 2 + 1];
        data[0] = characterManager.MyPosition;
        int index = 1;
        foreach (var t in bones) {
            data[index] = (Vector2)t.localPosition;
            index++;
            data[index] = t.localRotation;
            index++;
        }
        ///Log.Message("Ragdoll", "Recording FloatPacks for sync");
        NetworkRecordFloatPack(data);
    }

    public void OnFloatPack(FloatPack floatPack)
    {
        ///Log.Message("Ragdoll", "Got FloatPacks for sync");
        characterManager.transform.position = floatPack.NextVector();
        foreach (var t in bones) {
            t.localPosition = floatPack.NextVector();
            t.localRotation = floatPack.NextQuaternion();
        }
    }

    #endregion

    #region Weapon Handling

    /// <summary>
    /// Bend the head/body of the character in the given direction
    /// </summary>
    public void Bend(float angle, bool isFacingRight)
    {
        // Log.Message("CharacterAnimator", "Bend: " + angle);
        // AssertNoRagdoll();
        if (angle == BEND_RESET) {
            headIKCenterTransform.gameObject.SetActive(false);
            SetAnimatorActive(true);
        } else {
            headIKCenterTransform.rotation = Quaternion.Euler(0, 0, angle * (isFacingRight ? 1f : -1f));
            headIKCenterTransform.gameObject.SetActive(true);
            SetAnimatorActive(false);
        }
    }

    public void ResetBend()
    {
        // Log.Message("CharacterAnimator", "Reset bend");
        Bend(BEND_RESET, false);
    }

    public void PositionLeftArm(Vector2 arm, Vector2 hand)
    {
        // Log.Message("CharacterAnimator", "Position Left arm/hand at: " + arm + "/" + hand);
        // AssertNoRagdoll();
        leftArmIKTransform.position = arm;
        leftHandIKTransform.position = hand;
        leftArmIKTransform.gameObject.SetActive(arm != Vector2.zero);
        SetAnimatorActive(arm == Vector2.zero);
    }

    public void PositionRightArm(Vector2 arm, Vector2 hand)
    {
        // Log.Message("CharacterAnimator", "Position Right arm/hand at: " + arm + "/" + hand);
        // AssertNoRagdoll();
        rightArmIKTransform.position = arm;
        rightHandIKTransform.position = hand;
        rightArmIKTransform.gameObject.SetActive(arm != Vector2.zero);
        SetAnimatorActive(arm == Vector2.zero);
    }

    public void ResetLeftArm()
    {
        PositionLeftArm(Vector2.zero, Vector2.zero);
    }

    public void ResetRightArm()
    {
        PositionRightArm(Vector2.zero, Vector2.zero);
    }

    #endregion

    #region Helpers

    private void SetAnimatorActive(bool active)
    {
        if (IsRagdollActive) return;
        if (characterManager != null && characterManager.IsDead) return; // can be null in menu
        // Log.Message("CharacterAnimator", "SetAnimatorActive for: " + name + " active? " + active);

        if (active) {
            // Log.Message("CharacterAnimator", "SetAnimatorActive => animator.enabled = true for: " + name);
            animator.enabled = true;
        } else {
            int hash = Animator.StringToHash("Base Layer.Idle");
            animator.Play(hash, 0, 0);
            // Log.Message("CharacterAnimator", "SetAnimatorActive => animator.enabled = false (next frame) for: " + name);
            this.ExecuteNextUpdate(() => animator.enabled = false);
        }

    }

    private void AssertNoRagdoll()
    {
#if CC_DEBUG
        if (IsRagdollActive) {
            Debug.LogWarning("Ragdoll is active but your calling an animator related method");
        }
#endif
    }

    #endregion
}
