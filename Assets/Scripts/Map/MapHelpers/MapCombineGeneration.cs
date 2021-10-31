using UnityEngine;
using CC;
using Jrmgx.Helpers;
using System;

public static class MapCombineGeneration
{
    public static Paths GenerateMapSqrt3Circle(int width, int height)
    {
        float oneThird = width / 3f;
        float margin = width / 100f * 4f;
        var randomPostion = new Vector2(width * RandomNum.Value, height * RandomNum.Value);

        var square1 = Path.MakeRectPath(new Vector2(0, 0), oneThird, height * RandomNum.Value);
        var square2 = Path.MakeRectPath(new Vector2(oneThird - margin, 0), oneThird, height * RandomNum.Value);
        var square3 = Path.MakeRectPath(new Vector2(oneThird * 2f - margin, 0), oneThird, height * RandomNum.Value);
        var circle = Path.MakeCirclePath(randomPostion, height / 3f * (RandomNum.Value + 0.67f), 3);
        var sq = Path.MakeRectPath(
            new Vector2(oneThird * 2f * RandomNum.Value, height * RandomNum.Value),
            width * RandomNum.Value,
            height * RandomNum.Value
        );
        var paths = new Paths { square1 }.AddPath(square2).AddPath(square3).SubstractPath(sq);
        if (RandomNum.RandomOneOutOf(2)) {
            paths = paths.AddPath(circle);
        } else {
            paths = paths.SubstractPath(circle);
        }

        return paths;
    }

    public static Paths GenerateMapSqrt5Circle2(int width, int height)
    {
        float oneFifth = width / 5f;
        float overlap = oneFifth * 0.1f;
        height = (int)(height * 0.6f);

        Func<Vector2> RandomPostion = () => new Vector2(width * RandomNum.Value, height * RandomNum.Value);
        //Func<float, float> RandomValueClamped = (x) => Mathf.Clamp01(RandomNum.Value * x);

        var square1 = Path.MakeRectPath(new Vector2(0, 0), oneFifth, height * RandomNum.Value);
        var square2 = Path.MakeRectPath(new Vector2((oneFifth - overlap) * 1f, 0), oneFifth, height * RandomNum.Value);
        var square3 = Path.MakeRectPath(new Vector2((oneFifth - overlap) * 2f, 0), oneFifth, height * RandomNum.Value);
        var square4 = Path.MakeRectPath(new Vector2((oneFifth - overlap) * 3f, 0), oneFifth, height * RandomNum.Value);
        var square5 = Path.MakeRectPath(new Vector2((oneFifth - overlap) * 4f, 0), oneFifth, height * RandomNum.Value);
        var triangle = Path.MakeCirclePath(RandomPostion(), height / 3f * (RandomNum.Value + 0.67f), 3);
        var circle = Path.MakeCirclePath(RandomPostion(), height / 3f * (RandomNum.Value + 0.67f), 20);
        var square6 = Path.MakeRectPath(
            new Vector2(oneFifth * 2f * RandomNum.Value, height * RandomNum.Value),
            width * RandomNum.Value,
            height * RandomNum.Value
        );
        var paths = new Paths { square1 }
            .AddPath(square2)
            .AddPath(square3)
            .AddPath(square4)
            .AddPath(square5)
            .SubstractPath(square6)
            .SubstractPath(triangle)
            .SubstractPath(circle)
        ;

        return paths;
    }

}
