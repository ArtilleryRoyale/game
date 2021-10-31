using UnityEngine;
using TMPro;
using CC;
using System;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

public class UsernamePopupController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private FocusableButtonUIController confirmButton = default;
    [SerializeField] private TMP_InputField usernameInputField = default;

    // State
    private Action onSuccessAction;

    #endregion

    public void Show(Action then)
    {
        onSuccessAction = then;
        gameObject.SetActive(true);
        PopupManager.PopupHasFocus(true);
    }

    public void Hide()
    {
        if (MenuManager.Instance != null) {
            MenuManager.Instance.PlaySoundClick();
        }
        PopupManager.PopupHasFocus(false);
        gameObject.SetActive(false);
    }

    #region Actions

    public void ConfirmUsernameAction()
    {
        string name = usernameInputField.text.Trim();
        var regex = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase);
        if (regex.IsMatch(name)) {
            PopupManager.Init(
                "Error",
                "Error while setting your username.\nIt must only contains latin letters and numbers.",
                "Retry"
            ).Show();
            return;
        }
        if (name.Length > 12 || name.Length < 2) {
            PopupManager.Init(
                "Error",
                "Error while setting your username.\nIt must be between 2 and 12 characters.",
                "Retry"
            ).Show();
            return;
        }
        if (string.IsNullOrWhiteSpace(name)) return;
        User.SetUsername(name);
        onSuccessAction();
        Hide();
    }

    #endregion

    #region Input

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) {
            confirmButton.ActualButton.onClick.Invoke();
            return;
        }
    }

    #endregion
}
