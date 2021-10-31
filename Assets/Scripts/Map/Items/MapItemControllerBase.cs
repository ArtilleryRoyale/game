using UnityEngine;

public abstract class MapItemControllerBase : NetworkObject, MapItemInterface
{
    [Header("Style")]
    public MapController.MapStyle Style = MapController.MapStyle.Jungle;

    [Header("References")]
    [SerializeField] protected GameObject Object = default;
    [SerializeField] protected GameObject ObjectBackground = default;

    [Header("Predefined")]
    [SerializeField] protected bool isPredefined = false;

    public bool IsValid { get; protected set; } = false;
    protected bool hasSymmetry;

    protected override void Awake()
    {
        base.Awake();

        Object.SetActive(false);

        if (ObjectBackground == null) return;
        ObjectBackground.SetActive(false);
        foreach (var backgroundSpriteRenderer in ObjectBackground.GetComponentsInChildren<SpriteRenderer>()) {
            backgroundSpriteRenderer.color = Config.ColorExplosionBorder;
        }
    }

    protected override void Start()
    {
        // Most of derived class are not fully fledged network objects
        // So we do not call base.Start() here to prevent NetworkObject checks
        // if a derived class needs all the NetworkObject functionalities
        // it will need to override Start again
        // TODO prio 2 code smell either they are, or not
        // base.Start();
        if (isPredefined) {
            Force();
        }
    }

    [ContextMenu("Force Positioning")]
    protected void Force()
    {
        SetPosition(transform.position);
    }

    public virtual void SetSymmetry(bool status)
    {
        hasSymmetry = status;
        if (!status) return;
        var p = Object.transform.localScale;
        p.x *= -1;
        var r = Object.transform.localRotation.eulerAngles;
        r.z *= -1;
        Object.transform.localScale = p;
        Object.transform.localRotation = Quaternion.Euler(r);
        if (ObjectBackground == null) return;
        ObjectBackground.transform.localScale = p;
        ObjectBackground.transform.localRotation = Quaternion.Euler(r);
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        Object.SetActive(true);
        if (ObjectBackground != null) {
            ObjectBackground.SetActive(true);
        }
        IsValid = true;
        Physics2D.SyncTransforms();
    }
}
