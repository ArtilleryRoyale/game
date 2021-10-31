using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using Jrmgx.Helpers;
using CC.StreamPlay;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class CharacterWeapon : NetworkObject, FloatPackStreamPlay
{
    #region Fields

    public const int AIM_ACTION_UP = 1;
    public const int AIM_ACTION_DOWN = -1;
    public const int AIM_ACTION_NONE = 0;

    [Header("References")]
    [SerializeField] private Transform sightContainer = default;
    [SerializeField] private SpriteRenderer sightRenderer = default;
    [SerializeField] private Transform firePoint = default;
    [SerializeField] private SpriteRenderer fireLoadFill = default;
    [SerializeField] private SpriteRenderer crossHair = default;

    [Header("Weapons")] // entries below has to be referenced into Awake()
    [SerializeField] private GameObject bazookaContainer = default;
    [SerializeField] private GameObject grenadeContainer = default;
    [SerializeField] private GameObject stickyGrenadeContainer = default;
    [SerializeField] private GameObject shotgunContainer = default;
    [SerializeField] private GameObject bombContainer = default;
    [SerializeField] private GameObject mortarContainer = default;
    [SerializeField] private GameObject beamContainer = default;
    [SerializeField] private GameObject dashContainer = default;
    [SerializeField] private GameObject highJumpContainer = default;
    [SerializeField] private GameObject sniperContainer = default;
    [SerializeField] private GameObject teleportContainer = default;
    [SerializeField] private GameObject fireworksContainer = default;
    [SerializeField] private GameObject snakeContainer = default;
    [SerializeField] private GameObject rabbitContainer = default;
    [SerializeField] private GameObject ropeContainer = default;
    [SerializeField] private GameObject shieldContainer = default;
    [SerializeField] private GameObject meteorContainer = default;
    [SerializeField] private GameObject rainbowContainer = default;
    [SerializeField] private MortarWeaponController mortarController = default;
    [SerializeField] private BeamWeaponController beamController = default;
    [SerializeField] private DashWeaponController dashController = default;
    [SerializeField] private SniperWeaponController sniperController = default;
    private readonly Dictionary<Weapon.WeaponEnum, GameObject> weaponContainers = new Dictionary<Weapon.WeaponEnum, GameObject>();

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventErrorGeneric = default;
    [SerializeField] private StudioEventEmitter soundEventFireDefault = default;
    [SerializeField] private StudioEventEmitter soundEventFireLoadDefault = default;
    [SerializeField] private StudioEventEmitter soundEventFireBazooka = default; // TODO prio 5 those should be move on the weapon itself?
    [SerializeField] private StudioEventEmitter soundEventFireLoadBazooka = default;
    [SerializeField] private StudioEventEmitter soundEventFireGrenade = default;
    [SerializeField] private StudioEventEmitter soundEventFireLoadGrenade = default;
    [SerializeField] private StudioEventEmitter soundEventFireShotgun = default;
    [SerializeField] private StudioEventEmitter soundEventFireSniper = default;
    [SerializeField] private StudioEventEmitter soundEventFireBeam = default;
    [SerializeField] private StudioEventEmitter soundEventFireShield = default;

    // Parent, siblings and delegate
    private CharacterManager CharacterManager;

    // State
    public bool StartedToFire { get; protected set; }
    public bool IsWeaponSelectorShown { get; protected set; }
    public bool IsWeaponAiming { get; protected set; }
    public bool IsWeaponFiring { get; protected set; }
    private bool isActive;
    private bool hasFired;
    private Vector2 controllerWantsToAimMouse;
    private float weaponPower;
    private bool isDropping;
    private bool mouseClickDown = false;

    // Shortcuts
    private int playerWeaponsCount => CharacterManager.InventoryDelegate.GetPlayerWeaponsCountNoSpecial();
    private Dictionary<Weapon.WeaponGroupEnum, List<Weapon>> playerWeaponsByGroup => CharacterManager.InventoryDelegate.GetPlayerWeaponsByGroup(CharacterManager.Character);

    // Current weapon
    private Weapon.WeaponGroupEnum currentWeaponGroup;
    private int currentWeaponIndexForGroup;
    private Weapon currentWeapon;
    // Caching
    private bool isWeaponShown = false; // Use to cache/refresh the state
    private GameObject currentWeaponContainer;

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();

        weaponContainers.Add(Weapon.WeaponEnum.Bazooka, bazookaContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Grenade, grenadeContainer);
        weaponContainers.Add(Weapon.WeaponEnum.StickyGrenade, stickyGrenadeContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Shotgun, shotgunContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Bomb, bombContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Mortar, mortarContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Beam, beamContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Dash, dashContainer);
        weaponContainers.Add(Weapon.WeaponEnum.HighJump, highJumpContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Shield, shieldContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Meteor, meteorContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Rainbow, rainbowContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Sniper, sniperContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Teleport, teleportContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Fireworks, fireworksContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Snake, snakeContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Rabbit, rabbitContainer);
        weaponContainers.Add(Weapon.WeaponEnum.Rope, ropeContainer);

        CharacterManager = GetComponentInParent<CharacterManager>();
        WeaponPowerBar(0);
    }

    protected override void Start()
    {
        NetworkIdentifier = NetworkObject.NetworkIdentifierFrom(CharacterManager.NetworkIdentifier + "_weapon");
        crossHair.color = CharacterManager.PlayerController.PlayerColor;
        base.Start();
    }

    #endregion

    #region Public Methods

    public void Activate()
    {
        AimStop();
        isActive = true;
        isDropping = false;
        hasFired = false;
        StartedToFire = false;
        mouseClickDown = false;
        UpdateCurrentWeapon();
        sightContainer.localRotation = Quaternion.identity; // Reset rotation
    }

    public void Desactivate()
    {
        AimStop();
        isActive = false;
        IsWeaponAiming = false;
        IsWeaponFiring = false;
        HideWeapon();
    }

    #endregion

    #region Sound

    [StreamPlay(Method_FireLoadSound)]
    protected void FireLoadSound(int id)
    {
        switch ((Weapon.WeaponEnum) id) {
            case Weapon.WeaponEnum.Bazooka:
                soundEventFireLoadBazooka.Play();
                break;
            case Weapon.WeaponEnum.Grenade:
            case Weapon.WeaponEnum.StickyGrenade:
                soundEventFireLoadGrenade.Play();
                break;
            case Weapon.WeaponEnum.Shotgun:
                break;
            case Weapon.WeaponEnum.Skip:
                break;
            default:
                soundEventFireLoadDefault.Play();
                break;
        }
    }

    [StreamPlay(Method_WeaponItemSound)]
    protected void WeaponItemSound(int id)
    {
        FireLoadSoundStop(id);
        switch ((Weapon.WeaponEnum) id) {
            case Weapon.WeaponEnum.Bazooka:
            case Weapon.WeaponEnum.Mortar:
                soundEventFireBazooka.Play();
                break;
            case Weapon.WeaponEnum.Grenade:
            case Weapon.WeaponEnum.StickyGrenade:
            case Weapon.WeaponEnum.Bomb:
                soundEventFireGrenade.Play();
                break;
            case Weapon.WeaponEnum.Shotgun:
                soundEventFireShotgun.Play();
                break;
            case Weapon.WeaponEnum.Sniper:
                soundEventFireSniper.Play();
                break;
            case Weapon.WeaponEnum.Beam:
                soundEventFireBeam.Play();
                break;
            case Weapon.WeaponEnum.Shield:
                soundEventFireShield.Play();
                break;
            case Weapon.WeaponEnum.Dash:
            case Weapon.WeaponEnum.Skip:
            case Weapon.WeaponEnum.Teleport:
            case Weapon.WeaponEnum.Fireworks:
            case Weapon.WeaponEnum.Snake:
            case Weapon.WeaponEnum.Rabbit:
            case Weapon.WeaponEnum.Rope:
            case Weapon.WeaponEnum.HighJump:
            case Weapon.WeaponEnum.Meteor:
                break;
            default:
                soundEventFireDefault.Play();
                break;
        }
    }

    private void FireLoadSoundStop(int id)
    {
        switch ((Weapon.WeaponEnum) id) {
            case Weapon.WeaponEnum.Bazooka:
                soundEventFireLoadBazooka.Stop();
                break;
            case Weapon.WeaponEnum.Grenade:
            case Weapon.WeaponEnum.StickyGrenade:
                soundEventFireLoadGrenade.Stop();
                break;
            case Weapon.WeaponEnum.Shotgun:
                break;
            case Weapon.WeaponEnum.Skip:
                break;
            default:
                soundEventFireLoadDefault.Stop();
                break;
        }
    }

    #endregion

    #region Player Inputs

    public void AimToPosition(Vector2 pointerPosition)
    {
        if (hasFired) return;
        controllerWantsToAimMouse = pointerPosition;
    }

    private void AimStop()
    {
        controllerWantsToAimMouse = Vector2.zero;
    }

    private bool CanFireLoad()
    {
        if (!isActive) return false;
        if (hasFired) return false;
        if (CharacterManager.CharacterMove.IsMoving) return false;
        if (playerWeaponsCount == 0) return false; // Can happend in training mode

        // If character is into a Shield, prevent fire
        if (ShieldAmmoController.IntoShield(CharacterManager)) {
            soundEventErrorGeneric.Play();
            return false;
        }

        return true;
    }

    public void FireLoadAction(bool released)
    {
        if (!CanFireLoad()) return;
        // Unvalid mouse click, released is true (means mouse button up) while mouse button down has not been set to true
        // it can happend when the game lose/regain focus
        if (!mouseClickDown && released) {
            // Log.Message("Focus", "Discared unvalid mouse click");
            return;
        }
        if (!released) {
            mouseClickDown = true; // register the click down
        }

        IsWeaponFiring = true;

        // Fire (start)
        if (!StartedToFire) {

            HideWeaponSelector();

            // Weapon pre fire logic/check
            switch (currentWeapon.WeaponType) {
                case Weapon.WeaponEnum.Skip:
                    GameManager.Instance.RoundControllerInstance.AskForSkipTurn();
                    return;
                case Weapon.WeaponEnum.Beam:
                    float angle = WeaponManager.RotationToAngle(sightContainer.localRotation);
                    if (!beamController.IsBeamPositionValid(angle, CharacterManager.CharacterMove.IsFacingRight)) {
                        soundEventErrorGeneric.Play();
                        return;
                    }
                    break;
                default:
                    break;
            }

            StartedToFire = true;
            ShowWeapon();
            FireLoadSound((int) currentWeapon.WeaponType);
            NetworkRecordSnapshot(Method_FireLoadSound, (int) currentWeapon.WeaponType);
            CharacterManager.SetStartedLoadFire();
        }

        // Fire (confirm)
        if (StartedToFire && (released || weaponPower >= 0.95f || !currentWeapon.HasRetreat)) {
            FireConfirm();
        }
    }

    public void Drop()
    {
        if (!CanFireLoad()) return;
        if (!currentWeapon.IsDropable) return;

        IsWeaponFiring = true;
        StartedToFire = true;

        HideWeaponSelector();
        ShowWeapon();

        isDropping = true;
        weaponPower = 0.2f;
        FireConfirm();
    }

    private void FireConfirm()
    {
        if (hasFired) return;

        IsWeaponFiring = true;
        hasFired = true;

        if (weaponPower > 0.95f) {
            weaponPower = 1f;
        }

        WeaponItemInit(weaponPower);
        WeaponItemSound((int) currentWeapon.WeaponType);
        NetworkRecordSnapshot(Method_WeaponItemSound, (int) currentWeapon.WeaponType);
        CharacterManager.SetRetreatState(currentWeapon.HasRetreat);
    }

    private void Update()
    {
        if (!IsNetworkOwner) return;
        if (!isActive) return;
        if (hasFired) return;
        if (CharacterManager.CharacterMove.IsMoving) return;
        if (playerWeaponsCount == 0) return; // Can happend in training mode

        IsWeaponAiming = false;
        IsWeaponFiring = false;

        // Aiming
        if (controllerWantsToAimMouse != Vector2.zero) {

            float angle = 0.1f;
            Vector2 weaponPosition = transform.position;
            if (CharacterManager.CharacterMove.IsFacingRight) {
                angle = Vector2.SignedAngle(Vector2.right, controllerWantsToAimMouse - weaponPosition);
                if (controllerWantsToAimMouse.x < weaponPosition.x) {
                    // Character points right and mouse is on left (character's back)
                    CharacterManager.CharacterMove.Flip();
                    CharacterManager.CharacterMove.NetworkRecordSnapshot(Method_Flip);
                }
            } else {
                angle = -Vector2.SignedAngle(Vector2.left, controllerWantsToAimMouse - weaponPosition);
                if (controllerWantsToAimMouse.x > weaponPosition.x) {
                    CharacterManager.CharacterMove.Flip();
                    CharacterManager.CharacterMove.NetworkRecordSnapshot(Method_Flip);
                }
            }

            if (angle > currentWeapon.MaxAngle) {
                angle = currentWeapon.MaxAngle;
            }
            if (angle < currentWeapon.MinAngle) {
                angle = currentWeapon.MinAngle;
            }

            // Log.Message("Input", "AimToPosition angle: " + angle);
            sightContainer.localRotation = WeaponManager.AngleToRotation(angle);

            controllerWantsToAimMouse = Vector2.zero;

            IsWeaponAiming = true;

            ShowWeapon();
            HideWeaponSelector();
            Bend(angle);
        }

        // Fire (increase power)
        if (StartedToFire && weaponPower < 1f) {

            IsWeaponFiring = true;

            if (!currentWeapon.CanReceiveForce) {
                weaponPower = 1f;
            } else {
                weaponPower += 0.5f * Time.deltaTime;
                WeaponPowerBar(weaponPower);
            }
        }

        // Fire (confirm)
        if (StartedToFire && weaponPower >= 0.95f) {

            IsWeaponFiring = true;

            FireConfirm();
        }

        RecordUpdate();
    }

    private void Bend(float angle)
    {
        CharacterManager.CharacterAnimator.Bend(angle, CharacterManager.CharacterMove.IsFacingRight);
        if (currentWeaponContainer) {
            // Move weapon (with some extra care for specific weapons)
            switch (currentWeapon.WeaponType) {
                case Weapon.WeaponEnum.Mortar:
                    mortarController.SetAngle(angle, CharacterManager.CharacterMove.IsFacingRight);
                    // overriding the bend angle
                    CharacterManager.CharacterAnimator.Bend(angle - 45, CharacterManager.CharacterMove.IsFacingRight);
                    break;
                case Weapon.WeaponEnum.Sniper:
                    sniperController.SetAngle(angle, CharacterManager.CharacterMove.IsFacingRight);
                    currentWeaponContainer.transform.rotation = Quaternion.Euler(0, 0, angle * (CharacterManager.CharacterMove.IsFacingRight ? 1f : -1f));
                    break;
                case Weapon.WeaponEnum.Beam:
                    beamController.SetAngle(angle, CharacterManager.CharacterMove.IsFacingRight);
                    break;
                default:
                    currentWeaponContainer.transform.rotation = Quaternion.Euler(0, 0, angle * (CharacterManager.CharacterMove.IsFacingRight ? 1f : -1f));
                    break;
            }

            // Move arm/hand
            var leftArm = currentWeaponContainer.transform.Find("LeftArm");
            var leftHand = currentWeaponContainer.transform.Find("LeftHand");
            if (leftArm != null) {
                CharacterManager.CharacterAnimator.PositionLeftArm(leftArm.position, leftHand.position);
            }
            var rightArm = currentWeaponContainer.transform.Find("RightArm");
            var rightHand = currentWeaponContainer.transform.Find("RightHand");
            if (rightArm != null) {
                CharacterManager.CharacterAnimator.PositionRightArm(rightArm.position, rightHand.position);
            }
        }
    }

    #endregion

    #region AI

    public async UniTask AIFireTargetHit(CharacterTargetHit hit, CancellationToken cancellationToken)
    {
        if (!CharacterManager.IsActive) throw new OperationCanceledException();
        // Log.Message("AI Weapon", "Will fire " + hit);
        if (hit.WeaponIsFacingRight != CharacterManager.CharacterMove.IsFacingRight) {
            CharacterManager.CharacterMove.Flip();
            await UniTask.WaitForEndOfFrame(cancellationToken);
        }
        await AIFireAndAim(hit.WeaponIdentifier, hit.WeaponAngle, hit.WeaponPower, cancellationToken)
            .AttachExternalCancellation(cancellationToken);
    }

    public async UniTask AIFireBazooka(float angle, float power, CancellationToken cancellationToken)
    {
        if (!CharacterManager.IsActive) throw new OperationCanceledException();
        // Log.Message("AI Weapon", "Will fire a bazooka");
        await AIFireAndAim(Weapon.AmmoEnum.Bazooka, angle, power, cancellationToken)
            .AttachExternalCancellation(cancellationToken);
    }

    public async UniTask AIFireGrenade(float angle, float power, CancellationToken cancellationToken)
    {
        if (!CharacterManager.IsActive) throw new OperationCanceledException();
        // Log.Message("AI Weapon", "Will fire a grenade");
        await AIFireAndAim(Weapon.AmmoEnum.Grenade4, angle, power, cancellationToken)
            .AttachExternalCancellation(cancellationToken);
    }

    public async UniTask AIDrop(Weapon.AmmoEnum weaponIdentifier, CancellationToken cancellationToken)
    {
        if (!CharacterManager.IsActive) throw new OperationCanceledException();
        // Log.Message("AI Weapon", "Will drop a grenade");
        GetSpecificWeapon(weaponIdentifier);
        Drop();
        await UniTask.DelayFrame(3, cancellationToken: cancellationToken);
    }

    public async UniTask AIFireBeam(float angle, CancellationToken cancellationToken)
    {
        if (!CharacterManager.IsActive) throw new OperationCanceledException();
        // Log.Message("AI Weapon", "Will place a beam");
        await AIFireAndAim(Weapon.AmmoEnum.Beam, angle, power: 1, cancellationToken)
            .AttachExternalCancellation(cancellationToken);
    }

    private async UniTask AIFireAndAim(Weapon.AmmoEnum weaponIdentifier, float angle, float power, CancellationToken cancellationToken)
    {
        if (!CharacterManager.IsActive) throw new OperationCanceledException();
        // Select weapon
        GetSpecificWeapon(weaponIdentifier);

        // Aim
        sightContainer.localRotation = WeaponManager.AngleToRotation(angle);

        ShowWeapon();
        Bend(angle);

        // Fire
        FireLoadAction(released: false);
        await UniTask.WaitForEndOfFrame(cancellationToken);
        while (weaponPower < power) {
            FireLoadAction(released: false);
            await UniTask.WaitForEndOfFrame(cancellationToken);
        }
        weaponPower = power;
        FireLoadAction(released: true);
    }

    #endregion

    #region Showing Weapon

    private void ShowWeapon()
    {
        if (playerWeaponsCount == 0) return; // Can happend in training mode
        if (isWeaponShown) return;
        isWeaponShown = true;

        HideWeaponContainer();
        ShowWeaponContainer();

        // Reset angle if needed
        float angle = WeaponManager.RotationToAngle(sightContainer.localRotation);
        if (angle > currentWeapon.MaxAngle) {
            angle = currentWeapon.MaxAngle;
        }
        if (angle < currentWeapon.MinAngle) {
            angle = currentWeapon.MinAngle;
        }

        sightContainer.gameObject.SetActive(true);
        sightContainer.localPosition = Vector2.up * currentWeapon.SightOffset;
        sightContainer.transform.rotation = Quaternion.Euler(0, 0, angle * (CharacterManager.CharacterMove.IsFacingRight ? 1f : -1f));

        sightRenderer.gameObject.SetActive(currentWeapon.ShowSight);

        Bend(angle);
    }

    private void UpdateWeapon()
    {
        // NetworkAssertNotGuest();

        HideWeapon();
        ShowWeapon();

        NetworkRecordSnapshot(Method_UpdateWeapon_Guest);
    }

    [StreamPlay(Method_UpdateWeapon_Guest)]
    protected void UpdateWeapon_Guest()
    {
        HideWeapon();
        ShowWeapon();
    }

    // Used in CharacterMove to hide the weapon when moving/jumping
    public void HideWeapon()
    {
        HideWeaponSelector();
        HideWeaponContainer();
        WeaponPowerBar(0);
        sightContainer.gameObject.SetActive(false);
        CharacterManager.CharacterAnimator.ResetBend();
        weaponPower = 0;
        isWeaponShown = false;
    }

    #endregion

    #region Weapon Selector

    // Used in PlayerController to show the selector when the input is called
    public void ShowWeaponSelector()
    {
        UserInterfaceManager.Instance.ShowWeaponPanel(CharacterManager);
        UserInterfaceManager.Instance.UpdateWeaponPanel(currentWeapon);
        IsWeaponSelectorShown = true;
    }

    private void UpdateWeaponSelector()
    {
        ShowWeaponSelector();
    }

    // Used in PlayerController to hide the selector when the input is called
    public void HideWeaponSelector()
    {
        IsWeaponSelectorShown = false;
        UserInterfaceManager.Instance.HideWeaponPanel();
    }

    public void GetNextWeaponHorizontal(bool prev)
    {
        var weaponGroups = playerWeaponsByGroup.Keys.ToList();
        int index = weaponGroups.FindIndex(v => v == currentWeaponGroup);
        if (!prev) {
            index = Basics.NextIndex(index, weaponGroups.Count, looping: true);
        } else {
            index = Basics.PreviousIndex(index, weaponGroups.Count, looping: true);
        }

        currentWeaponIndexForGroup = 0;
        try {
            currentWeaponGroup = weaponGroups[index];
        } catch {
            currentWeaponGroup = weaponGroups.First();
        }

        UpdateWeaponSelector();
        UpdateCurrentWeapon();
        UpdateWeapon();
    }

    public void GetNextWeaponVertical(bool prev)
    {
        if (!playerWeaponsByGroup.ContainsKey(currentWeaponGroup)) {
            currentWeaponIndexForGroup = 0;
            return;
        }

        var group = playerWeaponsByGroup[currentWeaponGroup];
        if (!prev) {
            currentWeaponIndexForGroup = Basics.NextIndex(currentWeaponIndexForGroup, group.Count, looping: true);
        } else {
            currentWeaponIndexForGroup = Basics.PreviousIndex(currentWeaponIndexForGroup, group.Count, looping: true);
        }

        UpdateWeaponSelector();
        UpdateCurrentWeapon();
        UpdateWeapon();
    }

    public void SelectWeaponButton(Weapon weapon)
    {
        GetSpecificWeapon(weapon.AmmoType);
    }

    #endregion

    // This gameobject contains the weapon representation (ie: a bazooka)
    #region Weapon Container

    private void ShowWeaponContainer()
    {
        // We try/catch here as we got some exception on production, bug is still under investigation
        // https://dashboard.unity3d.com/organizations/9070992153360/projects/b14779fb-1986-416e-b87f-2c0d1df976a1/cloud-diagnostics/crashes-exceptions/problem/3da376f3f379b1069739e793f54ef9a7
#if CC_EXTRA_CARE
try {
#endif
        if (currentWeapon == null) {
            Log.Critical("CharacterWeapon", "Called ShowWeaponContainer but the currentWeapon is null");
            return;
        }
        if (!weaponContainers.ContainsKey(currentWeapon.WeaponType)) {
            Log.Critical("CharacterWeapon", "No weapon container for: " + currentWeapon.WeaponType);
            return;
        }
        currentWeaponContainer = weaponContainers[currentWeapon.WeaponType];
        currentWeaponContainer.SetActive(true);
        switch (currentWeapon.WeaponType) {
            case Weapon.WeaponEnum.Dash:
                dashController.StartShow();
                break;
            default: break;
        }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    private void HideWeaponContainer()
    {
        if (currentWeaponContainer == null) return;
        dashController.StopShow();
        foreach (GameObject go in weaponContainers.Values) {
            go.SetActive(false);
        }
        currentWeaponContainer = null;

        CharacterManager.CharacterAnimator.ResetLeftArm();
        CharacterManager.CharacterAnimator.ResetRightArm();
    }

    #endregion

    #region Helpers

    private void GetSpecificWeapon(Weapon.AmmoEnum weaponIdentifier)
    {
        currentWeaponGroup = 0;
        currentWeaponIndexForGroup = 0;

        foreach (var kv in playerWeaponsByGroup) {
            for (int index = 0, max = kv.Value.Count; index < max; index++) {
                if (kv.Value[index].AmmoType == weaponIdentifier) {
                    currentWeaponGroup = kv.Key;
                    currentWeaponIndexForGroup = index;
                    UpdateCurrentWeapon();
                    UpdateWeapon();
                    return;
                }
            }
        }

        UpdateCurrentWeapon();
        UpdateWeapon();
    }

    public void UpdateCurrentWeapon()
    {
        // NetworkAssertNotGuest();

        if (playerWeaponsCount == 0) {
            // Log.Critical("CharacterWeapon", "No weapon available");
            currentWeapon = Weapon.None;
            return;
        }

        // Log.Message("CharacterWeapon", "UpdateCurrentWeapon");
        var weaponsByGroup = CharacterManager.InventoryDelegate.GetPlayerWeaponsByGroup(CharacterManager.Character);
        if (!weaponsByGroup.ContainsKey(currentWeaponGroup)) {
            currentWeaponGroup = weaponsByGroup.Keys.First();
            currentWeaponIndexForGroup = 0;
        }

        if (currentWeaponIndexForGroup >= weaponsByGroup[currentWeaponGroup].Count) {
            currentWeaponIndexForGroup = 0;
        }

        currentWeapon = weaponsByGroup[currentWeaponGroup][currentWeaponIndexForGroup];
        NetworkRecordSnapshot(Method_UpdateCurrentWeapon_Guest, (int) currentWeapon.AmmoType);
    }

    [StreamPlay(Method_UpdateCurrentWeapon_Guest)]
    protected void UpdateCurrentWeapon_Guest(int weaponIdentifier)
    {
        // Log.Message("CharacterWeapon", "UpdateCurrentWeapon_Guest");
        currentWeapon = WeaponManager.Instance.GetWeapon((Weapon.AmmoEnum) weaponIdentifier);
    }

    // Used in GameManager for the AI
    public Vector2 FirePointPosition(float angle, Weapon weapon, bool isFacingRight)
    {
        Vector2 direction = WeaponManager.AngleToDirection(angle, isFacingRight);
        // Move the fire point so it does not collide with the character firing
        Vector2 position = (Vector2)firePoint.position + (direction.normalized * 2.5f);
        if (weapon.IsDropable) {
            position = firePoint.position;
        }
        if (weapon.IsCharacterItself) {
            position = CharacterManager.MyPosition + new Vector2(0, CharacterManager.MyHeight / 2f);
        }

        return position;
    }

    /// <summary>
    /// UI element showing the power of the upcoming fire (size is from 0 to 1)
    /// </summary>
    private void WeaponPowerBar(float size)
    {
        if (size <= 0) {
            fireLoadFill.transform.localPosition = Vector2.zero;
            fireLoadFill.transform.localScale = Vector2.one * 0.01f;
            fireLoadFill.color = Color.blue;
        } else {
            fireLoadFill.transform.localPosition = new Vector2(size, 0f);
            fireLoadFill.transform.localScale = Vector2.one * size;
            fireLoadFill.color = Color.Lerp(Color.blue, Color.red, size);
        }
    }

    #endregion

    #region Weapon Action

    // This code is similar to BoxWeaponController
    // if you change something here you may want to change it there
    private void WeaponItemInit(float power)
    {
        float angle = sightContainer.localRotation.eulerAngles.z;
        if (isDropping) {
            angle = -90;
        }
        Vector2 firePointPosition = FirePointPosition(angle, currentWeapon, CharacterManager.CharacterMove.IsFacingRight);

        // Equivalent to InstantiateNetworkObject()
        AmmoInterface weaponLogic = WeaponManager.Instance.GetWeaponLogic(currentWeapon.WeaponType, firePointPosition, firePoint.rotation);
        weaponLogic.Init(CharacterManager, currentWeapon, angle, CharacterManager.CharacterMove.IsFacingRight, power);

        if (currentWeapon.RequestCameraFollow) {
            CameraManager.Instance.RequestFollow(weaponLogic.transform);
        }

        // Extra action for specific weapons
        switch (currentWeapon.WeaponType) {
            case Weapon.WeaponEnum.Beam:
                beamController.ConfirmPosition(CharacterManager.CharacterMove.IsFacingRight);
                break;
            case Weapon.WeaponEnum.Shield:
                if (CharacterManager.Shield != null) {
                    CharacterManager.Shield.End();
                }
                CharacterManager.Shield = (ShieldAmmoController) weaponLogic;
                break;
            default:
                // Nothing special
                break;
        }

        NetworkRecordSnapshot(
            Method_WeaponItem_Guest,
            weaponLogic.NetworkIdentifier,
            (int) currentWeapon.AmmoType,
            firePointPosition,
            firePoint.rotation
        );

        // Warning, this will update currentWeapon, so we want to send the info to the network before executing it
        CharacterManager.InventoryDelegate.RemovePlayerWeapon(CharacterManager, currentWeapon);

        WeaponPowerBar(0);
        Desactivate();
    }

    [StreamPlay(Method_WeaponItem_Guest)]
    protected void WeaponItem_Guest(int ownerNetworkIdentifier, int weaponIdentifier, Vector2 position, Quaternion rotation)
    {
        Weapon weapon = WeaponManager.Instance.GetWeapon((Weapon.AmmoEnum) weaponIdentifier);
        AmmoInterface weaponLogic = WeaponManager.Instance.GetWeaponLogic(weapon.WeaponType, position, rotation);
        weaponLogic.NetworkIdentifier = ownerNetworkIdentifier;
        // Not my best move
        weaponLogic.gameObject.GetComponent<NetworkObject>().NetworkRefresh();

        if (weapon.RequestCameraFollow) {
            CameraManager.Instance.RequestFollow(weaponLogic.transform);
        }

        WeaponPowerBar(0);
        Desactivate();

        StreamPlayPlayer.Refresh();
    }

    #endregion

    #region Stream Play

    private void RecordUpdate()
    {
        if (!IsNetworkOwner) return;
        // Because this rotation is mouse based, we sync it all the time
        NetworkRecordFloatPack(weaponPower, sightContainer.localRotation, IsWeaponAiming || IsWeaponFiring);
    }

    public void OnFloatPack(FloatPack floatPack)
    {
        WeaponPowerBar(floatPack.NextFloat());

        sightContainer.localRotation = floatPack.NextQuaternion();
        if (floatPack.NextBool()) { // => IsWeaponAiming || IsWeaponFiring
            Bend(WeaponManager.RotationToAngle(sightContainer.localRotation));
            ShowWeapon();
        }
    }

    #endregion
}
