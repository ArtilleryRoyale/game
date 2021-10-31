using CC.StreamPlay;
using Cysharp.Threading.Tasks;

public interface RoundControllerInterface : NetworkObjectInterface, RoundLockIdentifier
{
    MapController MapController { get; set; }
    PlayerController CurrentPlayerController { get; }
    CharacterManager CurrentCharacterManager { get; }

    bool CurrentPlayerRedTeam { get; set; }
    float WindForce { get; }

    UniTask ReadyForStart();
    UniTask CalculateDamages(bool invert);
    void AskForSkipTurn();
    void StopGame();

    void RainbowStart(RainbowAmmoController rainbowAmmoController);
}
