using UnityEngine;

public interface SFXInterface
{
    void Init();
    void Attach(Transform parentTransform);
    void Detach();
    void EndSfx();

    // Trick
    Transform transform { get; }
    GameObject gameObject { get; }
}
