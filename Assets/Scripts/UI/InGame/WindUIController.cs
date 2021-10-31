using UnityEngine;
using TMPro;

public class WindUIController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private TextMeshProUGUI WindText = default;

    #endregion

    public void UpdateWind(float value)
    {
        string direction = value > 0 ? ">" : "<";
        string text = direction;
        if (value == 0) {
            text = "";
        } else if (Mathf.Abs(value) > 0.3) {
            text = direction + direction;
        }  else if (Mathf.Abs(value) > 0.6) {
            text = direction + direction + direction;
        }
        WindText.text = "Wind: " + text + " " + Mathf.Abs(Mathf.Ceil(value * 100)) + "% " + text;
    }
}
