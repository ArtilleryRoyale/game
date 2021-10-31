using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class MenuControllerBase : MonoBehaviour
{
    [SerializeField] protected MenuManager menuManager = default;
    [SerializeField] protected FocusableButtonUIController backButton = default;

    public NetworkManager NetworkManagerInstance => NetworkManager.Instance;

    protected virtual void OnEnable()
    {
        // Log.Message("MenuController", "OnEnable " + name);
        Cursor.visible = true;
    }

    protected virtual void OnDisable()
    {
        // Log.Message("MenuController", "OnDisable " + name);
    }

    protected void StartGame()
    {
        // Log.Message("MenuController", "StartGame");
        menuManager.ShowLoadingGamePanel();
        // Log.Message("SceneManager", "Loading Scene Main Async Started");
        var loading = SceneManager.LoadSceneAsync("Main");
        // Log.Message("MenuController", "Start Game Async");
        loading.completed += StartGameLoadingCompleted;
    }

    protected void StartGameLoadingCompleted(AsyncOperation operation)
    {
        // menuManager.HideLoadingGamePanel();
        // menuManager.HideAll();
        // Log.Message("SceneManager", "Loading Scene Main Async Done");
        // Log.Message("MenuController", "Start Game Completed");
    }
}
