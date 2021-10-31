using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jrmgx.Helpers;
using FMODUnity;
using CC.StreamPlay;

public class ExplosionController : NetworkObject, RoundLockIdentifier
{
    #region Fields

    [Header("References")]
    [SerializeField] private ParticleSystem rockFragmentsParticleSystem = default; // TODO prio 5 move to SFX

    public int RoundLockIdentifier { get; set; }

    [Header("Sound")]
    [SerializeField] private StudioEventEmitter soundEventDefaultExplode = default;
    [SerializeField] private StudioEventEmitter soundEventMineExplode = default;
    [SerializeField] private StudioEventEmitter soundEventBazookaExplode = default;
    [SerializeField] private StudioEventEmitter soundEventGrenadeExplode = default;
    [SerializeField] private StudioEventEmitter soundEventDamageGround = default;
    [SerializeField] private StudioEventEmitter soundEventDamageStatue = default;
    [SerializeField] private StudioEventEmitter soundEventDamagePlatform = default;

    // State
    public bool IsAISimulationActive { get; set; }
    public float RadiusMax { get; private set; }
    public int ExplosionUniqueIdentifier { get; protected set; }
    public float WeaponAngle { get; private set; }
    public float WeaponPower { get; private set; }
    public bool WeaponIsFacingRight { get; private set; }
    public Weapon Weapon { get; private set; }

    private const float destroyAfterTime = 2f;

    #endregion

    protected override void Awake()
    {
        base.Awake();
        // This is used by object that have multiple colliders and would get the same explosion multiple times
        ExplosionUniqueIdentifier = Basics.RandomIdentifier();
        RoundLockIdentifier = ExplosionUniqueIdentifier;
    }

    public void Init(Explosion explosion)
    {
        InitSimulable(explosion, 1f, null, 0f, false, 0f);
    }

    public void InitSimulable(
        Explosion explosion,
        float damageFactor,
        Weapon weapon,
        float angle,
        bool isFacingRight,
        float power
    )
    {
        // Log.Message("ExplosionController", "Init explosion, IsAISimulationActive: " + IsAISimulationActive);
        if (!IsAISimulationActive) {
            GameManager.Instance.RoundLock.LockChainReaction(this);
        }

        // This is only for simulation results later on
        WeaponAngle = angle;
        WeaponPower = power;
        WeaponIsFacingRight = isFacingRight;
        Weapon = weapon;

        RadiusMax = explosion.Radius;

        List<Collider2D> colliders = new List<Collider2D>();
        if (IsAISimulationActive) {
            GameManager.Instance.AIPhysicsScene
                .OverlapCircle(transform.position, explosion.Radius, new ContactFilter2D().NoFilter(), colliders);
        } else {
            Physics2D
                .OverlapCircle(transform.position, explosion.Radius, new ContactFilter2D().NoFilter(), colliders);
        }

        var tags = new List<string>();
        foreach (Collider2D collider in colliders) {
            if (collider.CompareTag(Config.TagSoundCharacter)) continue;
            ExplosionReceiver explosionReceiver = collider.GetComponentIncludingParent<ExplosionReceiver>();
            if (explosionReceiver == null) continue;

            // Log.Message("ExplosionController", "Collided with valid ExplosionReceiver");
            Vector2 explosionOrigin = transform.position;
            // Get a better object origin by using the center of the collider's bound
            Vector2 objectOrigin = collider.bounds.center;
            // Most of the time contactPoint == explosionOrigin
            // it means that the explosionReceiver is the one that triggered the explosionController creation
            Vector2 contactPoint = collider.ClosestPoint(explosionOrigin);
            // Direction is defined by the direction between contactPoint and objectOrigin
            Vector2 forceDirection = Basics.VectorBetween(contactPoint, objectOrigin).normalized;

            float damages = explosion.Damage;
            Vector2 force = forceDirection * explosion.Force;

            if (contactPoint != explosionOrigin) {
                // Magnitude is defined by the distance between explosionOrigin and contactPoint on the explosion radius
                float magnitude = 1 - (Basics.VectorBetween(explosionOrigin, contactPoint).magnitude / explosion.Radius);
                damages = magnitude * explosion.Damage;
                force = forceDirection * magnitude * explosion.Force;
            }

            damages *= damageFactor;
            if (damages < 0) {
                damages = 0;
            }

            // Debugging.DrawCircle(explosionOrigin, explosion.Radius, Color.red, 5);
            // Debugging.DrawLine(explosionOrigin, contactPoint, Color.cyan, duration: 5);
            // Debugging.DrawLine(contactPoint, objectOrigin, Color.blue, duration: 5);
            // Debugging.DrawLine(objectOrigin, objectOrigin + force, Color.red, duration: 5);

            explosionReceiver.OnReceiveExplosion(this, force, (int)damages);
            if (!IsAISimulationActive) {
                tags.Add(collider.tag);
            }
        }

        Init_Common(explosion, tags);
        NetworkRecordSnapshot(Method_ExplosionInit_Common, explosion, tags);

        StartCoroutine(DestoryAfter(destroyAfterTime));
    }

    [StreamPlay(Method_ExplosionInit_Common)]
    protected void Init_Common(Explosion explosion, List<string> tags)
    {
        if (IsAISimulationActive) return;
        InitExplosion(explosion);
        SoundsAndAnimations(tags);
    }

    private void InitExplosion(Explosion explosion)
    {
        if (explosion.WithExplosionAnimation) {
            SFXInterface sfx = SFXManager.Instance.GetSFX(explosion.SFXType, transform.position);
            sfx.Init();
            sfx.transform.localScale *= explosion.Radius / 7f /* Bazooka/Grenade/Default radius */;
        }

        switch (explosion.Type) {
            case Explosion.TypeEnum.Mine: soundEventMineExplode.Play(); break;
            case Explosion.TypeEnum.Bazooka: soundEventBazookaExplode.Play(); break;
            case Explosion.TypeEnum.Grenade: soundEventGrenadeExplode.Play(); break;
            default: soundEventDefaultExplode.Play(); break;
        }

        CameraManager.Instance.ShakeCamera(4, 0.4f);
    }

    private void SoundsAndAnimations(List<string> tags)
    {
        foreach (string tag in tags) {
            if (tag.Equals(Config.TagTypePlatform)) {
                soundEventDamagePlatform.Play();
                rockFragmentsParticleSystem.Play();
            } else if (tag.Equals(Config.TagTypeStatue)) {
                soundEventDamageStatue.Play();
                rockFragmentsParticleSystem.Play();
            } else if (tag.Equals(Config.TagTypeGround)) {
                soundEventDamageGround.Play();
                rockFragmentsParticleSystem.Play();
            }
        }
    }

    private IEnumerator DestoryAfter(float d)
    {
        yield return new WaitForSeconds(d);
        try {
            if (!IsAISimulationActive) {
                CameraManager.Instance.ReleaseFollow(transform);
                GameManager.Instance.RoundLock.UnlockChainReaction(this);
            }
            NetworkDestroy();
        } catch (System.Exception e) {
            Log.Critical("ExplosionController", "Exception in DestroyAfter " + e);
        }
    }
}
