using UnityEngine;
using CC;
using System.Collections.Generic;
using Jrmgx.Helpers;
using FMODUnity;
using DG.Tweening;

public abstract class BoxControllerBase : NetworkObject, PositionableInterface, ExplosionReceiver
{
    #region Fields

    [Header("References")]
    [SerializeField] protected SpriteRenderer spriteRenderer = default;
    [SerializeField] protected BoxCollider2D boxCollider = default;
    [SerializeField] protected Explosion Explosion = default;
    [SerializeField] protected StudioEventEmitter soundEventAppear = default;

    [Header("Predefined")]
    [SerializeField] protected bool isPredefined = false;

    // State
    public bool IsValid { get; protected set; }
    protected bool ExplosionHasBeenGenerated;

    protected Vector2 myPosition => transform.position;

    #endregion

    protected abstract void DidCollide(Collider2D collider);

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer.gameObject.SetActive(false);
        boxCollider.enabled = false;
    }

    protected override void Start()
    {
        base.Start();
        if (isPredefined) {
            SetPosition(transform.position);
        }
    }

    public virtual void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        if (!IsValid) return;
        if (ExplosionHasBeenGenerated) return;
        // Prevent multiple instantiation if hiting multiple colliders
        ExplosionHasBeenGenerated = true;

        if (GameManager.Instance == null) {
            // TODO prio 7 is this warning useful?
            Log.Error("BoxController", "GameManager.Instance is null in BoxControllerBase: probably because this box set as isPredefined");
        }
        ExplosionManager.Instance.GetExplosion(transform.position).Init(Explosion);

        NetworkDestroy();
    }

    protected void DidPickup()
    {
        IsValid = false;
        spriteRenderer.gameObject.SetActive(false);
        boxCollider.enabled = false;
        NetworkDestroy();
    }

    #region Positionable Interface

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        spriteRenderer.gameObject.SetActive(true);
        boxCollider.enabled = true;
        IsValid = true;
        spriteRenderer.color = Color.white;
        Physics2D.SyncTransforms();
        if (!isPredefined) {
            spriteRenderer.color = Color.white.WithA(0);
            // TODO prio 2 Better SFX appear
            DOTween.To(
                () => spriteRenderer.color,
                c => spriteRenderer.color = c,
                Color.white,
                1.33f
            );
            soundEventAppear.Play();
        }
    }

    public void SetSymmetry(bool status)
    {
        if (!status) return;
        Vector3 p = transform.localScale;
        p.x *= -1;
        transform.localScale = p;
    }

    #endregion

    private void FixedUpdate() // TODO prio 2 why not a OnTrigger or similar?
    {
        if (!IsValid || !IsNetworkOwner) return;
        var results = new List<Collider2D>();
        if (boxCollider.OverlapCollider(Layer.Character.ContactFilter, results) > 0) {
            DidCollide(results[0]);
        }
    }
}
