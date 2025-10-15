using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;


namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
    public class BirddogCameraFactory : EssentialsPluginDeviceFactory<BirddogCameraController>
    {
        public BirddogCameraFactory()
        {
            // Set the minimum Essentials Framework Version
            MinimumEssentialsFrameworkVersion = "2.16.2";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
            TypeNames = new List<string> { "birddogcamera" };
        }

        // Builds and returns an instance of EssentialsPluginDeviceTemplate
        public override EssentialsDevice BuildDevice(Core.Config.DeviceConfig dc)
        {
            Debug.LogInformation("Factory Attempting to create new device from type: {0}", dc.Type);

            var control = CommFactory.GetControlPropertiesConfig(dc);
            if (control == null)
            {
                Debug.LogError("[{0}] Birddog Camera: failed to get control properties for {1}", dc.Key, dc.Name);
                return null;
            }

            IBasicCommunication comms = null;
            if (control.Method != eControlMethod.Http && control.Method != eControlMethod.Https)
            {
                comms = CommFactory.CreateCommForDevice(dc);
                if (comms == null)
                {
                    Debug.LogError("[{0}] Birddog Camera: failed to create comms for {1}", dc.Key, dc.Name);
                    return null;
                }
            }
	        
            var propertiesConfig = dc.Properties.ToObject<BirddogCameraConfig>();
	        if (propertiesConfig != null) return new BirddogCameraController(dc.Key, dc.Name, control, comms, propertiesConfig);

	        Debug.LogError("[{0}] Birddog Camera: failed to read properties config for {1}", dc.Key, dc.Name);
	        return null;
        }
    }
}