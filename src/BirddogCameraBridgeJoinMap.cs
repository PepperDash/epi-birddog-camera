using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
    public class BirddogCameraBridgeJoinMap : JoinMapBaseAdvanced
    {
        #region Digital

        /// <summary>
        /// Camera tilt up
        /// </summary>
        [JoinName("TiltUp")]
        public JoinDataComplete TiltUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera tilt up",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera tilt down
        /// </summary>
        [JoinName("TiltDown")]
        public JoinDataComplete TiltDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera tilt down",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera pan left
        /// </summary>
        [JoinName("PanLeft")]
        public JoinDataComplete PanLeft = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera pan left",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera pan right
        /// </summary>
        [JoinName("PanRight")]
        public JoinDataComplete PanRight = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera pan right",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera zoom in
        /// </summary>
        [JoinName("ZoomIn")]
        public JoinDataComplete ZoomIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera zoom in",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera zoom out
        /// </summary>
        [JoinName("ZoomOut")]
        public JoinDataComplete ZoomOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera zoom out",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera power on
        /// </summary>
        [JoinName("PowerOn")]
        public JoinDataComplete PowerOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 7,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera power on",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera power off
        /// </summary>
        [JoinName("PowerOff")]
        public JoinDataComplete PowerOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 8,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera power off",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera is online
        /// </summary>
        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 9,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera is online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera home position
        /// </summary>
        [JoinName("Home")]
        public JoinDataComplete Home = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera home",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera preset recall
        /// </summary>
        [JoinName("PresetRecall")]
        public JoinDataComplete PresetRecall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 16
            },
            new JoinMetadata
            {
                Description = "Camera preset recall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera preset saved
        /// </summary>
        [JoinName("PresetSavedFeedback")]
        public JoinDataComplete PresetSavedFeedback = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 30,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera preset saved Feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera preset save
        /// </summary>
        [JoinName("PresetSave")]
        public JoinDataComplete PresetSave = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 31,
                JoinSpan = 16
            },
            new JoinMetadata
            {
                Description = "Camera preset save",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera privacy on
        /// </summary>
        [JoinName("PrivacyOn")]
        public JoinDataComplete PrivacyOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 48,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera privacy on",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Camera privacy off
        /// </summary>
        [JoinName("PrivacyOff")]
        public JoinDataComplete PrivacyOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 49,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera privacy off",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        #endregion


        #region Analog

        /// <summary>
        /// Camera pan speed
        /// </summary>
        [JoinName("PanSpeed")]
        public JoinDataComplete PanSpeed = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera pan speed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Camera tilt speed
        /// </summary>
        [JoinName("TiltSpeed")]
        public JoinDataComplete TiltSpeed = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera tilt speed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Camera zoom speed
        /// </summary>
        [JoinName("ZoomSpeed")]
        public JoinDataComplete ZoomSpeed = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera zoom speed",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Camera number of presets
        /// </summary>
        [JoinName("NumberOfPresets")]
        public JoinDataComplete NumberOfPresets = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera number of preset",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Camera preset select by number
        /// </summary>
        [JoinName("PresetSelectByNumber")]
        public JoinDataComplete PresetSelectByNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 13,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Preset select by number",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Camera preset store by number
        /// </summary>
        [JoinName("PresetStoreByNumber")]
        public JoinDataComplete PresetStoreByNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 14,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Preset store by number",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        #endregion


        #region Serial


        /// <summary>
        /// Camera device name
        /// </summary>
        [JoinName("DeviceName")]
        public JoinDataComplete DeviceName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera device name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        /// <summary>
        /// Camera IP Address
        /// </summary>
        [JoinName("IpAddress")]
        public JoinDataComplete IpAddress = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera IP Address",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        /// <summary>
        /// Camera preset names
        /// </summary>
        [JoinName("PresetNames")]
        public JoinDataComplete PresetNames = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Camera preset names",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });

        #endregion

        public BirddogCameraBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(BirddogCameraBridgeJoinMap))
        {
        }
    }
}