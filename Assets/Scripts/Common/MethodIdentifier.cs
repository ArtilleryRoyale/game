using CC.StreamPlay;
using UnityEngine;

abstract public class MethodIdentifier : MonoBehaviour
{
    // Network Messages (names have to be protected const string and start with NETWORK_MESSAGE_)
    protected const string NETWORK_MESSAGE_ROUND_READY = "r";
    protected const string NETWORK_MESSAGE_WAIT_FOR_SWITCH = "s";
    protected const string NETWORK_MESSAGE_SWITCH_SUCCESS = "t";
    protected const string NETWORK_MESSAGE_START = "a";
    protected const string NETWORK_MESSAGE_WAIT_FOR_GAMEOPTION = "g";
    protected const string NETWORK_MESSAGE_MAP_READY = "m";

    // Network Identifiers (names have to be protected const int and start with NETWORK_IDENTIFIER_)
    protected const int NETWORK_IDENTIFIER_GAME_MANAGER = 1;
    protected const int NETWORK_IDENTIFIER_MAP_CONTROLLER = 2;
    protected const int NETWORK_IDENTIFIER_ROUND_CONTROLLER = 3;
    protected const int NETWORK_IDENTIFIER_EXPLOSION_MANAGER = 4;

    // Method Identifiers (names have to be public const int and start with Method_)
    public const int Method_DeadOutOfBounds_Guest = 3;
    public const int Method_MakeDead_Guest = 4;
    public const int Method_CharacterManager_Sync_Guest = 5;
    public const int Method_ShowJump = 6;
    public const int Method_WeaponItem_Guest = 8;
    public const int Method_PositionCharacters = 9;
    public const int Method_RolesSwitched = 11;
    public const int Method_UIHideTimer = 12;
    public const int Method_UIUpdateTimer = 13;
    public const int Method_UIUpdateTimerRetreat = 14;
    public const int Method_InstantiateWeaponBox_Guest = 15;
    public const int Method_InstantiateHealthBox_Guest = 16;
    public const int Method_RiseLava = 18;
    public const int Method_UIUpdateInfoText = 19;
    public const int Method_UpdateRoundCount = 20;
    public const int Method_StartSuddenDeath = 21;
    public const int Method_UpdateWind = 22;
    public const int Method_SelectNextPlayerAndCharacter = 23;
    public const int Method_UIInitData = 24;
    public const int Method_ActivateCharacter = 25;
    public const int Method_DeactivateCharacter = 26;
    public const int Method_PositionPlatforms = 28;
    public const int Method_PositionDestructibleObjects = 29;
    public const int Method_Explosion = 31;
    public const int Method_Map_Sync_Guest = 32;
    public const int Method_BuildFromData = 33;
    public const int Method_BuildFirstPass = 34;
    public const int Method_BuildSecondPass = 35;
    public const int Method_PositionMine = 36;
    public const int Method_UIUpdateData = 37;
    public const int Method_Flip = 38;
    public const int Method_ExplosionInit_Common = 39;
    public const int Method_GetExplosion_Guest = 40;
    public const int Method_ActivateMine = 41;
    public const int Method_CharacterUserInterface_UpdateUI = 42;
    public const int Method_UpdateDamages = 43;
    public const int Method_FireLoadSound = 44;
    public const int Method_WeaponItemSound = 45;
    public const int Method_MortarInitStub = 46;
    public const int Method_CollectHealth_Common = 47;
    public const int Method_ShowTouchDown = 49;
    public const int Method_UpdateWeapon_Guest = 50;
    public const int Method_CameraFollowCharacter = 51;
    public const int Method_CameraFollowMine = 52;
    public const int Method_DashStamp = 53;
    public const int Method_ExplosionStamp = 54;
    public const int Method_PositionBeam = 55;
    public const int Method_Dash = 56;
    public const int Method_GameOver = 57;
    public const int Method_InitRagdoll = 60;
    public const int Method_DidEndRagdoll = 61;
    public const int Method_RoundIsReady = 62;
    public const int Method_ShowTimer = 64;
    public const int Method_FallSound = 65;
    public const int Method_GrenadeSound = 66;
    public const int Method_ShieldSFXHit = 68;
    public const int Method_FireworksInitDebris = 69;
    public const int Method_ShotgunSFX = 70;
    public const int Method_SniperSFX = 71;
    public const int Method_AmmoDestroy_Common = 72;
    public const int Method_SayNow = 73;
    public const int Method_UpdateCurrentWeapon_Guest = 74;
    public const int Method_WeaponItemBox_Guest = 75;
    public const int Method_UpdateOption = 76;
    public const int Method_RainbowStart = 77;
    public const int Method_RainbowDecrease = 78;
    public const int Method_FireworksLaunch = 79;
    public const int Method_FireworksDebris_Common = 80;
    public const int Method_FireworksExplode_Common = 81;
    public const int Method_LoadStyle = 82;
    public const int Method_PositionThrones = 83;
    public const int Method_SyncGameOption = 84;
    public const int Method_KingCaptured = 85;
    public const int Method_KingShield = 86;
    public const int Method_ShieldSFXEnd = 87;

    public static string DebugNetworkMessage(string m)
    {
        if (m == NETWORK_MESSAGE_ROUND_READY) return "ROUND_READY";
        if (m == NETWORK_MESSAGE_WAIT_FOR_SWITCH) return "WAIT_FOR_SWITCH";
        if (m == NETWORK_MESSAGE_SWITCH_SUCCESS) return "SWITCH_SUCCESS";
        if (m == NETWORK_MESSAGE_START) return "START";
        if (m == NETWORK_MESSAGE_WAIT_FOR_GAMEOPTION) return "WAIT_FOR_GAMEOPTION";
        if (m == NETWORK_MESSAGE_MAP_READY) return "MAP_READY";
        return "UNKOWN NETWORK MESSAGE: " + m;
    }

    public static string DebugNetworkIdentifier(int i)
    {
        switch (i) {
            case NETWORK_IDENTIFIER_GAME_MANAGER: return "GameManager";
            case NETWORK_IDENTIFIER_MAP_CONTROLLER: return "MapController";
            case NETWORK_IDENTIFIER_ROUND_CONTROLLER: return "RoundController";
            case NETWORK_IDENTIFIER_EXPLOSION_MANAGER: return "ExplosionManager";
            default: return "NetworkObject_" + i;
        }
    }

    public static string DebugMethodIdentifier(int m)
    {
        switch (m) {
            case Recorder.METHOD_DESTROY: return "DESTROY";

            case Method_DeadOutOfBounds_Guest: return "DeadOutOfBounds_Guest";
            case Method_MakeDead_Guest: return "MakeDead_Guest";
            case Method_CharacterManager_Sync_Guest: return "CharacterManager_Sync_Guest";
            case Method_ShowJump: return "ShowJump";
            case Method_WeaponItem_Guest: return "WeaponItem_Guest";
            case Method_PositionCharacters: return "PositionCharacters";
            case Method_RolesSwitched: return "RolesSwitched";
            case Method_UIHideTimer: return "UIHideTimer";
            case Method_UIUpdateTimer: return "UIUpdateTimer";
            case Method_UIUpdateTimerRetreat: return "UIUpdateTimerRetreat";
            case Method_InstantiateWeaponBox_Guest: return "InstantiateWeaponBox_Guest";
            case Method_InstantiateHealthBox_Guest: return "InstantiateHealthBox_Guest";
            case Method_RiseLava: return "RiseLava";
            case Method_UIUpdateInfoText: return "UIUpdateInfoText";
            case Method_UpdateRoundCount: return "UpdateRoundCount";
            case Method_StartSuddenDeath: return "StartSuddenDeath";
            case Method_UpdateWind: return "UpdateWind";
            case Method_SelectNextPlayerAndCharacter: return "SelectNextPlayerAndCharacter";
            case Method_UIInitData: return "UIInitData";
            case Method_ActivateCharacter: return "ActivateCharacter";
            case Method_DeactivateCharacter: return "DeactivateCharacter";
            case Method_PositionPlatforms: return "PositionPlatforms";
            case Method_PositionDestructibleObjects: return "PositionDestructibleObjects";
            case Method_Explosion: return "Explosion";
            case Method_Map_Sync_Guest: return "Map_Sync_Guest";
            case Method_BuildFromData: return "BuildFromData";
            case Method_BuildFirstPass: return "BuildFirstPass";
            case Method_BuildSecondPass: return "BuildSecondPass";
            case Method_PositionMine: return "PositionMine";
            case Method_UIUpdateData: return "UIUpdateData";
            case Method_Flip: return "Flip";
            case Method_ExplosionInit_Common: return "Explosion_InitCommon";
            case Method_GetExplosion_Guest: return "GetExplosion_Guest";
            case Method_ActivateMine: return "ActivateMine";
            case Method_CharacterUserInterface_UpdateUI: return "CharacterUserInterface_UpdateUI";
            case Method_UpdateDamages: return "UpdateDamages";
            case Method_FireLoadSound: return "FireLoadSound";
            case Method_WeaponItemSound: return "WeaponItemSound";
            case Method_MortarInitStub: return "Mortar_InitStub";
            case Method_CollectHealth_Common: return "CollectHealth_Common";
            case Method_ShowTouchDown: return "ShowTouchDown";
            case Method_UpdateWeapon_Guest: return "UpdateWeapon_Guest";
            case Method_CameraFollowCharacter: return "CameraFollowCharacter";
            case Method_CameraFollowMine: return "CameraFollowMine";
            case Method_DashStamp: return "DashStamp";
            case Method_ExplosionStamp: return "ExplosionStamp";
            case Method_PositionBeam: return "PositionBeam";
            case Method_Dash: return "Dash";
            case Method_GameOver: return "GameOver";
            case Method_InitRagdoll: return "InitRagdoll";
            case Method_DidEndRagdoll: return "DidEndRagdoll";
            case Method_RoundIsReady: return "RoundIsReady";
            case Method_ShowTimer: return "ShowTimer";
            case Method_FallSound: return "FallSound";
            case Method_GrenadeSound: return "GrenadeSound";
            case Method_ShieldSFXHit: return "ShieldSFXEnd";
            case Method_FireworksInitDebris: return "Fireworks_InitDebris";
            case Method_ShotgunSFX: return "ShotgunSFX";
            case Method_SniperSFX: return "SniperSFX";
            case Method_AmmoDestroy_Common: return "AmmoDestroy_Common";
            case Method_SayNow: return "SayNow";
            case Method_UpdateCurrentWeapon_Guest: return "UpdateCurrentWeapon_Guest";
            case Method_WeaponItemBox_Guest: return "WeaponItemBox_Guest";
            case Method_UpdateOption: return "UpdateOption";
            case Method_RainbowStart: return "RainbowStart";
            case Method_RainbowDecrease: return "RainbowDecrease";
            case Method_FireworksLaunch: return "Fireworks_Launch";
            case Method_FireworksDebris_Common: return "FireworksDebris_Common";
            case Method_FireworksExplode_Common: return "FireworksExplode_Common";
            case Method_LoadStyle: return "LoadStyle";
            case Method_PositionThrones: return "PositionThrones";
            case Method_SyncGameOption: return "SyncGameOption";
            case Method_KingCaptured: return "KingCaptured";
            case Method_KingShield: return "KingShield";
            case Method_ShieldSFXEnd: return "ShieldSFXEnd";

            default: return "Unknown Method " + m;
        }
    }
}
