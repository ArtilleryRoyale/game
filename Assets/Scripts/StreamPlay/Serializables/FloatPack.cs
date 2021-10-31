using System;
using UnityEngine;
using System.Collections.Generic;

namespace CC.StreamPlay
{
    [Serializable]
    public class FloatPack
    {
        #region Fields

        //private const float FLOAT_PRECISION_FACTOR = 1000;

        public float t;
        public int o;
        public List<float> f = new List<float>();

        private int currentIndex = 0;

        #endregion

        public float Time() => t;
        public int NetworkIdentifier() => o;
        public List<float> Data() => f;

        public FloatPack() { }

        public FloatPack(int networkIdentifier, float time)
        {
            t = time;
            o = networkIdentifier;
        }

        public FloatPack AddBool(bool value)
        {
            f.Add(value ? 1 : 0);
            return this;
        }

        public FloatPack AddInt(int value)
        {
            f.Add((int)value);
            return this;
        }

        public FloatPack AddFloat(float value)
        {
            f.Add(value);
            return this;
        }

        public FloatPack AddVector2(Vector2 value)
        {
            f.Add(value.x);
            f.Add(value.y);
            return this;
        }

        public FloatPack AddQuaternion(Quaternion value)
        {
            f.Add(value.x);
            f.Add(value.y);
            f.Add(value.z);
            f.Add(value.w);
            return this;
        }

        public bool NextBool()
        {
            var value = BoolAt(currentIndex);
            currentIndex++;
            return value;
        }

        public int NextInt()
        {
            var value = IntAt(currentIndex);
            currentIndex++;
            return value;
        }

        public float NextFloat()
        {
            var value = FloatAt(currentIndex);
            currentIndex += 1;
            return value;
        }

        public Vector2 NextVector()
        {
            var value = VectorAt(currentIndex);
            currentIndex += 2;
            return value;
        }

        public Quaternion NextQuaternion()
        {
            var value = QuaternionAt(currentIndex);
            currentIndex += 4;
            return value;
        }

        public bool BoolAt(int index)
        {
            return FloatAt(index) != 0;
        }

        public int IntAt(int index)
        {
            return (int)FloatAt(index);
        }

        public bool HasMore()
        {
            return f.Count > currentIndex;
        }

        public float FloatAt(int index)
        {
            if (f.Count <= index) {
#if CC_DEBUG
                Log.Critical("FloatPack", "Invalid index asked: " + index + " on " + this);
#endif
                return 0f;
            }

            return (float)f[index];
        }

        public Vector2 VectorAt(int index)
        {
            return new Vector2(FloatAt(index), FloatAt(index + 1));
        }

        public Quaternion QuaternionAt(int index)
        {
            return new Quaternion(
                FloatAt(index),
                FloatAt(index + 1),
                FloatAt(index + 2),
                FloatAt(index + 3)
            );
        }

        public override string ToString()
        {
#if CC_DEBUG
            return "[" + o + "] time: " + t + " data: " + string.Join(", ", Data());
#endif
            return "[" + o + "] time: " + t;
        }
    }
}
