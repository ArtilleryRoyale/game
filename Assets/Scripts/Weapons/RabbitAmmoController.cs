using UnityEngine;
using Jrmgx.Helpers;
using CC;

public class RabbitAmmoController : AmmoControllerBase
{
    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        base.Init(characterManager, weapon, angle, isFacingRight, power);
        ScheduleToDestroy(2);
    }
}
