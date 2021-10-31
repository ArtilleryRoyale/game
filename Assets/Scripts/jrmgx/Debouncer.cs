using UnityEngine;
using Jrmgx.Helpers;

public class Debouncer
{
    public Debouncer(MonoBehaviour behaviour, float seconds)
    {
        this.behaviour = behaviour;
        Seconds = seconds;
    }

    public float Seconds { get; }
    private bool isBlocked = false;
    private readonly MonoBehaviour behaviour;

    public bool NeedDebounce()
    {
        return isBlocked;
    }

    public void Debounce()
    {
        isBlocked = true;
        behaviour.ExecuteInSecond(Seconds, () => isBlocked = false);
    }
}
