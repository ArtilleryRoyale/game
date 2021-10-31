using UnityEngine;

public class DashWeaponController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private SFXSimpleSpawnController dashFire = default;

    #endregion

    #region Display Logic

    public void StartShow()
    {
        var main = dashFire.ParticleSystem.main;
        main.loop = true;
        dashFire.Init();
    }

    public void StopShow()
    {
        dashFire.DeInit();
    }

    #endregion
}
