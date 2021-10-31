using UnityEngine;
using System.Text;
using CC;

public class MultiPanelUIController : MenuControllerBase
{
    #region Fields

    [Header("Popups")]
    [SerializeField] private UsernamePopupController usernamePopup = default;
    [SerializeField] private NetworkPopupController networkJoinPopup = default;

    [Header("References")]
    [SerializeField] private MainButtonUIController localButton = default;
    [SerializeField] private MainButtonUIController onlineButton = default;
    [SerializeField] private FocusableButtonUIController discordButton = default;

    #endregion

    #region Actions

    public void LocalAction()
    {
        // Configure Game
        menuManager.CurrentGameOption.PlayingAgainstAI = false;
        menuManager.CurrentGameOption.AllWeaponsNoLimits = false;

        // Configure Network
        // Log.Message("NetworkManager", "Configure Local");
        NetworkManagerInstance.IsNetwork = false;
        NetworkManagerInstance.IsNetworkOwner = true;

        menuManager.PlaySoundClick();
        menuManager.ShowModePanel();
    }

    public void InternetAction()
    {
        menuManager.PlaySoundClick();
        if (!User.HasUsername()) {
            usernamePopup.Show(then: InternetAction);
            return;
        }

        // TODO prio 5 at some point this should be a new screen
        PopupManager.Init(
            "Network Game",
            "Do you want to create a new game\n" +
            "or join an existing one?",
            "Create", CreateGameAction,
            "Join", JoinGameAction
        ).Show();
    }

    private void CreateGameAction()
    {
        // Configure Game
        menuManager.CurrentGameOption.PlayingAgainstAI = false;
        menuManager.CurrentGameOption.AllWeaponsNoLimits = false;

        // Configure Network
        // Log.Message("NetworkManager", "Configure Create Game");
        NetworkManagerInstance.IsNetwork = true;
        NetworkManagerInstance.IsNetworkOwner = true;

        menuManager.ShowModePanel();
    }

    private void JoinGameAction()
    {
        networkJoinPopup.Show(then: async (gameId) => {

            var userId = User.GetFullIdentifier();

            // Configure Game
            menuManager.CurrentGameOption.PlayingAgainstAI = false;
            menuManager.CurrentGameOption.AllWeaponsNoLimits = false;

            // Configure Network
            // Log.Message("NetworkManager", "Configure Join Game");
            NetworkManagerInstance.IsNetwork = true;
            NetworkManagerInstance.IsNetworkOwner = false;
            NetworkManagerInstance.GameId = gameId;
            NetworkManagerInstance.UserId = userId;
            NetworkManagerInstance.UsernamePlayerTwo = User.GetUsername();

#if CC_EXTRA_CARE
try {
#endif
            await NetworkManagerInstance.StreamConnection.ConnectionDestroy(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            NetworkManagerInstance.ConnectionGame();
            NetworkManagerInstance.StreamConnection.WebSocket.OnMessage += WebSocketOnMessage;

            menuManager.ShowLoadingGamePanel(canClose: true);

#if CC_EXTRA_CARE
try {
#endif
            // await infinite
            await NetworkManagerInstance.StreamConnection.ConnectionStart(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        });
    }

    public void discordAction()
    {
        menuManager.PlaySoundClick();
        Application.OpenURL("https://artilleryroyale.com/discord");
    }

    public void BackAction()
    {
        menuManager.PlaySoundClick();
        menuManager.ShowMainPanel();
    }

    private void GotConfirmationFromOwner(string name)
    {
        // Log.Message("MultiPanelUI", "GotConfirmationFromOwner");
        NetworkManagerInstance.UsernamePlayerOne = name.Split('@')[0]; // TODO prio 2
        NetworkManagerInstance.StreamConnection.WebSocket.OnMessage -= WebSocketOnMessage;
        StartGame();
    }

    #endregion

    protected void WebSocketOnMessage(byte[] bytes)
    {
        string message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        // Log.Message("MultiPanelUI", "Message received: " + message);
        GotConfirmationFromOwner(message);
    }
}
