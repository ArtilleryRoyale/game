using System;
using UnityEngine;

[Serializable]
public class SzColor
{
    public Color Value() => new Color(r, g, b, a);

    public float r;
    public float g;
    public float b;
    public float a;

    public SzColor() { }

    public SzColor(Color c)
    {
        r = c.r;
        g = c.g;
        b = c.b;
        a = c.a;
    }
}

public static class SzColorExtension
{
    public static SzColor SzValue(this Color c)
    {
        return new SzColor(c);
    }
}
