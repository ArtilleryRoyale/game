using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using System.Collections;
using Jrmgx.Helpers;

namespace CC.StreamPlay
{
    /// <summary>
    /// Record a sequence of FloatPack and/or Snapshot so they can be stored/transfered
    /// </summary>
    public class Recorder : MonoBehaviour
    {
        #region Fields

        // Internal method ids
        public const int METHOD_DESTROY = -1;

        // Locks
        private readonly object bufferFloatPackLock = new object();
        private readonly object bufferSnapshotLock = new object();
        private readonly object playerMessageLock = new object();

        // Buffers and archived messages
        private readonly List<FloatPack> bufferFloatPacks = new List<FloatPack>();
        private readonly List<Snapshot> bufferSnapshots = new List<Snapshot>();
        private readonly List<string> bufferPlayerMessages = new List<string>();
        private readonly List<FloatPack> archivedFloatPacks = new List<FloatPack>();
        private readonly List<Snapshot> archivedSnapshots = new List<Snapshot>();

        // State
        private float recorderStartTime = 0f;
        private float recorderTime => Time.fixedTime - recorderStartTime;
        private int sequence = 0;
        private bool isSending = false;
        private bool askedToRecord = false;

        private WebSocketConnection connection;

        #endregion

        #region Recorder Methods

        /// <summary>
        /// Reset the Recorder and get it ready for a fresh sequence
        /// The important part here is the `recorderStartTime` reset so the next recorderTime are correct
        /// </summary>
        public void Begin()
        {
            // Log.Message("StreamRecorder", "Begin on Recorder");
            recorderStartTime = Time.fixedTime;
            lock (bufferFloatPackLock) {
                bufferFloatPacks.Clear();
            }
            lock (bufferSnapshotLock) {
                bufferSnapshots.Clear();
            }
            archivedFloatPacks.Clear();
            archivedSnapshots.Clear();
            askedToRecord = true;
        }

        public async UniTask End()
        {
            // Log.Message("StreamRecorder", "End recorder");
            await UniTask.WaitForFixedUpdate(cancellationToken: this.GetCancellationTokenOnDestroy());
            askedToRecord = false;
            // Log.Message("StreamRecorder", "Ended recorder");
        }

#if CC_DEBUG

        private Coroutine TooLongHandler;

        public async UniTask WaitForEnd()
        {
            // Log.Message("StreamRecorder", "End asked on Recorder");
            this.StopCoroutineNoFail(TooLongHandler);
            TooLongHandler = this.StartCoroutineNoFail(TooLong(3));
            await UniTask.WaitWhile(() => bufferFloatPacks.Count > 0 || bufferSnapshots.Count > 0).CancelOnDestroy(this);
            this.StopCoroutineNoFail(TooLongHandler);
            // Log.Message("StreamRecorder", "Actual end on Recorder");
        }

        private IEnumerator TooLong(int time)
        {
            yield return new WaitForSeconds(time);
            Debug.LogWarning("WaitForEnd > " + time + " sec, dumping for debug");
            Debug.LogWarning("FloatPack Buffer: " + Basics.ListToString(bufferFloatPacks));
            Debug.LogWarning("Snapshot Buffer: " + Basics.ListToString(bufferSnapshots));
        }

#else

        public async UniTask WaitForEnd()
        {
            await UniTask.WaitWhile(() => bufferFloatPacks.Count > 0 || bufferSnapshots.Count > 0).CancelOnDestroy(this);
        }

#endif

        /// <summary>
        /// Add a floatpack to the sequence
        /// </summary>
        public void RecordFloatPack(NetworkObject networkObject, params object[] data)
        {
            if (connection == null) return;
            if (!connection.IsOwner) return;

            var floatPack = new FloatPack(networkObject.NetworkIdentifier, recorderTime);
#if CC_DEBUG
            try { if (recorderTime > 45) {
                throw new Exception("Recorder Time is > 45 sec, weird! " + recorderTime);
            }} catch (Exception e) {
                Debug.LogException(e);
                // Log.Message("StreamRecorder", e.Message);
            }
#endif
            // TODO prio 7 at some point this should be moved into FloatPack
            foreach (var d in data) {
                if (d is Vector3 v3) {
                    floatPack.AddVector2(v3);
                } else if (d is Vector2 v) {
                    floatPack.AddVector2(v);
                } else if (d is Quaternion q) {
                    floatPack.AddQuaternion(q);
                } else if (d is float f) {
                    floatPack.AddFloat(f);
                } else if (d is int i) {
                    floatPack.AddInt(i);
                } else if (d is bool b) {
                    floatPack.AddBool(b);
                } else {
                    throw new Exception("Asked to pack non-float values");
                }
            }
            // Log.Message("StreamRecorder", "Record Floatpack " + networkObject.name + floatPack);
            lock (bufferFloatPackLock) {
                bufferFloatPacks.Add(floatPack);
            }
            // Log.Message("StreamRecorderExtra", "Buffer floatpack now has " + bufferFloatPacks.Count + " entries");
        }

        public void RecordSnapshot(NetworkObject networkObject, int methodIdentifier, params object[] data)
        {
            RecordSnapshot(networkObject, methodIdentifier, recorderTime, data);
        }

        public void RecordSnapshot(NetworkObject networkObject, int methodIdentifier, float dt, params object[] data)
        {
            if (connection == null) return;
            if (!connection.IsOwner) return;

            var snapshot = new Snapshot(networkObject.NetworkIdentifier, methodIdentifier, dt, data);
#if CC_DEBUG
            try { if (dt > 45) {
                throw new Exception("Recorder Time is > 45 sec, weird! " + recorderTime);
            }} catch (Exception e) {
                Debug.LogException(e);
                // Log.Message("StreamRecorder", e.Message);
            }
#endif
            // Log.Message("StreamRecorder", "Record Snapshot " + networkObject.name + snapshot);
            lock (bufferSnapshotLock) {
                bufferSnapshots.Add(snapshot);
            }
            // Log.Message("StreamRecorderExtra", "Buffer snapshot now has " + bufferSnapshots.Count + " entries");
        }

        #endregion

        #region Connection

        public async UniTask LogMarker()
        {
            if (connection == null) return;
            if (!connection.IsOwner) return;
#if CC_EXTRA_CARE
try {
#endif
            await connection.WebSocket.SendText("LogMarker:Recorder"); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        public void ConnectionBind(WebSocketConnection c)
        {
            // Log.Message("StreamRecorder", "ConnectionBind");
            connection = c;
            connection.WebSocket.OnMessage += (bytes) => RecorderWebSocketOnMessage(bytes);
        }

        #endregion

        #region Player Messages

        /// <summary>
        /// PlayerMessage is a way for the Recorder to wait for a specific message from the Player
        /// It can be used for synchronization: see Player.SendPlayerMessage(string message)
        /// </summary>
        public async UniTask WaitForPlayerMessage(string message)
        {
            // Log.Message("StreamRecorder", "Wait for PlayerMessage: " + GameManager.DebugNetworkMessage(message));
            await UniTask.WaitUntil(() => bufferPlayerMessages.Contains(message)).CancelOnDestroy(this);

            // Log.Message("StreamRecorder", "Wait is over for PlayerMessage: " + GameManager.DebugNetworkMessage(message));
            lock (playerMessageLock) {
                bufferPlayerMessages.Remove(message);
            }
        }

        #endregion

        #region Recorder Loop

        protected async UniTask FixedUpdate()
        {
            if (!askedToRecord) return;
            if (isSending) return; // Already busy sending, skip
            if (connection == null) return;
            if (!connection.IsWebsocketReady) return;

            isSending = true; // This is the sending lock mechanism
#if CC_EXTRA_CARE
try {
#endif
            await SendEventsStream().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        /// <summary>
        /// Sending data logic
        /// </summary>
        private async UniTask SendEventsStream()
        {
            isSending = true;

            if (!connection.IsWebsocketReady) {
                isSending = false;
                // Log.Message("StreamRecorder", "Sending events stream: not ready to send");
                return;
            }

            if (bufferFloatPacks.Count == 0 && bufferSnapshots.Count == 0) {
                // Log.Message("StreamRecorderExtra", "Sending events stream: no event in buffer, wait " + Config.THROTTLE_SEND + " sec");
                await UniTask.Delay(TimeSpan.FromSeconds(Config.THROTTLE_SEND), cancellationToken: this.GetCancellationTokenOnDestroy());
                isSending = false;
                return;
            }

            var floatPackCandidates = new List<FloatPack>();
            lock (bufferFloatPackLock) {
                floatPackCandidates = new List<FloatPack>(bufferFloatPacks);
                // TODO prio 2 note the time so we don't take event older than the last success sent time
                bufferFloatPacks.Clear();
            }

            var snapshotCandidates = new List<Snapshot>();
            lock (bufferSnapshotLock) {
                snapshotCandidates = new List<Snapshot>(bufferSnapshots);
                bufferSnapshots.Clear();
            }

            // Log.Message("StreamRecorder", "Will send: " + floatPackCandidates.Count + " floatpack / " + snapshotCandidates.Count + " snapshot");

            Message message = new Message(connection.GameId, connection.UserId, ++sequence, snapshotCandidates, floatPackCandidates);
            string json = JsonConvert.SerializeObject(message);
#if CC_EXTRA_CARE
try {
#endif
            await connection.WebSocket.SendText(json); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif

            // Archive events
            archivedFloatPacks.AddRange(floatPackCandidates);
            archivedSnapshots.AddRange(snapshotCandidates);
            // Log.Message("StreamRecorderExtra", "Sent in total: " + archivedFloatPacks.Count + " floatpack / " + archivedSnapshots.Count);

            // Throttling
            await UniTask.Delay(TimeSpan.FromSeconds(Config.THROTTLE_SEND), cancellationToken: this.GetCancellationTokenOnDestroy());
            isSending = false;
        }

        #endregion

        #region WebSocket

        protected void RecorderWebSocketOnMessage(byte[] bytes)
        {
            if (!connection.IsOwner) return;

            lock (playerMessageLock) {
                string message = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                // Log.Message("StreamRecorder", "Received PlayerMessage: " + GameManager.DebugNetworkMessage(message));
                bufferPlayerMessages.Add(message);
            }
        }

        #endregion
    }
}
