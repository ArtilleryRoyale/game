using System.Collections.Generic;
using UnityEngine;
using Jrmgx.Helpers;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Linq;
using System;
#if CC_DEBUG
using System.Runtime.CompilerServices;
#endif

namespace CC
{
    /// <summary>
    /// The Round Lock mechanism is used to prevent starting the next round if something is still happening
    /// Mostly used for chain reactions
    ///
    /// It is used like that:
    ///     Note: The object should implement UniqueIdentifier (and thus have an unique identifier)
    ///     When there is a long lasting event for a given object,
    ///     like a grenade going to explode in 5 sec => we call LockChainReaction(object)
    ///     and when that grenade is over (exploded) => we call UnlockChainReaction(object)
    ///
    /// The exact same mechanism is used for physics moves
    ///
    /// This class also implement a Coroutine Queue for event that should happen in sequence
    /// ie: a character is dead, we queue the corresponding coroutine, so if multiple die
    /// they do in a sequence instead of all in the same time
    /// </summary>
    public class RoundLock : MonoBehaviour, RoundLockIdentifier
    {

        public const int IdentifierRoundLock = 0;
        public const int IdentifierRoundController = 1;
        public const int IdentifierMapController = 2;

        private readonly Dictionary<int, bool> chainReactions = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> physicsMoves = new Dictionary<int, bool>();
        private readonly List<Func<UniTask>> taskQueue = new List<Func<UniTask>>();

        public int RoundLockIdentifier => IdentifierRoundLock;

        public static int NewRoundLockIdentifier()
        {
            return Basics.RandomIdentifier();
        }

#if CC_DEBUG

        public void EnqueueTaskInSequence(
            Func<UniTask> task,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "",
            [CallerLineNumber] int callerLineNumber = 0
        )
        {
            LockChainReaction(this);
            taskQueue.Add(task);
            callerFilePath = callerFilePath.Split('/').Last();
            // Log.Message("RoundLock", "Queing Task, now have: " + taskQueue.Count + " tasks (" + callerFilePath + ":" + callerLineNumber + " " + callerMemberName + "())");
        }

#else

        public void EnqueueTaskInSequence(Func<UniTask> task)
        {
            LockChainReaction(this);
            taskQueue.Add(task);
        }

#endif

        /// <summary>
        /// Note: this awaits forever
        /// </summary>
        public async UniTask DequeueTaskInSequence()
        {
            while (true) {
                if (taskQueue.Count > 0) {
                    LockChainReaction(this);
                    Func<UniTask> task = taskQueue[0];
                    taskQueue.RemoveAt(0);
#if CC_EXTRA_CARE
try {
#endif
                    await task().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
                } else {
                    UnlockChainReaction(this);
                    await UniTask.WaitForEndOfFrame(this.GetCancellationTokenOnDestroy());
                }
            }
        }

        public void LockChainReaction(RoundLockIdentifier source)
        {
            chainReactions[source.RoundLockIdentifier] = true;
        }

        public void UnlockChainReaction(RoundLockIdentifier source)
        {
            if (!chainReactions.ContainsKey(source.RoundLockIdentifier)) return;
            chainReactions.Remove(source.RoundLockIdentifier);
        }

        public void LockPhysicsMove(RoundLockIdentifier source)
        {
            physicsMoves[source.RoundLockIdentifier] = true;
        }

        public void UnlockPhysicsMove(RoundLockIdentifier source)
        {
            if (!physicsMoves.ContainsKey(source.RoundLockIdentifier)) return;
            physicsMoves.Remove(source.RoundLockIdentifier);
        }

#if CC_DEBUG
        private Coroutine TooLongHandler;

        public async UniTask WaitWhileIsLocked()
        {
            this.StopCoroutineNoFail(TooLongHandler);
            TooLongHandler = this.StartCoroutineNoFail(TooLong(3));
            await UniTask.WaitWhile(IsLocked).CancelOnDestroy(this);
            this.StopCoroutineNoFail(TooLongHandler);
        }

        private IEnumerator TooLong(int time)
        {
            yield return new WaitForSeconds(time);
            Debug.LogWarning("WaitWhileIsLocked > " + time + " sec, dumping for debug");
            Dump();
        }

        private void Dump()
        {
            var references = FindObjectsOfType<UnityEngine.Object>().OfType<RoundLockIdentifier>().ToList();
            Debug.LogWarning("Identifiers: " + references.Aggregate("", (prev, r) => { return prev + r.name + " => " + r.RoundLockIdentifier + "\n"; }));
            Debug.LogWarning("Chain Reactions: " + Basics.DictionaryToString(chainReactions));
            Debug.LogWarning("Physics Moves: " + Basics.DictionaryToString(physicsMoves));
        }
#else
        public UniTask WaitWhileIsLocked()
        {
            return UniTask.WaitWhile(IsLocked);
        }
#endif

        public bool IsLocked()
        {
            foreach (KeyValuePair<int, bool> chainReaction in chainReactions) {
                if (chainReaction.Value) return true;
            }
            foreach (KeyValuePair<int, bool> physicsMove in physicsMoves) {
                if (physicsMove.Value) return true;
            }
            return false;
        }

        public void ForceReset()
        {
            chainReactions.Clear();
            physicsMoves.Clear();
            taskQueue.Clear();
        }
    }
}
