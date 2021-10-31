using UnityEngine;

public class BeamAmmoController : AmmoControllerBase
{
    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);
    }

    protected override void Start()
    {
        base.Start();
        if (!IsNetworkOwner) return;
        UnlockAndDestroy();
    }
}
