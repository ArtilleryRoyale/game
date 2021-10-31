using System;
using CC.StreamPlay;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Jrmgx.Helpers;

abstract public class NetworkObject : MethodIdentifier, NetworkObjectInterface
{
    #region Fields

    public int NetworkIdentifier { get; set; } = 0;

    public NetworkManager NetworkManagerInstance => NetworkManager.Instance;
    public bool IsNetwork => NetworkManagerInstance.IsNetwork;
    public bool IsNetworkOwner => NetworkManagerInstance.IsNetworkOwner;
    public Recorder StreamPlayRecorder => NetworkManagerInstance.StreamPlayRecorder;
    public Player StreamPlayPlayer => NetworkManagerInstance.StreamPlayPlayer;

    private Dictionary<int, MethodInfo> methodReferences = new Dictionary<int, MethodInfo>();

    #endregion

    protected virtual void Awake() { }

    protected virtual void Start()
    {
        if (!IsNetwork) return;
#if CC_DEBUG
        if (NetworkIdentifier == 0) {
            throw new Exception("NetworkIdentifier is not defined for: " + name);
        }
#endif
        NetworkRefresh();
    }

    public bool NetworkAssertNotGuest()
    {
#if CC_DEBUG
        if (!IsNetworkOwner) throw new Exception("This method should not be call as guest!");
#endif
        return true;
    }

    public static string NetworkIdentifierHuman()
    {
        byte[] md5 = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        return BitConverter.ToUInt16(md5, 0).ToString();
    }

    public static int NetworkIdentifierNew()
    {
        return Basics.RandomIdentifier();
    }

    public static int NetworkIdentifierFrom(string data)
    {
        return Basics.IdentifierFrom(data);
    }

    #region Instantiate / Refresh / Destroy

    public T NetworkInstantiate<T>(T prefab)
        where T : NetworkObject
    {
        var ob = UnityEngine.Object.Instantiate<T>(prefab);
        ob.NetworkIdentifier = NetworkIdentifierNew();
        return ob;
    }

    public T NetworkInstantiate<T>(T prefab, int networkIdentifier)
        where T : NetworkObject
    {
        var ob = UnityEngine.Object.Instantiate<T>(prefab);
        ob.NetworkIdentifier = networkIdentifier;
        return ob;
    }

    public void NetworkRefresh()
    {
        StreamPlayPlayer.RefreshOne(this);
    }

    public virtual void NetworkDestroy()
    {
        StreamPlayRecorder.RecordSnapshot(this, Recorder.METHOD_DESTROY);
        Destroy(gameObject);
    }

    #endregion

    #region Snapshots

    /// <summary>
    /// Add a Snapshot to the sequence
    /// </summary>
    public void NetworkRecordSnapshot(int methodIdentifier, params object[] data)
    {
        StreamPlayRecorder.RecordSnapshot(this, methodIdentifier, data);
    }

    /// <summary>
    /// Add a Snapshot to the sequence
    /// This version will add the snapshot on top of the current sequence
    /// useful when you want this played by the guest as soon as possible
    /// </summary>
    public void NetworkRecordSnapshotInstant(int methodIdentifier, params object[] data)
    {
        StreamPlayRecorder.RecordSnapshot(this, methodIdentifier, -1, data);
    }

    #endregion

    #region FloatPack

    public void NetworkRecordFloatPack(params object[] data)
    {
        StreamPlayRecorder.RecordFloatPack(this, data);
    }

    #endregion

}
