using CC;
using UnityEngine;

public class ModePanelUIController : MenuControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private BasicPopupController captureTheKingRulesPopup = default;

    #endregion

    #region Actions

    public void CaptureTheKingAction()
    {
        menuManager.PlaySoundClick();

        // Configure Game
        menuManager.CurrentGameOption.PlayingCaptureTheKing = true;
        menuManager.CurrentGameOption.StartWithOrChallenger = GameOption.TeamEnum.Random;

        if (User.HasSeenPopup_CTKRules()) {
            menuManager.ShowGamePanel();
        } else {
            captureTheKingRulesPopup.Show(menuManager.ShowGamePanel);
        }
    }

    public void DeathMatchAction()
    {
        menuManager.PlaySoundClick();

        // Configure Game
        menuManager.CurrentGameOption.PlayingCaptureTheKing = false;
        menuManager.CurrentGameOption.StartWithOrChallenger = GameOption.TeamEnum.Random;
        menuManager.CurrentGameOption.PlayerOnePositioningConstraints = PositioningConstraints.Type.Default;
        menuManager.CurrentGameOption.PlayerTwoPositioningConstraints = PositioningConstraints.Type.Default;
        menuManager.ShowGamePanel();
    }

    public void BackAction()
    {
        menuManager.PlaySoundClick();
        menuManager.ShowMultiPanel();
    }

    #endregion
}
