using UnityEngine;

namespace CC
{
    public interface WithHeuristic<T>
    {
        float Heuristic(T goal);
    }
}
