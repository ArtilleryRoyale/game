using BoundaryTracing;
using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CC
{
    public class PathsOperationException : Exception
    {
        public PathsOperationException(string message) : base(message) { }
    }

    public class Paths : List<Path>
    {
        /// <summary>
        /// Given a PolygonCollider2D, returns the corresponding Paths
        /// </summary>
        public static Paths GetPaths(PolygonCollider2D collider)
        {
            var paths = new Paths();
            for (int i = 0, max = collider.pathCount; i < max; i++) {
                paths.Add(Path.GetPath(collider.GetPath(i)));
            }
            return paths;
        }

        public static Paths GetPaths(int[,] bitmap, int factor)
        {
            var paths = new Paths();
            var boundaryTracingPixels = new Pixels(bitmap);
            List<List<Vector2Int>> boundaries = BoundaryTracing.BoundaryTracing.Find(boundaryTracingPixels, factor);
            foreach (var points in boundaries) {
                paths.Add(Path.GetPath(points));
            }
            return paths;
        }

        public static Paths GetPaths(Path onePath)
        {
            var paths = new Paths();
            paths.Add(onePath);
            return paths;
        }

        public Paths Simplify()
        {
            return ClipperLib.Clipper.SimplifyPolygons(this);
        }

        public Path GetLongest()
        {
            int size = this[0].Count;
            Path longuest = this[0];
            foreach (var path in this) {
                if (path.Count > size) {
                    size = path.Count;
                    longuest = path;
                }
            }
            return longuest;
        }

        /// <summary>
        /// Apply this path to the given collider
        /// </summary>
        public void ApplyToCollider(PolygonCollider2D collider)
        {
            collider.pathCount = Count;
            for (int i = 0, max = Count; i < max; i++) {
                var path = this[i];
                collider.SetPath(i, path.ToListVector2());
            }
        }

        /// <summary>
        /// Return new Paths from those paths + the addee path
        /// </summary>
        public Paths AddPath(Path addee)
        {
            var clipper = new Clipper();
            clipper.StrictlySimple = true;
            clipper.AddPaths(this, PolyType.ptSubject, true);
            clipper.AddPath(addee, PolyType.ptClip, true);

            Paths resultPaths = new Paths();
            if (!clipper.Execute(ClipType.ctUnion, resultPaths, PolyFillType.pftNonZero, PolyFillType.pftNonZero)) {
                throw new PathsOperationException("Adding Paths failed!");
            }

            return resultPaths;
        }

        public Paths AddPaths(Paths addee)
        {
            Paths resultPaths = this;
            foreach (var path in addee) {
                resultPaths = resultPaths.AddPath(path);
            }
            return resultPaths;
        }

        /// <summary>
        /// Return new Paths from those paths - the sub path
        /// </summary>
        public Paths SubstractPath(Path sub)
        {
            var clipper = new Clipper();
            clipper.StrictlySimple = true;
            clipper.AddPaths(this, PolyType.ptSubject, true);
            clipper.AddPath(sub, PolyType.ptClip, true);

            Paths resultPaths = new Paths();
            if (!clipper.Execute(ClipType.ctDifference, resultPaths)) {
                throw new PathsOperationException("Substracting Paths failed!");
            }

            return resultPaths;
        }

        public Paths SubstractPaths(Paths subs)
        {
            Paths resultPaths = this;
            foreach (var path in subs) {
                resultPaths = resultPaths.SubstractPath(path);
            }
            return resultPaths;
        }

        public int HeightAmplitude()
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            var points = ToListListVector2().SelectMany(i => i).ToList();

            foreach (var c in points) {
                if (c.y < min) {
                    min = c.y;
                }
                if (c.y > max) {
                    max = c.y;
                }
            }

            return (int)Mathf.Max(max - min, 0);
        }

        public int WidthAmplitude()
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            List<Vector2> points = ToListListVector2().SelectMany(i => i).ToList();

            foreach (var c in points) {
                if (c.x < min) {
                    min = c.x;
                }
                if (c.x > max) {
                    max = c.x;
                }
            }

            return (int)Mathf.Max(max - min, 0);
        }

        public List<List<Vector2>> ToListListVector2()
        {
            return this.Select(i => i.ToListVector2()).ToList();
        }

        #region Debug

        public static void DebugDraw(Paths paths, Color color, float duration)
        {
            foreach (var path in paths) {
                Path.DebugDraw(path, color, duration);
            }
        }

        public override string ToString()
        {
            return "Paths with " + Count + " paths:\n" + Jrmgx.Helpers.Debugging.IEnumerableToString(this);
        }

        #endregion
    }
}
