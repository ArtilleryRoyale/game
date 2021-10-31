# Artillery Royale

Unity version 2019.4.23f1
PHP 7.2+ for some release scripts

Some library used:
 - Json lib: https://github.com/jilleJr/Newtonsoft.Json-for-Unity
 - Websocket lib: https://codeload.github.com/endel/NativeWebSocket
 - Await helper lib: https://github.com/Cysharp/UniTask

## Personal notes

https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types

### Unity life cycle and instantiate

Instantiate will call `Awake` before any other public method, so if you do:

```csharp
// MyMonoBehaviour.Awake() { x = 5; }
var o = Instantiate(MyMonoBehaviour); 
o.x = 2; 
// => o.x will be at 2
```

Note: `Start()` will be called next frame,
so with `Start() { x = 10; }` => o.x will be at 10 (next frame)

The point is, you can instantiate and pass some field values, 
they will be available in the object (and in `Start()`)
but don't override them in `Start()`

![monoBehaviourFlowchart](https://docs.unity3d.com/uploads/Main/monobehaviour_flowchart.svg)

### Async in fixed update

see some interesting blog post here: https://gametorrahod.com/unity-and-async-await/

With `protected void FixedUpdate() { /**/ }` you have an exec every 0.02 sec (default config)

```csharp
protected async UniTask FixedUpdate() {
    Debug.Log("Frame: " + Time.frameCount);
    await Task.Delay(TimeSpan.FromSeconds(3));
    // Some code
}
```

you also have an exec every 0.02 sec, it does not prevent the next `FixedUpdate` but for the rest it works as excepted `Some code` will be executed after 3 seconds

Awaitable code should have cancellation token, for example `.CancelOnDestroy(this)`
This is a regex to check if some are missing it `(?!.*(destroy|cancel))await`

### Unity colliders

If you put a layer on a gameobject with a collider and config in physics 2D that they should not collide,
it will work for sure (so they won't collide), but is the collider is in trigger mode, it will trigger.

When Instantiating object with a collider and moving it right away, the collider position is not updated  
(even if it seems so on the editor view). You need to manually call `Physics2D.SyncTransforms();`

### Unity new input system

 - Phases: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_phase

### Some C#

```csharp
// Map is Select:
Enumerable.Range(1, 10).Select(x => x + 2);
// Reduce is Aggregate:
Enumerable.Range(1, 10).Aggregate(0, (acc, x) => acc + x);
// Filter is Where:
Enumerable.Range(1, 10).Where(x => x % 2 == 0);
```

### Coding style and conventions

```csharp
using MyUsing;

namespace MyName // PascalCase
{
    public class MyClassName // PascalCase
    {
        public const string CONFIG_VALUE = "ok"; // UPPER_SNAKE_CASE
        public int SomeValue; // PascalCase
        private List<string> internalList = new List<string>(); // camelCase for protected/private

        [Header("Name For Those References")]
        [SerializedField] private GameObject someUnityReference = default; // camelCase for protected/private

        // Use region to group related methods
        #region Group Of Related Methods

        public int MyMethodName(bool myParamName) // PascalCase for the method name and camelCase for params
        {
            if (!myParamName) return 0; // return early if possible, allowing in this case only no { }

            foreach (string localName in internalList) { // camelCase for local variables
                // Do stuff
            }

            if (myParamName) {
                return 1;
            }

            return 0;
        }

        #endregion
    }
}
```

## StreamPlay documentation

StreamPlay is enabled when your object inherit from NetworkObject
the object must have a NetworkIdentifier set before executing Start()

### Instantiating

call InstantiateNetworkObject(Prefab, NetworkIdentifier).  
Note: after instantiation you probably want to call `Refresh()` on the Player if you use it

You have two helpers: NetworkObject.NetworkIdentifierFrom(string) that gives you a stable id for that string
and NetworkObject.NetworkIdentifierNew() that give you a random id

### Destroying

`DestroyNetworkObject()`

### Snapshots

Snapshots are used to exec a remote procedure call (RPC)

You must add an `[StreamPlayRPC(identifier)]` attribute on you method,
it must specify an uniq id that represent this method across all your code base.
You may maintain a file with all those ids

Then you should call NetworkRecordSnapshot(identifier, params) to remote call the method on the guest

### FloatPacks

Enabled when your object implement the FloatPackStreamPlay interface

Float packs are used to sync frequently changing values (in form of floats)
you can use this to sync position vector or similar

You do this by calling RecordFloatPack(compatible objects)
and get the data back on the guest in the callback OnFloatPack

## Technical documentation

### Character

Note: Player one is red, player two is blue

Prefabs/Characters/CharacterTypePrefab.prefab
Prefabs/Characters/Type/Type.prefab
ScriptableObjects/Characters/TypeCharacter.asset
Sprites/Characters/Type/*.png
Materials/Characters/Type*

### Weapons

The weapon can have two controllers, one is the weapon ammo (called weaponNameAmmoController) 
which is mandatory and the other is the weapon "container" (called weaponNameController).

A weapon is also defined by its configuration (which define its icon, power, etc).

The weaponAmmo defines what happen when the player fire with that weapon selected,
the weaponController contain validation methods and weaponController display methods.

ex: the mortarAmmo handle the mortar ammo and the mortarController handle the display of the weapon in the game.

#### Adding a weapon

To add a weapon you need:

1/ Make a new entry in enum WeaponEnum/AmmoEnum in `/ScriptableObjects/Weapon.cs` for that type of weapon.
2/ Add a weapon configuration file through the ArtilleryRoyale menu in assets, it should be placed in `/ScriptableObjects/Weapons`.
3/ Reference that file in the `/Prefabs/Managers/WeaponManager` prefab in either "Weapons defaults" or "Weapons Extra".
4/ Create a new nameAmmoController and make a prefab of it in `/Prefabs/Weapons`.
5/ This prefab must be referenced in the `/Prefabs/Managers/WeaponManager` prefab in its prefabs list (make a new entry).
6/ Add your weapon in `WeaponManager::GetWeaponLogic()` (same file)
7/ In the WeaponController prefab `/Prefabs/WeaponController` add a weapon container that will materialize your weapon visually in the game
8/ Reference it into the script is `/Scripts/Characters/CharacterWeapon.cs`
9/ Optional: a nameWeaponController to add more logic to it.

Note: in a few place in /Scripts/Characters/CharacterWeapon.cs you may want to add an entry for your weapon to have specific sounds or logic triggered,
see `FireLoadSound()` `WeaponItemSound()` `FireLoadSoundStop()` `WeaponItemInit()` `Bend()` `FireLoadAction()` methods.

## Production version

 - [ ] We want to disable CC_DEBUG macros and comment out all the Debugging namespace
 - [ ] Check all the TODO left in the code
 - [ ] Edit the Config.cs file with production values
 - [ ] Probably remove all the call (or most) to Log.Message
 - [ ] Check and fix warnings
 - [ ] Reactivate beta user interface

## Steam

### Build an app

use the steamcmd (~/Applications/Steam/sdk/tools/ContentBuilder/builder_osx/steamcmd) 
login with `login username`
and run `run_app_build /Users/jerome/Projets/ChessBattle/builds/steam/app_build_1474410.vdf`

Then on steam website: https://partner.steamgames.com/apps/builds/1474410
activate the build
