using UnityEngine;

namespace Phezu.Util
{
    public static class FMath
    {
        /// <summary>
        /// Framerate independent lerping
        /// </summary>
        /// <param name="start">The beginning point of Lerp</param>
        /// <param name="end">The destination of Lerp</param>
        /// <param name="life">The amount of time in seconds after which ratio percentage will be traversed</param>
        /// <param name="ratio">The percentage between start and end that will be traversed after life seconds</param>
        /// <param name="dt">Time in seconds from the previous frame</param>"
        /// <returns>The correct point between start and end</returns>
        public static Vector3 FreeLerp(Vector3 start, Vector3 end, float life, float ratio, float dt)
        {
            float K = Mathf.Pow(ratio, -life);
            float t = 1 - Mathf.Pow(K, dt);
            return start + (end - start) * t;
        }

        /// <summary>
        /// Use this to cache the value of K for lerping independent of framerate
        /// </summary>
        /// <param name="life">The amount of time in seconds after which ratio percentage will be traversed</param>
        /// <param name="ratio">The percentage between start and end that will be traversed after life seconds</param>
        /// <returns>The K constant for framerate independent lerping</returns>
        public static float GetFreeLerpK(float life, float ratio)
        {
            return Mathf.Pow(1 - ratio, 1f / life);
        }

        /// <summary>
        /// Use this to calculate the t parameter for framerate independent lerping => Lerp(a, b, t);
        /// </summary>
        /// <param name="K">Use the GetFreeLerpK to cache this constant</param>
        /// <param name="dt">Time in seconds from the previous frame</param>
        /// <returns>The t parameter for framerate independent lerping</returns>
        public static float GetFreeLerpT(float K, float dt)
        {
            return 1 - Mathf.Pow(K, dt);
        }

        /// <summary>
        /// Returns whether the layer is contained in layerMask
        /// </summary>
        /// <param name="layerMask">The LayerMask</param>
        /// <param name="layer">The layer to check</param>
        /// <returns>True if layer is contained in the layer mask</returns>
        public static bool IsInLayerMask(LayerMask layerMask, int layer)
        {
            return layerMask == (layerMask | 1 << layer);
        }

        /// <summary>
        /// Modulus that works with negative values for num.
        /// </summary>
        /// <param name="num">This can be negative.</param>
        /// <param name="mod">Mod base, did not test with negative values.</param>
        public static int Mod(int num, int mod) {
            if (num >= 0)
                return num % mod;

            return mod - ((-num) % mod);
        }
    }
}