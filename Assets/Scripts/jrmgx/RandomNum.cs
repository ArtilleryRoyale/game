using UnityEngine;

namespace Jrmgx.Helpers
{
    public static class RandomNum
    {
        /// <summary>
        /// Returns true randomly one time out of <paramref>value</paramref>
        /// Means:
        ///     - value = 0 => always false
        ///     - value = 1 => always true
        ///     - value = 2 => evenly true/false
        ///     - value = 4 => 25% true 75% false
        ///     ... etc
        /// </summary>
        public static bool RandomOneOutOf(int value)
        {
            if (value <= 0) return true;
            return Random.Range(0, value) == 0;
        }

        public static float Value => Random.value;
    }
}
