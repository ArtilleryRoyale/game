using Cysharp.Threading.Tasks;
using UnityEngine;

public class Config : MonoBehaviour
{
#if UNITY_EDITOR
    // Debug values
    public const bool WithMusic = true;
#else
    public const bool WithMusic = true;
#endif

    // Game constants
    public const float IsSeenAsGroundedPercent = 1.1f;
    public const float RaycastGroundDistance = 0.5f;
    public const float CalculateDamageInSeconds = 0.33f;
    public const float MapOutlineInPercent = 0.85f;
    public const float FallDistance = 15f;
    public const int SignalCharacterTime = 11;
    public const int RestTickNeeded = 30;
    public const float RestMaxMagnitude = 0.05f;
    public const float GlobalForceFactor = 6f;
    public const float CharacterScale = 1.15f;

    // Server
    //public const string ServerAddress = "192.168.1.10:8080";
    public const string ServerAddress = "game.artilleryroyale.com";
    public const string PlayerOneName = "Red";
    public const string PlayerTwoName = "Blue";

    // Time
    public const int DelayPreVoice = 500;
    public const int DelayVoiceBubble = 1000;
    public const int DelayBubbleOnly = 2000;
    public const int DelayDead = 2000;
    public const int DelayDamagesNewCalculus = 2000;
    public const int DelayBox = 3000;
    public const int DelayEndRagdoll = 1000;

    // Stream Play
    public const float THROTTLE_SEND = 0.1f;

    // Colors
    public static readonly Color MapOutlineColor = new Color(1, 1, 1, 0.25f);
    public static readonly Color ColorPlayerOne = Color.red;
    public static readonly Color ColorPlayerTwo = Color.blue;
    public static readonly Color ColorExplosionBorder = new Color(0x88 / 256f, 0x44 / 256f, 0x00 / 256f);

    // Tags are used for game logic mechanism (is: bounds detection, material type for sound, etc.)
    public const string TagUntagged = "Untagged";
    // Used for out of bounds detection
    public const string TagBounds = "Bounds";
    public const string TagBoundLava = "Bound Lava";
    // Used to know which sound/particle to play (regarding material type)
    // There is 2 step sounds (type ground=grass, other type=concrete)
    // and 3 destruct sounds (type ground=ground, type Platform=platform, type Statue=statue)
    public const string TagTypePlatform = "Type Platform";
    public const string TagTypeStatue = "Type Statue";
    public const string TagTypeGround = "Type Ground";
    // Used for positioning
    public const string TagPositioning = "Positioning";
    // Used to handle sounds/voices events
    public const string TagSoundCharacter = "Sound Character";

    // Sorting layers
    public const string SortingLayerCharacter = "Character";
    public const string SortingLayerCharacterBackground = "CharacterBackground";

    // FMOD
    public const string BusMusic = "bus:/Music";
    public const string BusSoundEffects = "bus:/SoundEffects";

    private void Awake()
    {
        UniTaskScheduler.UnobservedExceptionWriteLogType = UnityEngine.LogType.Log;
        Application.targetFrameRate = 60;
#if UNITY_EDITOR
        UnityEditor.CrashReporting.CrashReportingSettings.captureEditorExceptions = false;
#endif
    }
}
