using UnityEngine;
using System.Text;
using Cysharp.Threading.Tasks;
using CC;
using Jrmgx.Helpers;
using System.Collections.Generic;
using System;

public class GamePanelUIController : MenuControllerBase
{
    #region Fields

    [Header("References")]
    [SerializeField] private NetworkPopupController networkSharePopup = default;
    [SerializeField] private FocusableButtonUIController startButton = default;

    [Header("Options")]
    [SerializeField] private MultiChoiceEntryUIController mineCountChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController withWindChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController boxCountChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController lavaRiseRoundCountChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController timeRoundChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController positionPlayerOneChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController positionPlayerTwoChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController whoStartChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController challengerChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController AILevelChoiceEntry = default;

    [Header("Map Style")]
    [SerializeField] private List<MapButtonUIController> mapButtons = default;

    private GameOption option => menuManager.CurrentGameOption;

    #endregion

    protected void Awake()
    {
        foreach (var mapButton in mapButtons) {
            mapButton.ActualButton.onClick.AddListener(() => MapButtonAction(mapButton));
        }
    }

    protected void Start()
    {
        MapButtonAction(mapButtons[0]);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // Load current game option into UI
        LoadOptions();

        AILevelChoiceEntry.gameObject.SetActive(false);//option.PlayingAgainstAI);
        whoStartChoiceEntry.gameObject.SetActive(!option.PlayingCaptureTheKing);
        challengerChoiceEntry.gameObject.SetActive(option.PlayingCaptureTheKing);
        positionPlayerOneChoiceEntry.gameObject.SetActive(!option.PlayingCaptureTheKing);
        positionPlayerTwoChoiceEntry.gameObject.SetActive(!option.PlayingCaptureTheKing);
    }

    protected void LoadOptions()
    {
        mineCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(option.MineCount));
        withWindChoiceEntry.SetValue(option.WithWind ? "Yes" : "No");
        boxCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(option.BoxCount));
        lavaRiseRoundCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(option.LavaRiseRoundCount, "Immediately", "Never"));
        timeRoundChoiceEntry.SetValue(GameOption.ToStringAndSpecific(option.TimeRound));
        positionPlayerOneChoiceEntry.SetValue(PositioningConstraints.TypeToText(option.PlayerOnePositioningConstraints));
        positionPlayerTwoChoiceEntry.SetValue(PositioningConstraints.TypeToText(option.PlayerTwoPositioningConstraints));
        AILevelChoiceEntry.SetValue(option.AILevel.ToString());
        challengerChoiceEntry.SetValue(option.StartWithOrChallenger.ToString().Replace("_", " "));
        whoStartChoiceEntry.SetValue(option.StartWithOrChallenger.ToString().Replace("_", " "));
    }

    protected void ApplyOptions()
    {
        option.MineCount = GameOption.ParseIntAndSpecific(mineCountChoiceEntry.GetValue());
        option.WithWind = withWindChoiceEntry.GetValue() == "Yes";
        option.BoxCount = GameOption.ParseIntAndSpecific(boxCountChoiceEntry.GetValue());
        option.LavaRiseRoundCount = GameOption.ParseIntAndSpecific(lavaRiseRoundCountChoiceEntry.GetValue());
        option.TimeRound = GameOption.ParseIntAndSpecific(timeRoundChoiceEntry.GetValue());
        option.PlayerOnePositioningConstraints = PositioningConstraints.TextToType(positionPlayerOneChoiceEntry.GetValue());
        option.PlayerTwoPositioningConstraints = PositioningConstraints.TextToType(positionPlayerTwoChoiceEntry.GetValue());
        option.AILevel = (GameOption.AILevelEnum) Enum.Parse(typeof(GameOption.AILevelEnum), AILevelChoiceEntry.GetValue());
        option.StartWithOrChallenger = (GameOption.TeamEnum) Enum.Parse(typeof(GameOption.TeamEnum),
            (option.PlayingCaptureTheKing ? challengerChoiceEntry : whoStartChoiceEntry).GetValue().Replace(" ", "_")
        );
    }

    #region Actions

    protected void MapButtonAction(MapButtonUIController currentButton)
    {
        foreach (var mapButton in mapButtons) {
            mapButton.SetSelected(false);
        }
        currentButton.SetSelected(true);
        option.MapStyle = currentButton.Style;
    }

    /// <summary>
    /// Save current game option from UI (this is called in each MultiChoiceEntryUIController changes)
    /// </summary>
    public void UpdateOptionAction()
    {
        ApplyOptions();
    }

    public async void StartGameAction() // Action from button, not using UniTask here
    {
        menuManager.PlaySoundClick();
        if (NetworkManagerInstance.IsNetwork) {
#if CC_EXTRA_CARE
try {
#endif
            try {
                await CreateGameNetwork().CancelOnDestroy(this);
            } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        } else {
            StartGame();
        }
    }

    public async void CloseLoadingAction() // Action from button, not using UniTask here
    {
        // Log.Message("GamePanelUI", "CloseLoading");
#if CC_EXTRA_CARE
try {
#endif
        try {
            await NetworkManagerInstance.StreamConnection.ConnectionDestroy(); // Not cancelable .CancelOnDestroy(this)
        } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        networkSharePopup.Hide();
    }

    private async UniTask CreateGameNetwork()
    {
        var userId = User.GetFullIdentifier();
        var gameId = NetworkObject.NetworkIdentifierHuman();

        // Log.Message("NetworkManager", "Configure Create Game");
        NetworkManagerInstance.GameId = gameId;
        NetworkManagerInstance.UserId = userId;
        NetworkManagerInstance.UsernamePlayerOne = User.GetUsername();

#if CC_EXTRA_CARE
try {
#endif
        await NetworkManagerInstance.StreamConnection.ConnectionDestroy(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

        NetworkManagerInstance.ConnectionGame();
        NetworkManagerInstance.StreamConnection.WebSocket.OnMessage += WebSocketOnMessage;

        networkSharePopup.Show(gameId);

        // await infinite
#if CC_EXTRA_CARE
try {
#endif
        await NetworkManagerInstance.StreamConnection.ConnectionStart(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    public void BackAction()
    {
        menuManager.ShowMultiPanel();
    }

    private async UniTask GotConfirmationFromGuest(string name)
    {
        // Log.Message("GamePanelUI", "GotConfirmationFromGuest");
        networkSharePopup.Hide();
        NetworkManagerInstance.UsernamePlayerTwo = name.Split('@')[0]; // TODO prio 2 make a method (on player entity? + replace on the UI @ with #)
#if CC_EXTRA_CARE
try {
#endif
        await NetworkManagerInstance.StreamConnection.WebSocket.SendText(NetworkManagerInstance.UserId); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        NetworkManagerInstance.StreamConnection.WebSocket.OnMessage -= WebSocketOnMessage;

        PopupManager.Hide(); // Force here because of the upcoming await
        StartGame();
    }

    #endregion

    protected async void WebSocketOnMessage(byte[] bytes)
    {
        string message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        //Log.Message("GamePanelUI", "Message received: " + message);
        await GotConfirmationFromGuest(message); // TODO prio 2 make it more robust
    }
}
