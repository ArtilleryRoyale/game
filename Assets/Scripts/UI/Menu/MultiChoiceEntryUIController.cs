using System.Collections.Generic;
using Jrmgx.Helpers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using TMPro;

//[ExecuteInEditMode]
public class MultiChoiceEntryUIController : MonoBehaviour, FocusableButton
{
    [Serializable] public class EntryPrevValueEvent : UnityEvent<string> { }
    [Serializable] public class EntryNextValueEvent : UnityEvent<string> { }

    #region Fields

    [Header("References")]
    [SerializeField] private TMP_Text labelText = default;
    [SerializeField] private TMP_Text valueText = default;
    [SerializeField] private Button prevButton = default;
    [SerializeField] private Button nextButton = default;

    [Header("Sprites")]
    [SerializeField] private Sprite minusSprite = default;
    [SerializeField] private Sprite plusSprite = default;
    [SerializeField] private Sprite prevSprite = default;
    [SerializeField] private Sprite nextSprite = default;
    [SerializeField] private Sprite offSprite = default;
    [SerializeField] private Sprite onSprite = default;
    [SerializeField] private Sprite noSprite = default;
    [SerializeField] private Sprite yesSprite = default;

    [Header("Config")]
    [SerializeField] private EntryType entryType = EntryType.PrevNext;
    [SerializeField] private bool looping = false;
    [SerializeField] private bool readOnly = false;

    [Header("Values")]
    [SerializeField] private List<string> values = new List<string>();
    [SerializeField] private int currentIndex = 0;

    [Header("Events")]
    [SerializeField] private EntryPrevValueEvent prevActionEvent = default;
    [SerializeField] private EntryNextValueEvent nextActionEvent = default;

    private enum EntryType { PrevNext, MinusPlus, OnOff, YesNo }

    #endregion

    protected void Start()
    {
        switch (entryType) {
            case EntryType.MinusPlus:
                prevButton.GetComponent<Image>().sprite = minusSprite;
                nextButton.GetComponent<Image>().sprite = plusSprite;
                break;
            case EntryType.OnOff:
                prevButton.GetComponent<Image>().sprite = offSprite;
                nextButton.GetComponent<Image>().sprite = onSprite;
                break;
            case EntryType.YesNo:
                prevButton.GetComponent<Image>().sprite = noSprite;
                nextButton.GetComponent<Image>().sprite = yesSprite;
                break;
        }
        valueText.text = GetValue();
        SetReadOnly(readOnly);
    }

    #region Actions

    public void PrevAction()
    {
        currentIndex = Basics.PreviousIndex(currentIndex, values.Count, looping);
        valueText.text = GetValue();
        if (prevActionEvent != null) {
            prevActionEvent.Invoke(GetValue());
        }
    }

    public void NextAction()
    {
        currentIndex = Basics.NextIndex(currentIndex, values.Count, looping);
        valueText.text = GetValue();
        if (nextActionEvent != null) {
            nextActionEvent.Invoke(GetValue());
        }
    }

    #endregion

    #region Public

    public void SetValue(string v)
    {
        currentIndex = values.IndexOf(v);
        if (currentIndex < 0) {
            Log.Critical("MultiChoiceEntryUIController", name + " setting value: " + v + " but this value is not found in values: " + Debugging.IEnumerableToString(values));
            return;
        }
        valueText.text = GetValue();
    }

    public void SetValue(int v)
    {
        SetValue("" + v);
    }

    public string GetValue()
    {
        if (values.Count == 0 || currentIndex < 0 ||  currentIndex >= values.Count) {
            Log.Critical("MultiChoiceEntryUIController", name + " getting value at index: " + currentIndex + " but this index does not exist in values: " + Debugging.IEnumerableToString(values));
            return "?";
        }
        return values[currentIndex];
    }

    public void SetReadOnly(bool status)
    {
        readOnly = status;
        if (readOnly) {
            // Deactivate buttons
            prevButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
        } else {
            prevButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);
        }
    }

    #endregion

    #region FocusableButton

    public void SetFocused(bool status)
    {
        // var FocusOutline = GetComponent<Outline>();
        // FocusOutline.effectDistance = Vector2.one * 3;
        // FocusOutline.effectColor = new Color(0x00 / 255f, 0xDB / 255f, 0xFF / 255f);
        // FocusOutline.enabled = status;
    }

    #endregion
}
