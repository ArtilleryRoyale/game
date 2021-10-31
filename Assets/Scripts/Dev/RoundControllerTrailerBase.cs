using System.Linq;
using Cysharp.Threading.Tasks;
using Jrmgx.Helpers;
using UnityEngine;

public abstract class RoundControllerTrailerBase : RoundControllerBase
{
    #region Fields

    protected abstract string TrainingName { get; }

    #endregion

    protected override void SelectNextPlayerAndCharacter()
    {
        if (CurrentPlayerController == null || CurrentCharacterManager == null) {
            CurrentPlayerController = GameManager.Instance.PlayerControllers.Where(p => p.IsPlayerOne).First();
            CurrentCharacterManager = CurrentPlayerController.GetNextCharacter();
        }

        CameraManager.Instance.RequestFollow(CurrentCharacterManager.transform);
    }

    protected void InstantiateWeaponBox(Vector2 position, Weapon.AmmoEnum weaponIdentifier)
    {
        Weapon weapon = WeaponManager.Instance.GetWeapon(weaponIdentifier);
        BoxWeaponController box = NetworkInstantiate(boxWeaponPrefab);
        box.SetWeapon(weapon, GameManager.Instance.CurrentGameOption.ShowBoxContent);
        box.SetPosition(position);
    }

    protected void InstantiateHealthBox(Vector2 position)
    {
        BoxLifeController box = NetworkInstantiate(boxLifePrefab);
        box.SetPosition(position);
    }

    protected override void ClosePopup()
    {
        // We never close popup in training mode because they are used to inform the player
    }

    protected override void GameOver(string message)
    {
        this.ExecuteInSecond(3, () => {
            PopupManager.Init(
                "Game Over",
                "",
                "Menu", GameManager.Instance.BackToMenu
            ).Show();
        });
    }

    protected override void RetreatRound()
    {
        // base.RetreatRound();
        // In training mode we don't have retreat timer, instead we wait for the end of RoundLock.Lock
        // this should tell us when the ammo has exploded
        // TODO prio 2 for now we still have a little bit of time left because when asking for idle state
        // we go thru the whole resting process, but it will be changed at some point
        RetreatWhileLocked().Forget();
    }

    protected async UniTask RetreatWhileLocked()
    {
        await GameManager.Instance.RoundLock.WaitWhileIsLocked().CancelOnDestroy(this);
        await InteruptRound().CancelOnDestroy(this);
    }
}
