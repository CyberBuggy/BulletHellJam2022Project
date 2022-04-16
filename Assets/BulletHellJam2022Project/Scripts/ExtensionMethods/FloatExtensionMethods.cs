using UnityEngine;

namespace CyberBuggy
{
    public static class FloatExtensionMethods
    {
        public static float Remap (this float value, float inputMin, float inputMax, float outputMin, float outputMax) {
            return (value - inputMin) / (outputMin - inputMin) * (outputMax - inputMax) + inputMax;
}
    }
}
