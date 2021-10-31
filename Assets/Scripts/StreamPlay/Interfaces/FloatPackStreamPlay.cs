namespace CC.StreamPlay
{
    public interface FloatPackStreamPlay : NetworkObjectInterface
    {
        void OnFloatPack(FloatPack floatPack);

        // trick
        string name { get; }
    }
}
