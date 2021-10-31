using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterUIController : MonoBehaviour
{
    #region Fields

    [SerializeField] private Image characterIconImage = default;
    [SerializeField] private TextMeshProUGUI characterUsernameText = default;
    [SerializeField] private TextMeshProUGUI characterHealthText = default;

    #endregion

    public void ShowInfo(string username, Sprite icon)
    {
        characterIconImage.sprite = icon;
        characterUsernameText.text = username;
    }

    public void UpdateText(string text)
    {
        characterHealthText.text = text;
    }
}
