using UnityEngine;
using CC;
using Jrmgx.Helpers;
using CC.StreamPlay;
using FMODUnity;

public class ShieldAmmoController : AmmoControllerBase, ExplosionReceiver
{
    #region Fields

    public const float SHIELD_SIZE = 6.5f;
    public const float SHIELD_OFFSET = 2f; // Transform.localPosition.x

    [Header("References")]
    [SerializeField] private CircleCollider2D circleCollider = default;
    [SerializeField] private SFXSimpleSpawnController sfxMain = default;
    [SerializeField] private SFXSimpleSpawnController sfxHit = default;
    [SerializeField] private StudioEventEmitter soundEventHit = default;
    [SerializeField] private StudioEventEmitter soundEventEnd = default;

    // State
    public bool IsKing { get; private set; }
    private bool isActiveForKing;
    private bool hasEnded;

    #endregion

    public void SetKing()
    {
        IsKing = true;
        isActiveForKing = true;
    }

    public void InitKing(CharacterManager characterManager)
    {
        // Log.Message("ShieldAmmoController", "Init King Shield: Owner and Guest");

        this.characterManager = characterManager;
        this.initTime = Time.fixedTime;
        this.weapon = WeaponManager.Instance.GetWeapon(Weapon.AmmoEnum.Shield);

        IsKing = true;
        isActiveForKing = true;
    }

    public override void Init(CharacterManager characterManager, Weapon weapon, float angle, bool isFacingRight, float power)
    {
        // NetworkAssertNotGuest();

        // Not calling base on purpose, we don't want to lock GameManager.Instance.RoundLock.LockChainReaction(this);
        this.characterManager = characterManager;
        this.angle = angle;
        this.power = power;
        this.initTime = Time.fixedTime;
        this.weapon = weapon;
        this.isFacingRight = isFacingRight;

        isActive = true;
    }

    protected override void Start()
    {
        base.Start();
        circleCollider.transform.localScale = Vector3.one * SHIELD_SIZE;
        if (IsKing) {
            transform.localScale *= 1.5f;
        }
        if (IsAISimulationActive) return;
        // Log.Message("ShieldAmmoController", "Start");
        if (IsKing) {
           sfxMain.ParticleSystem.startColor = (characterManager.PlayerController.IsPlayerOne ? Config.ColorPlayerOne : Config.ColorPlayerTwo).WithA(0.5f);
        }
        sfxMain.Init();
    }

    protected override void FixedUpdate()
    {
        // Not calling base on purpose, we don't want the Network sync to be activated for this weapon
        // base.FixedUpdate();

        // This part is for the special king version of the shield
        if (!IsNetworkOwner) return;
        if (!isActiveForKing) return;
        if (!IsKing) return;
        // Not so good but it has to work fast / technical dept
        foreach (var playerController in GameManager.Instance.PlayerControllers) {
            if (playerController == characterManager.PlayerController) continue;
            foreach (var cm in playerController.Characters()) {
                var position = cm.MyPosition + Vector2.up * (cm.MyHeight / 2f);
                if (circleCollider.OverlapPoint(position)) {
                    characterManager.KingCaptured(cm);
                    isActiveForKing = false;
                    End();
                    break;
                }
            }
        }
    }

    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        if (IsKing) return;
        // Log.Message("ShieldAmmoController", "OnReceiveExplosion");
        // It's here to trigger the explosion + some reaction
        Hit();
        NetworkRecordSnapshot(Method_ShieldSFXHit);
        End();
    }

    [StreamPlay(Method_ShieldSFXHit)]
    protected void Hit()
    {
        sfxHit.Init();
        this.ExecuteInSecond(1.55f, () => {
            sfxHit.Stop();
            sfxHit.DeInit();
        });
    }

    public void End()
    {
        if (hasEnded) return;
        hasEnded = true;

        // Log.Message("ShieldAmmoController", "End");

        sfxMain.Stop();
        soundEventEnd.Play();
        NetworkRecordSnapshot(Method_ShieldSFXEnd);
        this.ExecuteInSecond(1.55f, () => {
            sfxMain.DeInit();
            UnlockAndDestroy();
        });
    }

    [StreamPlay(Method_ShieldSFXEnd)]
    private void End_Guest()
    {
        // Log.Message("ShieldAmmoController", "End Guest");
        sfxMain.Stop();
        soundEventEnd.Play();
    }

    public static bool IntoShield(CharacterManager characterManager)
    {
        var position = characterManager.MyPosition + Vector2.up * (characterManager.MyHeight / 2f);
        return Physics2D.OverlapCircle(position, 0.5f, Layer.Shield.Mask);
    }
}
