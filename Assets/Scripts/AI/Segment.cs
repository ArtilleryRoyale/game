using System;
using UnityEngine;

namespace CC
{
    public struct Segment : IEquatable<Segment>
    {
        public Vector2 A;
        public Vector2 B;

        public Segment(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }

        public bool Equals(Segment other)
        {
            return (other.A == A && other.B == B) || (other.A == B && other.B == A);
        }

        public override bool Equals(object other)
        {
            if (other == null || GetType() != other.GetType()) {
                return false;
            }

            return Equals((Segment) other);
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() & B.GetHashCode();
        }

        public override string ToString()
        {
            return "Segment " + A + " / " + B;
        }
    }
}
