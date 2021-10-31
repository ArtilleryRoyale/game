using UnityEngine;

public class SoloPanelUIController : MenuControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private MainButtonUIController trainingButton = default;
    [SerializeField] private MainButtonUIController missionButton = default;

    #endregion

    #region Actions

    public void TrainingAction()
    {
        // Configure Game
        menuManager.CurrentGameOption.PlayingCaptureTheKing = false;
        menuManager.CurrentGameOption.PlayingAgainstAI = true;
        menuManager.CurrentGameOption.AllWeaponsNoLimits = true;
        menuManager.CurrentGameOption.AILevel = GameOption.AILevelEnum.Dummy;

        // Configure Network
        // Log.Message("NetworkManager", "Configure Local");
        NetworkManagerInstance.IsNetwork = false;
        NetworkManagerInstance.IsNetworkOwner = true;

        menuManager.PlaySoundClick();
        menuManager.ShowGamePanel();
        menuManager.SaveMenuPath("Solo Panel");
    }

    public void MissionsAction()
    {
        // PopupManager.Init(
        //     "Work in progress",
        //     "Missions are not available yet!",
        //     "Soon!"
        // ).Show();
        // Configure Game
        menuManager.CurrentGameOption.PlayingCaptureTheKing = false;
        menuManager.CurrentGameOption.PlayingAgainstAI = true;
        menuManager.CurrentGameOption.AllWeaponsNoLimits = false;
        menuManager.CurrentGameOption.AILevel = GameOption.AILevelEnum.Max;

        // Configure Network
        // Log.Message("NetworkManager", "Configure Local");
        NetworkManagerInstance.IsNetwork = false;
        NetworkManagerInstance.IsNetworkOwner = true;

        menuManager.PlaySoundClick();
        menuManager.ShowGamePanel();
        menuManager.SaveMenuPath("Solo Panel");
    }

    public void BackAction()
    {
        menuManager.ShowMainPanel();
    }

    #endregion
}
