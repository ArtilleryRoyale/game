using System;

/// <summary>
/// Serializable reference type like this one
/// are meant to be injected in an existing object instance in the guest of the same type
/// </summary>
[Serializable]
public class SzGameOptionRef
{
    public int TimeRound;
    public int TimeRetreat;
    public int TimeInterRound;
    public int PlatformCount;
    public int DestructibleObjectCount;
    public int MineCount;
    public bool MapDestructible;
    public bool WithWind;
    public bool WithFloor;
    public int BoxCount;
    public int BoxOneOutOfByRound;
    public int LavaRiseRoundCount;
    public int MapStyle;
    public bool PlayingCaptureTheKing;
    public int StartWithOrChallenger;

    public SzGameOptionRef() { }

    public SzGameOptionRef(GameOption o)
    {
        TimeRound = o.TimeRound;
        TimeRetreat = o.TimeRetreat;
        TimeInterRound = o.TimeInterRound;
        PlatformCount = o.PlatformCount;
        DestructibleObjectCount = o.DestructibleObjectCount;
        MineCount = o.MineCount;
        MapDestructible = o.MapDestructible;
        WithWind = o.WithWind;
        WithFloor = o.WithFloor;
        BoxCount = o.BoxCount;
        BoxOneOutOfByRound = o.BoxOneOutOfByRound;
        LavaRiseRoundCount = o.LavaRiseRoundCount;
        MapStyle = (int) o.MapStyle;
        PlayingCaptureTheKing = o.PlayingCaptureTheKing;
        StartWithOrChallenger = (int) o.StartWithOrChallenger;
    }

    public void InjectIn(GameOption current)
    {
        current.TimeRound = TimeRound;
        current.TimeRetreat = TimeRetreat;
        current.TimeInterRound = TimeInterRound;
        current.PlatformCount = PlatformCount;
        current.DestructibleObjectCount = DestructibleObjectCount;
        current.MineCount = MineCount;
        current.MapDestructible = MapDestructible;
        current.WithWind = WithWind;
        current.WithFloor = WithFloor;
        current.BoxCount = BoxCount;
        current.BoxOneOutOfByRound = BoxOneOutOfByRound;
        current.LavaRiseRoundCount = LavaRiseRoundCount;
        current.MapStyle = (MapController.MapStyle) MapStyle;
        current.PlayingCaptureTheKing = PlayingCaptureTheKing;
        current.StartWithOrChallenger = (GameOption.TeamEnum) StartWithOrChallenger;
    }
}

public static class SzGameOptionRefExtension
{
    public static SzGameOptionRef SzValue(this GameOption o)
    {
        return new SzGameOptionRef(o);
    }
}
