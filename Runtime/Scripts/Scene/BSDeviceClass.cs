using UnityEngine;

namespace BS
{
    /// <summary>
    /// Sorts standalone headsets into two classes by their GPU generation, for content that is too heavy for the older
    /// ones (<see cref="BSPlatformFilter.includeOnLowEndMobile"/>, <see cref="BSScene.IsLowEndMobile"/>).
    ///
    /// High end: Quest 3, Quest 3S, Steam Frame, Pico 4 Ultra (Snapdragon XR2 Gen 2 or newer).
    /// Low end: Quest 2, Quest Pro, Quest 1, Pico 4, Pico Neo 3, and any Android headset this does not recognise,
    /// so a new or unknown headset gets the lighter content until it is added here.
    /// </summary>
    public static class BSDeviceClass
    {
        /// <summary>True for a standalone headset of the Quest 3 generation or newer.</summary>
        /// <param name="deviceModel">SystemInfo.deviceModel ("Oculus Quest 3", "Oculus Quest Pro", "Pico A9210", ...).</param>
        public static bool IsHighEndStandalone(string deviceModel)
        {
            var model = (deviceModel ?? string.Empty).ToLowerInvariant();
            if (model.Contains("quest"))
                // "Quest 3", "Quest 3S", "Quest3": the 3 generation. Quest 2, Pro and 1 are the older one.
                return model.Contains("quest 3") || model.Contains("quest3");
            if (model.Contains("frame") && (model.Contains("steam") || model.Contains("valve")))
                return true;
            if (model.Contains("pico"))
                // Pico reports model codes: A9210 is the Pico 4 Ultra (XR2 Gen 2); A8110/A8150 Pico 4 and the Neo 3
                // codes are XR2 Gen 1.
                return model.Contains("a9210") || model.Contains("ultra");
            return false;
        }

        /// <summary>
        /// True on an Android headset older than the Quest 3 generation (or one not recognised). Always false off
        /// Android: a PC is never "low end mobile", and neither is the Unity editor, whatever its build target.
        /// </summary>
        public static bool IsLowEndMobile(bool isAndroid, string deviceModel) =>
            isAndroid && !IsHighEndStandalone(deviceModel);

        /// <summary>This device, by <see cref="IsLowEndMobile(bool,string)"/>. Main thread only (SystemInfo).</summary>
        public static bool ThisDeviceIsLowEndMobile() =>
            IsLowEndMobile(Application.platform == RuntimePlatform.Android, SystemInfo.deviceModel);
    }
}
