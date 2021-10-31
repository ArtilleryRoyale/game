using DG.Tweening;
using Jrmgx.Helpers;
using UnityEngine;

public class Trailer01 : MonoBehaviour
{
    public GameObject marker = default;
    public MapItemMineController mine = default;

#if CC_DEBUG

    // Drop a grenade and leave / Tailer01a
    [ContextMenu("Plan 01")]
    public void Plan01()
    {
        // Init
        UserInterfaceManager.Instance.gameObject.SetActive(false);
        marker.SetActive(false);

        GameObject.Find("Duplicated").SetActive(false);
        GameObject.Find("LeavesParticleEmitter").SetActive(false);

        CameraManager.Instance.Size(20);

        CharacterManager c1 = GameManager.Instance.PlayerControllers[0].CharacterManagers[0];
        CharacterManager c2 = GameManager.Instance.PlayerControllers[1].CharacterManagers[0];

        c1.SetPosition(new Vector2(60+10, 22.96f));
        c2.SetPosition(new Vector2(60, 22.96f));

        CameraManager.Instance.RequestFollow(c2.transform, locked: true);
        CameraManager.Instance.Frame(new Vector2(.01f, .01f));

        c1.SetMoveAndAimState();

        // Action
        // Do it in playmode!
    }

    // Leave falls normals, but then the wind starts and everything is very fast / Tailer01a
    [ContextMenu("Plan 02")]
    public void Plan02()
    {
        // Init
        UserInterfaceManager.Instance.gameObject.SetActive(false);

        var roundController = FindObjectOfType<RoundControllerTrailer01>();

        CameraManager.Instance.Size(35);

        CharacterManager c1 = GameManager.Instance.PlayerControllers[0].CharacterManagers[0];
        CharacterManager c2 = GameManager.Instance.PlayerControllers[1].CharacterManagers[0];

        c1.SetPosition(new Vector2(148.35f, 57.65f));
        c2.SetPosition(new Vector2(191, 80.2f));
        marker.transform.position = new Vector2(167, 72);

        CameraManager.Instance.RequestFollow(marker.transform, locked: true);
        CameraManager.Instance.Frame(new Vector2(.01f, .01f));
        marker.SetActive(false);

        c1.SetMoveAndAimState();

        // LeavesParticlesEmitter override
        ParticleSystem leaves = GameObject.Find("LeavesParticleEmitter").GetComponent<ParticleSystem>(); // \o/
        leaves.maxParticles = 800;
        leaves.emissionRate = 60;
        leaves.transform.position = new Vector2(230, 50);

        // Action
        this.ExecuteInSecond(10, () => {
        DOTween.To(
            () => roundController.WindForce,
            v => roundController.ForceUpdateWind(v),
            -1f,
            5
        );
        });

        // Do it in playmode!
    }

    // Hit a mine / Tailer01b
    [ContextMenu("Plan 03")]
    public void Plan03()
    {
        // Init
        UserInterfaceManager.Instance.gameObject.SetActive(false);
        marker.SetActive(false);

        //GameObject.Find("Duplicated").SetActive(false);
        GameObject.Find("LeavesParticleEmitter").SetActive(false);

        CameraManager.Instance.Size(24);

        CharacterManager c1 = GameManager.Instance.PlayerControllers[0].CharacterManagers[0];
        CharacterManager c2 = GameManager.Instance.PlayerControllers[1].CharacterManagers[0];

        c1.SetPosition(new Vector2(6, 23));
        c2.SetPosition(new Vector2(17, 44f));
        marker.transform.position = new Vector2(27, 40);

        mine.SetPosition(mine.transform.position);

        CameraManager.Instance.RequestFollow(marker.transform, locked: true);
        CameraManager.Instance.Frame(new Vector2(.01f, .01f));
        marker.SetActive(false);

        c1.SetMoveAndAimState();

        // Action
        // Do it in playmode!
    }

    // Fall into lava / Tailer01b
    [ContextMenu("Plan 04")]
    public void Plan04()
    {
        // Init
        UserInterfaceManager.Instance.gameObject.SetActive(false);
        marker.SetActive(false);

        //GameObject.Find("Duplicated").SetActive(false);
        //GameObject.Find("LeavesParticleEmitter").SetActive(false);

        CameraManager.Instance.Size(30);

        CharacterManager c1 = GameManager.Instance.PlayerControllers[0].CharacterManagers[0];
        CharacterManager c2 = GameManager.Instance.PlayerControllers[1].CharacterManagers[0];

        c1.SetPosition(new Vector2(206, 39.7f));
        c2.SetPosition(new Vector2(185.78f, 27.89f));
        marker.transform.position = new Vector2(189, 25);

        mine.SetPosition(mine.transform.position);

        CameraManager.Instance.RequestFollow(marker.transform, locked: true);
        CameraManager.Instance.Frame(new Vector2(.01f, .01f));
        marker.SetActive(false);

        c1.SetMoveAndAimState();

        // Action
        // Do it in playmode!
    }

    // Exploding stuff / Tailer01c
    [ContextMenu("Plan 05")]
    public void Plan05()
    {
        // Init
        UserInterfaceManager.Instance.gameObject.SetActive(false);
        marker.SetActive(false);

        GameObject.Find("LeavesParticleEmitter").SetActive(false);

        CameraManager.Instance.Size(30);

        CharacterManager c1 = GameManager.Instance.PlayerControllers[0].CharacterManagers[0];
        CharacterManager c2 = GameManager.Instance.PlayerControllers[1].CharacterManagers[0];

        c1.SetPosition(new Vector2(14.36f, 53.55f));
        c2.SetPosition(new Vector2(41.51f, 68.92f));
        marker.transform.position = new Vector2(22, 53);

        mine.SetPosition(mine.transform.position);

        CameraManager.Instance.RequestFollow(marker.transform, locked: true);
        CameraManager.Instance.Frame(new Vector2(.01f, .01f));
        marker.SetActive(false);

        c1.SetMoveAndAimState();

        // Action
        // Do it in playmode!
    }

    // Exploding logo / Tailer01c
    [ContextMenu("Plan 06")]
    public void Plan06()
    {
        // Init
        UserInterfaceManager.Instance.gameObject.SetActive(false);
        marker.SetActive(false);

        GameObject.Find("LeavesParticleEmitter").SetActive(false);

        CameraManager.Instance.Size(30);

        CharacterManager c1 = GameManager.Instance.PlayerControllers[0].CharacterManagers[0];
        CharacterManager c2 = GameManager.Instance.PlayerControllers[1].CharacterManagers[0];

        c1.SetPosition(new Vector2(14.36f, 53.55f));
        c2.SetPosition(new Vector2(41.51f, 68.92f));
        marker.transform.position = new Vector2(160, 32);

        mine.SetPosition(mine.transform.position);

        CameraManager.Instance.RequestFollow(marker.transform, locked: true);
        CameraManager.Instance.Frame(new Vector2(.01f, .01f));
        marker.SetActive(false);

        c1.SetMoveAndAimState();

        // Action
        // Do it in playmode!
    }

#endif
}
