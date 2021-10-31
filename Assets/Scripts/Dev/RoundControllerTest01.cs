using CC;

public class RoundControllerTest01 : RoundControllerTrailerBase
{
    protected override string TrainingName => "Test 01";

    protected override void RetreatRound()
    {
        // We use the actual base implementation here as in test mode we want retreat time
        // Log.Message("RoundController", "Retreat round called");
        CameraManager.Instance.FollowPointerRelease();
        StopAndClearTimers();
        retreatTimer = new Timer(RetreatRoundTimer(GameManager.Instance.CurrentGameOption.TimeRetreat));
        retreatTimer.Start();
    }
}
