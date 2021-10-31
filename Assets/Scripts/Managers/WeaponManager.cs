using UnityEngine;
using System.Collections.Generic;
using System;

public class WeaponManager : MonoBehaviour
{
    #region Fields

    public static WeaponManager Instance;

    [Header("Prefabs")]
    [SerializeField] private BazookaAmmoController bazookaLogicPrefab = default;
    [SerializeField] private BazookaAmmoController bazookaLogicPrefabAI = default;
    [SerializeField] private GrenadeAmmoController grenadeLogicPrefab = default;
    [SerializeField] private GrenadeAmmoController grenadeLogicPrefabAI = default;
    [SerializeField] private StickyGrenadeAmmoController stickyGrenadeLogicPrefab = default;
    [SerializeField] private ShotgunAmmoController shotgunLogicPrefab = default;
    [SerializeField] private MortarAmmoController mortarLogicPrefab = default;
    [SerializeField] private BombAmmoController bombLogicPrefab = default;
    [SerializeField] private DashAmmoController dashLogicPrefab = default;
    [SerializeField] private HighJumpAmmoController highJumpLogicPrefab = default;
    [SerializeField] private BeamAmmoController beamLogicPrefab = default;
    [SerializeField] private SniperAmmoController sniperLogicPrefab = default;
    [SerializeField] private TeleportAmmoController teleportLogicPrefab = default;
    [SerializeField] private FireworksAmmoController fireworksLogicPrefab = default;
    [SerializeField] private SnakeAmmoController snakeLogicPrefab = default;
    [SerializeField] private RabbitAmmoController rabbitLogicPrefab = default;
    [SerializeField] private RopeAmmoController ropeLogicPrefab = default;
    [SerializeField] private ShieldAmmoController shieldLogicPrefab = default;
    [SerializeField] private MeteorAmmoController meteorLogicPrefab = default;
    [SerializeField] private RainbowAmmoController rainbowLogicPrefab = default;

    [Header("Weapons")]
    [SerializeField] private Weapon weaponRook = default;
    [SerializeField] private Weapon weaponBishop = default;
    [SerializeField] private Weapon weaponKnight = default;
    [SerializeField] private Weapon weaponQueen = default;
    [SerializeField] private Weapon weaponKing = default;
    public List<Weapon> WeaponsDefault = default;
    public List<Weapon> WeaponsExtra = default;
    public List<Weapon> WeaponsAll { get; protected set; } = new List<Weapon>();

    #endregion

    #region Init

    protected void Awake()
    {
        Instance = this;
        WeaponsAll.AddRange(WeaponsDefault);
        WeaponsAll.AddRange(WeaponsExtra);
        WeaponsAll.Add(weaponRook);
        WeaponsAll.Add(weaponBishop);
        WeaponsAll.Add(weaponKnight);
        WeaponsAll.Add(weaponQueen);
        WeaponsAll.Add(weaponKing);
        if (GameManager.Instance.CurrentGameOption.AllWeaponsNoLimits) {
            // All weapons in sandbox
            WeaponsDefault.AddRange(WeaponsExtra);
        }
    }

    #endregion

    #region Getter

    public Weapon GetWeaponForCharacter(Character.CharacterTypeEnum characterType)
    {
        if (GameManager.Instance.CurrentGameOption.IsMission) return Weapon.None;
        switch (characterType) {
            case Character.CharacterTypeEnum.Rook: return weaponRook;
            case Character.CharacterTypeEnum.Bishop: return weaponBishop;
            case Character.CharacterTypeEnum.Knight: return weaponKnight;
            case Character.CharacterTypeEnum.Queen: return weaponQueen;
            case Character.CharacterTypeEnum.King: return weaponKing;
        }
        return Weapon.None;
    }

    public Weapon GetWeapon(Weapon.AmmoEnum ammo)
    {
        foreach (Weapon weapon in WeaponsAll) {
            if (weapon.AmmoType.Equals(ammo)) return weapon;
        }
        throw new Exception("Invalid Weapon identifier: " + ammo.ToString());
    }

    public AmmoInterface GetWeaponLogic(Weapon.WeaponEnum weaponType, Vector3 position, Quaternion rotation, bool IsAISimulationActive = false)
    {
        AmmoInterface weaponLogic = null;
        switch (weaponType) {
            case Weapon.WeaponEnum.Bazooka:
                weaponLogic = Instantiate(
                    IsAISimulationActive ? bazookaLogicPrefabAI : bazookaLogicPrefab, position, rotation
                ); break;
            case Weapon.WeaponEnum.Grenade:
                weaponLogic = Instantiate(
                    IsAISimulationActive ? grenadeLogicPrefabAI : grenadeLogicPrefab, position, rotation
                ); break;
            case Weapon.WeaponEnum.Bomb:
                weaponLogic = Instantiate(bombLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.StickyGrenade:
                weaponLogic = Instantiate(stickyGrenadeLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Mortar:
                weaponLogic = Instantiate(mortarLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Shotgun:
                weaponLogic = Instantiate(shotgunLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Beam:
                weaponLogic = Instantiate(beamLogicPrefab, Vector2.zero, rotation); break;
            case Weapon.WeaponEnum.Dash:
                weaponLogic = Instantiate(dashLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.HighJump:
                weaponLogic = Instantiate(highJumpLogicPrefab, position, Quaternion.identity); break;
            case Weapon.WeaponEnum.Sniper:
                weaponLogic = Instantiate(sniperLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Teleport:
                weaponLogic = Instantiate(teleportLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Fireworks:
                weaponLogic = Instantiate(fireworksLogicPrefab, position, Quaternion.identity); break;
            case Weapon.WeaponEnum.Snake:
                weaponLogic = Instantiate(snakeLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Rabbit:
                weaponLogic = Instantiate(rabbitLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Rope:
                weaponLogic = Instantiate(ropeLogicPrefab, position, rotation); break;
            case Weapon.WeaponEnum.Shield:
                weaponLogic = Instantiate(shieldLogicPrefab, position, Quaternion.identity); break;
            case Weapon.WeaponEnum.Meteor:
                weaponLogic = Instantiate(meteorLogicPrefab, position, Quaternion.identity); break;
            case Weapon.WeaponEnum.Rainbow:
                weaponLogic = Instantiate(rainbowLogicPrefab, Vector2.zero, Quaternion.identity); break;
            default:
                throw new Exception("Unknown weapon " + weaponType.ToString());
        }
        weaponLogic.NetworkIdentifier = NetworkObject.NetworkIdentifierNew();
        weaponLogic.IsAISimulationActive = IsAISimulationActive;
        return weaponLogic;
    }

    #endregion

    #region Helpers

    public static float ClampAngle(float angle, Weapon weapon)
    {
        return Mathf.Clamp(angle, weapon.MinAngle, weapon.MaxAngle);
    }

    public static float RotationToAngle(Quaternion rotation)
    {
        float angle = rotation.eulerAngles.z;
        if (angle > 180) {
            angle -= 360;
        }
        return angle;
    }

    public static Quaternion AngleToRotation(float angle)
    {
        var q = new Quaternion();
        q.eulerAngles = new Vector3(0, 0, angle);
        return q;
    }

    public static Vector2 AngleToDirection(float angle, bool isFacingRight)
    {
        var direction = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));
        if (!isFacingRight) {
            direction = Vector2.Reflect(direction, Vector2.up);
            direction = Quaternion.Euler(180, 180, 0) * direction;
        }
        return direction;
    }

    public static float DirectionToAngle(Vector2 direction, bool isFacingRight)
    {
        var angle = Vector2.SignedAngle(direction, isFacingRight ? Vector2.right : Vector2.left);
        if (isFacingRight) return angle * -1;
        return angle;
    }

    #endregion
}
