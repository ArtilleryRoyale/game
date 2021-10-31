using CC;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Jrmgx.Helpers;
using UnityEngine;

public class VoiceManager : MonoBehaviour
{
    #region Fields

    public static VoiceManager Instance;

    public const int PRIORITY_LOW = 10;
    public const int PRIORITY_MEDIUM = 100;
    public const int PRIORITY_HIGH = 1000;

    public const string PHRASE_GOT_YOU = "!!";
    public const string PHRASE_MISSED_ME = "...";
    public const string PHRASE_FFA = "?!";
    public const string PHRASE_DROPABLE = "?";
    public const string PHRASE_DEAD = "x";
    public const string PHRASE_THINK = "..?";

    // State
    private bool nothingThisTurn;
    private int currentPriority = int.MinValue;
    private CharacterManager.Phrases currentPhrase = CharacterManager.Phrases.Nothing;
    private CharacterManager currentCharacterManagerReference;
    private string currentText;

    #endregion

    #region Init

    protected void Awake()
    {
        Instance = this;
    }

    #endregion

    #region Phrase Intentions

    /// <summary>
    /// Add a phrase to the list of potential phrases for this turn
    /// </summary>
    public void WantToSay(int priority, CharacterManager.Phrases phrase, CharacterManager characterManager, string text)
    {
        // Log.Message("VoiceManager", characterManager.name + " want to say: " + text);
        if (priority > currentPriority) {
            // Log.Message("VoiceManager", characterManager.name + " has highest priority with: " + text);
            currentPriority = priority;
            currentPhrase = phrase;
            currentCharacterManagerReference = characterManager;
            currentText = text;
        }
    }

    public void CancelSay(CharacterManager characterManager)
    {
        // Log.Message("VoiceManager", "Cancel Say for " + characterManager.name);
        if (currentCharacterManagerReference != characterManager) return;
        ResetState();
    }

    /// <summary>
    /// Say the best phrase for this turn (if any)
    /// </summary>
    public async UniTask SayBestPhrase()
    {
        // Wait a bit because this is said while showing damage and it's too many things at the same time
        await UniTask.Delay(Config.DelayPreVoice, cancellationToken: this.GetCancellationTokenOnDestroy());

        if (!nothingThisTurn && currentCharacterManagerReference != null) {
            string text = currentText;
#if CC_DEBUG
            switch (currentText) {
                case PHRASE_GOT_YOU: text = "PHRASE_GOT_YOU"; break;
                case PHRASE_MISSED_ME: text = "PHRASE_MISSED_ME"; break;
                case PHRASE_FFA: text = "PHRASE_FFA"; break;
                case PHRASE_DROPABLE: text = "PHRASE_DROPABLE"; break;
                case PHRASE_DEAD: text = "PHRASE_DEAD"; break;
            }
#endif
            // Log.Message("VoiceManager", "Best phrase for this turn " + currentCharacterManagerReference.name + " said: " + text);
#if CC_EXTRA_CARE
try {
#endif
            currentCharacterManagerReference.NetworkRecordSnapshot(MethodIdentifier.Method_SayNow, (int) currentPhrase, currentText);
            await currentCharacterManagerReference.SayNow((int) currentPhrase, currentText);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        } else {
            // Log.Message("VoiceManager", "Nothing to say for this turn...");
        }
        ResetState();
    }

    /// <summary>
    /// Prevent anything to be said this turn
    /// </summary>
    public void SayNothingThisTurn()
    {
        // Log.Message("VoiceManager", "Prevent anything to be said this turn");
        nothingThisTurn = true;
    }

    public void ResetState()
    {
        // Log.Message("VoiceManager", "ResetState");
        nothingThisTurn = false;
        currentPriority = int.MinValue;
        currentPhrase = CharacterManager.Phrases.Nothing;
        currentCharacterManagerReference = null;
        currentText = null;
    }

    #endregion

    #region General Helper

    public static void InitSoundSystem()
    {
        RuntimeManager.GetBus(Config.BusMusic).setVolume(User.GetMusicVolume() / 100f);
        RuntimeManager.GetBus(Config.BusSoundEffects).setVolume(User.GetSfxVolume() / 100f);
    }

    #endregion
}

public static class StudioEventEmitterExtension
{
    public static UniTask WaitUntilPlayed(this StudioEventEmitter soundEvent)
    {
        soundEvent.Play();
        return UniTask
            .WaitWhile(soundEvent.IsPlaying)
            .AttachExternalCancellation(Basics.TimeoutTask(Config.DelayVoiceBubble * 3));
    }
}
