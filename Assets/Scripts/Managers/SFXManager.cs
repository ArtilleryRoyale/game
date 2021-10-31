using UnityEngine;

public class SFXManager : MonoBehaviour
{
    #region Fields

    public static SFXManager Instance;

    public enum SFXType : int {
        WeaponBazooka = 0, WeaponDash = 1,
        ExplosionDead = 2, ExplosionDefault = 3, ExplosionWithText = 4, ExplosionFireDash = 5, ExplosionGun = 10,
        LavaFall = 6,
        PickupHealth = 7, PickupBox = 8, PickupTurn = 9,
    }

    [Header("Prefabs")]
    [SerializeField] private SFXWeaponBazookaController SFXWeaponBazookaPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXWeaponDashPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXExplosionDeadPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXExplosionDefaultPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXExplosionWithTextPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXExplosionGunPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXExplosionFireDash = default;
    [SerializeField] private SFXSimpleSpawnController SFXLavaFallPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXPickupHealthPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXPickupBoxPrefab = default;
    [SerializeField] private SFXSimpleSpawnController SFXPickupTurnPrefab = default;

    #endregion

    protected void Awake()
    {
        Instance = this;
    }

    protected GameObject GetPrefabForSFX(SFXType type)
    {
        switch (type) {
            case SFXType.WeaponBazooka: return SFXWeaponBazookaPrefab.gameObject;
            case SFXType.ExplosionDead: return SFXExplosionDeadPrefab.gameObject;
            case SFXType.ExplosionDefault: return SFXExplosionDefaultPrefab.gameObject;
            case SFXType.ExplosionWithText: return SFXExplosionWithTextPrefab.gameObject;
            case SFXType.ExplosionGun: return SFXExplosionGunPrefab.gameObject;
            case SFXType.LavaFall: return SFXLavaFallPrefab.gameObject;
            case SFXType.PickupHealth: return SFXPickupHealthPrefab.gameObject;
            case SFXType.PickupBox: return SFXPickupBoxPrefab.gameObject;
            case SFXType.PickupTurn: return SFXPickupTurnPrefab.gameObject;
            case SFXType.WeaponDash: return SFXWeaponDashPrefab.gameObject;
            case SFXType.ExplosionFireDash: return SFXExplosionFireDash.gameObject;
            default: return null;
        }
    }

    public SFXInterface GetSFX(SFXType type, Vector2 spawnAtPosition)
    {
        GameObject instance = Object.Instantiate(GetPrefabForSFX(type), spawnAtPosition, Quaternion.identity);
        SFXInterface sfxController = instance.GetComponent<SFXInterface>();
        sfxController.transform.SetParent(transform);
        return sfxController;
    }
}
