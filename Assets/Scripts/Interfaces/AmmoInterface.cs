using UnityEngine;
using CC.StreamPlay;

public interface AmmoInterface : NetworkObjectInterface
{
    void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power);
    bool IsAISimulationActive { get; set; }

    // trick
    Transform transform { get; }
}
