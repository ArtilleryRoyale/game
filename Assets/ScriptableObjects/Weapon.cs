using System;
using UnityEngine;

[CreateAssetMenu(fileName="Weapon", menuName="ArtilleryRoyale/Weapon")]
public class Weapon : ScriptableObject, IEquatable<Weapon>
{
    public enum WeaponEnum : int {
        None = -1, Bazooka = 0, Grenade = 1, Shotgun = 2, Mortar = 3, Bomb = 4, Beam = 5, Dash = 6, Skip = 7, Sniper = 8, Teleport = 9, Fireworks = 10, Snake = 11, Rabbit = 12, Rope = 13, Shield = 14, StickyGrenade = 15, HighJump = 16, Meteor = 17, Rainbow = 18
    }

    public enum AmmoEnum : int {
        None = -1, Bazooka = 0, Grenade3 = 1, Grenade4 = 2, Grenade5 = 3, Shotgun = 4, Mortar = 5, Bomb = 6, Beam = 7, Dash = 8, Skip = 9, Sniper = 10, Teleport = 11, Fireworks = 12, Snake = 13, Rabbit = 14, Rope = 15, Shield = 16, StickyGrenade = 17, HighJump = 18, Meteor = 19, Rainbow = 20
    }

    public enum WeaponGroupEnum : int {
        None = -1, Bazooka = 10, Grenade = 20, Shotgun = 30, Move = 35, Beam = 40, SpellMain = 100, SpellSecond = 110, Extra = 200
    }

    [Header("Weapon")]
    public WeaponEnum WeaponType;
    public string WeaponHumanName;
    public Sprite Icon;
    public WeaponGroupEnum WeaponGroup;

    [Header("Config")]
    public bool CanReceiveForce;
    public bool IsDropable;
    [Tooltip("IsCharacterItself superseeds IsDropable regarding firePoint positioning")]
    public bool IsCharacterItself;
    public bool HasRetreat = true;
    public int MinAngle = -90;
    public int MaxAngle = 90;
    public bool ShowSight = true;
    public bool RequestCameraFollow = true;
    public bool AIKnowsHowToUse = false;
    public Character.CharacterTypeEnum SpecialFor = Character.CharacterTypeEnum.None;

    [Header("Ammo")]
    public AmmoEnum AmmoType;
    public Explosion Explosion;
    public int ExplodeInSeconds;
    public float WindMultiplier;
    public float Speed;
    public int UnitsDefault = 1;
    public bool UnitsUnlimited;

    [Header("Visual")]
    public float SightOffset = 0f; // Y axis

    public bool IsValid => WeaponType != WeaponEnum.None;
    public bool IsSpecial => SpecialFor != Character.CharacterTypeEnum.None;

    private string internalWeaponId => "" + WeaponType + "+" + AmmoType;

    public static Weapon None => WeaponManager.Instance.GetWeapon(AmmoEnum.None);

    public override string ToString()
    {
       return "Weapon " + internalWeaponId;
    }

    public bool Equals(Weapon w)
    {
        return internalWeaponId.Equals(w.internalWeaponId);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) {
            return false;
        }

        return internalWeaponId.Equals(((Weapon)obj).internalWeaponId);
    }

    public override int GetHashCode()
    {
        return internalWeaponId.GetHashCode() / 2;
    }
}
