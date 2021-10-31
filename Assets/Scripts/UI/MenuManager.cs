using UnityEngine;
using static UnityEngine.InputSystem.InputAction; // Used in CC_DEBUG
using UnityEngine.InputSystem;
using Jrmgx.Helpers;
using FMODUnity;
using CC;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MenuManager : MonoBehaviour
{
    #region Fields

    public static MenuManager Instance;

    [Header("Menu Path")]
    [SerializeField] protected MenuPath MenuPath = default;

    [Header("References")]
    [SerializeField] private MainPanelUIController mainPanel = default;
    [SerializeField] private SoloPanelUIController soloPanel = default;
    [SerializeField] private ModePanelUIController modePanel = default;
    [SerializeField] private MultiPanelUIController multiPanel = default;
    [SerializeField] private GamePanelUIController gamePanel = default;
    [SerializeField] private OptionPanelUIController optionPanel = default;
    [SerializeField] private HelpPanelUIController helpPanel = default;
    [SerializeField] private GameObject loadingGamePanel = default;
    [SerializeField] private Button loadingGamePanelCloseButton = default;
    [SerializeField] private PopupManager popupManager = default;

    [Header("Sounds")]
    [SerializeField] private StudioEventEmitter soundEventMusic = default;
    [SerializeField] private StudioEventEmitter soundClickGeneric = default;
    [SerializeField] private StudioEventEmitter soundErrorGeneric = default;

    public GameOption CurrentGameOption { get; protected set; }

    private Debouncer debouncerInputValueAsButton;

    // Status
    protected Component currentPanel;
    private float konamiTryStarted;
    private int konamiStep;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        Instance = this;
        // Log.Message("MenuManager", "Awake + Find GameOption and NetworkManager Instance");
        CurrentGameOption = FindObjectOfType<GameOption>();
        debouncerInputValueAsButton = new Debouncer(this, 0.3f);
        popupManager.gameObject.SetActive(true);
    }

    private void Start()
    {
        ShowMainPanel();
        VoiceManager.InitSoundSystem();
        if (Config.WithMusic) {
            soundEventMusic.Play();
        }
    }

    protected void OnEnable()
    {
        Cursor.visible = true;
    }

    #endregion

    #region Public

    public void PlaySoundClick()
    {
        soundClickGeneric.Play();
    }

    public void PlaySoundError()
    {
        soundErrorGeneric.Play();
    }

    public void ShowOptionsPanel()
    {
        ActiveGivenPanel(optionPanel);
    }

    public void ShowHelpPanel()
    {
        ActiveGivenPanel(helpPanel);
    }

    public void ShowMainPanel()
    {
        ActiveGivenPanel(mainPanel);
    }

    public void ShowSoloPanel()
    {
        ActiveGivenPanel(soloPanel);
    }

    public void ShowModePanel()
    {
        ActiveGivenPanel(modePanel);
    }

    public void ShowMultiPanel()
    {
        ActiveGivenPanel(multiPanel);
    }

    public void ShowGamePanel()
    {
        ActiveGivenPanel(gamePanel);
    }

    public void ShowLoadingGamePanel(bool canClose = false)
    {
        loadingGamePanel.SetActive(true);
        loadingGamePanelCloseButton.gameObject.SetActive(canClose);
        loadingGamePanelCloseButton.onClick.RemoveAllListeners();
        loadingGamePanelCloseButton.onClick.AddListener(HideLoadingGamePanel);
    }

    public void HideLoadingGamePanel()
    {
        loadingGamePanel.SetActive(false);
    }

    #endregion

    #region Panel Management

    public void SaveMenuPath(string identifier)
    {
        MenuPath.Identifier = identifier;
    }

    private void ActiveGivenPanel(Component panel)
    {
        // Override Panel
        if (!string.IsNullOrEmpty(MenuPath.Identifier)) {
            var identifier = MenuPath.Identifier;
            MenuPath.Identifier = "";
            foreach (var p in new Component[]{ soloPanel, multiPanel }) {
                if (identifier == p.gameObject.name) {
                    panel = p;
                }
            }
        }
        // Hide All
        foreach (var p in new Component[] {
            mainPanel,
            soloPanel,
            modePanel,
            multiPanel,
            gamePanel,
            optionPanel,
            helpPanel,
        }) {
            if (panel.gameObject.name == p.gameObject.name) continue;
            p.gameObject.SetActive(false);
        }
        // Set current
        panel.gameObject.SetActive(true);
        currentPanel = panel;
    }

    private void DeActiveGivenPanel(Component panel)
    {
        panel.gameObject.SetActive(false);
    }

    #endregion

    #region Input

    private void Update()
    {
#if CC_DEBUG && UNITY_EDITOR
        if (Keyboard.current.kKey.wasPressedThisFrame) {
            /**/Debugging.ClearLogs();
        }
#endif

        if (PopupManager.HasFocus) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) {
            QuitGame();
        }

        // Konami code up, up, down, down, left, right, left, right, b, a (QWERTY Keyboard)
        if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame) {
            // Start the sequence
            konamiTryStarted = Time.time + 500;
            konamiStep = 1;
        }
        if (konamiTryStarted > Time.time) { // Continue the sequence
            if (konamiStep == 1 && Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 2 && Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 3 && Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 4 && Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 5 && Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 6 && Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 7 && Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 8 && Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame) {
                konamiTryStarted = Time.time + 500;
                konamiStep++;
            } else if (konamiStep == 9 && Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame) {
                konamiTryStarted = 0;
                konamiStep = 0;
                Konami();
            }
        } else {
            konamiStep = 0;
        }

        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) {
            Log.Critical("MenuManager", "LogMarker Asked");
            NetworkManager.Instance.StreamPlayPlayer.LogMarker().Forget();
            NetworkManager.Instance.StreamPlayRecorder.LogMarker().Forget();
        }

    }

    private void Konami()
    {
        // Log.Message("MenuManager", "Konami unlocked");
        PopupManager.Init(
            "Konami",
            "Reset all progress?",
            "Reset", () => {},
            "Noooo!"
        ).Show();
    }

    public void QuitGame()
    {
        PopupManager.Init(
            "Quit Artillery Royale",
            "Do you want to quit?",
            "Stay", () => { },
            "Leave", Application.Quit
        ).Show();
    }

    #endregion
}
