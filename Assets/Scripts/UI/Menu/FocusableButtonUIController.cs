using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FocusableButtonUIController : MonoBehaviour, FocusableButton, IPointerEnterHandler, IPointerExitHandler
{
    public Button ActualButton => actualButton;

    [SerializeField] private Image borderImage = default;
    [SerializeField] private Button actualButton = default;
    [SerializeField] private Sprite borderSelectedSprite = default;
    private Sprite borderSprite = null;

    private void Awake()
    {
        borderSprite = borderImage.sprite;
    }

    public void SetFocused(bool status)
    {
        if (borderSelectedSprite != null) {
            borderImage.sprite = status ? borderSelectedSprite : borderSprite;
        } else {
            borderImage.color = status ? Color.yellow : Color.white;
        }
    }

    private void OnDisable()
    {
        SetFocused(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetFocused(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetFocused(false);
    }
}
