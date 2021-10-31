using System;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CC.StreamPlay
{
    [Serializable]
    public class Snapshot
    {
        #region Fields

        public float t;
        public int o;
        public int m;
        public object[] d;
        public int[] v; // Types

        public float Time() => t;
        public int NetworkIdentifier() => o;
        public int MethodIdentifier() => m;

        private const int TYPE_DEFAULT = 0;
        private const int TYPE_VECTOR2 = 1;
        private const int TYPE_QUATERNION = 2;
        private const int TYPE_COLOR = 3;
        private const int TYPE_PATHS = 4;
        private const int TYPE_LIST_MAP_ITEMS = 5;
        private const int TYPE_INT32 = 6;
        private const int TYPE_LIST_INT32 = 7;
        private const int TYPE_SINGLE = 8;
        private const int TYPE_LIST_SINGLE = 9;
        private const int TYPE_VECTOR3 = 10;
        private const int TYPE_EXPLOSION = 11;
        private const int TYPE_LIST_STRING = 12;
        private const int TYPE_GAMEOPTION = 13;

        #endregion

        public Snapshot() { }

        public Snapshot(int networkIdentifier, int methodIdentifier, float time, params object[] data)
        {
            o = networkIdentifier;
            m = methodIdentifier;
            t = time;
            d = new object[data.Length];
            v = new int[data.Length];

            // Convert to serializable values
            for (int i = 0; i < data.Length; i++) {
                object dataEntry = data[i];
                if (dataEntry is Vector3 v3) {
                    d[i] = v3.SzValue();
                    v[i] = TYPE_VECTOR3;
#if CC_DEBUG
                    Debug.LogWarning(
                        "Serializing a Vector3 in " +
                        GameManager.DebugNetworkIdentifier(o) + "->" + GameManager.DebugMethodIdentifier(m) +
                        ", try to use a Vector2 instead"
                    );
#endif
                } else if (dataEntry is Vector2 v2) {
                    d[i] = v2.SzValue();
                    v[i] = TYPE_VECTOR2;
                } else if (dataEntry is Int32) { // int
                    d[i] = dataEntry;
                    v[i] = TYPE_INT32;
                } else if (dataEntry is Single) { // float
                    d[i] = dataEntry;
                    v[i] = TYPE_SINGLE;
                } else if (dataEntry is List<int>) {
                    d[i] = dataEntry;
                    v[i] = TYPE_LIST_INT32;
                } else if (dataEntry is List<Single>) {
                    d[i] = dataEntry;
                    v[i] = TYPE_LIST_SINGLE;
                } else if (dataEntry is List<string>) {
                    d[i] = dataEntry;
                    v[i] = TYPE_LIST_STRING;
                } else if (dataEntry is Quaternion q) {
                    d[i] = q.SzValue();
                    v[i] = TYPE_QUATERNION;
                } else if (dataEntry is Color c) {
                    d[i] = c.SzValue();
                    v[i] = TYPE_COLOR;
                } else if (dataEntry is Paths p){
                    d[i] = p.SzValue();
                    v[i] = TYPE_PATHS;
                } else if (dataEntry is Explosion e){
                    d[i] = e.SzValue();
                    v[i] = TYPE_EXPLOSION;
                } else if (dataEntry is List<MapItemObjectSerializable>) {
                    d[i] = dataEntry;
                    v[i] = TYPE_LIST_MAP_ITEMS;
                } else if (dataEntry is GameOption gameOption) {
                    d[i] = gameOption.SzValue();
                    v[i] = TYPE_GAMEOPTION;
                } else if (dataEntry.GetType().IsEnum) {
                    throw new Exception("Can not use Enum in Snapshot, use int instead.");
                } else {
                    d[i] = dataEntry;
                    v[i] = TYPE_DEFAULT;
                }
            }
        }

        public object[] Data()
        {
            object[] data = new object[d.Length];

            // Convert from serializable to values or to reference types
            for (int i = 0; i < d.Length; i++) {
                object dataEntry = d[i];
                switch (v[i]) {
                    case TYPE_VECTOR2:
                        data[i] = JsonCast<SzVector>(dataEntry).Value();
                        break;
                    case TYPE_VECTOR3:
                        data[i] = (Vector3) JsonCast<SzVector>(dataEntry).Value();
                        break;
                    case TYPE_QUATERNION:
                        data[i] = JsonCast<SzQuaternion>(dataEntry).Value();
                        break;
                    case TYPE_EXPLOSION:
                        data[i] = JsonCast<SzExplosion>(dataEntry).Value();
                        break;
                    case TYPE_PATHS:
                        data[i] = JsonCast<SzPaths>(dataEntry).Value();
                        break;
                    case TYPE_COLOR:
                        data[i] = JsonCast<SzColor>(dataEntry).Value();
                        break;
                    case TYPE_LIST_MAP_ITEMS:
                        data[i] = JsonCast<List<MapItemObjectSerializable>>(dataEntry);
                        break;
                    case TYPE_INT32:
                        data[i] = JsonCast<int>(dataEntry);
                        break;
                    case TYPE_LIST_STRING:
                        data[i] = JsonCast<List<string>>(dataEntry);
                        break;
                    case TYPE_SINGLE:
                        data[i] = JsonCast<float>(dataEntry);
                        break;
                    case TYPE_LIST_INT32:
                        data[i] = JsonCast<List<int>>(dataEntry);
                        break;
                    case TYPE_LIST_SINGLE:
                        data[i] = JsonCast<List<float>>(dataEntry);
                        break;
                    case TYPE_GAMEOPTION:
                        data[i] = JsonCast<SzGameOptionRef>(dataEntry);
                        break;
                    default:
                        data[i] = dataEntry;
                        break;
                }
            }

            return data;
        }

        private T JsonCast<T>(object objectToCast)
        {
            string json = JsonConvert.SerializeObject(objectToCast);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public override string ToString()
        {
            return
                "[" + o + "]::" +
                GameManager.DebugMethodIdentifier(m) + "(" + string.Join(", ", Data().Select(d => d.GetType().ToString())) + ") " +
                "time: " + t
            ;
        }
    }
}
