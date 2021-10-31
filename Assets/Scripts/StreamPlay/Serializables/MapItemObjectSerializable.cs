using System;
using UnityEngine;

[Serializable]
public class MapItemObjectSerializable
{
    public int i;
    public float x;
    public float y;
    public bool s;

    public Vector2 Position() => new Vector2(x, y);
    public int Index() => i;
    public bool Symmetry() => s;
    // That same object is used to place the king's throne, we use the symmetry field to know if player one/two
    public bool IsPlayerOne() => s;

    public MapItemObjectSerializable() { }

    public MapItemObjectSerializable(int index, Vector2 position, bool symmetry)
    {
        i = index;
        x = position.x;
        y = position.y;
        s = symmetry;
    }
}
