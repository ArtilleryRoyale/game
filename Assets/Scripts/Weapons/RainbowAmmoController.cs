using DG.Tweening;
using UnityEngine;

/// <summary>
/// This is a "fake" weapon as it will only change the state on RoundController
/// </summary>
public class RainbowAmmoController : AmmoControllerBase
{
    #region Fields

    [SerializeField] private Material rainbowMaterial = default;

    // State
    private int fadeAmountId;
    private int fadeBurnWidthId;
    private float fadeIn;

    #endregion

    // TODO prio 1 block new rainbow if already one active
    protected override void Awake()
    {
        base.Awake();
        fadeAmountId = Shader.PropertyToID("_FadeAmount"); // 1 => 0
        fadeBurnWidthId = Shader.PropertyToID("_FadeBurnWidth"); // 0 => 1
        rainbowMaterial.SetFloat(fadeAmountId, 1f);
        rainbowMaterial.SetFloat(fadeBurnWidthId, 0f);
    }

    protected override void Start()
    {
        base.Start();
        DOTween.To(() => fadeIn, f => {
            fadeIn = f;
            rainbowMaterial.SetFloat(fadeAmountId, 1f - f);
            rainbowMaterial.SetFloat(fadeBurnWidthId, f);
        }, 1f, 5f);
    }

    public void End()
    {
        DOTween.To(() => fadeIn, f => {
            fadeIn = f;
            rainbowMaterial.SetFloat(fadeAmountId, 1f - f);
            rainbowMaterial.SetFloat(fadeBurnWidthId, f);
        }, 0f, 2f).OnComplete(UnlockAndDestroy);
    }

    private void OnDestroy()
    {
        rainbowMaterial.SetFloat(fadeAmountId, 0f);
        rainbowMaterial.SetFloat(fadeBurnWidthId, 1f);
    }

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        // Not calling base.Init to prevent GameManager.Instance.RoundLock.LockChainReaction(this)
        // base.Init(weapon, angle, isFacingRight, power);
        // TODO prio 1 sync
        GameManager.Instance.RoundControllerInstance.RainbowStart(this);
    }

    protected override void FixedUpdate()
    {
        // Not calling base.FixedUpdate() as we don't need it
    }
}
