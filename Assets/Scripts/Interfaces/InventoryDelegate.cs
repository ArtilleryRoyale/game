using System.Collections.Generic;

public interface InventoryDelegate
{
    /// <summary>
    /// Return the weapon/ammo inventory for this player
    /// Key is Weapon, value is ammonution left
    /// Note: if no ammo left, the weapon is not returned
    /// This dictionary is ordered by Key.WeaponGroup
    /// </summary>
    Dictionary<Weapon, int> GetPlayerWeapons(Character character);
    Dictionary<Weapon.WeaponGroupEnum, List<Weapon>> GetPlayerWeaponsByGroup(Character character);
    /// <summary>
    /// Return the number of weapon for this player,
    /// this does not take special weapon into account.
    /// </summary>
    int GetPlayerWeaponsCountNoSpecial();
    void AddPlayerWeapon(CharacterManager characterManager, Weapon weapon);
    void RemovePlayerWeapon(CharacterManager characterManager, Weapon weapon, int unit = 1, bool forceAll = false);
}
