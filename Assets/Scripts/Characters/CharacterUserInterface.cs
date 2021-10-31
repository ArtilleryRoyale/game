using TMPro;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using CC.StreamPlay;
using Jrmgx.Helpers;

public class CharacterUserInterface : NetworkObject
{
    #region Fields

    [Header("References")]
    [SerializeField] protected TextMeshPro healthText = default;
    [SerializeField] protected GameObject bubbleObject = default;
    [SerializeField] protected TextMeshPro bubbleText = default;
    [SerializeField] protected SpriteRenderer bubbleIcon = default;
    [SerializeField] protected SpriteRenderer isActiveIndicator = default;

    [Header("Sprites")]
    [SerializeField] protected Sprite deadIconSprite = default;
    [SerializeField] protected Sprite thinkingIconSprite = default;

    // Parent
    private CharacterManager CharacterManager;

    #endregion

    protected override void Awake()
    {
        base.Awake();
        CharacterManager = GetComponentInParent<CharacterManager>();
        isActiveIndicator.gameObject.SetActive(false);
        bubbleObject.SetActive(false);
        gameObject.SetActive(false);
    }

    protected override void Start()
    {
        NetworkIdentifier = NetworkObject.NetworkIdentifierFrom(CharacterManager.NetworkIdentifier + "_UI");
        base.Start();
    }

    public void Init(bool isPlayerOne, int health, bool isActive)
    {
#if CC_DEBUG
        if (GameManager.Instance.CurrentGameOption.IsTrailer) return;
#endif
        gameObject.SetActive(true);
        healthText.gameObject.SetActive(true);
        healthText.color = CharacterManager.PlayerController.PlayerColor;
        UpdateUI(health, isActive);
    }

    [StreamPlay(Method_UpdateDamages)]
    public async UniTask UpdateDamages(int health, int damages, bool invert)
    {
        // Log.Message("Damages", "UI update damages: -" + damages);
        TextMeshPro damagesText = Instantiate(healthText, transform);
        damagesText.text = (invert ? "+" : "-") + damages;
        int value = health;
#if CC_EXTRA_CARE
try {
#endif
        await UniTask.WhenAll( // .CancelOnDestroy(this)
            DOTween.To(
                () => value,
                x => healthText.text = Indicator() + x,
                Mathf.Max(0, health - (damages * (invert ? -1 : 1))),
                Config.CalculateDamageInSeconds
            )
            .SetEase(Ease.Linear)
            .AwaitForComplete() // .CancelOnDestroy(this)
        ,
            damagesText.transform
            .DOLocalMoveY(damagesText.transform.localPosition.y + 1.5f, Config.CalculateDamageInSeconds)
            .AwaitForComplete() // .CancelOnDestroy(this)
        ).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        await UniTask.Delay((int)(Config.CalculateDamageInSeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        Destroy(damagesText);
    }

    [StreamPlay(Method_CharacterUserInterface_UpdateUI)]
    public void UpdateUI(int health, bool isActive)
    {
        healthText.text = Indicator() + health;
        healthText.color = isActive ? Color.green : CharacterManager.PlayerController.PlayerColor;
        healthText.transform.position = healthText.transform.position.WithZ(isActive ? -2 : 0);
    }

    public void BubbleShow(string text)
    {
        bubbleText.enabled = false;
        bubbleIcon.enabled = false;

        switch (text) {
            case VoiceManager.PHRASE_DEAD:
                bubbleIcon.enabled = true;
                bubbleIcon.sprite = deadIconSprite;
                break;
            case VoiceManager.PHRASE_THINK:
                bubbleIcon.enabled = true;
                bubbleIcon.sprite = thinkingIconSprite;
                break;
            default:
                bubbleText.enabled = true;
                bubbleText.alignment = text == "..." ? TextAlignmentOptions.Top : TextAlignmentOptions.Center;
                bubbleText.text = text;
                break;
        }
        bubbleObject.SetActive(true);
    }

    public void BubbleShowAndHide(string text)
    {
        BubbleShow(text);
        this.ExecuteInSecond(Config.DelayBubbleOnly / 1000f, BubbleHide);
    }

    public void BubbleHide()
    {
        bubbleObject.SetActive(false);
    }

    private string Indicator()
    {
        return "<sprite=\"indicators\" tint=1 name=\"" + CharacterManager.Character.Indicator.name + "\"> ";
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }

    public void CounterFlip()
    {
        var s = gameObject.transform.localScale;
        s.x = transform.localScale.x * -1;
        gameObject.transform.localScale = s;
        // Bubble
        s = bubbleObject.transform.localScale;
        s.x *= -1;
        bubbleObject.transform.localScale = s;
        s = bubbleObject.transform.localPosition;
        s.x *= -1;
        bubbleObject.transform.localPosition = s;
        // Bubble text
        s = bubbleText.transform.localScale;
        s.x *= -1;
        bubbleText.transform.localScale = s;
    }
}
