using Jrmgx.Helpers;
using UnityEngine;

// TODO prio 3 rename to SFXSmokeDetachableController
public class SFXWeaponBazookaController : SFXSpawnAndFollowController
{
    [SerializeField] private ParticleSystem smokeParticleSystem = default;

    public void DetachAndStop(float inSeconds)
    {
        base.Detach();
        smokeParticleSystem.Stop();
        this.ExecuteInSecond(inSeconds, EndSfx);
    }
}
