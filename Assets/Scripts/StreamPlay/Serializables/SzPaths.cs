using System;
using CC;
using ClipperLib;

[Serializable]
public class SzPaths
{
    public IntPoint[][] p;

    public SzPaths() { }

    public SzPaths(Paths paths)
    {
        p = new IntPoint[paths.Count][];
        for (int i = 0; i < paths.Count; i++) {
            Path path = paths[i];
            p[i] = path.ToArray();
        }
    }

    public Paths Value()
    {
        var paths = new Paths();
        foreach (var onePath in p) {
            var path = new Path();
            foreach (var onPoint in onePath) {
                path.Add(onPoint);
            }
            paths.Add(path);
        }
        return paths;
    }
}

public static class SzPathsExtension
{
    public static SzPaths SzValue(this Paths q)
    {
        return new SzPaths(q);
    }
}
