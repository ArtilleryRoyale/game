using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Jrmgx.Helpers
{
    public static class Basics
    {
        public static int RandomIdentifier()
        {
            return IdentifierFrom(Guid.NewGuid().ToString());
        }

        public static int IdentifierFrom(string data)
        {
            byte[] md5 = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToInt32(md5, 0);
        }

        /// <summary>
        /// Merge two dictionaries in a new one
        /// If duplicate keys, the one from the last overrides the first
        /// </summary>
        public static Dictionary<T, U> Merge<T, U>(Dictionary<T, U> a, Dictionary<T, U> b)
        {
            var d = new Dictionary<T, U>();
            foreach (var kv in a) {
                d[kv.Key] = kv.Value;
            }
            foreach (var kv in b) {
                d[kv.Key] = kv.Value;
            }
            return d;
        }

        public static Vector2 VectorRotate(Vector2 vector, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = vector.x;
            float ty = vector.y;
            vector.x = (cos * tx) - (sin * ty);
            vector.y = (sin * tx) + (cos * ty);

            return vector;
        }

        public static Vector2 PointRotateAroundPivot(Vector2 point, Vector2 pivot, float degrees)
        {
            var direction = point - pivot;
            var rotated = VectorRotate(direction, degrees);
            return rotated + pivot;
        }

        public static bool IsPolygonClockwise(Vector2[] polygon)
        {
            if (polygon.Length < 3) throw new Exception("A polygon needs at least 3 points");
            var a = polygon[0];
            var b = polygon[1];
            var c = polygon[2];
            return (b.x * c.y + a.x * b.y + a.y * c.x) - (a.y * b.x + b.y * c.x + a.x * c.y) < 0;
        }

        public static bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
        {
            return Vector2.Distance(point, center) < radius;
        }

        /// <summary>
        /// Gets a vector that points from source to target
        /// Get the direction of this vector via .normalized
        /// Get the magnitude/length of this vector via .magnitude
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns>Vector2 that points from source to target</returns>
        public static Vector2 VectorBetween(Vector2 source, Vector2 target)
        {
            return target - source;
        }

        public static Vector3 VectorAbs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        /// <summary>
        /// Given a position and a radius, returns raycast hits for each colliders crossing that circle
        /// Rays are sent from top to bottom with a length of <param>length</param>
        /// </summary>
        public static List<RaycastHit2D> CircleCheckRaycast(
            Vector3 position,
            float radius,
            float length,
            int layerMask,
            int segments = 64
        )
        {
            var results = new List<RaycastHit2D>();
            float angle = 0f;
            Quaternion rot = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Vector3 lastPoint = Vector3.zero;
            Vector3 thisPoint = Vector3.zero;

            for (int i = 0; i < segments + 1; i++) {
                thisPoint.x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                thisPoint.y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                if (i > 0) {
                    Vector2 start = rot * lastPoint + position;
                    var hits = Physics2D.RaycastAll(start, Vector2.down, length, layerMask);
                    // Keep only hits that crossed the collider
                    foreach (RaycastHit2D hit in hits) {
                        if (hit.point != start) {
                            results.Add(hit);
                        }
                    }
                }

                lastPoint = thisPoint;
                angle += 360f / segments;
            }

            return results;
        }

        public static Action<Task> Nothing = /*(task) => { };
        public static Action<Task> BasicErrorHandling =*/
        (task) => {
            //Debug.LogWarning("Continue With Helper.Nothing used, consider awaiting if possible");
            if (task.IsFaulted) {
                Debug.LogError("Faulty task in BasicErrorHandling");
                Debug.LogException(task.Exception);
            }
        };

        public static CancellationToken TimeoutTask(int ms)
        {
            var ctx = new CancellationTokenSource();
            ctx.CancelAfterSlim(ms);
            return ctx.Token;
        }

        public static bool IsSamePosition(Vector3 position1, Vector3 position2, float epsilon = .1f)
        {
            return
                Math.Abs(position1.x - position2.x) < epsilon &&
                Math.Abs(position1.y - position2.y) < epsilon &&
                Math.Abs(position1.z - position2.z) < epsilon
            ;
        }

        public static UniTask CancelOnDestroy(this UniTask task, Component gameObject)
        {
            return task.AttachExternalCancellation(gameObject.GetCancellationTokenOnDestroy());
        }

        public static UniTask<T> CancelOnDestroy<T>(this UniTask<T> task, Component gameObject)
        {
            return task.AttachExternalCancellation(gameObject.GetCancellationTokenOnDestroy());
        }

        public static bool IsSameFloat(float float1, float float2, float epsilon = .1f)
        {
            return Math.Abs(float1 - float2) < epsilon;
        }

        /// <summary>
        /// Given an index and a length
        /// return the next valid index for that length (clamped)
        /// if looping is true it goes back to zero/length and vice versa
        /// </summary>
        public static int NextIndex(int current, int length, bool looping = true)
        {
            int index = current + 1;
            if (index >= length) {
                return looping ? 0 : length - 1;
            }

            if (index < 0) {
                return looping ? length - 1 : 0;
            }

            return index;
        }

        public static int PreviousIndex(int current, int length, bool looping = true)
        {
            int index = current - 1;
            if (index >= length) {
                return looping ? 0 : length - 1;
            }

            if (index < 0) {
                return looping ? length - 1 : 0;
            }

            return index;
        }

        /// <summary>
        /// Given an index and a length
        /// return the a valid index for that length (clamped)
        /// if looping is true it goes back to zero/length and vice versa
        /// </summary>
        public static int ClampIndex(int current, int length, bool looping = true)
        {
            if (current >= length) {
                return looping ? 0 : length - 1;
            }

            if (current < 0) {
                return looping ? length - 1 : 0;
            }

            return current;
        }

        public static bool IsAxisActive(float axisValue, float secondaryAxisValue = 0f)
        {
            return axisValue > 0.1 || axisValue < -0.1 || secondaryAxisValue > 0.1 || secondaryAxisValue < -0.1;
        }

        public static Vector3 RoundVector(Vector3 v)
        {
            return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
        }

        public static int Timestamp => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        /// <summary>
        /// Take a "screenshot" of a camera's Render Texture
        /// Note: the camera must have a render texture set
        /// </summary>
        public static Texture2D RenderTextureTexture(Camera camera)
        {
            // The Render Texture in RenderTexture.active is the one that will be read by ReadPixels
            var originalRenderTexture = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            camera.Render();

            // Make a new texture and read the active Render Texture into it
            Texture2D texture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
            texture.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            texture.Apply();

            // Replace the original active Render Texture
            RenderTexture.active = originalRenderTexture;
            return texture;
        }

        /// <summary>
        /// Save a render texture to a PNG
        /// destination would probably be like: Application.persistentDataPath + "/capture_" + Basics.Timestamp + ".png"
        /// Note: if you want transparent render, the render texture must have 32bit
        /// </summary>
        public static void RenderTexturePNG(RenderTexture renderTexture, string destination)
        {
            var currentRenderTexture = RenderTexture.active;

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(renderTexture.width, renderTexture.height);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            File.WriteAllBytes(destination, texture.EncodeToPNG());

            RenderTexture.active = currentRenderTexture;
        }

        public static string ListToString<T>(IList<T> list)
        {
            if (list == null) {
                return "NULL";
            }

            string data = "";
            for (int c = 0, max = list.Count; c < max; c++) {
                data += c + ": " + list[c].ToString() + "\n";
            }
            return data;
        }

        public static string DictionaryToString<K, V>(IDictionary<K, V> dictionary)
        {
            if (dictionary == null) {
                return "NULL";
            }

            string data = "";
            foreach (KeyValuePair<K, V> keyValue in dictionary) {
                data += keyValue.Key.ToString() + ": " + keyValue.Value.ToString() + "\n";
            }
            return data;
        }

        /// <summary>
        /// Loads the texture from an URL
        /// How to use it? Either:
        ///  - you are out of a coroutine, then use the callback param
        ///  - you already are in a coroutine/IEnumerator, then
        /// IEnumerator loader = LoadTextureFromFile();
        /// yield return loader;
        /// Texture2D result = loader.Current as Texture2D;
        /// </summary>
        /// <returns>The texture from file coroutine.</returns>
        /// <param name="url">URL</param>
        public static IEnumerator LoadTextureFromUrl(string url, Action<Texture2D> callback = null)
        {
            UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(url);
            yield return unityWebRequest.SendWebRequest();
            Texture2D texture = DownloadHandlerTexture.GetContent(unityWebRequest);
            if (callback != null) {
                callback(texture);
            } else {
                yield return texture;
            }
        }
    }
}
