using UnityEngine;
using CC.StreamPlay;

public class NetworkManager : MonoBehaviour
{
    #region Fields

    public const string RESOURCE_NAME_DEV = "NetworkManagerDev";
    public const int VERSION = 2;

    public static NetworkManager Instance;

    public bool IsNetwork = false;
    public bool IsNetworkOwner = true;
    public string GameId = "Local Game";
    public string UserId = "1";
    public string UsernamePlayerOne { get; set; } = Config.PlayerOneName;
    public string UsernamePlayerTwo { get; set; } = Config.PlayerTwoName;

    public Player StreamPlayPlayer { get; private set; }
    public Recorder StreamPlayRecorder { get; private set; }
    public WebSocketConnection StreamConnection { get; private set; }
    public float HardLatency = 2f;
    #endregion

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        // Log.Message("NetworkManager", "Awake() + DontDestroy ON");
        DontDestroyOnLoad(this);

        StreamPlayPlayer = gameObject.GetComponent<Player>();
        StreamPlayRecorder = gameObject.GetComponent<Recorder>();
        StreamConnection = gameObject.GetComponent<WebSocketConnection>();
    }

    public void ConnectionGame()
    {
        // Log.Message("NetworkManager", "ConnectionGame");
        StreamConnection.SetConnectionInfo(GameId, UserId, IsNetworkOwner);
        StreamConnection.ConnectionInit("/" + VERSION + "/game/" + GameId + "/" + UserId);
    }

    private void OnDestroy()
    {
        // Log.Message("InitDance", "NetworkManager Destroyed()");
    }
}
