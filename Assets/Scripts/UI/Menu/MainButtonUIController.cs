using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainButtonUIController : MonoBehaviour, FocusableButton, IPointerEnterHandler, IPointerExitHandler
{
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
        borderImage.sprite = status ? borderSelectedSprite : borderSprite;
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
