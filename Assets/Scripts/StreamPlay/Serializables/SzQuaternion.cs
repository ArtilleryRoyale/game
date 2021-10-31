using System;
using UnityEngine;

[Serializable]
public class SzQuaternion
{
    public Quaternion Value() => new Quaternion(x, y, z, w);

    public float x;
    public float y;
    public float z;
    public float w;

    public SzQuaternion() { }

    public SzQuaternion(Quaternion q)
    {
        x = q.x;
        y = q.y;
        z = q.z;
        w = q.w;
    }
}

public static class SzQuaternionExtension
{
    public static SzQuaternion SzValue(this Quaternion q)
    {
        return new SzQuaternion(q);
    }
}
