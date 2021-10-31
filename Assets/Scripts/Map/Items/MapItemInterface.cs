using UnityEngine;

interface MapItemInterface : PositionableInterface
{
    bool IsValid { get; }
    // Trick
    GameObject gameObject { get; }
}
