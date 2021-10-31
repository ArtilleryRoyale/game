using UnityEngine;

namespace CC.StreamPlay
{
    public interface NetworkObjectInterface
    {
        int NetworkIdentifier { get; set; }
        Recorder StreamPlayRecorder { get; }
        bool IsNetworkOwner { get; }
        bool IsNetwork { get; }

        // trick
        GameObject gameObject { get; }
    }
}
