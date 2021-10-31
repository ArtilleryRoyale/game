using System;
using UnityEngine;

public class GameOption : MonoBehaviour
{
    public const string RESOURCE_NAME_DEV = "GameOptionDev";
    public enum AILevelEnum { Dummy = 0, /* Bad = 100, Average = 500, Good = 700, */ Max = 1000 };
    public enum TeamEnum { Red_Team = 0, Blue_Team = 1, Random = 2 };

    [Header("Timers")]
    public int TimeRound = 30;
    public int TimeRetreat = 5;
    public int TimeInterRound = 2;

    [Header("Map")]
    [Range(0, 10)] public int PlatformCount = 4;
    [Range(0, 10)] public int DestructibleObjectCount = 3;
    [Range(0, 10)] public int MineCount = 4;
    // TODO prio 3 when false prevent/hide explosion debrit
    public bool MapDestructible = true;
    public bool WithWind = true;
    public bool WithFloor = true;
    public MapController.MapStyle MapStyle = MapController.MapStyle.Jungle;

    [Header("Positioning")]
    public PositioningConstraints.Type PlayerOnePositioningConstraints = PositioningConstraints.Type.Default;
    public PositioningConstraints.Type PlayerTwoPositioningConstraints = PositioningConstraints.Type.Default;

    [Header("Box")]
    public int BoxCount = 5;
    [Tooltip("One chance out of VALUE to have a new box spawned")]
    [Range(0, 10)] public int BoxOneOutOfByRound = 3;
    public bool ShowBoxContent = false;

    [Header("Gameplay")]
    public TeamEnum StartWithOrChallenger = TeamEnum.Random;
    public AILevelEnum AILevel = AILevelEnum.Dummy;

    [Header("Sudden Death")]
    public int LavaRiseRoundCount = 30;

    [Header("Internals")]
    public bool PlayingAgainstAI = false;
    public bool PlayingCaptureTheKing = false;
    public bool AllWeaponsNoLimits = false;
    public bool IsTraining = false;
    public bool IsMission = false;
    public bool IsTesting = false;
    public bool IsTrailer = false;

    private void Awake()
    {
        // Log.Message("InitDance", "GameOption Awake()");
        if (!IsTraining || !IsMission || !IsTesting) {
            DontDestroyOnLoad(this);
        }
    }

    private void OnDestroy()
    {
        // Log.Message("InitDance", "GameOption Destroyed()");
    }

    #region Helpers

    /// <summary>
    /// Given a string that represent an int, return this int
    /// This will also parse english values representing specific int
    /// </summary>
    public static int ParseIntAndSpecific(string value)
    {
        value = value.ToLower();
        try {
            switch (value) {
                case "none": return 0;
                case "immediately": return 0;
                case "never": return int.MaxValue;
                case "infinite": return int.MaxValue;
                default: return int.Parse(value);
            }
        } catch (Exception e) {
            Log.Critical("GameOption", "ParseIntAndSpecific trying to parse: " + value);
#if CC_DEBUG
            throw e;
#else
            return 10; // Some intermediate value that could do the job
#endif
        }
    }

    /// <summary>
    /// Given an int, return a human representation of it
    /// This will also convert values to some english representation of it
    /// </summary>
    public static string ToStringAndSpecific(int value, string minText = "None", string maxText = "Infinite")
    {
        if (value == 0) return minText;
        if (value == int.MaxValue) return maxText;
        return "" + value;
    }

    #endregion
}
