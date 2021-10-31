using UnityEngine;
using System;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public class BasicPopupController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private FocusableButtonUIController confirmButton = default;

    // State
    private Action onSuccessAction;

    #endregion

    public void Show()
    {
        gameObject.SetActive(true);
        PopupManager.PopupHasFocus(true);
    }

    public void Show(Action then)
    {
        onSuccessAction = then;
        Show();
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

    public void ConfirmAction()
    {
        onSuccessAction();
        Hide();
    }

    #endregion

    #region Input

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) {
            if (confirmButton == null) return;
            confirmButton.ActualButton.onClick.Invoke();
            return;
        }
    }

    #endregion
}
