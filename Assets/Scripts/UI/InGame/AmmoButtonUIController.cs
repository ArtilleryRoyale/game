using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoButtonUIController : MonoBehaviour
{
    #region Fields

    public const int SIZE = 160;

    public Weapon Weapon { get; private set; }
    public Vector2 Position { get; private set; }
    public RectTransform RectTransform => GetComponent<RectTransform>();

    [Header("References")]
    [SerializeField] private Button button = default;
    [SerializeField] private Image image = default;
    [SerializeField] private TextMeshProUGUI nameText = default;
    [SerializeField] private TextMeshProUGUI ammoText = default;
    [SerializeField] private GameObject focusGameObject = default;
    [SerializeField] private Image backgroundImage = default;
    [SerializeField] private Sprite normalWeaponBackgroundSprite = null;
    [SerializeField] private Sprite specialWeaponBackgroundSprite = default;

    private CharacterWeapon CharacterWeapon;

    #endregion

    public void Init(Weapon weapon, Vector2 position, CharacterWeapon characterWeapon)
    {
        CharacterWeapon = characterWeapon;
        Weapon = weapon;
        Position = position;
        image.sprite = weapon.Icon;
        nameText.text = weapon.WeaponHumanName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
        backgroundImage.sprite = weapon.SpecialFor == Character.CharacterTypeEnum.None ?
            normalWeaponBackgroundSprite : specialWeaponBackgroundSprite;
    }

    public void SetQuantity(int quantity)
    {
        if (Weapon.UnitsUnlimited || GameManager.Instance.CurrentGameOption.AllWeaponsNoLimits) {
            ammoText.text = "";
            return;
        }
        ammoText.text = quantity <= 0 ? "" : "x" + quantity;
    }

    public void SetFocused(bool status)
    {
        focusGameObject.SetActive(status);
    }

    private void OnClick()
    {
        SetFocused(true);
        CharacterWeapon.SelectWeaponButton(Weapon);
    }
}
