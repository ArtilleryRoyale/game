using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;

namespace Jrmgx.Helpers
{
    public static class Debugging
    {
        public static void LogTraceFromHere()
        {
            try {
                throw new Exception("Debugging.LogTraceFromHere() called!");
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void ClearLogs()
        {
#if UNITY_EDITOR
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
#endif
        }

        public static void DrawEllipse(Vector3 pos, Vector3 forward, Vector3 up, float radiusX, float radiusY, Color color, float duration = 0, int segments = 64)
        {
            if (segments < 3) {
                segments = 3;
            }
            float angle = 0f;
            Quaternion rot = Quaternion.LookRotation(forward, up);
            Vector3 lastPoint = Vector3.zero;
            Vector3 thisPoint = Vector3.zero;

            for (int i = 0; i < segments + 1; i++) {
                thisPoint.x = Mathf.Sin(Mathf.Deg2Rad * angle) * radiusX;
                thisPoint.y = Mathf.Cos(Mathf.Deg2Rad * angle) * radiusY;

                if (i > 0) {
                    Debug.DrawLine(rot * lastPoint + pos, rot * thisPoint + pos, color, duration);
                }

                lastPoint = thisPoint;
                angle += 360f / segments;
            }
        }

        public static void DrawBox2D(Vector2 center, Vector2 size, Color color, float degrees = 0, float duration = 0)
        {
            Vector2 halfSize = size / 2f;
            DrawPolygon2D(new List<Vector2> {
                Basics.PointRotateAroundPivot(center + new Vector2(-halfSize.x, +halfSize.y), center, degrees),
                Basics.PointRotateAroundPivot(center + new Vector2(+halfSize.x, +halfSize.y), center, degrees),
                Basics.PointRotateAroundPivot(center + new Vector2(+halfSize.x, -halfSize.y), center, degrees),
                Basics.PointRotateAroundPivot(center + new Vector2(-halfSize.x, -halfSize.y), center, degrees),
            }, color, duration);
        }

        public static void DrawPath(IEnumerable<Vector2> points, Color color, float duration = 0)
        {
            for (int i = 0, max = points.Count() - 1; i < max; i++) {
                var current = points.ElementAt(i);
                var next = points.ElementAt(i + 1);
                Debug.DrawLine(current, next, color, duration);
            }
        }

        public static void DrawPolygon2D(IEnumerable<Vector2> points, Color color, float duration = 0)
        {
            for (int i = 0, max = points.Count(); i < max; i++) {
                var current = points.ElementAt(i);
                var next = i == max - 1 ? points.ElementAt(0) : points.ElementAt(i + 1);
                Debug.DrawLine(current, next, color, duration);
            }
        }

        public static void DrawAngle(Vector2 from, Vector2 direction, float angle, Color color, float duration = 0)
        {
            Vector2 to = Basics.PointRotateAroundPivot(from + direction, from, angle);
            DrawCircle(from, 0.1f, color, duration);
            DrawLine(from, from + direction, color, duration);
            DrawLine(from, to, color, duration);
        }

        public static void DrawPolygons2D(IEnumerable<IEnumerable<Vector2>> polygons, Color color, float duration = 0)
        {
            foreach (var polygon in polygons) {
                DrawPolygon2D(polygon, color, duration);
            }
        }

        public static void DrawPoints(IEnumerable<Vector2> points, Color color, float duration = 0)
        {
            DrawPoints(points.Select(v => new Vector3(v.x, v.y)), color, duration);
        }

        public static void DrawPoints(IEnumerable<Vector3> points, Color color, float duration = 0)
        {
            foreach (Vector3 point in points) {
                DrawPoint(point, color, duration);
            }
        }

        public static void DrawRay(Vector3 start, Vector3 direction, Color color, float duration = 0)
        {
            Debug.DrawRay(start, direction, color, duration);
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0)
        {
            Debug.DrawLine(start, end, color, duration);
        }

        public static void DrawPoint(Vector3 point, Color color, float duration = 0)
        {
            DrawCross(point, 1f, color, duration);
        }

        public static void DrawCircle(Vector3 pos, float radius, Color color, float duration = 0, int segments = 64)
        {
            DrawEllipse(pos, Vector3.forward, Vector3.up, radius, radius, color, duration, segments);
        }

        public static void DrawBitmapMap(int[,] map, Vector2 initialPosition)
        {
            for (int y = 0; y < map.GetLength(1); y++) {
                for (int x = 0; x < map.GetLength(0); x++) {
                    // Debugging.DrawBox2D(new Vector2(x, y) + initialPosition, Vector2.one, map[x, y] == 0 ? Color.white : Color.red, 0, 10);
                }
            }
        }

        /// 2D space only
        public static void DrawCross(Vector2 position, float size, Color color, float duration = 0)
        {
            DrawLine(position + Vector2.left * (size / 2f), position + Vector2.right * (size / 2f), color, duration);
            DrawLine(position + Vector2.up * (size / 2f), position + Vector2.down * (size / 2f), color, duration);
        }

        public static void DrawBounds2D(Bounds bounds, Color color, float duration = 0)
        {
            Debug.DrawLine(bounds.min, (Vector2)bounds.min + Vector2.right, color, duration);
            Debug.DrawLine(bounds.min, (Vector2)bounds.min + Vector2.up, color, duration);
            Debug.DrawLine(bounds.max, (Vector2)bounds.max + Vector2.left, color, duration);
            Debug.DrawLine(bounds.max, (Vector2)bounds.max + Vector2.down, color, duration);
        }

        /// <summary>
        /// Printing of strings using Debug.DrawLine
        /// It uses a 16 segment display. If you want more characters, just add them.
        /// From: http://dinodini.weebly.com/debugdrawtext.html
        /// </summary>
        ///  a b
        /// cdefg
        ///  h i
        /// jklmn
        ///  o p
        ///
        /// bits: abcd efgh ijkl mnop
        public static void DrawText(Vector2 position, string text, Color color, float duration = 0, float size = 1f)
        {
            text = " " + text; // Some space for the cross
            DrawCross(position, size, color, duration);
            Vector4[] segments = new Vector4[] {
                new Vector4(-1, 1, 0, 1),    // a
                new Vector4(0, 1, 1, 1),     // b
                new Vector4(-1, 1, -1, 0),   // c
                new Vector4(-1, 1, 0, 0),    // d
                new Vector4(0, 1, 0, 0),     // e
                new Vector4(1, 1, 0, 0),     // f
                new Vector4(1, 1, 1, 0),     // g
                new Vector4(-1, 0, 0, 0),    // h
                new Vector4(0, 0, 1, 0),     // i
                new Vector4(-1, 0, -1, -1),  // j
                new Vector4(0, 0, -1, 0-1),  // k
                new Vector4(0, 0, 0, -1),    // l
                new Vector4(0, 0, 1, -1),    // m
                new Vector4(1, 0, 1, -1),    // n
                new Vector4(-1, -1, 0, -1),  // o
                new Vector4(0, -1, 1, -1),   // p
            };

            int[] segmentsForChars = {
                0xe667, // 0
                0x0604, // 1
                0xc3c3, // 2
                0xc287, // 3
                0x2990, // 4
                0xe187, // 5
                0xe1c7, // 6
                0xc410, // 7
                0xe3c7, // 8
                0xe384, // 9

                0xe3c4, // A
                0xca97, // B
                0xe043, // C
                0xca17, // D
                0xe1c3, // E
                0xe140, // F
                0xe0c7, // G
                0x23c4, // H
                0xc813, // I
                0xc852, // J
                0x2548, // K
                0x2043, // L
                0x3644, // M
                0x324c, // N
                0xe247, // O
                0xe3c0, // P
                0xe24f, // Q
                0xe3c8, // R
                0xd087, // S
                0xc810, // T
                0x2247, // U
                0x2460, // V
                0x226c, // W
                0x1428, // X
                0x1410, // Y
                0xc423, // Z

                0x0000, // space
                0x0002, // .
                0x0100, // -
            };

            for (int characterIndex = 0; characterIndex < text.Length; characterIndex++) {
                int character = text[characterIndex];
                int bitMask = -1;
                if (character >= '0' && character <= '9') {
                    character = character - '0';
                    bitMask = segmentsForChars[character];
                } else if (character >= 'A' && character <= 'Z') {
                    character = character - 'A' + 10;
                    bitMask = segmentsForChars[character];
                } else if (character >= 'a' && character <= 'z') {
                    character = character - 'a' + 10;
                    bitMask = segmentsForChars[character];
                } else if (character == ' ') {
                    bitMask = segmentsForChars[26 + 10];
                } else if (character == '.') {
                    bitMask = segmentsForChars[26 + 11];
                } else if (character == '-') {
                    bitMask = segmentsForChars[26 + 11];
                }

                for (int segment = 0; segment < 16; segment++) {
                    if ((bitMask & (1 << (15 - segment))) != 0) {
                        Debug.DrawLine(
                            new Vector2(
                                characterIndex * 2.0f * size + position.x + segments[segment].x / 2 * size,
                                position.y + segments[segment].y * size
                            ),
                            new Vector2(
                                characterIndex * 2.0f * size + position.x + segments[segment].z / 2 * size,
                                position.y + segments[segment].w * size
                            ),
                            color,
                            duration
                        );
                    }
                }
            }
        }

        public static string IEnumerableToString(IEnumerable enumerable, bool inline = false, int level = 0)
        {
            string text = "";
            int loop = 0;
            foreach (var e in enumerable) {
                loop++;
                if (e.GetType() != typeof(string) && typeof(IEnumerable).IsAssignableFrom(e.GetType())) {
                    level++;
                    text += "(level: " + level + ")\n" + IEnumerableToString(e as IEnumerable, inline, level);
                } else {
                    text += new string('\t', level) + (inline ? "" : " - ") + e + (inline ? " | " : "\n");
                }
            }
            if (loop == 0) {
                text += "Enumerable is empty";
            }

            return text;
        }

        public static void DebugLog(this object parent, string message)
        {
            Debug.Log("[" + parent.GetType().ToString() + "] " + message);
        }

        /// From: https://gist.github.com/SolidAlloy/1f87fe7e529a64ba5dc31d0cc82d9a25
        #region BoxCastDrawer

        /// <summary>
        ///     Visualizes BoxCast with help of debug lines.
        /// </summary>
        /// <param name="hitInfo"> The cast result. </param>
        /// <param name="origin"> The point in 2D space where the box originates. </param>
        /// <param name="size"> The size of the box. </param>
        /// <param name="angle"> The angle of the box (in degrees). </param>
        /// <param name="direction"> A vector representing the direction of the box. </param>
        /// <param name="distance"> The maximum distance over which to cast the box. </param>
        public static void DrawBoxCast2D(
            RaycastHit2D hitInfo,
            Vector2 origin,
            Vector2 size,
            float angle,
            Vector2 direction,
            float distance = Mathf.Infinity,
            float duration = 0
        ) {
            // Set up points to draw the cast.
            Vector2[] originalBox = CreateOriginalBox(origin, size, angle);
            Vector2 distanceVector = GetDistanceVector(distance, direction);
            Vector2[] shiftedBox = CreateShiftedBox(originalBox, distanceVector);

            // Draw the cast.
            Color castColor = hitInfo ? Color.red : Color.green;
            DrawBox(originalBox, castColor, duration);
            DrawBox(shiftedBox, castColor, duration);
            ConnectBoxes(originalBox, shiftedBox, Color.gray, duration);

            if (hitInfo) {
                Debug.DrawLine(hitInfo.point, hitInfo.point + (hitInfo.normal.normalized * 0.2f), Color.yellow, duration);
            }
        }

        private static Vector2[] CreateOriginalBox(Vector2 origin, Vector2 size, float angle)
        {
            float w = size.x * 0.5f;
            float h = size.y * 0.5f;
            Quaternion q = Quaternion.AngleAxis(angle, new Vector3(0, 0, 1));

            var box = new Vector2[4] {
                new Vector2(-w, h),
                new Vector2(w, h),
                new Vector2(w, -h),
                new Vector2(-w, -h),
            };

            for (int i = 0; i < 4; i++) {
                box[i] = (Vector2)(q * box[i]) + origin;
            }

            return box;
        }

        private static Vector2[] CreateShiftedBox(Vector2[] box, Vector2 distance)
        {
            var shiftedBox = new Vector2[4];
            for (int i = 0; i < 4; i++) {
                shiftedBox[i] = box[i] + distance;
            }

            return shiftedBox;
        }

        private static void DrawBox(Vector2[] box, Color color, float duration = 0)
        {
            Debug.DrawLine(box[0], box[1], color, duration);
            Debug.DrawLine(box[1], box[2], color, duration);
            Debug.DrawLine(box[2], box[3], color, duration);
            Debug.DrawLine(box[3], box[0], color, duration);
        }

        private static void ConnectBoxes(Vector2[] firstBox, Vector2[] secondBox, Color color, float duration = 0)
        {
            Debug.DrawLine(firstBox[0], secondBox[0], color, duration);
            Debug.DrawLine(firstBox[1], secondBox[1], color, duration);
            Debug.DrawLine(firstBox[2], secondBox[2], color, duration);
            Debug.DrawLine(firstBox[3], secondBox[3], color, duration);
        }

        private static Vector2 GetDistanceVector(float distance, Vector2 direction)
        {
            if (distance == Mathf.Infinity) {
                // Draw some large distance e.g. 5 scene widths long.
                float sceneWidth = Camera.main.orthographicSize * Camera.main.aspect * 2f;
                distance = sceneWidth * 5f;
            }

            return direction.normalized * distance;
        }

        #endregion
    }
}
