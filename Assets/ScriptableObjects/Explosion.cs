using UnityEngine;

[CreateAssetMenu(fileName = "Explosion", menuName="ArtilleryRoyale/Explosion")]
public class Explosion : ScriptableObject
{
    public enum TypeEnum : int {
        // Specials
        Mine = 0, Character = 1, Box = 2,
        // Weapons
        Bazooka = 3, Grenade = 4, Shotgun = 5, Mortar = 6, Bomb = 7, Dash = 8, Fireworks = 9, Rabbit = 10, Sniper = 11, Snake = 12, Meteor = 13,
    }

    public TypeEnum Type;
    public float Force;
    public float Damage;
    public float Radius;

    [Header("Explosion Type")]
    public bool WithExplosionAnimation = true;
    public SFXManager.SFXType SFXType = SFXManager.SFXType.ExplosionDefault;
}
