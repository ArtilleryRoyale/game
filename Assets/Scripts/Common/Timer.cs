using System;
using System.Collections;
using UnityEngine;

namespace CC
{
    public class Timer
    {
        public const string TIME_OVER = "__TIME_OVER__";

        public readonly IEnumerator Enumerator;

        private TimerHandle timerHandle;
        private Coroutine coroutine;
        private bool forceIsOver;

        public Timer(IEnumerator enumerator)
        {
            Enumerator = enumerator;
        }

        public Timer Start()
        {
            forceIsOver = false;
            coroutine = GetHandle().StartCoroutine(Enumerator);
            return this;
        }

        public Timer Stop()
        {
            if (coroutine != null) {
                try {
                    GetHandle().StopCoroutine(coroutine);
                } catch (Exception e) {
                    Log.Critical("Timer", "Exception while stoping: " + e);
                }
            }
            return this;
        }

        public Timer ForceOver()
        {
            forceIsOver = true;
            Stop();
            return this;
        }

        public bool IsOver()
        {
            return
                (Enumerator.Current is string && ((string)Enumerator.Current).Equals(TIME_OVER)) ||
                forceIsOver;
        }

        private TimerHandle GetHandle()
        {
            if (timerHandle != null) return timerHandle;

            string goName = "TimerHandle";

            GameObject go = GameObject.Find(goName);
            if (go != null) {
                timerHandle = go.GetComponent<TimerHandle>();
                if (timerHandle == null) {
                    timerHandle = go.AddComponent<TimerHandle>();
                }
            } else {
                go = new GameObject(goName);
                timerHandle = go.AddComponent<TimerHandle>();
            }

            return timerHandle;
        }
    }

    public class TimerHandle : MonoBehaviour { }
}
