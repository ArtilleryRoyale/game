using System;
using UnityEngine;

[Serializable]
public class SzVector
{
    public Vector2 Value() => new Vector2(x, y);

    public float x;
    public float y;

    public SzVector() { }

    public SzVector(Vector3 v)
    {
        x = v.x;
        y = v.y;
    }

    public SzVector(Vector2 v)
    {
        x = v.x;
        y = v.y;
    }
}

public static class SzVectorExtension
{
    public static SzVector SzValue(this Vector3 v)
    {
        return new SzVector(v);
    }

    public static SzVector SzValue(this Vector2 v)
    {
        return new SzVector(v);
    }
}
