using Cysharp.Threading.Tasks;
using Jrmgx.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PopupManager : MonoBehaviour
{
    #region Fields

    public static PopupManager Instance;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText = default;
    [SerializeField] private TextMeshProUGUI messageText = default;
    [SerializeField] private FocusableButtonUIController primaryButton = default;
    [SerializeField] private FocusableButtonUIController secondaryButton = default;
    [SerializeField] private TextMeshProUGUI primaryText = default;
    [SerializeField] private TextMeshProUGUI secondaryText = default;

    // State
    public static bool HasFocus => Instance.popupVisible || externPopupVisible;
    private bool popupVisible; // This one
    private static bool externPopupVisible; // Other standalone popups (used in menu)
    private bool waiting;
    private FocusableButtonUIController currentSelected = null;

    #endregion

    #region Init

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void ResetState()
    {
        waiting = false;
        primaryButton.ActualButton.onClick.RemoveAllListeners();
        currentSelected = primaryButton;
        currentSelected.SetFocused(true);
        secondaryButton.ActualButton.onClick.RemoveAllListeners();
        secondaryButton.SetFocused(false);
        secondaryButton.gameObject.SetActive(false);
    }

    #endregion

    #region Public

    public static PopupManager Init(
        string title,
        string message,
        string primaryButtonText,
        UnityAction primaryButtonAction = null,
        string secondaryButtonText = null,
        UnityAction secondaryButtonAction = null
    ) {
        return Instance
            .SetTitle(title)
            .SetMessage(message)
            .SetPrimaryButton(primaryButtonText, primaryButtonAction)
            .SetSecondaryButton(secondaryButtonText, secondaryButtonAction)
        ;
    }

    public PopupManager SetTitle(string title)
    {
        titleText.text = title;
        return this;
    }

    public PopupManager SetMessage(string message)
    {
        messageText.text = message;
        return this;
    }

    public PopupManager SetPrimaryButton(string text, UnityAction action)
    {
        primaryText.text = text;
        primaryButton.ActualButton.onClick.AddListener(() => {
            if (MenuManager.Instance != null) {
                MenuManager.Instance.PlaySoundClick();
            }
            if (action != null) {
                action();
            }
            Hide();
        });
        return this;
    }

    public PopupManager SetSecondaryButton(string text, UnityAction action)
    {
        if (string.IsNullOrEmpty(text)) return this;

        secondaryText.text = text;
        secondaryButton.gameObject.SetActive(true);
        secondaryButton.ActualButton.onClick.AddListener(() => {
            if (MenuManager.Instance != null) {
                MenuManager.Instance.PlaySoundClick();
            }
            if (action != null) {
                action();
            }
            Hide();
        });
        return this;
    }

    public PopupManager Show()
    {
        popupVisible = true;
        gameObject.SetActive(true);
        primaryButton.SetFocused(false);
        return this;
    }

    public async UniTask Wait()
    {
        waiting = true;
        await UniTask.WaitWhile(() => waiting).CancelOnDestroy(this);
    }

    public static void Hide()
    {
        Instance.ResetState();
        Instance.popupVisible = false;
        Instance.gameObject.SetActive(false);
    }

    #endregion

    #region Popup Input Bypass

    /// This will prevent InputManager handlers
    /// Use this to favors your own popups
    public static void PopupHasFocus(bool state)
    {
        // Log.Message("PopupManager", "Calling for focus: " + state.ToString());
        externPopupVisible = state;
    }

    #endregion
}
