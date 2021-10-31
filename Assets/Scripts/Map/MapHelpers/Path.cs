using ClipperLib;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace CC
{
    [Serializable]
    public class Path : List<IntPoint>
    {
        // This is used to get a better precision with IntPoint regarding that we work with floats
        // we multiply our float values with Multiplyer, use the algo, and then divide back to floats
        // It's important to use the exported values of those classes (Path and Paths)
        // and not the intermediate methods
        private const float Multiplyer = 1000f;

        /// <summary>
        /// Given an array of Vector2, returns the corresponding Path object
        /// </summary>
        public static Path GetPath(Vector2[] points)
        {
            return GetPath(new List<Vector2>(points));
        }

        /// <summary>
        /// Given a list of Vector2, returns the corresponding Path object
        /// </summary>
        public static Path GetPath(List<Vector2> points)
        {
            var path = new Path();
            foreach (var p in points) {
                path.Add(new IntPoint(p.x * Multiplyer, p.y * Multiplyer));
            }
            return path;
        }

        /// Used in `Paths`
        public static Path GetPath(List<Vector2Int> points)
        {
            var path = new Path();
            foreach (var p in points) {
                path.Add(new IntPoint(p.x * Multiplyer, p.y * Multiplyer));
            }
            return path;
        }

        /// <summary>
        /// Return a path representing a rectangle from origin (bottom left)
        /// of size with and height clockwise
        /// </summary>
        public static Path MakeRectPath(Vector2 origin, float width, float height)
        {
            return new Path {
                new IntPoint(origin.x * Multiplyer, origin.y * Multiplyer),
                new IntPoint(origin.x * Multiplyer, (origin.y + height) * Multiplyer),
                new IntPoint((origin.x + width) * Multiplyer, (origin.y + height) * Multiplyer),
                new IntPoint((origin.x + width) * Multiplyer, origin.y * Multiplyer)
            };
        }

        /// <summary>
        /// Return a path representing a circle from origin (center)
        /// with given radius and given segments
        /// </summary>
        public static Path MakeCirclePath(Vector2 origin, float radius, int segments)
        {
            var path = new Path();
            float angle = 0f;
            Vector2 thisPoint = Vector2.zero;

            for (int i = 0; i < segments + 1; i++) {
                thisPoint.x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                thisPoint.y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                if (i > 0) {
                    var current = thisPoint + origin;
                    path.Add(new IntPoint(current.x * Multiplyer, current.y * Multiplyer));
                }

                angle += 360f / segments;
            }

            return path;
        }

        public Path Simplify()
        {
            return ClipperLib.Clipper.SimplifyPolygon(this)[0];
        }

        public bool Contains(Path other)
        {
            foreach (var p in other) {
                if (Clipper.PointInPolygon(p, this) == 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Return the current Path height amplitude
        /// Note: Multiplyer factor has been de-multiplied at this point
        /// </summary>
        public int HeightAmplitude()
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (var c in this) {
                if (c.Y < min) {
                    min = c.Y;
                }
                if (c.Y > max) {
                    max = c.Y;
                }
            }

            return (int)(Mathf.Max(max - min, 0) / Multiplyer);
        }

        /// <summary>
        /// Return the current Path width amplitude
        /// Note: Multiplyer factor has been de-multiplied at this point
        /// </summary>
        public int WidthAmplitude()
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (var c in this) {
                if (c.X < min) {
                    min = c.X;
                }
                if (c.X > max) {
                    max = c.X;
                }
            }

            return (int)(Mathf.Max(max - min, 0) / Multiplyer);
        }

        /// <summary>
        /// Return the current Path to a list of Vector2 points
        /// Note: Multiplyer factor has been de-multiplied at this point
        /// </summary>
        public List<Vector2> ToListVector2()
        {
            var vectors = new List<Vector2>();
            foreach (var p in this) {
                vectors.Add(new Vector2(p.X / Multiplyer, p.Y / Multiplyer));
            }
            return vectors;
        }

        #region Debug

        public static void DebugDraw(Path path, Color color, float duration)
        {
            for (int i = 0, max = path.Count; i < max; i++) {
                Vector3 prev;
                if (i == 0) {
                    prev = new Vector3(path[max - 1].X / Multiplyer, path[max - 1].Y / Multiplyer);
                } else {
                    prev = new Vector3(path[i - 1].X / Multiplyer, path[i - 1].Y / Multiplyer);
                }
                Vector3 current = new Vector3(path[i].X / Multiplyer, path[i].Y / Multiplyer);
                Debug.DrawLine(prev, current, color, duration);
            }
        }

        public override string ToString()
        {
            return "Path with " + Count + " points:\n" + Jrmgx.Helpers.Debugging.IEnumerableToString(this);
        }

        #endregion
    }
}
