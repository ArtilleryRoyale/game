using System;
using UnityEngine;

[Serializable]
public class SzExplosion
{
    public Explosion Value()
    {
        var e = ScriptableObject.CreateInstance<Explosion>();
        e.Type = (Explosion.TypeEnum) t;
        e.Force = f;
        e.Damage = d;
        e.Radius = r;
        e.WithExplosionAnimation = a;
        e.SFXType = (SFXManager.SFXType) s;
        return e;
    }

    public int t;
    public float f;
    public float d;
    public float r;
    public bool a;
    public int s;

    public SzExplosion() { }

    public SzExplosion(Explosion e)
    {
        this.t = (int) e.Type;
        this.f = e.Force;
        this.d = e.Damage;
        this.r = e.Radius;
        this.a = e.WithExplosionAnimation;
        this.s = (int) e.SFXType;
    }
}

public static class SzExplosionExtension
{
    public static SzExplosion SzValue(this Explosion e)
    {
        return new SzExplosion(e);
    }
}
