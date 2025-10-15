using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
    public static class ScalingExtensions
    {
        static int Scale(this int input, int inMin, int inMax, int outMin, int outMax)
        {
            var inputRange = inMax - inMin;

            if (inputRange <= 0)
            {
                Debug.LogError($"Invalid Input Range '{inputRange}' for Scaling.  Min '{inMin}' Max '{inMax}'.");
            }

            int outputRange = outMax - outMin;

            var output = (((input - inMin) * outputRange) / inputRange) + outMin;

            return output;
        }

        public static int ScaleForCameraControls(this int input)
        {
            const int maxValue = 49;
            const int minValue = 1;

            return input.Scale(ushort.MinValue, ushort.MaxValue, minValue, maxValue);
        }
    }
}