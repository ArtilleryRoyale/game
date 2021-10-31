public class RoundControllerTrailer01 : RoundControllerTrailerBase
{
    protected override string TrainingName => "Trailer 01";

    public void ForceUpdateWind(float windForce)
    {
        UpdateWind(windForce);
    }
}
