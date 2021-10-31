public struct CharacterTargetHit
{
    public CharacterTargetTag TargetTag;
    public Weapon.AmmoEnum WeaponIdentifier;
    public float WeaponPower;
    public float WeaponAngle;
    public bool WeaponIsFacingRight;
    public float ExplosionDamages;
    public int ExplosionIdentifier;

    public bool IsHuman => TargetTag.Equals(CharacterTargetTag.Human);

    public CharacterTargetHit(CharacterTargetTag tag, ExplosionController explosion, float damages)
    {
        TargetTag = tag;
        WeaponPower = explosion.WeaponPower;
        WeaponAngle = explosion.WeaponAngle;
        WeaponIsFacingRight = explosion.WeaponIsFacingRight;
        WeaponIdentifier = explosion.Weapon.AmmoType;
        ExplosionDamages = damages;
        ExplosionIdentifier = explosion.ExplosionUniqueIdentifier;
    }

    public override string ToString()
    {
        return "CharacterTargetHit" +
            " explosion: " + ExplosionIdentifier +
            //" tag: " + TargetTag +
            " weapon: " + WeaponIdentifier +
            " power: " + WeaponPower +
            " angle: " + WeaponAngle +
            " facing right: " + WeaponIsFacingRight +
            " damages: " + ExplosionDamages;
    }
}
