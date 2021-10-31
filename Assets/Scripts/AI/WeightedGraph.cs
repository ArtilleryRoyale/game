using System.Collections.Generic;

namespace CC
{
    public interface WeightedGraph<T>
    {
        float Cost(T a, T b);
        IEnumerable<T> Neighbors(T id);
    }
}
