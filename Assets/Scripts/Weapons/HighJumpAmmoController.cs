using Jrmgx.Helpers;

/// <summary>
/// This is a "fake" weapon as it will only change the state on CharacterManager to allow for a special high jump
/// </summary>
public class HighJumpAmmoController : AmmoControllerBase
{
    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        // Not calling base.Init to prevent GameManager.Instance.RoundLock.LockChainReaction(this)
        // base.Init(weapon, angle, isFacingRight, power);
        characterManager.HighJumpWeapon();
    }

    protected override void Start()
    {
        base.Start();
        if (!IsNetworkOwner) return;
        UnlockAndDestroy();
    }

    protected override void FixedUpdate()
    {
        // Not calling base.FixedUpdate() as we don't need it
    }
}
