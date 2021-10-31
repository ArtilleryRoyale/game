using UnityEngine;

public abstract class SFXControllerBase : MonoBehaviour, SFXInterface
{
    [SerializeField] protected GameObject Object = default;
    [SerializeField] protected ParticleSystem innerParticleSystem = default;

    public ParticleSystem ParticleSystem => innerParticleSystem;

    protected void Awake()
    {
        Object.SetActive(false);
    }

    public virtual void Init()
    {
        Object.SetActive(true);
    }

    public void Stop()
    {
        ParticleSystem.Stop();
    }

    public virtual void DeInit()
    {
        Object.SetActive(false);
    }

    public virtual void Attach(Transform parentTransform)
    {
        transform.position = parentTransform.position;
        transform.SetParent(parentTransform, worldPositionStays: true);
        transform.localRotation = Quaternion.identity;
    }

    public virtual void Detach()
    {
        transform.SetParent(SFXManager.Instance.transform, worldPositionStays: false);
    }

    public virtual void EndSfx()
    {
        Destroy(gameObject);
    }
}
