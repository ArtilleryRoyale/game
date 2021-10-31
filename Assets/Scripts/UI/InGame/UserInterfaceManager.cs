using System.Collections.Generic;
using Jrmgx.Helpers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserInterfaceManager : MonoBehaviour
{
    #region Fields

    public static UserInterfaceManager Instance;

    [Header("Prefabs")]
    [SerializeField] private AmmoButtonUIController AmmoButtonUIPrefab = default;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI infoText = default;
    [SerializeField] private TextMeshProUGUI timerValueText = default;
    [SerializeField] private TextMeshProUGUI roundCountText = default;
    [SerializeField] private RectTransform ammoTransform = default;
    [SerializeField] private GameObject timerContainer = default;
    [SerializeField] private GameObject infoContainer = default;
    [SerializeField] public TeamUIController Team01UIController = default;
    [SerializeField] public TeamUIController Team02UIController = default;
    [SerializeField] public CharacterUIController CharacterTeam01UIController = default;
    [SerializeField] public CharacterUIController CharacterTeam02UIController = default;
    [SerializeField] private WindUIController windUIController = default;
    private GameObject ammoPanel => ammoTransform.gameObject;
    private readonly List<AmmoButtonUIController> ammoButtonUIControllers = new List<AmmoButtonUIController>();

    private CharacterManager CurrentCharacterManager = default;

    // State
    private bool hideTimer;

    #endregion

    #region Init

    private void Awake()
    {
        Instance = this;

        timerContainer.SetActive(false);
        infoContainer.SetActive(false);
        windUIController.gameObject.SetActive(false);
        ammoPanel.SetActive(false);
        CharacterTeam01UIController.gameObject.SetActive(false);
        CharacterTeam02UIController.gameObject.SetActive(false);
        Team01UIController.gameObject.SetActive(false);
        Team02UIController.gameObject.SetActive(false);
    }

    #endregion

    #region Current character Info

    public void ShowCurrentCharacter(CharacterManager characterManager)
    {
        CurrentCharacterManager = characterManager;
        HideCurrentCharacter();
        if (characterManager.PlayerController.IsPlayerOne) {
            CharacterTeam01UIController.gameObject.SetActive(true);
            CharacterTeam01UIController.ShowInfo(characterManager.PlayerController.PlayerUsername, characterManager.Character.IconPlayerOne);
        } else {
            CharacterTeam02UIController.gameObject.SetActive(true);
            CharacterTeam02UIController.ShowInfo(characterManager.PlayerController.PlayerUsername, characterManager.Character.IconPlayerTwo);
        }
        UpdateCurrentCharacter(characterManager);
    }

    // TODO prio 3 this makes a call to the canvas/layer system or it does not count?
    // Note: it would be ok if not called every frame
    public void UpdateCurrentCharacter(CharacterManager characterManager)
    {
        if (characterManager.PlayerController.IsPlayerOne) {
            CharacterTeam01UIController.UpdateText(characterManager.Character.HumainName + ": " + characterManager.Health);
        } else {
            CharacterTeam02UIController.UpdateText(characterManager.Character.HumainName + ": " + characterManager.Health);
        }
    }

    public void HideCurrentCharacter()
    {
        CharacterTeam01UIController.gameObject.SetActive(false);
        CharacterTeam02UIController.gameObject.SetActive(false);
    }

    #endregion

    #region Weapon

    public void ShowWeaponPanel(CharacterManager characterManager)
    {
        InventoryDelegate inventoryDelegate = characterManager.InventoryDelegate;
        var weaponItems = inventoryDelegate.GetPlayerWeapons(characterManager.Character);
        RefreshWeaponPanel(weaponItems);
        ammoPanel.SetActive(true);
    }

    public void UpdateWeaponPanel(Weapon weapon)
    {
        foreach (AmmoButtonUIController button in ammoButtonUIControllers) {
            if (button.Weapon.Equals(weapon)) {
                button.SetFocused(true);
            } else {
                button.SetFocused(false);
            }
        }
    }

    public void HideWeaponPanel()
    {
        ammoPanel.SetActive(false);
    }

    private void RefreshWeaponPanel(Dictionary<Weapon, int> weapons)
    {
        int y = 0;
        int x = -1;
        int maxY = 0;
        float width = AmmoButtonUIController.SIZE;
        float height = AmmoButtonUIController.SIZE;
        Weapon.WeaponGroupEnum prevGroup = Weapon.WeaponGroupEnum.None;

        ammoButtonUIControllers.Clear();
        ammoPanel.transform.RemoveAllChildren(immediate: true);

        foreach (var kv in weapons) {
            Weapon weapon = kv.Key;
            AmmoButtonUIController ammoButton = Instantiate(AmmoButtonUIPrefab, ammoPanel.transform, false);

            if (weapon.WeaponGroup != prevGroup) {
                y = 0;
                x++;
                prevGroup = weapon.WeaponGroup;
            } else {
                y++;
                if (y > maxY) { maxY = y; }
            }

            ammoButton.Init(weapon, new Vector2(x, y), CurrentCharacterManager.CharacterWeapon);
            ammoButton.SetQuantity(kv.Value);

            // Position the ammo button
            RectTransform ammoButtonRectTransform = ammoButton.RectTransform;
            ammoButtonRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, x * width, width);
            ammoButtonRectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, y * height, height);

            ammoButtonUIControllers.Add(ammoButton);
        }
    }

    #endregion

    #region Wind

    public void UpdateWind(float value)
    {
        windUIController.gameObject.SetActive(true);
        windUIController.UpdateWind(value);
    }

    #endregion

    #region Info Text

    /// <summary>
    /// Update the UI, passing a null string will hide the box
    /// </summary>
    public void UpdateInfoText(string message, float hideInSeconds = 0)
    {
        infoContainer.SetActive(true);
        infoText.text = message;
        if (hideInSeconds > 0) {
            this.ExecuteInSecond(hideInSeconds, () => HideInfoText());
        }
    }

    /// <summary>
    /// Update the UI, passing a null string will hide the box
    /// </summary>
    public void HideInfoText()
    {
        if (infoContainer == null) return;
        infoContainer.SetActive(false);
    }

    #endregion

    #region Timer and Round

    public void UpdateTimer(int value, bool hasUrgency)
    {
        if (hideTimer) return;
        timerContainer.SetActive(true);
        if (value > 3600) {
            timerValueText.text = "++";
        } else if (value > 90) {
            var minutes = Mathf.FloorToInt(value / 60f);
            timerValueText.text = minutes + "m";
        } else {
            timerValueText.text = value.ToString("D2");
        }
        timerValueText.color = Color.white;
        if (hasUrgency) {
            timerValueText.color = Color.red;
        }
    }

    public void HideTimer()
    {
        timerContainer.SetActive(false);
    }

    public void UpdateRoundCount(int value)
    {
        roundCountText.text = "Round: " + value;
    }

    #endregion
}
