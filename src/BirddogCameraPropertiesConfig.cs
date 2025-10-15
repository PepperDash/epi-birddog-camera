using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
	public class BirddogCameraConfig : CameraPropertiesConfig
	{
		[JsonProperty("panSpeed")]
		public uint PanSpeed { get; set; }

		[JsonProperty("tiltSpeed")]
		public uint TiltSpeed { get; set; }

		[JsonProperty("ZoomSpeed")]
		public uint ZoomSpeed { get; set; }

		[JsonProperty("FocusSpeed")]
		public uint FocusSpeed { get; set; }

		[JsonProperty("PrivacyOnPreset")]
		public uint PrivacyOnPreset { get; set; }

		[JsonProperty("PrivacyOffPreset")]
		public uint PrivacyOffPreset { get; set; }

		[JsonProperty("pollTimeMs")]
		public int PollTimeMs { get; set; }

		public BirddogCameraConfig()
		{
		}
		
		public EssentialsControlPropertiesConfig GetControlFromDeviceConfig(DeviceConfig dc)
		{
			return JsonConvert.DeserializeObject<EssentialsControlPropertiesConfig>(dc.Properties.ToString());
		}
	}
}