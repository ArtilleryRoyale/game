using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamUIController : MonoBehaviour
{
    #region Fields

    [SerializeField] private Slider healthSlider = default;
    [SerializeField] private TextMeshProUGUI characterCountText = default;
    [SerializeField] private Image sliderFillImage = default;

    #endregion

    public void Init(Color color)
    {
        gameObject.SetActive(true);
        sliderFillImage.color = color;
    }

    public void UpdateData(int numberOfCharacter, int healthSum, int healthAvailable)
    {
        characterCountText.text = "" + numberOfCharacter;
        healthSlider.minValue = 0;
        healthSlider.maxValue = healthAvailable;
        healthSlider.value = Mathf.Min(healthSum, healthAvailable);
    }
}
