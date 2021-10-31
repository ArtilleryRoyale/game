using CC;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class OptionPanelUIController : MenuControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private UsernamePopupController usernamePopup = default;
    [SerializeField] private MultiChoiceEntryUIController musicVolumeMultiChoice = default;
    [SerializeField] private MultiChoiceEntryUIController sfxVolumeMultiChoice = default;
    [SerializeField] private StudioEventEmitter soundEventTestSound = default;
    private Bus busMusic = default;
    private Bus busSoundEffects = default;

    #endregion

    protected void Awake()
    {
        busMusic = RuntimeManager.GetBus(Config.BusMusic);
        busSoundEffects = RuntimeManager.GetBus(Config.BusSoundEffects);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        musicVolumeMultiChoice.SetValue(User.GetMusicVolume());
        sfxVolumeMultiChoice.SetValue(User.GetSfxVolume());
    }

    #region Actions

    public void ChangeUsernameAction()
    {
        usernamePopup.Show(() => {});
    }

    public void MusicVolumeAction(string intAsString)
    {
        int intValue = int.Parse(intAsString);
        busMusic.setVolume(intValue / 100f);
        User.SetMusicVolume(intValue);
    }

    public void SfxVolumeAction(string intAsString)
    {
        int intValue = int.Parse(intAsString);
        busSoundEffects.setVolume(intValue / 100f);
        User.SetSfxVolume(intValue);
        soundEventTestSound.Play();
    }

    public void BackAction()
    {
        menuManager.ShowMainPanel();
    }

    #endregion
}
