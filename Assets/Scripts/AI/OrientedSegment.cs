using UnityEngine;

namespace CC
{
    public struct OrientedSegment
    {
        public Vector2 Start;
        public Vector2 End;

        public OrientedSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }

        public Segment ToSegment()
        {
            return new Segment(Start, End);
        }

        public static OrientedSegment Invert(OrientedSegment o)
        {
            return new OrientedSegment(o.End, o.Start);
        }

        public override string ToString()
        {
            return "Oriented Segment " + Start + " => " + End;
        }
    }
}
