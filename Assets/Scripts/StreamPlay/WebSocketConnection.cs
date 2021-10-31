using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class WebSocketConnection : MonoBehaviour
{
    public string ServerAddress => Config.ServerAddress;

    public WebSocket WebSocket { get; protected set; }
    public bool IsWebsocketReady => WebSocket != null && WebSocket.State == WebSocketState.Open;

    // State
    public string GameId { get; private set; }
    public string UserId { get; private set; }
    public bool IsOwner { get; private set; }

    public void SetConnectionInfo(string gameId, string userId, bool isOwner)
    {
        GameId = gameId;
        UserId = userId;
        IsOwner = isOwner;
    }

    public void SetOwner(bool owner)
    {
        // Log.Message("WebSocketConnection", "SetOwner: " + owner);
        IsOwner = owner;
    }

    public void ConnectionInit(string path)
    {
        WebSocket = new WebSocket("ws://" + ServerAddress + path);
        WebSocket.OnOpen += () => WebSocketOnOpen();
        WebSocket.OnError += (e) => WebSocketOnError(e);
        WebSocket.OnClose += (e) => WebSocketOnClose(e);
    }

    /// <summary>
    /// Start the connection to the server
    /// Note: this is blocking (awaiting forever)
    /// </summary>
    public async Task ConnectionStart()
    {
        // Log.Message("WebSocketConnection", "Start connection");
#if CC_EXTRA_CARE
try {
#endif
        await WebSocket.Connect(); // awaiting forever / Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    /// <summary>
    /// Stop the connection from the server
    /// Note: this will also distroy the socket
    /// </summary>
    public async Task ConnectionDestroy()
    {
#if CC_EXTRA_CARE
try {
#endif
        await WebSocketClose(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    protected async UniTask OnApplicationQuit()
    {
#if CC_EXTRA_CARE
try {
#endif
        await ConnectionDestroy(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    /// <summary>
    /// Important, this must be called by child classes
    /// </summary>
    protected virtual void FixedUpdate()
    {
        if (WebSocket == null) return;
#if !UNITY_WEBGL || UNITY_EDITOR
        // This is to receive message and will go to OnMessage delegate
        WebSocket.DispatchMessageQueue();
#endif
    }

    #region WebSocket

    protected async Task WebSocketClose()
    {
        if (WebSocket == null) return;
#if CC_EXTRA_CARE
try {
#endif
        await WebSocket.Close(); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        WebSocket = null;
    }

    protected void WebSocketOnOpen() { }

    protected void WebSocketOnError(string errorMessage)
    {
        Log.Critical("WebSocketConnection", "WebSocket error: " + errorMessage);
    }

    protected void WebSocketOnClose(WebSocketCloseCode closeCode)
    {
        // Log.Message("WebSocketConnection", "WebSocket closed: " + closeCode.ToString());
    }

    protected void WebSocketOnMessage(byte[] bytes)
    {
        // Log.Message("WebSocketConnection", "WebSocket message size: " + bytes.Length);
    }

    #endregion
}
