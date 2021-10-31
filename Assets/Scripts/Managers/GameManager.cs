using CC;
using FMODUnity;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Linq;
using Jrmgx.Helpers;
using UnityEngine.InputSystem;
using CC.StreamPlay;

public class GameManager : NetworkObject
{
    #region Fields

    public static GameManager Instance;

    [Header("Extern References")]
    [SerializeField] protected MapController mapController = default;

    [Header("Prefabs")]
    [SerializeField] protected GameObject RoundPrefab = default;
    [SerializeField] protected PlayerController PlayerPrefab = default;
    [SerializeField] protected CharacterTarget CharacterTargetPrefab = default;

    [Header("Sound")]
    [SerializeField] protected StudioEventEmitter soundEventMusic = default;
    public StudioEventEmitter SoundEventMusic => soundEventMusic;

    [Header("Predefined")]
    [SerializeField] public bool IsPredefined = false;
    [SerializeField] public List<CharacterManager> PredefinedCharacters = new List<CharacterManager>();
    [SerializeField] private GameOption PredefinedGameOption = default;

    public RoundLock RoundLock { get; protected set; }

    public MapController MapController => mapController;
    public RoundControllerInterface RoundControllerInstance { get; protected set; }
    public GameOption CurrentGameOption { get; protected set; }

    public List<PlayerController> PlayerControllers { get; protected set; } = new List<PlayerController>();
    public PlayerController PlayerOne => PlayerControllers[0];
    public PlayerController PlayerTwo => PlayerControllers[1];

    // Global state
    public bool HasFocus { get; protected set; } = true;

    // AI
    public bool PlayingAgainstAI => CurrentGameOption.PlayingAgainstAI;
    public Scene AIScene { get; private set; }
    public PhysicsScene2D AIPhysicsScene { get; private set; }
    private bool AISimulationActive = false;
    private const float AISimulationSpeed = 1f;
    private const int AIAnglesToTest = 9;
    private const int AIPowerToTest = 5;
    public PlayerController PlayerHumain => PlayerControllers[0];
    public PlayerController PlayerAI => PlayerControllers[1];

    #endregion

    #region Init

    /// <summary>
    /// This is the very first piece of code executed
    /// </summary>
    protected override void Awake()
    {
        Instance = this;
        base.Awake();
        // Log.Message("InitDance", "GameManager Awake");
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        Physics2D.autoSimulation = false;

#if CC_DEBUG
        // Special case for dev
        if (FindObjectOfType<NetworkManager>() == null) {
            // Log.Message("NetworkManager", "Instantiated NetworkManagerDev");
            Debug.LogWarning("Instantiated NetworkManagerDev");
            Instantiate(Resources.Load<NetworkManager>(NetworkManager.RESOURCE_NAME_DEV));
        }
#endif
        // Log.Message("NetworkManager", "DontDestroy OFF");
        this.DestroyOnLoad(NetworkManagerInstance);

        if (IsPredefined) {
            CurrentGameOption = PredefinedGameOption;
            // Not very clean but, remove other GameOptions
            var gameOptions = FindObjectsOfType<GameOption>();
            foreach (var option in gameOptions) {
                if (option != CurrentGameOption) {
                    Destroy(option.gameObject);
                }
            }
        } else {
            // Comes from Menu
            CurrentGameOption = FindObjectOfType<GameOption>();
#if CC_DEBUG
            // Special case for dev
            if (CurrentGameOption == null) {
                Debug.LogWarning("Instantiated GameOptionDev");
                CurrentGameOption = Instantiate(Resources.Load<GameOption>(GameOption.RESOURCE_NAME_DEV));
            }
#endif
        }
        // Calculate some game option values (Randoms ones)
        if (CurrentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Random) {
            CurrentGameOption.StartWithOrChallenger = RandomNum.RandomOneOutOf(2) ? GameOption.TeamEnum.Red_Team : GameOption.TeamEnum.Blue_Team;
        }
        this.DestroyOnLoad(CurrentGameOption);

        NetworkIdentifier = NETWORK_IDENTIFIER_GAME_MANAGER;

        RoundLock = gameObject.AddComponent<RoundLock>();
    }

    protected override async void Start()
    {
        // Log.Message("InitDance", "GameManager Start");
        if (IsNetwork) {
            StreamPlayPlayer.ConnectionBind(NetworkManagerInstance.StreamConnection);
            StreamPlayRecorder.ConnectionBind(NetworkManagerInstance.StreamConnection);

            if (IsNetworkOwner) {
                StreamPlayRecorder.Begin();
            } else {
                StreamPlayPlayer.Play(withBufferSeconds: 0);
            }
        }

        base.Start();
        AIInitPhysicsScene();

        VoiceManager.InitSoundSystem();
        if (Config.WithMusic && !CurrentGameOption.IsTrailer) {
            soundEventMusic.Play();
        }

        await UniTask.WaitUntil(() => mapController.IsStarted);

        if (IsNetwork) {
            try {
                if (IsNetworkOwner) {
                    // Log.Message("InitDance", "GameManager WaitForPlayerMessage(NETWORK_MESSAGE_START)");
                    await StreamPlayRecorder.WaitForPlayerMessage(NETWORK_MESSAGE_START).CancelOnDestroy(this);
                    // Log.Message("InitDance", "GameManager NetworkRecordSnapshotInstant(Method_SyncGameOption)");
                    NetworkRecordSnapshotInstant(Method_SyncGameOption, CurrentGameOption);
                    // Log.Message("InitDance", "GameManager WaitForPlayerMessage(NETWORK_MESSAGE_WAIT_FOR_GAMEOPTION)");
                    await StreamPlayRecorder.WaitForPlayerMessage(NETWORK_MESSAGE_WAIT_FOR_GAMEOPTION).CancelOnDestroy(this);
                    // Log.Message("InitDance", "GameManager Ping/pong phase done");
                    mapController.StateGenerateCandidate();
                } else {
                    // We force wait here in case the Owner takes some time to get there too
                    // because in this case and the message will be send to the void (Owner connection not bindded)
                    await UniTask.Delay(2000).CancelOnDestroy(this);
                    // Log.Message("InitDance", "GameManager SendPlayerMessage(NETWORK_MESSAGE_START)");
                    await StreamPlayPlayer.SendPlayerMessage(NETWORK_MESSAGE_START, waitForReady: true).CancelOnDestroy(this);
                    // Log.Message("InitDance", "GameManager Ping/pong phase done (guest)");
                }
            } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
        } else {
            mapController.StateGenerateCandidate();
        }

        if (IsPredefined) {
#if CC_EXTRA_CARE
try {
#endif
            try {
                await StartContinue().CancelOnDestroy(this);
            } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
            return;
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

#if CC_EXTRA_CARE
try {
#endif
        try {
            // Log.Message("InitDance", "GameManager Wait for mapController.IsBuildFirstPassDone");
            await UniTask.WaitUntil(() => mapController.IsBuildFirstPassDone).CancelOnDestroy(this);
            // Log.Message("InitDance", "GameManager Wait for mapController.IsBuildFirstPassDone over");
            // We add a sync point here
            if (IsNetwork) {
                if (IsNetworkOwner) {
                    await StreamPlayRecorder.WaitForPlayerMessage(NETWORK_MESSAGE_MAP_READY).CancelOnDestroy(this);
                } else {
                    await StreamPlayPlayer.SendPlayerMessage(NETWORK_MESSAGE_MAP_READY).CancelOnDestroy(this);
                }
            }
            await StartContinue().CancelOnDestroy(this);
        } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    [StreamPlay(Method_SyncGameOption)]
    private async UniTask SyncGameOption(SzGameOptionRef gameOptionRef)
    {
        // Log.Message("InitDance", "GameManager Sync GameOption");
        gameOptionRef.InjectIn(CurrentGameOption);
        // Log.Message("InitDance", "GameManager SendPlayerMessage(NETWORK_MESSAGE_WAIT_FOR_GAMEOPTION)");
        await StreamPlayPlayer.SendPlayerMessage(NETWORK_MESSAGE_WAIT_FOR_GAMEOPTION).CancelOnDestroy(this);
    }

    private void OnDestroy()
    {
        // Log.Message("InitDance", "GameManager Destroyed()");
    }

    protected async UniTask StartContinue()
    {
        // Log.Message("InitDance", "GameManager StartContinue");

        InstantiatePlayers();

        // Log.Message("InitDance", "GameManager Round Instantiate");
        // Similar to: RoundControllerInstance = NetworkInstantiate(RoundPrefab, NETWORK_IDENTIFIER_ROUND_CONTROLLER);
        RoundControllerInstance = Instantiate(RoundPrefab).GetComponent<RoundControllerInterface>();
        RoundControllerInstance.NetworkIdentifier = NETWORK_IDENTIFIER_ROUND_CONTROLLER;
        RoundControllerInstance.MapController = mapController;
        RoundControllerInstance.name = "RoundController";
        RoundControllerInstance.CurrentPlayerRedTeam = CurrentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Red_Team;
        // Log.Message("GameManager", "RoundController.CurrentPlayerRedTeam = " + RoundControllerInstance.CurrentPlayerRedTeam);

        // Log.Message("InitDance", "GameManager Wait Until AllPlayerInited");
        await UniTask.WaitUntil(AreAllPlayersInited).CancelOnDestroy(this);
        // Log.Message("InitDance", "GameManager Wait Until AllPlayerInited OK");
        if (IsPredefined) {
#if CC_EXTRA_CARE
try {
#endif
            await mapController.StateFirstPass().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }
#if CC_EXTRA_CARE
try {
#endif
        await RoundControllerInstance.ReadyForStart().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        if (!IsNetworkOwner) {
            // This is here because it needs all the previous object to be created
            // so the Setup() phase of Player can find all the Players/Recorders
            // Guest only will start StreamPlay player (to receive/replay map generation)
            StreamPlayPlayer.Refresh();
        }

#if CC_EXTRA_CARE
try {
#endif
        // awaiting forever
        await RoundLock.DequeueTaskInSequence().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    protected void InstantiatePlayers()
    {
        PlayerControllers.Add(InstantiatePlayer(PlayerController.PLAYER_ID_1, CurrentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Red_Team, InputSystem.devices.ToArray()));
        PlayerControllers.Add(InstantiatePlayer(PlayerController.PLAYER_ID_2, CurrentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Blue_Team, InputSystem.devices.ToArray()));
    }

    protected PlayerController InstantiatePlayer(int playerId, bool startWithMe, params InputDevice[] devices)
    {
        // Log.Message("InitDance", "GameManager Player Instantiate");
        PlayerController playerController = Instantiate<PlayerController>(PlayerPrefab);
        playerController.NetworkIdentifier = NetworkObject.NetworkIdentifierFrom("PlayerController_" + playerId);
        playerController.PlayerId = playerId;
        playerController.StartWithMe = startWithMe;
        playerController.MapController = mapController;
        playerController.name = "PlayerController_" + playerId;
        playerController.PlayerUsername = playerId == PlayerController.PLAYER_ID_1 ?
            NetworkManagerInstance.UsernamePlayerOne :
            NetworkManagerInstance.UsernamePlayerTwo;

        return playerController;
    }

    #endregion

    #region Public methods

    public void SetOwner(bool owner)
    {
        if (!IsNetwork) return;
        // Log.Message("NetworkManager", "Set IsNetworkOwner: " + owner);
        NetworkManagerInstance.IsNetworkOwner = owner;
        NetworkManagerInstance.StreamConnection.SetOwner(owner);
    }

    public void GameOver(string message)
    {
        PopupManager.Init(
            "Game Over", message,
            "Menu", BackToMenu
        )
        .Show();
    }

    private void QuitGame()
    {
        if (!IsNetwork) {
            Time.timeScale = 0;
        }
        PopupManager.Init(
            "Quit Game?",
            "Do you want to leave this game?",
            "Stay", () => Time.timeScale = 1,
            "Leave", BackToMenu
        ).Show();
    }

    public void BackToMenu()
    {
        Time.timeScale = 1;

        if (RoundControllerInstance != null) {
            RoundControllerInstance.StopGame();
        }
        RoundLock.ForceReset();

        // Log.Message("SceneManager", "Loading Scene Menu Async Started");
        var loading = SceneManager.LoadSceneAsync("Menu");
        loading.completed += LoadingCompleted;
    }

    private void LoadingCompleted(AsyncOperation operation)
    {
        // Log.Message("SceneManager", "Loading Scene Menu Async Done");
    }

    private void Update()
    {
#if CC_DEBUG && UNITY_EDITOR
        if (Keyboard.current.kKey.wasPressedThisFrame) {
            /**/Debugging.ClearLogs();
        }
#endif

        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) {
            Log.Critical("GameManager", "LogMarker Asked");
            StreamPlayPlayer.LogMarker().Forget();
            StreamPlayRecorder.LogMarker().Forget();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            QuitGame();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) {
            this.ExecuteNextUpdate(() => {
                // Log.Message("Focus", "Regained focus", HasFocus);
                HasFocus = true;
            });
        }
    }

    #endregion

    #region AI

    private void AIInitPhysicsScene()
    {
        if (!PlayingAgainstAI) return;
        AIScene = SceneManager.CreateScene("PhysicsScene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        AIPhysicsScene = AIScene.GetPhysicsScene2D();
    }

    public void AIRefreshPhysicsScene()
    {
        if (!PlayingAgainstAI) return;

        // Delete all previous gameobjects
        foreach (var go in AIScene.GetRootGameObjects()) {
            Destroy(go);
        }

        var duplicable = mapController.GetDuplicable();
        var duplicated = Instantiate(duplicable);
        MoveGameObjectToAIScene(duplicated);

        foreach (MapItemInterface m in duplicated.GetComponentsInChildren<MapItemInterface>()) {
            if (m.gameObject.transform.position != Vector3.zero) {
                m.SetPosition(m.gameObject.transform.position);
            }
        }

        foreach (Component component in duplicated.GetComponentsInChildren<Component>()) {
            if (component is Collider2D) {
                component.gameObject.AddComponent<DummyExplosionReceiver>();
            } else if (component is Behaviour behaviour) {
                behaviour.enabled = false;
            } else if (component is Renderer renderer) {
                renderer.enabled = false;
            }
        }
    }

    public async UniTask<CharacterTargetHit?> AITestWeapons(CharacterManager currentCharacterManager)
    {
        AIRefreshPhysicsScene();

        List<UniTask<List<CharacterTargetHit>>> AISimulations = new List<UniTask<List<CharacterTargetHit>>>();

        // Position targets
        // Log.Message("AI Dance", "Positioning targets");
        foreach (var playerController in PlayerControllers) {
            int count = 0;
            foreach (var character in playerController.Characters()) {
                count++;
                CharacterTarget target = Instantiate(CharacterTargetPrefab);
                if (playerController.IsPlayerAI) {
                    target.TargetTag = CharacterTargetTag.AI;
                    target.name = "CharacterTarget AI " + count;
                } else {
                    target.TargetTag = CharacterTargetTag.Human;
                    target.name = "CharacterTarget Human " + count;
                }
                MoveGameObjectToAIScene(target.gameObject);
                target.transform.position = character.transform.position;
                AISimulations.Add(target.WaitForAISimulation());
            }
        }

        // Tests weapons and associated params (angle/power/isFacingRight)
        foreach (var weapon in currentCharacterManager.PlayerController.GetPlayerWeapons(currentCharacterManager.Character).Keys) {
            if (!weapon.AIKnowsHowToUse) continue;
            // Log.Message("AI Dance", "Simulate weapon: " + weapon.WeaponHumanName);
            for (float angleCount = 1; angleCount <= AIAnglesToTest; angleCount++) {
                for (float powerCount = 1; powerCount <= AIPowerToTest; powerCount++) {
                    for (int isFacingRightIndex = 0; isFacingRightIndex < 2; isFacingRightIndex++) {

                        bool isFacingRight = isFacingRightIndex == 0;
                        float power = powerCount / (float)AIPowerToTest;
                        float angle = ((angleCount / ((float)AIAnglesToTest / 2f)) - 1f) * 90f;

                        Vector2 firePointPosition = currentCharacterManager
                            .CharacterWeapon
                            .FirePointPosition(angle, weapon, isFacingRight)
                        ;

                        AmmoInterface weaponAmmoController = WeaponManager.Instance
                            .GetWeaponLogic(weapon.WeaponType, firePointPosition, Quaternion.identity, IsAISimulationActive: true)
                        ;
                        MoveGameObjectToAIScene(weaponAmmoController.gameObject);
                        weaponAmmoController.Init(currentCharacterManager, weapon, angle, isFacingRight, power);
                        // await UniTask.WaitForEndOfFrame().CancelOnDestroy(this); // TODO prio 5 Should load all the stuff on multiple frames
                    }
                }
            }
        }

        AISimulationActive = true;

        IEnumerable<CharacterTargetHit> resultHits = (await UniTask.WhenAll(AISimulations).CancelOnDestroy(this)).SelectMany(i => i);

        AISimulationActive = false;

        if (resultHits.Count() == 0) return null;

        // Find the hit where damages inflicted to the human - AI is the greatest
        // Log.Message("AI Dance", "Got " + resultHits.Count() + " hits");
        var indexedHits = new Dictionary<int, CharacterTargetHit>();
        foreach (var hit in resultHits) {
            // Log.Message("AI Dance", "Hit: " + hit);
            indexedHits[hit.ExplosionIdentifier] = hit;
        }
        var indexedDamages = new Dictionary<int, float>();
        foreach (var hit in resultHits) {
            float damages = (hit.IsHuman ? 1 : -1) * hit.ExplosionDamages;
            if (!indexedDamages.ContainsKey(hit.ExplosionIdentifier)) {
                indexedDamages[hit.ExplosionIdentifier] = damages;
            } else {
                indexedDamages[hit.ExplosionIdentifier] += damages;
            }
        }

        int explosionIdentifier = indexedDamages.OrderByDescending(i => i.Value).First().Key;
        if (indexedDamages[explosionIdentifier] <= 0) {
            return null;
        }
        return indexedHits[explosionIdentifier];
    }

    public void MoveGameObjectToAIScene(GameObject gameObject)
    {
        SceneManager.MoveGameObjectToScene(gameObject, AIScene);
    }

    private void FixedUpdate()
    {
        if (!PlayingAgainstAI) return;
        if (!AISimulationActive) return;
        // We can accelerate the simulation here, but be careful because you will lose precision (x2 is already a bit off)
        AIPhysicsScene.Simulate(Time.fixedDeltaTime * AISimulationSpeed);
    }

    #endregion

    #region Helpers

    private bool AreAllPlayersInited()
    {
        foreach (PlayerController playerController in PlayerControllers) {
            if (!playerController.IsInited) {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Focus Management

    protected void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) return;
        // Log.Message("Focus", "Lost focus");
        HasFocus = false;
    }

    #endregion
}
