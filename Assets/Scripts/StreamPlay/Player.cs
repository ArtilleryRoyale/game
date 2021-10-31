using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using System.Reflection;
using Jrmgx.Helpers;
using System.Collections;

namespace CC.StreamPlay
{
    /// <summary>
    /// Play a sequence of FloatPacks and/or Snapshots
    /// </summary>
    public class Player : MonoBehaviour
    {
        #region Fields

        // Objects references, they will contain null references, this is by design
        private readonly Dictionary<int, FloatPackStreamPlay> floatPackReferences = new Dictionary<int, FloatPackStreamPlay>();
        private readonly Dictionary<int, NetworkObject> objectReferences = new Dictionary<int, NetworkObject>();
        private readonly Dictionary<int, MethodInfo> methodReferences = new Dictionary<int, MethodInfo>();
#if CC_DEBUG
        private readonly Dictionary<int, string> nameReferences = new Dictionary<int, string>();
#endif

        // Locks
        private readonly object bufferFloatPackLock = new object();
        private readonly object bufferSnapshotLock = new object();

        // Buffers
        // Events are already pretty much ordered by time
        // Note: can contain null values
        private readonly List<FloatPack> bufferFloatPacks = new List<FloatPack>();
        private readonly List<Snapshot> bufferSnapshots = new List<Snapshot>();

        // State
        private float playerStartTime = 0f;
        private float playerTime => Time.fixedTime - playerStartTime;
        private bool askedToPlay = false;
        private static bool FixedUpdateLogErrorOnce;

        private WebSocketConnection connection;

        #endregion

        #region Player Methods

        /// <summary>
        /// Play the current sequence
        /// The important part here is the `playerStartTime` reset so the next playerTime are correct
        /// </summary>
        public void Play(float withBufferSeconds)
        {
            // Log.Message("StreamPlayer", "Asked to play stream");
            Refresh();
            playerStartTime = Time.fixedTime + withBufferSeconds;
            askedToPlay = true;
        }

        /// <summary>
        /// Refresh the list of objects in the scene that are NetworkObject or implements FloatPackStreamPlay
        /// by adding this new object
        /// </summary>
        public void RefreshOne(NetworkObject networkObject)
        {
            if (!NetworkManager.Instance.IsNetwork) return;
            if (networkObject.NetworkIdentifier == 0) return;

            if (networkObject is FloatPackStreamPlay floatPackStreamPlay) {
                floatPackReferences[floatPackStreamPlay.NetworkIdentifier] = floatPackStreamPlay;
            }
            objectReferences[networkObject.NetworkIdentifier] = networkObject;
#if CC_DEBUG
            nameReferences[networkObject.NetworkIdentifier] = networkObject.name;
#endif
            Type type = networkObject.GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (MethodInfo method in methods) {
                foreach (StreamPlayAttribute attribute in method.GetCustomAttributes<StreamPlayAttribute>()) {
                    methodReferences[attribute.MethodIdentifier] = method;
                }
            }

            // Log.Message("StreamPlayer", "Refreshed Added one: " + networkObject.name + " => " + networkObject.NetworkIdentifier);
        }

#if CC_DEBUG

        [ContextMenu("Dump References")]
        public void DumpReferences()
        {
            string text = "Dump References Object:\n";
            foreach (var kv in objectReferences) {
                try {
                    text += "\t> " + kv.Key + " => " + kv.Value.name + " \n";
                } catch {
                    string was = nameReferences[kv.Key];
                    text += "\t> " + kv.Key + " => Deleted reference (was " + was + ")\n";
                }
            }
            text += "Dump References FloatPack:\n";
            foreach (var kv in floatPackReferences) {
                try {
                    text += "\t> " + kv.Key + " => " + kv.Value.name + " \n";
                } catch {
                    string was = nameReferences[kv.Key];
                    text += "\t> " + kv.Key + " => Deleted reference (was " + was + ")\n";
                }
            }
            Debug.Log(text);
        }

#endif

        /// <summary>
        /// Refresh the list of objects in the scene that are NetworkObject or implements FloatPackStreamPlay
        /// so the data can be delivered to those.
        /// Note: this is cpu intensive and should not be used frequently, nor while instantiating object,
        /// use RefreshAdd(object) instead
        /// </summary>
        public void Refresh()
        {
            if (!NetworkManager.Instance.IsNetwork) return;
            // Log.Message("StreamPlayer", "Refreshed All");
            var networkObjects = FindObjectsOfType<NetworkObject>();
            foreach (NetworkObject networkObject in networkObjects) {
                RefreshOne(networkObject);
            }
        }

        /// <summary>
        /// Stop the current sequence and reset it
        /// We wait a frame to be sure that all the "instant" message will be sent
        /// </summary>
        public async UniTask Stop()
        {
            // Log.Message("StreamPlayer", "Stop player");
            await UniTask.WaitForFixedUpdate(cancellationToken: this.GetCancellationTokenOnDestroy());
            askedToPlay = false;

            lock (bufferFloatPackLock) {
                bufferFloatPacks.Clear();
            }
            lock (bufferSnapshotLock) {
                bufferSnapshots.Clear();
            }
            // Log.Message("StreamPlayer", "Stopped player");
        }

#if CC_DEBUG

        private Coroutine TooLongHandler;

        /// <summary>
        /// Wait until no more message are in buffers
        /// </summary>
        public async UniTask WaitForFinish()
        {
            // Log.Message("StreamPlayer", "Waiting for player to finish");
            this.StopCoroutineNoFail(TooLongHandler);
            TooLongHandler = this.StartCoroutineNoFail(TooLong(3));
            await UniTask.WaitWhile(() => bufferFloatPacks.Count > 0 || bufferSnapshots.Count > 0).CancelOnDestroy(this);
            this.StopCoroutineNoFail(TooLongHandler);
            // Log.Message("StreamPlayer", "Player has finished");
        }

        private IEnumerator TooLong(int time)
        {
            yield return new WaitForSeconds(time);
            Debug.LogWarning("WaitForFinish > " + time + " sec, dumping for debug");
            Debug.LogWarning("FloatPack Buffer: " + Basics.ListToString(bufferFloatPacks));
            Debug.LogWarning("Snapshot Buffer: " + Basics.ListToString(bufferSnapshots));
        }

#else

        public async UniTask WaitForFinish()
        {
            await UniTask.WaitWhile(() => bufferFloatPacks.Count > 0 || bufferSnapshots.Count > 0).CancelOnDestroy(this);
        }

#endif

        #endregion

        #region Player Messages

        /// <summary>
        /// PlayerMessage is a way for the Player to send a specific message to the Recorder
        /// It can be used for synchronization: see Recorder.WaitForPlayerMessage(string message)
        /// </summary>
        public async UniTask SendPlayerMessage(string message, bool waitForReady = false)
        {
            if (connection == null) {
                Log.Critical("StreamPlayer", "Call to Player.SendPlayerMessage while connection is NULL");
                throw new Exception("Call to Player.SendPlayerMessage while connection is NULL");
            }
            if (waitForReady) {
                // Log.Message("StreamPlayer", "Wait WebsocketReady before sending PlayerMessage: " + GameManager.DebugNetworkMessage(message));
                await UniTask.WaitUntil(() => connection.IsWebsocketReady);
            } else if (!connection.IsWebsocketReady) {
                Log.Error("StreamPlayer", "Call to Player.SendPlayerMessage while connection NOT IsWebsocketReady");
                return;
            }
            // Log.Message("StreamPlayer", "Sending PlayerMessage: " + GameManager.DebugNetworkMessage(message));
#if CC_EXTRA_CARE
try {
#endif
            await connection.WebSocket.SendText(message); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        #endregion

        #region Connection

        public async UniTask LogMarker()
        {
            if (connection == null) return;
            if (connection.IsOwner) return;
#if CC_EXTRA_CARE
try {
#endif
            await connection.WebSocket.SendText("LogMarker:Player"); // Not cancelable .CancelOnDestroy(this)
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        public void ConnectionBind(WebSocketConnection c)
        {
            // Log.Message("StreamPlayer", "ConnectionBind");
            connection = c;
            connection.WebSocket.OnMessage += (bytes) => PlayerWebSocketOnMessage(bytes);
        }

        #endregion

        #region Player Loop

        protected void FixedUpdate()
        {
            if (!askedToPlay) return;
            if (connection == null) {
                if (FixedUpdateLogErrorOnce) {
                    Log.Error("StreamPlayer", "Call to Player.FixedUpdate while connection is NULL");
                } else {
                    FixedUpdateLogErrorOnce = true;
                    Log.Critical("StreamPlayer", "Call to Player.FixedUpdate while connection is NULL");
                }
                return;
            }
            if (connection.IsOwner) return;

            PlayNextFloatPack();
            PlayNextSnapshot();
        }

        private void PlayNextSnapshot()
        {
            if (bufferSnapshots.Count == 0) return;

            // Find past snapshots
            for (int i = 0, max = bufferSnapshots.Count; i < max; i++) {
                Snapshot snapshot = bufferSnapshots[i];
                if (snapshot == null) continue;
                // Log.Message("StreamPlayerExtra", "Snapshot candidate with time: " + snapshot.Time() + " player: " + playerTime);
                if (snapshot.Time() < 0 /* Instant */ || snapshot.Time() <= playerTime) {
                    PlaySnapshot(snapshot);
                    bufferSnapshots[i] = null;
                }
            }

            // Remove consumed events
            lock (bufferSnapshotLock) {
                bufferSnapshots.RemoveAll(item => item == null);
            }
        }

        private void PlayNextFloatPack()
        {
            if (bufferFloatPacks.Count == 0) return;

            // Find past positions
            for (int i = 0, max = bufferFloatPacks.Count; i < max; i++) {
                FloatPack floatPack = bufferFloatPacks[i];
                if (floatPack == null) continue;
                if (floatPack.Time() <= playerTime) {
                    PlayFloatPack(floatPack);
                    bufferFloatPacks[i] = null;
                }
            }

            // Remove consumed events
            lock (bufferFloatPackLock) {
                bufferFloatPacks.RemoveAll(item => item == null);
            }
        }

        /// <summary>
        /// Send the FloatPack to the object
        /// </summary>
        private void PlayFloatPack(FloatPack floatPack)
        {
            if (!floatPackReferences.ContainsKey(floatPack.NetworkIdentifier())) {
                Log.Error("StreamPlayer", "Floatpack arrived with no corresponding object: " + floatPack);
                return;
            }

            FloatPackStreamPlay networkObject = floatPackReferences[floatPack.NetworkIdentifier()];
            if (networkObject == null) {
                Log.Error("StreamPlayer", "Object has been destroyed, but a FloatPack still needs it: " + floatPack);
                return;
            }

            // Log.Message("StreamPlayerExtra", "Play floatpack " + networkObject.name + floatPack + " player: " + playerTime + " floatpack " + floatPack.Time());
            networkObject.OnFloatPack(floatPack);
        }

        /// <summary>
        /// Send the Snapshot to the object
        /// </summary>
        private void PlaySnapshot(Snapshot snapshot)
        {
            if (!objectReferences.ContainsKey(snapshot.NetworkIdentifier())) {
                Log.Error("StreamPlayer", "Snapshot arrived with no corresponding object: " + snapshot);
                return;
            }

            NetworkObject networkObject = objectReferences[snapshot.NetworkIdentifier()];
            if (networkObject == null) {
                Log.Error("StreamPlayer", "Object has been destroyed, but a Snapshot still needs it: " + snapshot);
                return;
            }

            if (snapshot.MethodIdentifier() == Recorder.METHOD_DESTROY) {

                Destroy(networkObject.gameObject);
                // Log.Message("StreamPlayer", "Play " + networkObject.name + snapshot + " player: " + playerTime);

            } else if (methodReferences.ContainsKey(snapshot.MethodIdentifier())) {
                try {
                    // Log.Message("StreamPlayer", "Play " + networkObject.name + snapshot + " player: " + playerTime);
                    MethodInfo method = methodReferences[snapshot.MethodIdentifier()];
                    object result = method.Invoke(networkObject, snapshot.Data());
                    if (result is UniTask task) {
#if CC_EXTRA_CARE
try {
#endif
                        task.Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
                    }
                } catch (Exception e) {
                    Log.Error("StreamPlayer", "Error while executing " + snapshot + " exception: " + e);
                    throw e;
                }
            } else {
                Log.Critical("StreamPlayer", "Error " + snapshot + " not found in methodReferences");
            }
        }

        #endregion

        #region WebSocket

        protected void PlayerWebSocketOnMessage(byte[] bytes)
        {
            if (connection.IsOwner) return;

            try {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                // Log.Message("StreamPlayerExtra", json);
                Message message = JsonConvert.DeserializeObject<Message>(json);
                lock (bufferFloatPackLock) {
                    bufferFloatPacks.AddRange(message.FloatPacks());
                }
                lock (bufferSnapshotLock) {
                    bufferSnapshots.AddRange(message.Snapshots());
                }
            } catch (Exception e) {
                Log.Error("StreamPlayer", "Deserialization failed: " + e.Message);
                throw;
            }
        }

        #endregion
    }
}
