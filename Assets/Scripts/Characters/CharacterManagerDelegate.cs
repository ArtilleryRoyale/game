public interface CharacterManagerDelegate
{
    void CharacterStartedLoadFire();
    void CharacterAskRetreat(bool hasRetreat);
    void CharacterAskStopRound();
    void CharacterFall();
}
