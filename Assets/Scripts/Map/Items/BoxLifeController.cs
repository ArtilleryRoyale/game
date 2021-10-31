using FMODUnity;
using UnityEngine;

public class BoxLifeController : BoxControllerBase // :( this is called "Life" and should be "Health" but whatever
{
    [Header("References")]
    [SerializeField] private StudioEventEmitter soundEventHealthPackTaken = default;

    protected override void DidCollide(Collider2D collision)
    {
        if (!IsValid) return;
        var characterManager = collision.GetComponent<CharacterManager>();
        if (characterManager != null && characterManager.IsActive) {
            characterManager.CollectHealth(25);
            DidPickup();
            SFXManager.Instance.GetSFX(SFXManager.SFXType.PickupHealth, transform.position).Init();
            soundEventHealthPackTaken.Play();
        }
    }
}
