using UnityEngine;
using CC.StreamPlay;
using FMODUnity;
using Anima2D;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Jrmgx.Helpers;
using System.Collections.Generic;
using System;
using System.Linq;
using CC;
using System.Threading;

public class CharacterManager : NetworkObject, PositionableInterface
{
    #region Fields

    [Header("Predefined")]
    [SerializeField] public bool IsPredefined = false;
    [SerializeField] public bool IsPredefinedAI = false;

    [Header("References")]
    [SerializeField] public Character Character = default;
    [SerializeField] public Rigidbody2D Rigidbody = default;
    [SerializeField] private GameObject MainGameObject = default;
    [SerializeField] private CapsuleCollider2D capsuleCollider = default;

    // Managed references
    public CharacterMove CharacterMove { get; private set; }
    public CharacterWeapon CharacterWeapon { get; private set; }
    public CharacterUserInterface CharacterUserInterface { get; private set; }
    public CharacterAnimator CharacterAnimator = default;

    [Header("Color")]
    [SerializeField] public Material PlayerMaterial1 = default; // Red
    [SerializeField] public Material PlayerMaterial2 = default; // Blue
    [SerializeField] private SpriteMeshInstance[] meshInstances = default;

    [Header("Knight Boots")]
    [SerializeField] private SpriteMeshAnimation spriteMeshAnimationBoot01 = default;
    [SerializeField] private SpriteMeshAnimation spriteMeshAnimationBoot02 = default;

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventLava = default;
    [SerializeField] private StudioEventEmitter soundEventHello = default;
    [SerializeField] private StudioEventEmitter soundEventGoodbye = default;
    [SerializeField] private StudioEventEmitter soundEventMiss = default;
    [SerializeField] private StudioEventEmitter soundEventHit = default;
    [SerializeField] private StudioEventEmitter soundEventIdle = default;
    public enum Phrases : int { Nothing = 0, Hello = 1, Goodbye = 2, Miss = 3, Hit = 4, Idle = 5, Think = 6 }

    // Delegates and parents
    public CharacterManagerDelegate CharacterDelegate { get; set; }
    public InventoryDelegate InventoryDelegate { get; set; }
    public PlayerController PlayerController { get; set; }

    // States
    public bool IsInited { get; set; }
    public bool IsActive { get; private set; }
    public int Health { get; set; }
    public int PendingDamages { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsDeadOrWillDie => IsDead || Health - PendingDamages <= 0;
    public bool IsKingToCapture { get; private set; }
    public enum StateEnum : int { Idle = 0, MoveAndAim = 1, Retreat = 2, Dead = 3 }
    public StateEnum State { get; private set; }
    public ShieldAmmoController Shield { get; set; }
    public bool HighJumpAllowed { get; private set; } = false;
    public bool PreventFall { get; private set; } = false;
    private bool hasSaySomethingForDropable;

    // AI
    public bool IsAIPlaying => GameManager.Instance.PlayingAgainstAI && PlayerController.IsPlayerAI;
    private CancellationTokenSource AICancellationTokenSource;
    private CancellationToken cancellationToken => AICancellationTokenSource.Token;

    // AI Jumping
    private enum AIJumpType { Normal = 0, Down, StraitUp }
    private AIJumpType AIJumpKind = AIJumpType.Normal;
    private bool AIJumpFirst;
    private bool AIJumpSecond;
    private Vector2 AIJumpPreviousPosition;

    public Vector2 MyPosition => transform.position;
    public float MyHeight { get; protected set; }

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();
        if (IsPredefined) return;
        // Log.Message("InitDance", "Character Awake " + name);

        Health = Character.HealthDefault;
        MyHeight = capsuleCollider.size.y;
        InitRigidBody();

        CharacterMove = GetComponent<CharacterMove>();
        CharacterWeapon = GetComponentInChildren<CharacterWeapon>();
        CharacterUserInterface = GetComponentInChildren<CharacterUserInterface>();

        MainGameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        // Log.Message("InitDance", "Character Start " + name);
    }

    private void InitRigidBody()
    {
        Rigidbody.bodyType = RigidbodyType2D.Static;
        Rigidbody.gravityScale = Character.GravityScale;
        Physics2D.SyncTransforms();
    }

    #endregion

    #region Positionable

    public void SetPosition(Vector3 position)
    {
        // Log.Message("InitDance", "Character SetPosition " + name);
        transform.position = position;
        MainGameObject.SetActive(true);
        if (PlayerController != null) { // PlayerController can be null in Test Env
            SetColor(PlayerController.IsPlayerOne);
            CharacterUserInterface.Init(PlayerController.IsPlayerOne, Health, IsActive);
        } else {
            SetColor(isPlayerOne: true);
            CharacterUserInterface.Init(isPlayerOne: true, Health, IsActive);
        }
        // Log.Message("InitDance", "Character Inited " + name);
        IsInited = true;
        InitRigidBody();
    }

    #endregion

    #region Color

    private void SetColor(bool isPlayerOne)
    {
        foreach (SpriteMeshInstance spriteMeshInstance in meshInstances) {
            spriteMeshInstance.sharedMaterial = isPlayerOne ? PlayerMaterial1 : PlayerMaterial2;
        }
    }

    #endregion

    #region Capture The King

    public void KingCaptured(CharacterManager by)
    {
        // Log.Message("CaptureTheKing", "King Captured");
        // NetworkAssertNotGuest();
        by.SetRetreatState(hasRetreat: true);
        SetKingToCapture(false);
        PlayerController.SetKingIsCaptured();
        Shield.End();
        NetworkRecordSnapshot(Method_KingCaptured);
    }

    public void KingCaptured()
    {
        // Log.Message("CaptureTheKing", "King Captured (not by a character)");
        // NetworkAssertNotGuest();
        SetKingToCapture(false);
        PlayerController.SetKingIsCaptured();
        Shield.End();
        NetworkRecordSnapshot(Method_KingCaptured);
    }

    [StreamPlay(Method_KingCaptured)]
    private void KingCaptured_Guest()
    {
        SetKingToCapture(false);
        PlayerController.SetKingIsCaptured();
    }

    public void SetKingToCapture(bool state)
    {
        // Log.Message("CaptureTheKing", "Set King to Capture: " + state);
        IsKingToCapture = state;
        SetIdleState();
    }

    public void InstantiateKingShield()
    {
        // Log.Message("CaptureTheKing", "Activate King Shield");

        Shield = (ShieldAmmoController) WeaponManager.Instance.GetWeaponLogic(Weapon.WeaponEnum.Shield, MyPosition, Quaternion.identity);
        Shield.SetKing();
        Shield.InitKing(this);
    }

    #endregion

    #region Weapon Actions

    public void DashWeapon(Vector2 direction, float distance, float duration)
    {
        VoiceManager.Instance.SayNothingThisTurn();

        // TODO prio 2 needs sync
        CameraManager.Instance.RequestFollow(transform);

        Rigidbody.bodyType = RigidbodyType2D.Kinematic;
        // TODO prio 2 Small jump + force jump animator
        // Move the character
        DOTween.To(
            () => transform.position,
            p => Rigidbody.MovePosition(p),
            transform.position + ((Vector3)direction * distance),
            duration
        ).OnComplete(() => {
            // Ask for rest
            CharacterMove.Dashed(direction * distance);
        });
    }

    #endregion

    #region Knight Boots/jump

    public void HighJumpWeapon()
    {
        if (Character.CharacterType != Character.CharacterTypeEnum.Knight) {
#if CC_DEBUG
            Log.Critical("CharacterManager", "HighJumpWeapon on non-knight character");
#endif
            return;
        }
        spriteMeshAnimationBoot01.frame = 1;
        spriteMeshAnimationBoot02.frame = 1;
        HighJumpAllowed = true;
        PreventFall = true;
    }

    protected void HighJumpWeaponEnd()
    {
        // Called in Deactivate()
        HighJumpAllowed = false;
        PreventFall = false;
        if (Character.CharacterType != Character.CharacterTypeEnum.Knight) return;
        spriteMeshAnimationBoot01.frame = 0;
        spriteMeshAnimationBoot02.frame = 0;
    }

    #endregion

    #region Damages

    public void AddDamages(int amount)
    {
        // Log.Message("Damages", "Add damages " + amount);
        // NetworkAssertNotGuest();

        // Okay, discutable piece of code follow
        // --- start
        // Basically the idea is, if some character get damages,
        // it probably means that some other inflicted it (but not all the time: like mine or falls)
        // So we take that other characterManager and make them say something
        if (IsActive) {
            // Auto damages
            VoiceManager.Instance.WantToSay(VoiceManager.PRIORITY_HIGH + 2, Phrases.Nothing, this, VoiceManager.PHRASE_FFA);
        } else {
            var other = GameManager.Instance.RoundControllerInstance.CurrentCharacterManager;
            if (other != null && other.NetworkIdentifier != NetworkIdentifier) {
                if (other.PlayerController.PlayerId == PlayerController.PlayerId) {
                    // TODO prio 2 FFA phrases
                    VoiceManager.Instance.WantToSay(VoiceManager.PRIORITY_HIGH, Phrases.Nothing, this, VoiceManager.PHRASE_FFA);
                } else {
                    VoiceManager.Instance.WantToSay(VoiceManager.PRIORITY_HIGH + 1, Phrases.Hit, other, VoiceManager.PHRASE_GOT_YOU);
                }
            }
        }
        // --- end

        if (IsDead) return;
        PendingDamages += amount;
        // Log.Message("Damages", "Add damages confirmed, pending " + PendingDamages);

        if (IsActive) {
            StopRound();
        }
    }

    public async UniTask CalculateDamage(bool invert)
    {
        // Log.Message("Damages", "Calculate damages");
        if (IsDead) return;

        if (IsDeadOrWillDie) {
            VoiceManager.Instance.SayNothingThisTurn();
        }

        if (PendingDamages > 0) {
            // Log.Message("Damages", "Calculate damages with pending: " + PendingDamages);
#if CC_EXTRA_CARE
try {
#endif
            await CharacterUserInterface.UpdateDamages(Health, PendingDamages, invert).CancelOnDestroy(this);
            CharacterUserInterface.NetworkRecordSnapshot(Method_UpdateDamages, Health, PendingDamages, invert);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        // Log.Message("Health", name + " Update Health from " + Health + " to " + (Health - PendingDamages) + " in Calculate Damage IsOwner: " + IsNetworkOwner);
        Health -= (invert ? -1 : 1) * PendingDamages;
        PendingDamages = 0;

        if (Health <= 0) {
            IsDead = true;
            GameManager.Instance.RoundLock.EnqueueTaskInSequence(MakeDead);
            return;
        }

        CharacterUserInterface.UpdateUI(Health, IsActive);
        CharacterUserInterface.NetworkRecordSnapshot(Method_CharacterUserInterface_UpdateUI, Health, IsActive);
    }

    public void EventFall(float distance)
    {
        if (!IsNetworkOwner) return;
        if (!IsInited) return;
        // Log.Message("CharacterManager", "Event Fall: " + distance);
        AddDamages(Mathf.Min(15, (int)(distance + 1)));
        VoiceManager.Instance.CancelSay(this);

        if (CharacterDelegate != null) {
            CharacterDelegate.CharacterFall();
        }

        if (IsActive) {
            StopRound();
        }
    }

    public void DeadOutOfBounds(string tag)
    {
        if (!IsNetworkOwner) return;
        // NetworkAssertNotGuest(); // I guess !IsOwner prevents it anyway
        if (!IsInited) return;
        if (IsDead) return;

        NetworkRecordSnapshot(Method_DeadOutOfBounds_Guest);

        if (IsActive) {
            StopRound();
        }

        CharacterMove.NetworkCameraRequestFollow();
        CharacterMove.NetworkRecordSnapshot(Method_CameraFollowCharacter);

        SetDeadState();

        // 99% of the time it will be because of lava
        SFXManager.Instance.GetSFX(SFXManager.SFXType.LavaFall, transform.position).Init();
        soundEventLava.Play();

        gameObject.SetActive(false);

        GameManager.Instance.RoundLock.UnlockPhysicsMove(CharacterMove);
        GameManager.Instance.RoundLock.UnlockChainReaction(CharacterMove);
    }

    [StreamPlay(Method_DeadOutOfBounds_Guest)]
    protected void DeadOutOfBounds_Guest()
    {
        IsDead = true;
        SetDeadState();
        SFXManager.Instance.GetSFX(SFXManager.SFXType.LavaFall, transform.position).Init();
        soundEventLava.Play();
        gameObject.SetActive(false);
    }

    private async UniTask MakeDead()
    {
        // NetworkAssertNotGuest();

        CharacterMove.NetworkCameraRequestFollow();
        CharacterMove.NetworkRecordSnapshot(Method_CameraFollowCharacter);

        GameManager.Instance.RoundLock.LockChainReaction(CharacterMove);

#if CC_EXTRA_CARE
try {
#endif
        await SayNow((int) Phrases.Goodbye, VoiceManager.PHRASE_DEAD).CancelOnDestroy(this);
        NetworkRecordSnapshot(Method_SayNow, (int) Phrases.Goodbye, VoiceManager.PHRASE_DEAD);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        NetworkRecordSnapshot(Method_MakeDead_Guest);

        SetDeadState();

        InstantiateDeadExplosionController(MyPosition + 2f * Vector2.up);

        gameObject.SetActive(false);

        await UniTask.Delay(Config.DelayDead, cancellationToken: this.GetCancellationTokenOnDestroy()); // Wait a bit between deaths

        GameManager.Instance.RoundLock.UnlockPhysicsMove(CharacterMove);
        GameManager.Instance.RoundLock.UnlockChainReaction(CharacterMove);
    }

    [StreamPlay(Method_MakeDead_Guest)]
    protected void MakeDead_Guest()
    {
        SetDeadState();
        gameObject.SetActive(false);
    }

    private void SetDeadState()
    {
        State = StateEnum.Dead;

        if (IsAIPlaying) {
            AISetDead();
        }

        IsDead = true;
        Health = 0;
        PendingDamages = 0;

        Desactivate();
        CharacterWeapon.Desactivate();
        CharacterMove.Desactivate();
        CharacterUserInterface.HideUI();

        IsActive = false;
    }

    private void StopRound()
    {
        if (!IsNetworkOwner) return;
        if (CharacterDelegate == null) return;
        CharacterDelegate.CharacterAskStopRound(); // => will call StateIdle()
    }

    private void InstantiateDeadExplosionController(Vector2 origin)
    {
        ExplosionManager.Instance.GetExplosion(origin).Init(Character.Explosion);
    }

    #endregion

    #region Public Methods

    public void SetIdleState()
    {
        if (!IsNetworkOwner) return;
        if (IsDead) return;

        State = StateEnum.Idle;

        if (IsAIPlaying) {
            AISetIdleState();
        }

        Desactivate();
        CharacterWeapon.Desactivate();
        CharacterMove.Desactivate();
        CharacterUserInterface.UpdateUI(Health, IsActive);
        CharacterUserInterface.NetworkRecordSnapshot(Method_CharacterUserInterface_UpdateUI, Health, IsActive);
    }

    public void SetMoveAndAimState()
    {
        if (!IsNetworkOwner) return;

        State = StateEnum.MoveAndAim;

        Activate();
        CharacterWeapon.Activate();
        CharacterMove.Activate();
        CharacterUserInterface.UpdateUI(Health, IsActive);
        CharacterUserInterface.NetworkRecordSnapshot(Method_CharacterUserInterface_UpdateUI, Health, IsActive);

        if (IsAIPlaying) {
#if CC_EXTRA_CARE
try {
#endif
            AIStateMoveAndFire().Forget(); // Exception handled
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    /// <summary>
    /// Called when the player just start to load their fire
    /// </summary>
    public void SetStartedLoadFire()
    {
        if (!IsNetworkOwner) return;
        CharacterDelegate.CharacterStartedLoadFire();
    }

    /// <summary>
    /// Called when the player finished their fire
    /// </summary>
    public void SetRetreatState(bool hasRetreat)
    {
        if (!IsNetworkOwner) return;
        if (State == StateEnum.Retreat) return; // Retreat can be called multiple time

        State = StateEnum.Retreat;

        CharacterDelegate.CharacterAskRetreat(hasRetreat);

        if (IsAIPlaying) {
#if CC_EXTRA_CARE
try {
#endif
            AIStateRetreat(hasRetreat).Forget(); // Exception handled
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    public void SignalPosition(bool idle = false)
    {
        // We don't sync signal position because it's only for the active player
        if (IsAIPlaying) return; // same with AI, skip signal
        if (idle) {
            soundEventIdle.Play();
        } else {
            soundEventHello.Play();
        }
        SFXManager.Instance.GetSFX(SFXManager.SFXType.PickupTurn, MyPosition).Init();
    }

    #endregion

    #region Activate

    private void Activate()
    {
        IsActive = true;
        MoveToForeground(true);
    }

    private void Desactivate()
    {
        IsActive = false;
        HighJumpWeaponEnd();
        MoveToForeground(false);
    }

    private void MoveToForeground(bool state)
    {
        foreach (SpriteMeshInstance spriteMeshInstance in meshInstances) {
            try {
                spriteMeshInstance.sortingLayerName = state ? Config.SortingLayerCharacter : Config.SortingLayerCharacterBackground;
            } catch (Exception) {
                Log.Critical("CharacterManager", "Exception while accessing some of the spriteMeshInstance in " + name);
            }
        }
    }

    #endregion

    #region Collectable Items

    /// <summary>
    /// Called by BoxWeaponController
    /// </summary>
    public void CollectWeapon(Weapon weapon)
    {
        // Log.Message("CharacterManager", "Collected weapon: " + weapon);
        InventoryDelegate.AddPlayerWeapon(this, weapon);
        if (string.IsNullOrEmpty(weapon.WeaponHumanName)) return; // For grenade in training
        UserInterfaceManager.Instance.UpdateInfoText("Piked-up " + weapon.WeaponHumanName, hideInSeconds: 1);
    }

    public void CollectHealth(int health)
    {
        CollectHealth_Common(health);
        NetworkRecordSnapshot(Method_CollectHealth_Common, health);
        UserInterfaceManager.Instance.UpdateInfoText("Picked-up " + health + " hp", hideInSeconds: 1);
    }

    [StreamPlay(Method_CollectHealth_Common)]
    protected void CollectHealth_Common(int health)
    {
        Health += health;
        CharacterUserInterface.UpdateUI(Health, IsActive);
        UserInterfaceManager.Instance.UpdateCurrentCharacter(this);
    }

    #endregion

    #region StreamPlay

    public void Sync()
    {
        // Log.Message("Health", name + " Send New Health " + Health + " from Sync IsOwner: " + IsNetworkOwner);
        NetworkRecordSnapshot(
            Method_CharacterManager_Sync_Guest,
            Health,
            PendingDamages,
            IsDead,
            MyPosition
        );
    }

    [StreamPlay(Method_CharacterManager_Sync_Guest)]
    protected void Sync_Guest(int health, int damages, bool dead, Vector2 position)
    {
        // Log.Message("Health", name + " Got New Health " + health + " from Sync IsOwner: " + IsNetworkOwner);
        IsDead = dead;
        Health = health;
        PendingDamages = damages;
        transform.position = position;
        CharacterUserInterface.UpdateUI(Health, IsActive);
    }

    #endregion

    #region Sound And Bubble

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.layer != Layer.WeaponDropable.Index && collider.gameObject.layer != Layer.Weapon.Index) return;
        if (IsActive) return;

        float distance = Vector2.Distance(MyPosition, collider.transform.position);

        // If that character.PlayerController is active, it means it's my team
        if (PlayerController.IsActive) {
            // VoiceManager.Instance.WantToSay(VoiceManager.PRIORITY_LOW, soundEventHit, this, VoiceManager.PHRASE_FFA);
            return;
        }

        // TODO prio 3 this does not work, the distance will be the same has they all have a circle
        // collider of the same size thus the onTriggerEnter enters at the same distance no matter what
        VoiceManager.Instance.WantToSay(VoiceManager.PRIORITY_LOW - (int)distance, Phrases.Miss, this, VoiceManager.PHRASE_MISSED_ME);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (hasSaySomethingForDropable) return;
        if (collider.gameObject.layer != Layer.WeaponDropable.Index) return;
        if (IsActive) return;

        float distance = Vector2.Distance(MyPosition, collider.transform.position);

        if (collider.gameObject.layer == Layer.WeaponDropable.Index && distance < 6.5f) {
            // TODO prio 3 if many character are close-by too many bubbles pop
            // A dropable is entering and the distance is quite close to me, it means that it has just been dropped nearby!
            hasSaySomethingForDropable = true;
            this.ExecuteInSecond(8, () => hasSaySomethingForDropable = false); // Reset the state the ugly way
#if CC_EXTRA_CARE
try {
#endif
            SayNow((int) Phrases.Nothing, VoiceManager.PHRASE_DROPABLE).Forget();
            NetworkRecordSnapshot(Method_SayNow, (int) Phrases.Nothing, VoiceManager.PHRASE_DROPABLE);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    protected StudioEventEmitter PhraseToSoundEvent(Phrases phrase)
    {
        switch (phrase) {
            case Phrases.Hello: return soundEventHello;
            case Phrases.Goodbye: return soundEventGoodbye;
            case Phrases.Miss: return soundEventMiss;
            case Phrases.Hit: return soundEventHit;
            case Phrases.Idle: return soundEventIdle;
        }
        return null;
    }

    [StreamPlay(Method_SayNow)]
    public async UniTask SayNow(int phrase, string text)
    {
        // Log.Message("CharacterManager", name + " saying now: " + text);
        var soundEvent = PhraseToSoundEvent((Phrases) phrase);
        if (soundEvent == null) {
            CharacterUserInterface.BubbleShowAndHide(text);
        } else {
            CharacterUserInterface.BubbleShow(text);
#if CC_EXTRA_CARE
try {
#endif
            try {
                await soundEvent.WaitUntilPlayed().CancelOnDestroy(this); // Exception handled
            } catch (OperationCanceledException) {
                Log.Error("CharacterManager", "SayNow await soundEvent.WaitUntilPlayed throwed OperationCanceledException");
            }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            CharacterUserInterface.BubbleHide();
        }
        await UniTask.Delay(Config.DelayVoiceBubble, cancellationToken: this.GetCancellationTokenOnDestroy()); // Wait a bit in any case
    }

    #endregion

    #region AI

    private void OnDestroy()
    {
        if (AICancellationTokenSource == null) return;
        AICancellationTokenSource.Cancel();
    }

    private async UniTask AIWaitFrame()
    {
        await UniTask.WaitForEndOfFrame(cancellationToken);
    }

    private async UniTask AIStateMoveAndFire()
    {
        AICancellationTokenSource = new CancellationTokenSource();

        if (GameManager.Instance.CurrentGameOption.AILevel == GameOption.AILevelEnum.Dummy) {
            // Skip turn
            SetRetreatState(false);
            return;
        }

        var map = GameManager.Instance.MapController;
        var graph = map.AIRefreshGraphs().AIGetGraphFor(MyPosition);
        int walkableDistance = 30; // TODO prio 2 use timer value here

        await UniTask.Delay(500, cancellationToken: cancellationToken);

        // Check for bonnus boxes on our path
        // Log.Message("AI Dance", "Looking for boxes");
        Vector2? boxGoal = await AIFindPointWithSmallestPath(graph, map.AIFindBox(), walkableDistance * 0.6f).AttachExternalCancellation(cancellationToken);
        if (boxGoal.HasValue) {
            // Log.Message("AI Dance", "Found an accessible box, moving");
            await AIMoveTo(graph, boxGoal.Value).AttachExternalCancellation(cancellationToken);
            walkableDistance -= 15;
        }

        // Check for enemy on our path
        // Log.Message("AI Dance", "Looking for enemy");
        IEnumerable<Vector2> enemies = map.AIFindEnemies();
        Vector2? enemyGoal = await AIFindPointWithSmallestPath(graph, enemies, walkableDistance).AttachExternalCancellation(cancellationToken);
        if (enemyGoal.HasValue) {
            // Log.Message("AI Dance", "Found an accessible enemy, moving");
            await AIDropGrenade(graph, enemyGoal.Value).AttachExternalCancellation(cancellationToken); // Exception propagates
            return;
        }

        // Log.Message("AI Dance", "Check where my line of sight is maximal");
        Vector2 positionToFire = AIBestPositionToFire(graph, walkableDistance);
        // Go there and simulate a shot
        await AIMoveTo(graph, positionToFire).AttachExternalCancellation(cancellationToken);
        while (ShieldAmmoController.IntoShield(this)) {
            var newPosition = graph.Points.GetRandomValue();
            await AIMoveTo(graph, newPosition).AttachExternalCancellation(cancellationToken);
        }
        await UniTask.Delay(500, cancellationToken: cancellationToken);

        SayNow((int) Phrases.Think, VoiceManager.PHRASE_THINK).AttachExternalCancellation(cancellationToken).Forget();

        CharacterTargetHit? hit = await GameManager.Instance.AITestWeapons(this).AttachExternalCancellation(cancellationToken);
        if (hit.HasValue) {
            // Log.Message("AI Dance", "Found a valid shot");
            await CharacterWeapon.AIFireTargetHit(hit.Value, cancellationToken).AttachExternalCancellation(cancellationToken); // Exception propagates
            return;
        }

        // Fallbacks

        // Bazooka to closest enemy
        // Log.Message("AI Dance", "Did no find any valid shot");
        if (await AIFireBazookaToEnemy(enemies).AttachExternalCancellation(cancellationToken)) { // Exception propagates
            // Log.Message("AI Dance", "Fallback shot bazooka to closest enemy");
            return;
        }

        // Log.Message("AI Dance", "Fallback beam 45 degres");
        // Beam on the right
        var beamHit = Physics2D.CircleCast(MyPosition + new Vector2(5, 7), 4.5f, Vector2.up, 0.01f);
        // Debugging.DrawCircle(MyPosition + new Vector2(5, 7), 4.5f, Color.green, 20f, 4);
        if (!beamHit) {
            if (!CharacterMove.IsFacingRight) {
                CharacterMove.Flip();
                await AIWaitFrame();
            }
            await CharacterWeapon.AIFireBeam(45, cancellationToken).AttachExternalCancellation(cancellationToken); // Exception propagates
            return;
        }
        // Beam on the left
        beamHit = Physics2D.CircleCast(MyPosition + new Vector2(-5, 7), 4.5f, Vector2.up, 0.01f);
        // Debugging.DrawCircle(MyPosition + new Vector2(-5, 7), 4.5f, Color.green, 20f, 4);
        if (!beamHit) {
            if (CharacterMove.IsFacingRight) {
                CharacterMove.Flip();
                await AIWaitFrame();
            }
            await CharacterWeapon.AIFireBeam(45, cancellationToken).AttachExternalCancellation(cancellationToken); // Exception propagates
            return;
        }

        // Log.Message("AI Dance", "Fallback shot bazooka 45 degres");
        if (map.AIMostEnemiesDirection(MyPosition) > 0 /* Right */ !=  CharacterMove.IsFacingRight) {
            CharacterMove.Flip();
            await AIWaitFrame();
        }
        await CharacterWeapon.AIFireBazooka(angle: 45, power: 0.85f, cancellationToken).AttachExternalCancellation(cancellationToken); // Exception propagates
    }

    private async UniTask AIStateRetreat(bool hasRetreat) // TODO use hasRetreat
    {
        if (!IsActive) throw new OperationCanceledException();
        // Wait for the fire to be over
        await UniTask.WaitUntil(() => !CharacterWeapon.IsWeaponFiring, PlayerLoopTiming.Update, cancellationToken: cancellationToken);

        if (!IsActive) throw new OperationCanceledException();
        // TODO prio 2 it would be best to be able to wait for the explosion etc so the map is in its last state
        // overwise the AI is often falling in a newly created hole
        var map = GameManager.Instance.MapController;
        var graph = map.AIRefreshGraphs().AIGetGraphFor(transform.position);

        // Log.Message("AI Dance", "Check where my line of sight is minimal");
        Vector2 positionToHide = AIBestPositionToHide(graph, 10);
        // Log.Message("AI Dance", "Found where it is minimal, moving");
        // Go there
        await AIMoveTo(graph, positionToHide).AttachExternalCancellation(cancellationToken);
    }

    /// <summary>
    /// Look for a point where we can maximize our sight
    /// </summary>
    private Vector2 AIBestPositionToFire(Graph graph, float maxDistance)
    {
        if (!IsActive) throw new OperationCanceledException();

        // TODO prio 2 by doing that our AI is often going in the same place
        // it may be more humain to add some randomness
        var map = GameManager.Instance.MapController;
        var candidates = new List<GraphPoint>();
        if (map.AIMostEnemiesDirection(transform.position) <= 0) { // Left
            candidates.AddRange(AICalculatePathTo(graph, new Vector2(0, map.CalculatedHeight)));
        } else { // Right
            candidates.AddRange(AICalculatePathTo(graph, new Vector2(map.CalculatedWidth, map.CalculatedHeight)));
        }
        // Remove one out of 2 to add some randomness
        candidates = candidates.Where(c => RandomNum.RandomOneOutOf(2)).ToList();
        Vector2 goal = transform.position;
        float biggestDistance = float.MinValue;
        var directions = new Vector2[] {
            Vector2.up, Vector2.left, Vector2.right,
            new Vector2(+1, +1), new Vector2(-1, -1),
            new Vector2(+1, -1), new Vector2(-1, +1),
        };
        foreach (var p in candidates) {
            var raycastPossibles = map.AIRaycastPossibles(p.Point + Vector2.up, directions, map.CalculatedHeight);
            if (raycastPossibles > biggestDistance) {
                biggestDistance = raycastPossibles;
                goal = p.Point;
            }
        }

        return goal;
    }

    private Vector2 AIBestPositionToHide(Graph graph, float maxDistance)
    {
        if (!IsActive) throw new OperationCanceledException();

        var map = GameManager.Instance.MapController;
        var candidates = new List<GraphPoint>();
        candidates.AddRange(AICalculatePathTo(graph, new Vector2(0, map.CalculatedHeight)));
        candidates.AddRange(AICalculatePathTo(graph, new Vector2(map.CalculatedWidth, map.CalculatedHeight)));

        // Look for a point where our line of sight is minimal on one side only (left or right but not both),
        // it means that our character is not very accessible (because close to a wall) but
        // not into a hole (because of the openness on one side).
        // Add a bonus if not close to another Character.
        // Add a big penalty if it is near a WeaponDropable.
        // Extra bonus for higher positions.

        // TODO prio 2 by doing that our AI is often going in the same place
        // it may be more humain to add some randomness
        Vector2 goal = transform.position;
        float biggestValue = float.MinValue;
        var distance = 50f;
        foreach (var p in candidates) {
            // Check for terrain/walls
            var tLeft  = Physics2D.Raycast(p.Point + Vector2.up, Vector2.left,  distance, Layer.Terrain.Mask);
            var tRight = Physics2D.Raycast(p.Point + Vector2.up, Vector2.right, distance, Layer.Terrain.Mask);
            var sum = Mathf.Abs((tLeft ? tLeft.distance : distance) - (tRight ? tRight.distance : distance));
            // Check for characters nearby
            var cLeft  = Physics2D.Raycast(p.Point + Vector2.up, Vector2.left,  distance / 2f, Layer.Character.Mask);
            var cRight = Physics2D.Raycast(p.Point + Vector2.up, Vector2.right, distance / 2f, Layer.Character.Mask);
            sum += (cLeft ? cLeft.distance : distance / 2f) - (cRight ? cRight.distance : distance / 2f);
            // Check for weapon
            if (Physics2D.CircleCast(p.Point, 10f, Vector2.up, 0.01f, Layer.WeaponDropable.Mask)) {
                sum -= distance * 2f;
            }
            // Extra bonus for higher position
            sum += p.Point.y;
            if (sum > biggestValue) {
                biggestValue = sum;
                goal = p.Point;
            }
        }

        return goal;
    }

    private async UniTask AIDropGrenade(Graph graph, Vector2 goal)
    {
        if (!IsActive) throw new OperationCanceledException();
        // Go there
        await AIMoveTo(graph, goal).AttachExternalCancellation(cancellationToken);
        // Drop grenade
        await CharacterWeapon.AIDrop(Weapon.AmmoEnum.Grenade4, cancellationToken).AttachExternalCancellation(cancellationToken); // Exception propagates
    }

    private async UniTask<bool> AIFireBazookaToEnemy(IEnumerable<Vector2> enemies)
    {
        if (!IsActive) throw new OperationCanceledException();
        foreach (Vector2 enemyPosition in enemies) {
            Vector2 myPositionUp = MyPosition + Vector2.up;
            Vector2 directionEnemy = Basics.VectorBetween(myPositionUp, enemyPosition).normalized;
            // Debugging.DrawLine(myPositionUp, myPositionUp + directionEnemy * 10f, Color.green, 20f);
            if (!Physics2D.Raycast(myPositionUp, directionEnemy, 10f, Layer.Terrain.Mask)) {
                if (enemyPosition.x > myPositionUp.x /* Enemy is on my right */ != CharacterMove.IsFacingRight) {
                    CharacterMove.Flip();
                    await AIWaitFrame();
                }
                float angle = WeaponManager.DirectionToAngle(directionEnemy, CharacterMove.IsFacingRight);
                await CharacterWeapon.AIFireBazooka(angle, power: 1, cancellationToken).AttachExternalCancellation(cancellationToken); // Exception propagates
                return true;
            }
        }
        return false;
    }

    private void AISetIdleState()
    {
        if (AICancellationTokenSource == null) return;
        AICancellationTokenSource.Cancel();
    }

    private void AISetDead()
    {
        if (AICancellationTokenSource == null) return;
        AICancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Given a list of points and a graph
    /// Return the point where the path to go there is the smallest
    /// </summary>
    private async UniTask<Vector2?> AIFindPointWithSmallestPath(Graph graph, IEnumerable<Vector2> points, float maxDistance)
    {
        if (!IsActive) throw new OperationCanceledException();
        await UniTask.WaitForEndOfFrame(cancellationToken); // TODO prio 5 make a thread for path computation
        float minimal = float.MaxValue;
        Vector2? found = null;
        foreach (var p in points) {
            var point = graph.PointIsNearGraph(p, 1f);
            if (!point.HasValue) continue;
            var distance = (p - (Vector2)transform.position).magnitude;
            if (distance > maxDistance) continue;
            if (distance < minimal) {
                minimal = distance;
                found = point;
            }
        }
        return found;
    }

    private IEnumerable<GraphPoint> AICalculatePathTo(Graph graph, Vector2 to)
    {
        if (!IsActive) throw new OperationCanceledException();
        return GameManager.Instance.MapController
            .AICalculatePathOnGraph(transform.position, to, graph);
    }

    private async UniTask AIMoveTo(Graph graph, Vector2 goal)
    {
        if (!IsActive) throw new OperationCanceledException();
        // Log.Message("AI Pathfinder", "Will move from " + transform.position + " to " + goal);

        var points = AICalculatePathTo(graph, goal).ToList();
        for (int i = 0, max = points.Count - 1; i < max; i++) {

            // Debugging.DrawPoint(transform.position, Color.blue, 5f);
            Graph.DrawDebugGraph(graph, 5f);
            Graph.DrawDebugPath(points, graph, 5f);

            var orientedSegment = new OrientedSegment(points[i].Point, points[i + 1].Point);
            var type = graph.GetSegmentType(orientedSegment.ToSegment());

            if (type == Graph.SegmentType.Link) {
                await AIAgentHandleJump(orientedSegment, goal).AttachExternalCancellation(cancellationToken);
            } else {
                await AIAgentHandleSegment(orientedSegment, goal).AttachExternalCancellation(cancellationToken);
            }
        }

        await UniTask.DelayFrame(3, cancellationToken: cancellationToken);
        CharacterMove.AIMoveForceStop();
        // Log.Message("AI Pathfinder", "Moved to " + goal + " with success");
    }

    private async UniTask AIAgentHandleSegment(OrientedSegment orientedSegment, Vector2 goal)
    {
        // Log.Message("AI Pathfinder", "Segment start: " + orientedSegment);
        var segmentDirection = SegmentDirection(orientedSegment);
        float time = 0;

        while (true) {
            if (!IsActive) throw new OperationCanceledException();

            // Check if the segment has been traversed by
            // looking if the goal has been bypassed
            if (AIAgentByPassedSegment(segmentDirection, orientedSegment)) {
                CharacterMove.MoveStop();
                // Log.Message("AI Pathfinder", "Segment done: " + orientedSegment);
                return;
            }

            CharacterMove.MoveAction(segmentDirection);
            await AIWaitFrame();
            time += Time.deltaTime;

            if (time > 2f) {
                // Log.Message("AI Pathfinder", "Stuck on segment, jumping");
                // Convert this segment into a forced jump
                await AIAgentHandleJump(new OrientedSegment(orientedSegment.Start, orientedSegment.End + Vector2.up * 3f), goal).AttachExternalCancellation(cancellationToken);
                return;
            }
        }
    }

    private async UniTask AIAgentHandleJump(OrientedSegment orientedSegment, Vector2 goal)
    {
        AIJumpFirst = false;
        AIJumpSecond = false;
        AIJumpKind = AIJumpType.Normal;
        AIJumpPreviousPosition = transform.position;

        var segmentDirection = SegmentDirection(orientedSegment);
        float angle = Mathf.Abs(Vector2.SignedAngle(
            orientedSegment.End - orientedSegment.Start,
            orientedSegment.End.x > orientedSegment.Start.x ? Vector2.right : Vector2.left)
        );

        if (orientedSegment.Start.y > orientedSegment.End.y) {
            AIJumpKind = AIJumpType.Down;
        } else if(angle > 50) {
            AIJumpKind = AIJumpType.StraitUp;
        }

        // Log.Message("AI Pathfinder", "Jump start: " + orientedSegment + "\n" + "type: " + AIJumpKind + " angle: " + angle);
        while (true) {
            if (!IsActive) throw new OperationCanceledException();

            if (!AIJumpFirst) {
                AIJumpFirst = true;
                CharacterMove.JumpAction(); // first jump
            } else {
                // Check if the jump has been successful either by:
                // - checking if character is grounded
                // - looking if the goal has been bypassed
                if (CharacterMove.IsGrounded || AIAgentByPassedSegment(segmentDirection, orientedSegment)) {
                    CharacterMove.MoveStop();
                    if (CharacterMove.IsGrounded) {
                        // Jump is over
                        // Log.Message("AI Pathfinder", "Jump done: " + orientedSegment);
                        return;
                    }
                } else {
                    // Character is going down: try a second jump
                    if (
                        !AIJumpSecond &&
                        transform.position.y < AIJumpPreviousPosition.y &&
                        AIJumpKind != AIJumpType.Down // if linkType = down, do not make the second jump
                    ) {
                        AIJumpSecond = true;
                        CharacterMove.JumpAction(); // second jump
                        if (AIJumpKind == AIJumpType.StraitUp) {
                            AIJumpKind = AIJumpType.Normal;
                        }
                    }

                    if (AIJumpKind != AIJumpType.StraitUp) {
                        CharacterMove.MoveAction(segmentDirection);
                    }
                }
            }

            AIJumpPreviousPosition = transform.position;
            await AIWaitFrame();
        }
    }

    private bool AIAgentByPassedSegment(int segmentDirection, OrientedSegment orientedSegment)
    {
        if (transform == null) throw new OperationCanceledException();
        return
            (segmentDirection == CharacterMove.MOVE_ACTION_LEFT  && transform.position.x < orientedSegment.End.x) ||
            (segmentDirection == CharacterMove.MOVE_ACTION_RIGHT && transform.position.x > orientedSegment.End.x)
        ;
    }

    private static int SegmentDirection(OrientedSegment segment)
    {
        if (segment.Start.x - segment.End.x < 0) {
            return CharacterMove.MOVE_ACTION_RIGHT;
        } else {
            return CharacterMove.MOVE_ACTION_LEFT;
        }
    }

    private static int AIAgentFindDirection(Vector2 agentPosition, Vector2 goalPosition)
    {
        // Goal is on the right relative to agent
        if (agentPosition.x - goalPosition.x < 0) {
            return CharacterMove.MOVE_ACTION_RIGHT;
        } else {
            return CharacterMove.MOVE_ACTION_LEFT;
        }
    }

    #endregion
}
