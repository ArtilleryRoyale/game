using CC;
using TMPro;
using UnityEngine;

public class MainPanelUIController : MenuControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private TextMeshProUGUI soloProgressText = default;
    [SerializeField] private TextMeshProUGUI multiLockedText = default;
    [SerializeField] private MainButtonUIController soloButton = default;
    [SerializeField] private MainButtonUIController multiButton = default;
    [SerializeField] private FocusableButtonUIController settingsButton = default;
    [SerializeField] private FocusableButtonUIController helpButton = default;

    // State
    private bool isUp;

    #endregion

    protected override void OnEnable()
    {
        base.OnEnable();
        soloProgressText.text = "";
        multiLockedText.text = "";
    }

    #region Actions

    public void SoloAction()
    {
        menuManager.PlaySoundClick();
        menuManager.ShowSoloPanel();
    }

    public void MultiAction()
    {
        menuManager.PlaySoundClick();
        menuManager.ShowMultiPanel();
    }

    public void OptionAction()
    {
        menuManager.PlaySoundClick();
        menuManager.ShowOptionsPanel();
    }

    public void HelpAction()
    {
        menuManager.PlaySoundClick();
        menuManager.ShowHelpPanel();
    }

    public void QuitAction()
    {
        menuManager.QuitGame();
    }

    #endregion
}
