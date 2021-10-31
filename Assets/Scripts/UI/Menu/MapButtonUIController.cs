using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapButtonUIController : MonoBehaviour
{
    #region Fields

    [Header("Style")]
    public MapController.MapStyle Style = MapController.MapStyle.None;

    [Header("References")]
    [SerializeField] public Button ActualButton = default;
    [SerializeField] private Image frameImage = default;
    [SerializeField] private Image iconImage = default;
    [SerializeField] private TMP_Text nameText = default;
    [SerializeField] private Sprite selectedFrameSprite = default;
    private Sprite baseFrameSprite = default;

    #endregion

    private void Awake()
    {
        baseFrameSprite = frameImage.sprite;
    }

    public void SetSelected(bool status)
    {
        frameImage.sprite = status ? selectedFrameSprite : baseFrameSprite;
    }
}
