using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Jrmgx.Helpers
{
    public static class Extensions
    {
        public static void DestroyOnLoad(this UnityEngine.Object ojb, Component component)
        {
            SceneManager.MoveGameObjectToScene(component.gameObject, SceneManager.GetActiveScene());
        }

        /// From: https://forum.unity.com/threads/actions-with-modifiers.764795/
        public static float ReadValueBugFixFloat(this InputAction.CallbackContext context)
        {
            unsafe {
                float value;
                void* ptr = &value;
                context.ReadValue(ptr, sizeof(float));
                return value;
            }
        }

        public static Vector2 ReadValueBugFixVector2(this InputAction.CallbackContext context)
        {
            unsafe {
                Vector2 value;
                void* ptr = &value;
                context.ReadValue(ptr, sizeof(Vector2));
                return value;
            }
        }

        /// <summary>
        /// Get the translated and scaled path that corespond to that collider.
        /// Translated means: get the collider's position relative to its parent (it is probably what you want)
        /// Scaled means: relative to itself as collider paths values are not scaled
        /// @deprecated
        /// </summary>
        /// Note: this code has some limitations, for example it does not take into account rotation
        /// It's better to use PathBerserker2d.ColliderConverter instead
        /// <returns>Corresponding points</returns>
        public static Vector2[] GetTranslatedScaledPath(this PolygonCollider2D collider, int index)
        {
            Vector2 scale = collider.transform.localScale;
            Vector2 position = collider.transform.position;
            var points = collider.GetPath(index);
            for (int i = 0, max = points.Length; i < max; i++) {
                points[i] = points[i] * scale + position;
            }
            return points;
        }

        /// <summary>
        /// Populate the specified scrollRect with prefab instance for each enumerable
        /// You must specify the height of the prefab
        /// You can give a callback where the prefab instance and the enumerable item will be passed to
        /// </summary>
        /// <returns>The calculated size of the scrollRect (it has already been applied to it)</returns>
        public static float Populate<T, U>(
            this ScrollRect scrollRect,
            IEnumerable<U> enumerable,
            T prefab,
            float prefabHeight,
            Action<T, U> Init = null
        ) where T : MonoBehaviour
        {
            float y = 0;
            scrollRect.content.RemoveAllChildren();
            foreach (U item in enumerable) {
                T gameObject = UnityEngine.Object.Instantiate(prefab, scrollRect.content, false);
                Init?.Invoke(gameObject, item);
                gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -y);
                y += prefabHeight;
            }

            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, y);
            return y;
        }

        public static Vector3 WithX(this Vector3 v, float x)
        {
            v.x = x;
            return v;
        }

        public static Vector3 WithX(this Vector2 v, float x)
        {
            var r = (Vector3)v;
            r.x = x;
            return r;
        }

        public static Vector3 WithY(this Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        public static Vector3 WithY(this Vector2 v, float y)
        {
            var r = (Vector3)v;
            r.y = y;
            return r;
        }

        public static Vector3 WithZ(this Vector3 v, float z)
        {
            v.z = z;
            return v;
        }

        public static Vector3 WithZ(this Vector2 v, float z)
        {
            var r = (Vector3)v;
            r.z = z;
            return r;
        }

        public static Color WithA(this Color color, float a)
        {
            return new Color(color.r, color.g, color.b, a);
        }

        public static T GetRandomValue<T>(this IEnumerable<T> list)
        {
            if (list.Count() == 0) return default;
            return list.ElementAt(UnityEngine.Random.Range(0, list.Count()));
        }

        public static V GetFirstValue<T, V>(this Dictionary<T, V> dictionnary)
        {
            foreach (var item in dictionnary) {
                return item.Value;
            }
            return default;
        }

        public static T GetComponentIncludingParent<T>(this Component component)
        {
            T t = component.GetComponent<T>();
            if (t == null) {
                t = component.GetComponentInParent<T>();
            }
            return t;
        }

        public static Transform FindIncludingChildren(this Transform parent, string name)
        {
            var result = parent.Find(name);
            if (result != null) return result;
            foreach (Transform child in parent) {
                result = child.FindIncludingChildren(name);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static Transform RemoveAllChildren(this Transform transform, bool immediate = false)
        {
            if (immediate) {
                for (int i = transform.childCount - 1; i >= 0; i--)
                    GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
            } else {
                for (int i = transform.childCount - 1; i >= 0; i--)
                    GameObject.Destroy(transform.GetChild(i).gameObject);
            }

            return transform;
        }

        public static Coroutine StartCoroutineNoFail(this MonoBehaviour parent, IEnumerator routine)
        {
            if (!parent.isActiveAndEnabled) {
                return null;
            }
            return parent.StartCoroutine(routine);
        }

        public static void StopCoroutineNoFail(this MonoBehaviour parent, Coroutine coroutine)
        {
            if (coroutine == null) {
                return;
            }
            parent.StopCoroutine(coroutine);
        }

        public static Coroutine ExecuteNextUpdate(this MonoBehaviour parent, Action action)
        {
            return parent.StartCoroutine(parent.ExecuteNextUpdateCoroutine(action));
        }

        private static IEnumerator ExecuteNextUpdateCoroutine(this MonoBehaviour parent, Action action)
        {
            yield return new WaitForUpdate();
            action();
        }

        public static Coroutine ExecuteNextFixedUpdate(this MonoBehaviour parent, Action action)
        {
            return parent.StartCoroutine(parent.ExecuteNextFixedUpdateCoroutine(action));
        }

        private static IEnumerator ExecuteNextFixedUpdateCoroutine(this MonoBehaviour parent, Action action)
        {
            yield return new WaitForFixedUpdate();
            action();
        }

        public static Coroutine ExecuteInSecond(this MonoBehaviour parent, float seconds, Action action)
        {
            return parent.StartCoroutine(parent.ExecuteInSecondCoroutine(seconds, action));
        }

        private static IEnumerator ExecuteInSecondCoroutine(this MonoBehaviour parent, float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }
    }
}
