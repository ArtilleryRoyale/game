using System;
using System.Collections.Generic;

namespace CC.StreamPlay
{
    [Serializable]
    public class Message
    {
        #region Fields

        public string g;
        public int s;
        public string u;
        public List<Snapshot> n;
        public List<FloatPack> p;

        #endregion

        public string GameId() => g;
        public string UserId() => u;
        public int Sequence() => s;
        public List<Snapshot> Snapshots() => n;
        public List<FloatPack> FloatPacks() => p;

        public Message() { }

        public Message(string gameId, string userId, int sequence, List<Snapshot> snapshots, List<FloatPack> floatPacks)
        {
            g = gameId;
            u = userId;
            s = sequence;
            n = snapshots;
            p = floatPacks;
        }

        public override string ToString()
        {
            var snapshotsText = string.Join("\n", n);
            var floatPacksText = string.Join(" / ", p);
            return "Message Game: " + g + " User: " + u + " Seq#" + s + "\n" +
            "Snapshots: " + snapshotsText + "\n" +
            "Float packs: " + floatPacksText;
        }
    }
}
