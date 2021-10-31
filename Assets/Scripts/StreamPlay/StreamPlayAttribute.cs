using System;

namespace CC.StreamPlay
{
    [AttributeUsage(AttributeTargets.Method)]
    public class StreamPlayAttribute : Attribute
    {
        public int MethodIdentifier { get; private set; }

        public StreamPlayAttribute(int methodIdentifier)
        {
            MethodIdentifier = methodIdentifier;
        }
    }
}
