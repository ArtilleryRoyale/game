using UnityEngine;

public interface PositionableInterface
{
    void SetPosition(Vector3 position);

    // Trick
    string name { get; }
}
