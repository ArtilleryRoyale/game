using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName="ArtilleryRoyale/Character")]
public class Character : ScriptableObject
{
    public enum CharacterTypeEnum : int {
        Rook = 0, Bishop = 1, Knight = 2, Queen = 3, King = 4, None = 5
    }

    [Header("Identity")]
    public CharacterTypeEnum CharacterType;
    public string HumainName;
    public Sprite IconPlayerOne; // Red
    public Sprite IconPlayerTwo; // Blue
    public Sprite Indicator;
    public int HealthDefault;

    [Header("Config")]
    public Explosion Explosion;

    [Header("Move")]
    public float GravityScale = 7;
    public float MoveSpeed;
    public float MoveJumpHeight;

    [Header("Animation")]
    public float AnimationFactor = 1f;
}
