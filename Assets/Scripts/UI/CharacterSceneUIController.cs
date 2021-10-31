using UnityEngine;
using Anima2D;

public class CharacterSceneUIController : MonoBehaviour
{
    [SerializeField] protected Animator animator = default;
    [SerializeField] protected SpriteMeshInstance[] meshInstances = default;
    [SerializeField] public Material PlayerMaterialUI = default;

    protected void Start()
    {
        foreach (SpriteMeshInstance spriteMeshInstance in meshInstances) {
            spriteMeshInstance.sharedMaterial = PlayerMaterialUI;
        }
    }
}
