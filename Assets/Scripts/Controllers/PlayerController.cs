using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Jrmgx.Helpers;
using static UnityEngine.InputSystem.InputAction;
using UnityEngine.InputSystem;
using CC.StreamPlay;
using Cysharp.Threading.Tasks;
using System.Collections;

public class PlayerController : NetworkObject, InventoryDelegate
{
    #region Fields

    public static Vector2 ThroneOffset = new Vector2(2.4f, 7.2f);
    public static Vector2 ThronePlateformOffset = new Vector2(5f, 7f);

    public const int PLAYER_ID_1 = 1;
    public const int PLAYER_ID_2 = 2;
    public const int PLAYER_ID_AI = PLAYER_ID_2;
    private static int CHARACTER_IDENTIFIER = 0;

    [Header("Prefabs")]
    [SerializeField] private CharacterManager rookCharacterPrefabs = default;
    [SerializeField] private CharacterManager bishopCharacterPrefabs = default;
    [SerializeField] private CharacterManager knightCharacterPrefabs = default;
    [SerializeField] private CharacterManager queenCharacterPrefabs = default;
    [SerializeField] private CharacterManager kingCharacterPrefabs = default;

    [Header("References")]
    [SerializeField] private PlayerInput playerInput = default;
    public PlayerInput PlayerInput => playerInput; // used for InfoTextUIController in training
    [HideInInspector] public MapController MapController = default;
    public List<CharacterManager> CharacterManagers { get; private set; } = new List<CharacterManager>();

    // State
    public bool StartWithMe { get; set; } // In CTK mode, means that they has no king
    public bool IsActive { get; private set; }
    public bool IsInited { get; set; }
    public int PlayerId { get; set; } = 0;
    public string PlayerUsername { get; set; }
    public Color PlayerColor => IsPlayerOne ? Config.ColorPlayerOne : Config.ColorPlayerTwo;
    public bool IsPlayerOne => PlayerId == PLAYER_ID_1;
    public bool IsPlayerAI => PlayerId == PLAYER_ID_AI;
    public CharacterManager CurrentCharacterManager { get; private set; }
    private int characterIndex = -1;
    private Coroutine SignalCharacterHandler;

    // Capture the king state
    public bool KingIsCaptured { get; private set; } = false;
    private CharacterManager kingCharacterManager = null;

    // Weapons
    private Dictionary<Weapon, int> playerWeapons = new Dictionary<Weapon, int>();
    private readonly Dictionary<Weapon.WeaponGroupEnum, List<Weapon>> playerWeaponsByGroup = new Dictionary<Weapon.WeaponGroupEnum, List<Weapon>>();

    private Debouncer debouncerInputScrollwheel;
    private GameOption currentGameOption => GameManager.Instance.CurrentGameOption;

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();
        CHARACTER_IDENTIFIER = 0;
        // Log.Message("InitDance", "Player Awake " + name);
        debouncerInputScrollwheel = new Debouncer(this, 0.3f);
    }

    /// <summary>
    /// Start the PlayerController and creates their CharacterControllers
    /// </summary>
    protected override async void Start()
    {
        base.Start();
        // Log.Message("InitDance", "Player Start " + name);

        foreach (Weapon weapon in WeaponManager.Instance.WeaponsDefault) {
            //playerWeapons.Add(weapon, weapon.UnitsDefault);
            InitPlayerWeapon(weapon);
        }

        if (GameManager.Instance.IsPredefined) {
            InstantiatePredefinedCharacter();
        } else {
            // Create my character controllers and add them to a list that I own
            CharacterManagers.Add(InstantiateCharacter(rookCharacterPrefabs));
            CharacterManagers.Add(InstantiateCharacter(bishopCharacterPrefabs));
            CharacterManagers.Add(InstantiateCharacter(knightCharacterPrefabs));
            CharacterManagers.Add(InstantiateCharacter(queenCharacterPrefabs));
            if (currentGameOption.PlayingCaptureTheKing) {
                if (!StartWithMe) {
                    kingCharacterManager = InstantiateCharacter(kingCharacterPrefabs);
                    kingCharacterManager.SetKingToCapture(true);
                    CharacterManagers.Add(kingCharacterManager);
                }
            } else {
                CharacterManagers.Add(InstantiateCharacter(kingCharacterPrefabs));
            }
        }

        if (IsNetworkOwner) {
#if CC_EXTRA_CARE
try {
#endif
            try {
                await PositionCharacters().CancelOnDestroy(this);
            } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        bool playerInputDisabled =
            // If playing versus AI: deactivate input for IA playerController
            (currentGameOption.PlayingAgainstAI && IsPlayerAI) ||
            // If playing online: deactivate the "other" playerController
            (IsNetwork && ((IsPlayerOne && !IsNetworkOwner) || (!IsPlayerOne && IsNetworkOwner)))
        ;
        playerInput.enabled = !playerInputDisabled;
    }

    private CharacterManager InstantiateCharacter(CharacterManager characterPrefab)
    {
        // Log.Message("InitDance", "Player Character Instantiate " + name);
        CHARACTER_IDENTIFIER++;
        CharacterManager characterManager = NetworkInstantiate(
            characterPrefab,
            NetworkObject.NetworkIdentifierFrom("CharacterController_" + PlayerId + "_" + CHARACTER_IDENTIFIER)
        );
        characterManager.transform.SetParent(MapController.transform);
        characterManager.transform.localScale = Vector3.one * Config.CharacterScale;
        characterManager.PlayerController = this;
        characterManager.InventoryDelegate = this;
        characterManager.name = "CharacterController_" + PlayerId + "_" + characterManager.Character.HumainName + "_" + CHARACTER_IDENTIFIER;
        return characterManager;
    }

    private async UniTask PositionCharacters()
    {
        // Log.Message("InitDance", "Player Position Characters Owner " + name);
        // Ask the map to place my characters
        List<MapItemObjectSerializable> positions = new List<MapItemObjectSerializable>();
        foreach (CharacterManager characterManager in CharacterManagers) {
            try {
                if (GameManager.Instance.IsPredefined) {
                    // Character are positionned on instantiate
                    IsInited = true;
                    return;
                }

                UserInterfaceManager.Instance.UpdateInfoText("Positioning " + (IsPlayerOne ? Config.PlayerOneName : Config.PlayerTwoName) + " " + characterManager.Character.HumainName + "...");
                // Note: position always has a value because we set canFail to false
                Vector2 position = Vector2.zero;
                if (characterManager.IsKingToCapture) {
                    position = new Vector2(IsPlayerOne ? ThroneOffset.x : MapController.CalculatedWidth - ThroneOffset.x, MapController.CalculatedHeight + ThroneOffset.y);
                } else {
                    PositioningConstraints positioningConstraints = PositioningConstraints.None;
                    if (currentGameOption.PlayingCaptureTheKing) {
                        // Bypass positioning constraints
                        if (IsPlayerOne) {
                            positioningConstraints = PositioningConstraints.CalculateConstraint(
                                currentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Red_Team ?
                                    PositioningConstraints.Type.BottomLeft : PositioningConstraints.Type.TopLeft,
                                MapController.CalculatedWidth,
                                MapController.CalculatedHeight
                            );
                        } else {
                            positioningConstraints = PositioningConstraints.CalculateConstraint(
                                currentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Blue_Team ?
                                    PositioningConstraints.Type.BottomRight : PositioningConstraints.Type.TopRight,
                                MapController.CalculatedWidth,
                                MapController.CalculatedHeight
                            );
                        }
                    } else {
                        positioningConstraints = PositioningConstraints.CalculateConstraint(
                            IsPlayerOne ?
                                currentGameOption.PlayerOnePositioningConstraints :
                                currentGameOption.PlayerTwoPositioningConstraints,
                            MapController.CalculatedWidth,
                            MapController.CalculatedHeight
                        );
                    }
#if CC_EXTRA_CARE
try {
#endif
                    position = (await MapController.PositionOnMap(characterManager, 5, 55f, positioningConstraints, canFail: false).AttachExternalCancellation(this.GetCancellationTokenOnDestroy())).Value;
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
                }
                characterManager.SetPosition(position);
                if (characterManager.IsKingToCapture) {
                    kingCharacterManager.InstantiateKingShield();
                }
                bool flip = !IsPlayerOne;
                if (flip) {
                    characterManager.CharacterMove.Flip();
                }
                positions.Add(new MapItemObjectSerializable(0, position, flip));
            } catch (Exception e) {
                Log.Error("PlayerController", "Exception in PositionCharacters " + e);
                PopupManager.Init(
                    "Error",
                    "Impossible to position a character!\n" +
                    "You must create a new game, sorry.",
                    "Quit", GameManager.Instance.BackToMenu
                ).Show();
                throw e;
            }
        }

        NetworkRecordSnapshotInstant(Method_PositionCharacters, positions);

        UserInterfaceManager.Instance.HideInfoText();
        // Log.Message("InitDance", "Player Inited " + name);
        IsInited = true;
    }

    [StreamPlay(Method_PositionCharacters)]
    protected void PositionCharacters(List<MapItemObjectSerializable> items)
    {
        // Log.Message("InitDance", "Player Position Characters Guest " + name);
        // for loop used because the CharacterManagers already exist
        for (int i = 0, max = CharacterManagers.Count; i < max; i++) {
            MapItemObjectSerializable character = items[i];
            CharacterManager characterManager = CharacterManagers[i];
            characterManager.SetPosition(character.Position());
            if (characterManager.IsKingToCapture) {
                kingCharacterManager.InstantiateKingShield();
            }
            if (character.Symmetry()) {
                characterManager.CharacterMove.Flip();
            }
        }
        // Log.Message("InitDance", "Player Inited " + name);
        IsInited = true;
    }

    #endregion

    #region Predefined Specific

    public void InstantiatePredefinedCharacter()
    {
        foreach (var characterPredef in GameManager.Instance.PredefinedCharacters) {
            if ((IsPlayerOne && !characterPredef.IsPredefinedAI) || (IsPlayerAI && characterPredef.IsPredefinedAI)) {
                var characterManager = InstantiateCharacter(characterPredef);
                characterManager.IsPredefined = false;
                characterManager.gameObject.SetActive(true);
                characterManager.SetPosition(characterPredef.MyPosition);
                if (!IsPlayerOne) {
                    characterManager.CharacterMove.Flip();
                }
                CharacterManagers.Add(characterManager);
                DestroyImmediate(characterPredef.gameObject);
            }
        }
    }

    #endregion

    #region Public Methods

    public void SetKingIsCaptured() // Called by CharacterManager
    {
        if (KingIsCaptured) return;
        // Log.Message("CaptureTheKing", "Set king is captured on PlayerController: IsPlayerOne? " + IsPlayerOne);
        KingIsCaptured = true;

        var nextIndex = Basics.NextIndex(characterIndex, CharacterManagers.Count, looping: true);
        CharacterManagers.Remove(kingCharacterManager);
        CharacterManagers.Insert(nextIndex, kingCharacterManager); // This works because the king is always the last character of the list

        // TODO prio 2 a bit of technical dept
        // Find the throne by looking at scene objects
        //ThroneController.KingIsCaptured();
        foreach (var throne in FindObjectsOfType<MapThroneController>()) {
            if (IsPlayerOne && throne.IsPlayerOne) {
                throne.KingIsCaptured();
                break;
            }
            if (!IsPlayerOne && !throne.IsPlayerOne) {
                throne.KingIsCaptured();
                break;
            }
        }
    }

    /// <summary>
    /// Return the next non dead character for this player,
    /// if all character are dead, return null
    /// </summary>
    public CharacterManager GetNextCharacter()
    {
        if (HasLost()) {
            throw new Exception("No character available");
        }

        characterIndex = Basics.NextIndex(characterIndex, CharacterManagers.Count, looping: true);
        CharacterManager candidate = CharacterManagers[characterIndex];

        // The king is the only non dead character => set to captured
        if (
            currentGameOption.PlayingCaptureTheKing &&
            candidate.IsKingToCapture &&
            Characters().Count() == 1
        ) {
            candidate.KingCaptured();
        }

        if (
            candidate.IsDead ||
            (currentGameOption.PlayingCaptureTheKing && candidate.IsKingToCapture)
        ) {
            return GetNextCharacter();
        }

        CurrentCharacterManager = candidate;
        SignalCharacterReset();
        return candidate;
    }

    /// <summary>
    /// Return alive characters
    /// </summary>
    public IEnumerable<CharacterManager> Characters()
    {
        return CharacterManagers.Where(c => !c.IsDead);
    }

    public int HealthSum()
    {
        return CharacterManagers.Sum(c => c.Health);
    }

    public bool HasLost()
    {
        if (
            currentGameOption.PlayingCaptureTheKing && !StartWithMe &&
            (KingIsCaptured || Characters().Count() == 1 /* King only left */)
        ) {
            return true;
        }
        return Characters().Count() == 0;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Desactivate()
    {
        SignalCharacterStop();
        IsActive = false;
    }

    public void TrainingSessionOver()
    {
        IsActive = false;
    }

    #endregion

    #region Signal Character

    public void SignalCharacterReset()
    {
        if (!IsActive || CurrentCharacterManager == null) return;
        this.StopCoroutineNoFail(SignalCharacterHandler);
        SignalCharacterHandler = this.StartCoroutineNoFail(SignalCharacterStart());
    }

    private IEnumerator SignalCharacterStart()
    {
        yield return new WaitForSeconds(Config.SignalCharacterTime);
        if (!IsActive || CurrentCharacterManager == null) yield break;
        try {
            CurrentCharacterManager.SignalPosition(idle: true);
            SignalCharacterReset();
        } catch (Exception e) {
            Log.Critical("PlayerController", "Exception in SignalCharacterStart: " + e);
        }
    }

    private void SignalCharacterStop()
    {
        this.StopCoroutineNoFail(SignalCharacterHandler);
        SignalCharacterHandler = null;
    }

    #endregion

    #region Input

    private bool CanInputBase()
    {
        if (!GameManager.Instance.HasFocus) return false;
        if (PopupManager.HasFocus) return false;
        if (!IsActive) return false;
        if (CurrentCharacterManager == null) return false;
        return true;
    }

    private bool CanInputMoveOrAim()
    {
        if (!CanInputBase()) return false;
        if (CurrentCharacterManager.CharacterWeapon.IsWeaponSelectorShown) return false;
        return true;
    }

    private bool CanInputWeaponSelect()
    {
        if (!CanInputBase()) return false;
        if (CurrentCharacterManager.CharacterWeapon.StartedToFire) return false;
        return true;
    }

    private bool CanInputView()
    {
#if CC_DEBUG
        if (currentGameOption.IsTrailer) return false;
#endif
        if (PopupManager.HasFocus) return false;
        if (CurrentCharacterManager != null && CurrentCharacterManager.CharacterWeapon.IsWeaponSelectorShown) return false;
        if (currentGameOption.PlayingAgainstAI) {
            return !IsPlayerAI;
        }

        return IsActive || IsNetwork;
    }

    public void InputMove(CallbackContext context)
    {
        if (!CanInputMoveOrAim()) return;

        // Vertical axis => float value
        // Log.Message("Input", "InputMove: " + context.ReadValue<float>());
        int horizontal = 0;
        float input = context.ReadValue<float>();
        SignalCharacterReset();
        // If the axis is not above .75f we skip that input value
        if (Mathf.Abs(input) >= 0.75f) {
            horizontal = input < 0 ? -1 : 1;
        }

        if (context.canceled) { // Value: using canceled
            horizontal = 0;
        }

        CurrentCharacterManager.CharacterMove.MoveAction(horizontal);
    }

    public void InputJump(CallbackContext context)
    {
        if (!CanInputMoveOrAim()) return;
        if (!context.performed) return; // Button: using performed

        // Log.Message("Input", "InputJump");
        SignalCharacterReset();
        CurrentCharacterManager.CharacterMove.JumpAction();
    }

    public void InputAim(CallbackContext context)
    {
        if (!CanInputMoveOrAim()) return;
        if (GetPlayerWeaponsCountNoSpecial() == 0) return;
        // Damn testing for composite manually
        if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed) {
            return;
        }

        Vector2 input = context.ReadValueBugFixVector2();
        var worldPosition = CameraManager.Instance.ScreenToWorldPoint(input);
        // Log.Message("Input", "InputMouseAim: " + input + " world position: " + worldPosition);

        CurrentCharacterManager.CharacterWeapon.AimToPosition(worldPosition);
    }

    public void InputDrop(CallbackContext context)
    {
        if (!CanInputMoveOrAim()) return;
        if (GetPlayerWeaponsCountNoSpecial() == 0) return;
        if (!context.performed) return; // Button: using performed

        // Log.Message("Input", "InputDrop");
        CurrentCharacterManager.CharacterWeapon.Drop();
    }

    public void InputFire(CallbackContext context)
    {
        if (!CanInputMoveOrAim()) return;
        if (GetPlayerWeaponsCountNoSpecial() == 0) return;

        // Log.Message("Input", "InputFire: context.canceled " + context.canceled);
        SignalCharacterReset();
        CurrentCharacterManager.CharacterWeapon.FireLoadAction(released: context.canceled);
    }

    public void InputWeaponSelector(CallbackContext context)
    {
        if (!CanInputWeaponSelect()) return;
        if (GetPlayerWeaponsCountNoSpecial() == 0) return;
        if (!context.performed) return; // Button: using performed

        // Log.Message("Input", "InputWeaponSelector");
        SignalCharacterReset();
        if (CurrentCharacterManager.CharacterWeapon.IsWeaponSelectorShown) {
            CurrentCharacterManager.CharacterWeapon.HideWeaponSelector();
        } else {
            CurrentCharacterManager.CharacterWeapon.ShowWeaponSelector();
        }
    }

    public void InputMoveView(CallbackContext context)
    {
        if (!CanInputView()) return;
        if (!context.performed) return; // Use this to prevent getting a start+performed event

        // Log.Message("Input", "InputMoveViewMouse: " + context.ReadValueBugFixVector2());
        Vector2 input = context.ReadValueBugFixVector2();
        CameraManager.Instance.FollowPointer(input);
    }

    public void InputZoomView(CallbackContext context)
    {
        if (!CanInputView()) return;
        if (!context.performed) return; // Use this to prevent getting a start+performed event
        if (debouncerInputScrollwheel.NeedDebounce()) return;
        debouncerInputScrollwheel.Debounce();

        // Vertical axis => float value
        // Log.Message("Input", "InputZoomView: " + context.ReadValueBugFixFloat());
        float input = context.ReadValueBugFixFloat();
        if (input > 0) {
            CameraManager.Instance.ZoomIn();
        } else {
            CameraManager.Instance.ZoomOut();
        }
    }

    #endregion

    // Note: Inventory is not sync over the network, each player maintain its own state
    #region InventoryDelegate

    /// <summary>
    /// Return the number of weapon for this player,
    /// this does not take special weapon into account.
    /// Used only for training and mission but heavily called in any case
    /// </summary>
    public int GetPlayerWeaponsCountNoSpecial()
    {
        return playerWeapons.Count;
    }

    public Dictionary<Weapon, int> GetPlayerWeapons(Character character)
    {
        Weapon special = WeaponManager.Instance.GetWeaponForCharacter(character.CharacterType);
        if (!special.IsValid) return playerWeapons;

        var playerWeaponsAndSpecial = new Dictionary<Weapon, int>(playerWeapons);
        playerWeaponsAndSpecial.Add(special, special.UnitsDefault);
        return playerWeaponsAndSpecial;
    }

    public Dictionary<Weapon.WeaponGroupEnum, List<Weapon>> GetPlayerWeaponsByGroup(Character character)
    {
        Weapon special = WeaponManager.Instance.GetWeaponForCharacter(character.CharacterType);
        if (!special.IsValid) return playerWeaponsByGroup;

        var playerWeaponsByGroupAndSpecial = new Dictionary<Weapon.WeaponGroupEnum, List<Weapon>>(playerWeaponsByGroup);
        playerWeaponsByGroupAndSpecial.Add(special.WeaponGroup, new List<Weapon>{ special });
        return playerWeaponsByGroupAndSpecial;
    }

    private void InitPlayerWeapon(Weapon weapon)
    {
        int previousUnits = 0;
        int givenUnits = weapon.UnitsDefault;
        if (playerWeapons.ContainsKey(weapon)) {
            previousUnits = playerWeapons[weapon];
        }
        playerWeapons[weapon] = previousUnits + givenUnits;
        playerWeapons = playerWeapons
            .OrderBy(kv => kv.Key.WeaponGroup)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Update groups
        if (playerWeaponsByGroup.ContainsKey(weapon.WeaponGroup)) {
            playerWeaponsByGroup[weapon.WeaponGroup].Add(weapon);
        } else {
            playerWeaponsByGroup.Add(weapon.WeaponGroup, new List<Weapon>{ weapon });
        }
    }

    public void AddPlayerWeapon(CharacterManager characterManager, Weapon weapon)
    {
        // NetworkAssertNotGuest();

        // Log.Message("PlayerController", "Added new weapon: " + weapon);
        InitPlayerWeapon(weapon);
        characterManager.CharacterWeapon.UpdateCurrentWeapon(); // It will call a guest version
    }

    public void RemovePlayerWeapon(CharacterManager characterManager, Weapon weapon, int unit = 1, bool forceAllUnits = false)
    {
        // NetworkAssertNotGuest();

        if (currentGameOption.AllWeaponsNoLimits) return; // do not remove weapon in sandbox
        if (weapon.IsSpecial) return;
        if (!playerWeapons.ContainsKey(weapon)) {
            Log.Critical("PlayerController", "Tried to remove weapon ammo: " + weapon + " but the player did not have any");
            return;
        }
        if (forceAllUnits) { // Used in training
            RemoveWeapon(characterManager, weapon);
            return;
        }
        if (weapon.UnitsUnlimited) return;
        // Log.Message("PlayerController", "Removing one unit of weapon: " + weapon);
        playerWeapons[weapon] -= unit;
        if (playerWeapons[weapon] < 0) {
            Log.Critical("PlayerController", "Weapon ammo unit for: " + weapon + " is < 0");
        }

        // Remove references if unit <= 0
        if (playerWeapons[weapon] <= 0) {
            RemoveWeapon(characterManager, weapon);
        }
    }

    private void RemoveWeapon(CharacterManager characterManager, Weapon weapon)
    {
        playerWeapons.Remove(weapon);
        playerWeaponsByGroup[weapon.WeaponGroup].Remove(weapon);
        if (playerWeaponsByGroup[weapon.WeaponGroup].Count == 0) {
            playerWeaponsByGroup.Remove(weapon.WeaponGroup);
        }
        characterManager.CharacterWeapon.UpdateCurrentWeapon();
    }

    #endregion
}
