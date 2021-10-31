using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CC;
using System.Linq;
using CC.StreamPlay;
using Jrmgx.Helpers;
using FMODUnity;
using Cysharp.Threading.Tasks;
using System;

public abstract class RoundControllerBase : NetworkObject, RoundControllerInterface, CharacterManagerDelegate
{
    #region Fields

    [Header("Prefabs")]
    [SerializeField] protected BoxLifeController boxLifePrefab = default;
    [SerializeField] protected BoxWeaponController boxWeaponPrefab = default;

    [Header("Sound")]
    [SerializeField] protected StudioEventEmitter soundEventAmbientJungle = default;
    [SerializeField] protected StudioEventEmitter soundEventAmbientDeath = default;
    [SerializeField] protected StudioEventEmitter soundEventLavaRise = default;
    [SerializeField] protected StudioEventEmitter soundEventTimerUrgent = default;

    public int RoundLockIdentifier => RoundLock.IdentifierRoundController;

    // State
    public float WindForce { get; private set; } = 0f;
    public bool CurrentPlayerRedTeam { get; set; } = true;
    protected bool isRoundActive;
    protected int totalHealth = 0;
    protected int roundCount = 0;
    protected bool suddenDeathPhase;
    public PlayerController CurrentPlayerController { get; protected set; }
    public CharacterManager CurrentCharacterManager { get; protected set; }
    private RainbowAmmoController rainbowInstance;
    private int rainbowActive;

    // References
    public MapController MapController { get; set; }
    protected List<CharacterManager> characterManagers = new List<CharacterManager>();
    protected Timer turnTimer;
    protected Timer retreatTimer;

    protected PlayerController playerOne => GameManager.Instance.PlayerControllers[0];
    protected PlayerController playerTwo => GameManager.Instance.PlayerControllers[1];

    #endregion

    #region Init

    public async UniTask ReadyForStart()
    {
        // Log.Message("InitDance", "Round ReadyForStart");
        totalHealth = playerOne.HealthSum();
        if (Config.WithMusic && soundEventAmbientJungle != null) {
            soundEventAmbientJungle.Play();
        }

        CacheCharactersReferences();

        if (IsNetworkOwner) {
#if CC_EXTRA_CARE
try {
#endif
            await WaitForCharacters().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        await UniTask.WaitUntil(() => MapController.IsReady).CancelOnDestroy(this);
        OnMapControllerIsReady();

        if (!IsNetwork) {
#if CC_EXTRA_CARE
try {
#endif
            await InterRound().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        } else if (IsNetworkOwner) {
#if CC_EXTRA_CARE
try {
#endif
            await IsReady_Owner().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    protected void CacheCharactersReferences()
    {
        // Log.Message("InitDance", "Round CacheCharactersReferences");
        foreach (PlayerController playerController in GameManager.Instance.PlayerControllers) {
            characterManagers.AddRange(playerController.CharacterManagers);
        }
    }

    protected async UniTask WaitForCharacters()
    {
        // Log.Message("InitDance", "Round WaitForCharacters");
        // We need to wait for the character to be Inited as positioning is done async too
        // Log.Message("InitDance", "Round Wait Until AllCharactersInited");
        await UniTask.WaitUntil(AreAllCharactersInited).CancelOnDestroy(this);
        // Log.Message("InitDance", "Round Wait Until AllCharactersInited OK");
        UIInitData(
            Config.ColorPlayerOne, Config.ColorPlayerTwo,
            playerOne.Characters().Count(), playerOne.HealthSum(),
            playerTwo.Characters().Count(), playerTwo.HealthSum(),
            playerOne.HealthSum()
        );
        NetworkRecordSnapshotInstant(
            Method_UIInitData,
            Config.ColorPlayerOne, Config.ColorPlayerTwo,
            playerOne.Characters().Count(), playerOne.HealthSum(),
            playerTwo.Characters().Count(), playerTwo.HealthSum(),
            playerOne.HealthSum()
        );

#if CC_EXTRA_CARE
try {
#endif
        await MapController.StateSecondPass().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    protected void OnMapControllerIsReady()
    {
        // Log.Message("InitDance", "Round OnMapControllerIsReady");
        DesactivatePlayers();
    }

    protected void DesactivatePlayers()
    {
        playerOne.Desactivate();
        playerTwo.Desactivate();
    }

    protected async UniTask IsReady_Owner()
    {
        // Log.Message("InitDance", "Round IsReady_Owner");

        // Signal to guest that owner is ready
        NetworkRecordSnapshotInstant(Method_RoundIsReady);
        // Wait for guest ready
#if CC_EXTRA_CARE
try {
#endif
        // Log.Message("InitDance", "Round WaitForPlayerMessage from Guest: NETWORK_MESSAGE_ROUND_READY");
        await StreamPlayRecorder.WaitForPlayerMessage(NETWORK_MESSAGE_ROUND_READY).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        StreamPlayRecorder.Begin(); // Start a new stream

        // Switch Owner/Guest role if the player who should start is not the Owner
        if (IsNetwork && !CurrentPlayerRedTeam) {
#if CC_EXTRA_CARE
try {
#endif
             await AskForRolesSwitch().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        } else {
#if CC_EXTRA_CARE
try {
#endif
            await InterRound().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    [StreamPlay(Method_RoundIsReady)]
    protected async UniTask IsReady_Guest()
    {
        // Log.Message("InitDance", "Round IsReady_Guest");
        // Send ready to owner
#if CC_EXTRA_CARE
try {
#endif
        // Log.Message("InitDance", "Round SendPlayerMessage to Owner: NETWORK_MESSAGE_ROUND_READY");
        await StreamPlayPlayer.SendPlayerMessage(NETWORK_MESSAGE_ROUND_READY).CancelOnDestroy(this);
        await StreamPlayPlayer.WaitForFinish().CancelOnDestroy(this);
        await StreamPlayPlayer.Stop().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        StreamPlayPlayer.Play(NetworkManagerInstance.HardLatency);
    }

    #endregion

    #region Round Logic

    protected virtual async UniTask InterRound()
    {
        // Log.Message("InitDance", "Round InterRound");
        // Log.Message("RoundController", "Physics2D.autoSimulation = true");
        Physics2D.autoSimulation = true;

        VoiceManager.Instance.ResetState();
        CameraManager.Instance.FollowPointerRelease();

        var roundCountNext = roundCount + 1;
        UpdateRoundCount(roundCountNext);
        NetworkRecordSnapshot(Method_UpdateRoundCount, roundCountNext);

        RainbowDecrease();
        NetworkRecordSnapshot(Method_RainbowDecrease);

        // Log.Message("RoundController", "New Inter Round: " + roundCount);

        // Sudden death, start if needed
        if (!suddenDeathPhase && roundCount >= GameManager.Instance.CurrentGameOption.LavaRiseRoundCount) {
#if CC_EXTRA_CARE
try {
#endif
            await StartSuddenDeath().CancelOnDestroy(this);
            NetworkRecordSnapshot(Method_StartSuddenDeath);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            foreach (var c in characterManagers) {
                if (c.IsDead) continue;
                c.AddDamages(c.Health - 1);
                // if (GameManager.Instance.CurrentGameOption.PlayingCaptureTheKing && c.IsKingToCapture) {
                //     c.KingCaptured();
                // }
            }
            await CalculateDamages(invert: false);
        }

        // Spawning a box, will wait 2 sec if a box appears
#if CC_EXTRA_CARE
try {
#endif
        await InstantiateRandomBox().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        // Change wind
        if (GameManager.Instance.CurrentGameOption.WithWind) {
            // Extreme wind is rarer and even forbidden in death mode
            var rand = UnityEngine.Random.Range(-0.5f, 0.5f);
            if (!suddenDeathPhase && RandomNum.RandomOneOutOf(4)) {
                rand = UnityEngine.Random.Range(-1f, 1f);
            }
            UpdateWind(rand);
            NetworkRecordSnapshot(Method_UpdateWind, rand);
        }

        // TODO prio 2 if !IsNetwork change the text
        // TODO prio 1 if Training prevent this (it may not show because of TimeInterRound == 0)
        if (!GameManager.Instance.CurrentGameOption.PlayingAgainstAI || CurrentPlayerRedTeam) {
            UserInterfaceManager.Instance.UpdateInfoText((CurrentPlayerRedTeam ? Config.PlayerOneName : Config.PlayerTwoName) + " Get Ready!");
        }

        SelectNextPlayerAndCharacter();
        NetworkRecordSnapshot(Method_SelectNextPlayerAndCharacter);

        if (GameManager.Instance.CurrentGameOption.TimeInterRound > 0) {
            await UniTask.Delay(millisecondsDelay: GameManager.Instance.CurrentGameOption.TimeInterRound * 1000, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        UserInterfaceManager.Instance.HideInfoText();

        // Start actual turn timer
        turnTimer = new Timer(RoundTimer(GameManager.Instance.CurrentGameOption.TimeRound));
        turnTimer.Start();

        // Activate next character
        ActivateCharacter();
        NetworkRecordSnapshot(Method_ActivateCharacter);

        // Round is ready, Update() will be called and our timer will run
        // Log.Message("RoundController", "round is active (means Update running)");
        isRoundActive = true;
    }

    protected async UniTask Update() // FixedUpdate?
    {
        if (!isRoundActive) return;

        // Current round finish because of some timer is over
        if (IsAnyTimerOver()) {
            // Log.Message("RoundController", "A timer is over");
#if CC_EXTRA_CARE
try {
#endif
            await InteruptRound().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    protected virtual void RetreatRound()
    {
        // Log.Message("RoundController", "Retreat round called");
        CameraManager.Instance.FollowPointerRelease();
        StopAndClearTimers();
        retreatTimer = new Timer(RetreatRoundTimer(GameManager.Instance.CurrentGameOption.TimeRetreat));
        retreatTimer.Start();
    }

    protected async UniTask InteruptRound()
    {
        // Log.Message("RoundController", "Interupt round called, round is NOT active (means update() stopped)");
        CameraManager.Instance.FollowPointerRelease();
        isRoundActive = false;
        StopAndClearTimers();
#if CC_EXTRA_CARE
try {
#endif
        await CloseRound().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    protected virtual async UniTask CloseRound()
    {
        ClosePopup(); // If open

        // Log.Message("RoundController", "Close round called");
        DeactivateCharacter();
        NetworkRecordSnapshot(Method_DeactivateCharacter);

        // Wait for physic lock
        await GameManager.Instance.RoundLock.WaitWhileIsLocked().CancelOnDestroy(this);

#if CC_EXTRA_CARE
try {
#endif
        await UniTask.WhenAll( // .CancelOnDestroy(this)
            CalculateDamages(rainbowActive > 0),
            VoiceManager.Instance.SayBestPhrase()
        ).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        // Wait for physic lock that could come from death explosions
#if CC_EXTRA_CARE
try {
#endif
        await GameManager.Instance.RoundLock.WaitWhileIsLocked().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        bool needNewCloseRound = false;
        foreach (CharacterManager characterManager in characterManagers) {
            if (characterManager.PendingDamages > 0) {
                // Log.Message("Damages", "Some character has pending damages, ask for a new round of calculus");
                needNewCloseRound = true;
                break;
            }
        }

        // Recurse while all the pending damages are not applied
        if (needNewCloseRound) {
#if CC_EXTRA_CARE
try {
#endif
            await UniTask.Delay(Config.DelayDamagesNewCalculus, cancellationToken: this.GetCancellationTokenOnDestroy());
            await CloseRound().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            return;
        }

        // Sudden death logic
        if (suddenDeathPhase) {
            // Check if someone won before rising the lava, because rising the lava can kill a character
            var (playerWonEarly, messageEarly) = IsAnyPlayerWon();
            if (playerWonEarly) {
                GameOver(messageEarly);
                NetworkRecordSnapshot(Method_GameOver, messageEarly);
                return;
            }

#if CC_EXTRA_CARE
try {
#endif
            await RiseLava().CancelOnDestroy(this);
            NetworkRecordSnapshot(Method_RiseLava);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            CheckLava(); // will fill up RoundLock if something collides

            // Wait for a second death lock that would be triggered by the lava touching a character
            await GameManager.Instance.RoundLock.WaitWhileIsLocked().CancelOnDestroy(this);
        }

        UIUpdateData(
            playerOne.Characters().Count(), playerOne.HealthSum(),
            playerTwo.Characters().Count(), playerTwo.HealthSum(),
            totalHealth
        );
        NetworkRecordSnapshot(
            Method_UIUpdateData,
            playerOne.Characters().Count(), playerOne.HealthSum(),
            playerTwo.Characters().Count(), playerTwo.HealthSum(),
            totalHealth
        );

        SyncCharacters();
        SyncMap();

        var (playerWon, message) = IsAnyPlayerWon();
        if (playerWon) {
            GameOver(message);
            NetworkRecordSnapshot(Method_GameOver, message);
            return;
        }

#if CC_EXTRA_CARE
try {
#endif
        if (IsNetwork) {
            await AskForRolesSwitch().CancelOnDestroy(this);
        } else {
            await InterRound().CancelOnDestroy(this);
        }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

    }

    protected async UniTask AskForRolesSwitch()
    {
        // Owner part of the method
        // Log.Message("RoundController", "Physics2D.autoSimulation = false");
        Physics2D.autoSimulation = false;
        // Log.Message("RoundController", "Send Snapshot_Guest Method_RolesSwitched");
        NetworkRecordSnapshot(Method_RolesSwitched);
        // Log.Message("RoundController", "AskForRolesSwitch called");
#if CC_EXTRA_CARE
try {
#endif
        await StreamPlayRecorder.WaitForEnd().CancelOnDestroy(this);
        await StreamPlayRecorder.End().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        // Wait for other to confirm switch
        // Log.Message("RoundController", "Wait for PlayerMessage NETWORK_MESSAGE_WAIT_FOR_SWITCH");
#if CC_EXTRA_CARE
try {
#endif
        await StreamPlayRecorder.WaitForPlayerMessage(NETWORK_MESSAGE_WAIT_FOR_SWITCH).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        // SWITCH
        GameManager.Instance.SetOwner(false);

        // Guest part of the method
        // Log.Message("RoundController", "Send PlayerMessage NETWORK_MESSAGE_SWITCH_SUCCESS");
#if CC_EXTRA_CARE
try {
#endif
        await StreamPlayPlayer.SendPlayerMessage(NETWORK_MESSAGE_SWITCH_SUCCESS).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        StreamPlayPlayer.Play(NetworkManagerInstance.HardLatency);
    }

    [StreamPlay(Method_RolesSwitched)]
    protected async UniTask RolesSwitched()
    {
        // Guest part of the method
        // Log.Message("RoundController", "RolesSwitched called");
#if CC_EXTRA_CARE
try {
#endif
        await StreamPlayPlayer.WaitForFinish().CancelOnDestroy(this);
        await StreamPlayPlayer.Stop().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        // Send to owner that switch is done
        // Log.Message("RoundController", "Send PlayerMessage NETWORK_MESSAGE_WAIT_FOR_SWITCH");
#if CC_EXTRA_CARE
try {
#endif
        await StreamPlayPlayer.SendPlayerMessage(NETWORK_MESSAGE_WAIT_FOR_SWITCH).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        // SWITCH
        GameManager.Instance.SetOwner(true);

        // Owner part of the method
        // Log.Message("RoundController", "Wait for PlayerMessage NETWORK_MESSAGE_SWITCH_SUCCESS");
#if CC_EXTRA_CARE
try {
#endif
        await StreamPlayRecorder.WaitForPlayerMessage(NETWORK_MESSAGE_SWITCH_SUCCESS).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        StreamPlayRecorder.Begin();
#if CC_EXTRA_CARE
try {
#endif
        await InterRound().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    [StreamPlay(Method_GameOver)]
    protected virtual void GameOver(string message)
    {
        GameManager.Instance.GameOver(message);
    }

    // Called from GameManager when the game is about to stop (back to menu)
    public void StopGame()
    {
        StopAndClearTimers();
        StopAllCoroutines();
    }

    [StreamPlay(Method_UpdateRoundCount)]
    protected void UpdateRoundCount(int roundCount)
    {
        this.roundCount = roundCount;
        UserInterfaceManager.Instance.UpdateRoundCount(roundCount);
    }

    // Value is from -1f to 1f
    [StreamPlay(Method_UpdateWind)]
    protected void UpdateWind(float windForce)
    {
        WindForce = windForce;
        UserInterfaceManager.Instance.UpdateWind(WindForce);
        MapController.UpdateWind(WindForce);
    }

    // Character already have their damages kept in sync
    public virtual async UniTask CalculateDamages(bool invert)
    {
        // Log.Message("Damages", "RoundController: Calculate damages");
#if CC_EXTRA_CARE
try {
#endif
        await UniTask.WhenAll(characterManagers.Select(i => i.CalculateDamage(invert))).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    #endregion

    #region Rainbow

    public void RainbowStart(RainbowAmmoController rainbowAmmoController)
    {
        rainbowInstance = rainbowAmmoController;
        rainbowActive = 3;
        NetworkRecordSnapshot(Method_RainbowStart);
    }

    [StreamPlay(Method_RainbowStart)]
    protected void RainbowStart()
    {
        rainbowActive = 3;
    }

    [StreamPlay(Method_RainbowDecrease)]
    protected void RainbowDecrease()
    {
        if (rainbowActive <= 0) return;
        rainbowActive--;
        if (rainbowActive <= 0 && rainbowInstance != null) {
            rainbowInstance.End();
        }
    }

    #endregion

    #region Skip Turn

    public virtual void AskForSkipTurn()
    {
        PopupManager.Init(
            "Skip your turn?", "Are you sure you want to skip your turn?",
            "No", () => {},
            "Skip", () => {
#if CC_EXTRA_CARE
try {
#endif
                InteruptRound().Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            }
        ).Show();
    }

    protected virtual void ClosePopup()
    {
        PopupManager.Hide();
    }

    #endregion

    #region Sudden Death

    [ContextMenu("Start Sudden Death")]
    [StreamPlay(Method_StartSuddenDeath)]
    protected async UniTask StartSuddenDeath()
    {
        suddenDeathPhase = true;
        UserInterfaceManager.Instance.UpdateInfoText("Sudden Death!");

        if (Config.WithMusic) {
            soundEventAmbientJungle.Stop();
            soundEventAmbientDeath.Play();
        }

        await UniTask.Delay(3000, cancellationToken: this.GetCancellationTokenOnDestroy()); // 3 secs
        UserInterfaceManager.Instance.HideInfoText();
    }

    [StreamPlay(Method_RiseLava)]
    protected async UniTask RiseLava()
    {
        soundEventLavaRise.Play();
        CameraManager.Instance.ShakeCamera(4, LiquidController.LiquidRiseTime / 2f);
#if CC_EXTRA_CARE
try {
#endif
        await UniTask.WhenAll( // .CancelOnDestroy(this)
            MapController.LiquidController.Rise(LiquidController.LiquidRiseLevel, LiquidController.LiquidRiseTime),
            CameraManager.Instance.RiseBounds(LiquidController.LiquidRiseLevel, LiquidController.LiquidRiseTime)
        ).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    protected void CheckLava()
    {
        // NetworkAssertNotGuest();

        GameManager.Instance.RoundLock.LockChainReaction(this);
        foreach (var c in characterManagers) {
            if (MapController.LiquidController.OverlapPosition(c.transform.position)) {
                c.DeadOutOfBounds(Config.TagBoundLava);
            }
        }
        GameManager.Instance.RoundLock.UnlockChainReaction(this);
    }

    #endregion

    #region Character Delegate

    public void CharacterStartedLoadFire()
    {
        // Log.Message("RoundController", "Started Load Fire");
        turnTimer.Stop();
    }

    public void CharacterAskRetreat(bool hasRetreat)
    {
        // Log.Message("RoundController", "Ask Retreat");
        if (hasRetreat) {
            RetreatRound();
        } else {
#if CC_EXTRA_CARE
try {
#endif
            InteruptRound().Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
    }

    public void CharacterAskStopRound()
    {
        // Log.Message("RoundController", "Stop Round");
        if (!isRoundActive) return;
#if CC_EXTRA_CARE
try {
#endif
        InteruptRound().Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    public virtual void CharacterFall() { }

    #endregion

    #region Box

    protected async UniTask InstantiateRandomBox()
    {
        if (roundCount < 3) return;
        if (suddenDeathPhase) return;
        var count = FindObjectsOfType<BoxControllerBase>().Length;
        if (count < GameManager.Instance.CurrentGameOption.BoxCount) {
            if (RandomNum.RandomOneOutOf(GameManager.Instance.CurrentGameOption.BoxOneOutOfByRound)) {
#if CC_EXTRA_CARE
try {
#endif
                await (RandomNum.RandomOneOutOf(4) ? InstantiateHealthBox() : InstantiateRandomWeaponBox()).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            }
        }
    }

    [ContextMenu("Add Health Box")]
    protected async UniTask InstantiateHealthBox()
    {
        BoxLifeController box = NetworkInstantiate(boxLifePrefab);
        Vector2? position = null;
#if CC_EXTRA_CARE
try {
#endif
        position = await MapController.PositionOnMap(box, 5, 90f, canFail: true).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        if (!position.HasValue) return;

        box.SetPosition(position.Value);
        box.SetSymmetry(RandomNum.RandomOneOutOf(2)); // Not sync on purpose
        CameraManager.Instance.RequestFollow(box.transform);
        NetworkRecordSnapshot(Method_InstantiateHealthBox_Guest, box.NetworkIdentifier, position.Value);
        await UniTask.Delay(Config.DelayBox, cancellationToken: this.GetCancellationTokenOnDestroy()); // Wait a bit so the player can see the box
    }

    [StreamPlay(Method_InstantiateHealthBox_Guest)]
    protected void InstantiateHealthBox_Guest(int ownerNetworkIdentifier, Vector2 position)
    {
        BoxLifeController box = NetworkInstantiate(boxLifePrefab, ownerNetworkIdentifier);
        box.SetPosition(position);
        CameraManager.Instance.RequestFollow(box.transform);
    }

    [ContextMenu("Add Weapon Box")]
    protected async UniTask InstantiateRandomWeaponBox()
    {
        BoxWeaponController box = NetworkInstantiate(boxWeaponPrefab);
        Vector2? position = null;
#if CC_EXTRA_CARE
try {
#endif
        position = await MapController.PositionOnMap(box, 5, 90f, canFail: true).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        if (!position.HasValue) return;

        Weapon weapon = WeaponManager.Instance.WeaponsExtra.GetRandomValue();

        box.SetWeapon(weapon, GameManager.Instance.CurrentGameOption.ShowBoxContent);
        box.SetPosition(position.Value);
        box.SetSymmetry(RandomNum.RandomOneOutOf(2)); // Not sync on purpose
        CameraManager.Instance.RequestFollow(box.transform);
        NetworkRecordSnapshot(Method_InstantiateWeaponBox_Guest, box.NetworkIdentifier, position.Value, (int) weapon.AmmoType);
        await UniTask.Delay(Config.DelayBox, cancellationToken: this.GetCancellationTokenOnDestroy()); // Wait a bit so the player can see the box
    }

    [StreamPlay(Method_InstantiateWeaponBox_Guest)]
    protected void InstantiateWeaponBox_Guest(int ownerNetworkIdentifier, Vector2 position, int weaponIdentifier)
    {
        Weapon weapon = WeaponManager.Instance.GetWeapon((Weapon.AmmoEnum) weaponIdentifier);
        BoxWeaponController box = NetworkInstantiate(boxWeaponPrefab, ownerNetworkIdentifier);
        box.SetWeapon(weapon, GameManager.Instance.CurrentGameOption.ShowBoxContent);
        box.SetPosition(position);
        CameraManager.Instance.RequestFollow(box.transform);
    }

    #endregion

    #region User Interface

    [StreamPlay(Method_UIUpdateTimer)]
    protected void UIUpdateTimer(int value, bool hasUrgency)
    {
        if (hasUrgency) {
            try {
                soundEventTimerUrgent.Play();
            } catch (Exception e) {
                Log.Critical("FMOD Crash", "FMOD has produced an exception in UIUpdateTimer: " + e);
            }
        }
        UserInterfaceManager.Instance.UpdateTimer(value, hasUrgency);
    }

    [StreamPlay(Method_UIUpdateTimerRetreat)]
    protected void UIUpdateTimerRetreat(int value)
    {
        UserInterfaceManager.Instance.UpdateTimer(value, hasUrgency: false);
    }

    [StreamPlay(Method_UIHideTimer)]
    protected void UIHideTimer()
    {
        UserInterfaceManager.Instance.HideTimer();
    }

    /// <summary>
    /// UIInitData has some duplicated code with UIUpdateData on purpose
    /// it's because the former has instant sync and not the later
    /// </summary>
    [StreamPlay(Method_UIInitData)]
    protected virtual void UIInitData(
        Color colorP1, Color colorP2,
        int nbCharacterP1, int healthSumP1,
        int nbCharacterP2, int healthSumP2,
        int healthAvailable
    ) {
        UserInterfaceManager.Instance.Team01UIController.Init(colorP1);
        UserInterfaceManager.Instance.Team02UIController.Init(colorP2);

        UserInterfaceManager.Instance.Team01UIController.UpdateData(nbCharacterP1, healthSumP1, healthAvailable);
        UserInterfaceManager.Instance.Team02UIController.UpdateData(nbCharacterP2, healthSumP2, healthAvailable);
    }

    [StreamPlay(Method_UIUpdateData)]
    protected virtual void UIUpdateData(int nbCharacterP1, int healthSumP1, int nbCharacterP2, int healthSumP2, int healthAvailable)
    {
        UserInterfaceManager.Instance.Team01UIController.UpdateData(nbCharacterP1, healthSumP1, healthAvailable);
        UserInterfaceManager.Instance.Team02UIController.UpdateData(nbCharacterP2, healthSumP2, healthAvailable);
    }

    [StreamPlay(Method_UIUpdateInfoText)]
    protected void UIUpdateInfoText(string text)
    {
        UserInterfaceManager.Instance.UpdateInfoText(text);
    }

    #endregion

    #region Player And Character Selection

    [StreamPlay(Method_SelectNextPlayerAndCharacter)]
    protected virtual void SelectNextPlayerAndCharacter()
    {
        // Activate the right player: we alternate via a currentPlayerPlayerOne bool
        foreach (PlayerController playerController in GameManager.Instance.PlayerControllers) {
            if (
                (CurrentPlayerRedTeam && playerController.IsPlayerOne) ||
                (!CurrentPlayerRedTeam && !playerController.IsPlayerOne)
            ) {
                CurrentPlayerController = playerController;
                CurrentCharacterManager = playerController.GetNextCharacter();
                // Log.Message("RoundController", "Selected player and character: " + CurrentPlayerController.NetworkIdentifier + " and " + CurrentCharacterManager.name + "/" + CurrentCharacterManager.NetworkIdentifier);
            }
        }
        CurrentPlayerRedTeam = !CurrentPlayerRedTeam; // Invert the player for the next turn

        // Focus the camera on the new active character (and reset zoom if not network/not AI)
        CameraManager.Instance.RequestFollow(CurrentCharacterManager.transform);
        if (!IsNetwork && !GameManager.Instance.PlayingAgainstAI) {
            CameraManager.Instance.ResetCamera();
        }
    }

    [StreamPlay(Method_ActivateCharacter)]
    protected void ActivateCharacter()
    {
        CurrentPlayerController.Activate();
        CurrentCharacterManager.CharacterDelegate = this;
        CurrentCharacterManager.SetMoveAndAimState();

        // Log.Message("RoundController", "Activated character: " + CurrentCharacterManager.name + "/" + CurrentCharacterManager.NetworkIdentifier);

        // TODO prio 2 could be shared accross the network
        UserInterfaceManager.Instance.ShowCurrentCharacter(CurrentCharacterManager);
    }

    [StreamPlay(Method_DeactivateCharacter)]
    protected void DeactivateCharacter()
    {
        if (CurrentPlayerController != null) {
            // Log.Message("RoundController", "Deselected player: " + CurrentPlayerController.NetworkIdentifier);
            CurrentPlayerController.Desactivate();
        }

        if (CurrentCharacterManager != null) {
            // Log.Message("RoundController", "Deactivated character: " + CurrentCharacterManager.name + "/" + CurrentCharacterManager.NetworkIdentifier);
            CurrentCharacterManager.SetIdleState();
        }

        UserInterfaceManager.Instance.HideCurrentCharacter();
    }

    #endregion

    #region Helpers

    protected void SyncMap() => MapController.Sync();

    protected void SyncCharacters()
    {
        foreach (CharacterManager characterManager in characterManagers) {
            characterManager.Sync();
        }
    }

    protected bool AreAllCharactersInited()
    {
        foreach (CharacterManager characterManager in characterManagers) {
            if (!characterManager.IsInited) {
                return false;
            }
        }
        return true;
    }

    protected Tuple<bool, string> IsAnyPlayerWon()
    {
        string text;
        var player1hasLost = playerOne.HasLost();
        var player2hasLost = playerTwo.HasLost();
        if (player1hasLost && player2hasLost) {
            text = "Tie";
        } else if (player1hasLost) {
            text = Config.PlayerTwoName + " Won";
        } else if (player2hasLost) {
            text = Config.PlayerOneName + " Won";
        } else {
            return Tuple.Create(false, "");
        }

        return Tuple.Create(true, text);
    }

    #endregion

    #region Timers

    protected bool IsAnyTimerOver()
    {
        return
            (turnTimer != null && turnTimer.IsOver()) ||
            (retreatTimer != null && retreatTimer.IsOver())
            ;
    }

    protected void StopAndClearTimers()
    {
        // Log.Message("RoundController", "Stop and clear timers");
        if (turnTimer != null) {
            turnTimer.Stop();
            turnTimer = null;
        }

        if (retreatTimer != null) {
            retreatTimer.Stop();
            retreatTimer = null;
        }

        UIHideTimer();
        NetworkRecordSnapshot(Method_UIHideTimer);
    }

    protected IEnumerator RoundTimer(int seconds)
    {
        if (gameObject == null) yield break;
        for (; seconds >= 0; seconds--) {
            try {
                UIUpdateTimer(seconds, seconds <= 5);
                NetworkRecordSnapshot(Method_UIUpdateTimer, seconds, seconds <= 5);
            } catch (Exception e) {
                Log.Critical("RoundController", "Exception in RoundTimer: " + e);
            }
            yield return new WaitForSeconds(1);
        }

        yield return Timer.TIME_OVER;
    }

    protected IEnumerator RetreatRoundTimer(int seconds)
    {
        if (gameObject == null) yield break;
        for (; seconds >= 0; seconds--) {
            try {
                UIUpdateTimerRetreat(seconds);
                NetworkRecordSnapshot(Method_UIUpdateTimerRetreat, seconds);
            } catch (Exception e) {
                Log.Critical("RoundController", "Exception in RetreatRoundTimer: " + e);
            }
            yield return new WaitForSeconds(1);
        }

        yield return Timer.TIME_OVER;
    }

    #endregion
}
