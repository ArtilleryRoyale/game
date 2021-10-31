using CC.StreamPlay;
using FMODUnity;
using Jrmgx.Helpers;
using UnityEngine;

public class BoxWeaponController : BoxControllerBase
{
    public Weapon.AmmoEnum predefinedWeapon = Weapon.AmmoEnum.None;
    public Weapon Weapon { get; protected set; }
    public bool ShowContent { get; protected set; } = false;

    [Header("References")]
    [SerializeField] private StudioEventEmitter soundEventWeaponPackTaken = default;
    [SerializeField] private Sprite boxWeaponUnknownSprite = default;
    [SerializeField] private SpriteRenderer weaponIconRenderer = default;

    protected override void Start()
    {
        base.Start();
        if (isPredefined && predefinedWeapon != Weapon.AmmoEnum.None) {
            SetWeapon(WeaponManager.Instance.GetWeapon(predefinedWeapon), GameManager.Instance.CurrentGameOption.ShowBoxContent);
        }
    }

    public void SetWeapon(Weapon weapon, bool showContent)
    {
        Weapon = weapon;
        ShowContent = showContent;
        if (showContent) {
            weaponIconRenderer.sprite = weapon.Icon;
        } else {
            weaponIconRenderer.gameObject.SetActive(false);
            spriteRenderer.sprite = boxWeaponUnknownSprite;
        }
    }

    protected override void DidCollide(Collider2D collision)
    {
        if (!IsValid) return;
        var characterManager = collision.GetComponent<CharacterManager>();
        if (characterManager != null && characterManager.IsActive) {
            characterManager.CollectWeapon(Weapon);
            DidPickup();
            if (string.IsNullOrEmpty(Weapon.WeaponHumanName)) return; // For grenade in training
            SFXManager.Instance.GetSFX(SFXManager.SFXType.PickupBox, transform.position).Init();
            soundEventWeaponPackTaken.Play();
        }
    }

    public override void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        if (!IsValid) return;
        if (ExplosionHasBeenGenerated) return;
        // Prevent multiple instantiation if hiting multiple colliders
        ExplosionHasBeenGenerated = true;

        ExplosionManager.Instance.GetExplosion(transform.position).Init(Explosion);

        // If it gets the explosion on its left, it means that the box shot is "facing" right
        bool isFacingRight = explosion.transform.position.x < myPosition.x;
        float power = Mathf.Clamp01(damages / 100f);
        float angle = Random.Range(45, 90);
        var rotation = WeaponManager.AngleToRotation(90);

        // Debugging.DrawAngle(transform.position, isFacingRight ? Vector2.right : Vector2.left, angle * (isFacingRight ? 1 : -1), Color.blue, 10);

        // Log.Message("BoxWeaponController", "Weapon Item Init power: " + power + " angle: " + angle + " isFacingRight: " + isFacingRight);
        WeaponItemInit(power, angle, rotation, isFacingRight);

        NetworkDestroy();
    }

    #region Weapon Action

    // This code is similar to CharacterWeapon
    // if you change something here you may want to change it there
    // TODO prio 5 at some point a refactoring could be envisaged (but it may not be a good idea)
    private void WeaponItemInit(float power, float angle, Quaternion rotation, bool isFacingRight)
    {
        Vector2 position = myPosition + Vector2.up;

        // Equivalent to InstantiateNetworkObject()
        AmmoInterface weaponLogic = WeaponManager.Instance.GetWeaponLogic(Weapon.WeaponType, position, rotation);
        weaponLogic.Init(null, Weapon, angle, isFacingRight, power);

        if (Weapon.RequestCameraFollow) {
            CameraManager.Instance.RequestFollow(weaponLogic.transform);
        }

        NetworkRecordSnapshot(
            Method_WeaponItemBox_Guest,
            weaponLogic.NetworkIdentifier,
            (int) Weapon.AmmoType,
            position,
            rotation
        );
    }

    [StreamPlay(Method_WeaponItemBox_Guest)]
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

        StreamPlayPlayer.Refresh();
    }

    #endregion
}
