using System;
using System.Collections.Generic;
using PepperDash.Core;
using Serilog.Events;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
    /// <summary>
    /// Represents a Birddog API command with path, method, and optional body
    /// </summary>
    public class BirddogApiCommand
    {
        public string Path { get; set; }
        public string Method { get; set; } = "GET";
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public BirddogApiCommand(string path, string method = "GET", string body = null)
        {
            Path = path;
            Method = method;
            Body = body;
        }
    }

    /// <summary>
    /// Birddog Camera Commands builder for RESTful API v2.0 commands
    /// </summary>
    public class BirddogCameraCommands
    {
        // PTZ Speed settings (Birddog API ranges)
        private int _panSpeed = 8;   // Range 0-21
        private int _tiltSpeed = 8;  // Range 0-18
        private int _zoomSpeed = 4;  // Range 0-7

        private int _privacyOn = 99;  // Placeholder, implement as needed
        private int _privacyOff = 1;  // Placeholder, implement as needed

        public int PanSpeed
        {
            get => _panSpeed;
            set => _panSpeed = Math.Max(0, Math.Min(21, value));
        }

        public int TiltSpeed
        {
            get => _tiltSpeed;
            set => _tiltSpeed = Math.Max(0, Math.Min(18, value));
        }

        public int ZoomSpeed
        {
            get => _zoomSpeed;
            set => _zoomSpeed = Math.Max(0, Math.Min(7, value));
        }

        public int PrivacyOn
        {
            get => _privacyOn;
            set => _privacyOn = value; // Implement validation if needed
        }

        public int PrivacyOff
        {
            get => _privacyOff;
            set => _privacyOff = value; // Implement validation if needed
        }



        #region RESTful API v2.0 Commands

        public BirddogApiCommand PositionHome()
        {
            // This might be implemented via preset recall to a "home" preset
            return RecallPresetRest(1); // Assuming preset 1 is home position
        }

        public BirddogApiCommand PositionPrivacy()
        {
            // This might be implemented via preset recall to a "privacy" preset
            return RecallPresetRest(PrivacyOn); // Assuming PrivacyOn preset is defined
        }

        public BirddogApiCommand PowerOn()
        {
            throw new NotImplementedException("Power control commands need to be implemented based on specific Birddog camera model API");
        }

        public BirddogApiCommand PowerOff()
        {
            throw new NotImplementedException("Power control commands need to be implemented based on specific Birddog camera model API");
        }

        // Basic Device Information
        public BirddogApiCommand GetAbout() => new BirddogApiCommand("about");
        public BirddogApiCommand GetHostname() => new BirddogApiCommand("hostname");
        public BirddogApiCommand GetVersion() => new BirddogApiCommand("version");
        public BirddogApiCommand Reboot() => new BirddogApiCommand("reboot", "POST");
        public BirddogApiCommand Restart() => new BirddogApiCommand("restart", "POST");

        // PTZ Settings (RESTful API)
        public BirddogApiCommand GetPtzSettings() => new BirddogApiCommand("birddogptzsetup");

        public BirddogApiCommand SetPtzSettings(int panSpeed, int tiltSpeed, int zoomSpeed)
        {
            // Use Birddog API native ranges (Pan: 0-21, Tilt: 0-18, Zoom: 0-7)
            var apiPanSpeed = Math.Max(0, Math.Min(21, panSpeed));
            var apiTiltSpeed = Math.Max(0, Math.Min(18, tiltSpeed));
            var apiZoomSpeed = Math.Max(0, Math.Min(7, zoomSpeed));

            var body = JsonConvert.SerializeObject(new
            {
                PanSpeed = apiPanSpeed.ToString(),
                TiltSpeed = apiTiltSpeed.ToString(),
                ZoomSpeed = apiZoomSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzsetup", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Pan the camera left at the configured pan speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand PanLeft()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "left",
                //PanSpeed = _panSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Pan the camera right at the configured pan speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand PanRight()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "right",
                //PanSpeed = _panSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Tilt the camera up at the configured tilt speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand TiltUp()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "up",
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Tilt the camera down at the configured tilt speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand TiltDown()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "down",
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Tilt the camera up and left at the configured pan/tilt speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand TiltUpLeft()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "upleft",
                //PanSpeed = _panSpeed.ToString(),
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Tilt the camera up and right at the configured pan/tilt speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand TiltUpRight()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "upright",
                //PanSpeed = _panSpeed.ToString(),
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Tilt the camera down and left at the configured pan/tilt speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand TiltDownLeft()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "downleft",
                //PanSpeed = _panSpeed.ToString(),
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Tilt the camera down and right at the configured pan/tilt speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand TiltDownRight()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "downright",
                //PanSpeed = _panSpeed.ToString(),
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Stop all pan/tilt movement
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand PanTiltStop()
        {
            var body = JsonConvert.SerializeObject(new
            {
                PanTilt = "stop",
                //PanSpeed = _panSpeed.ToString(),
                //TiltSpeed = _tiltSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Zoom the camera in at the configured zoom speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand ZoomIn()
        {
            var body = JsonConvert.SerializeObject(new
            {
                Zoom = "tele",
                ZoomSpeed = _zoomSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Zoom the camera out at the configured zoom speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand ZoomOut()
        {
            var body = JsonConvert.SerializeObject(new
            {
                Zoom = "wide",
                ZoomSpeed = _zoomSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Stop all zoom movement
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand ZoomStop()
        {
            var body = JsonConvert.SerializeObject(new
            {
                Zoom = "stop",
                ZoomSpeed = _zoomSpeed.ToString()
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Focus the camera nearer at the configured focus speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand FocusNear()
        {
            var body = JsonConvert.SerializeObject(new
            {
                Focus = "in"
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Focus the camera farther at the configured focus speed
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand FocusFar()
        {
            var body = JsonConvert.SerializeObject(new
            {
                Focus = "out"
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        /// <summary>
        /// Stop all focus movement
        /// <see cref="https://documenter.getpostman.com/view/29602224/2sAYHxn45i#6ae27f19-9259-43ae-918b-48ca161d50e5"/>
        /// </summary>
        /// <returns></returns>
        public BirddogApiCommand FocusStop()
        {
            var body = JsonConvert.SerializeObject(new
            {
                Focus = "stop"
            });

            var command = new BirddogApiCommand("birddogptzcontrol", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // Preset Commands (RESTful API)
        public BirddogApiCommand RecallPresetRest(int preset)
        {
            // TODO - Validate preset name or `Preset-x` 
            var presetName = $"Preset-{Math.Max(1, Math.Min(9, preset))}";
            var body = JsonConvert.SerializeObject(new { Preset = presetName });
            
            var command = new BirddogApiCommand("recall", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        public BirddogApiCommand SavePresetRest(int preset)
        {
            var presetName = $"Preset-{Math.Max(1, Math.Min(9, preset))}";
            var body = JsonConvert.SerializeObject(new { Preset = presetName });
            
            var command = new BirddogApiCommand("save", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // Encode Settings
        public BirddogApiCommand GetEncodeSettings() => new BirddogApiCommand("encodesetup");

        public BirddogApiCommand SetEncodeSettings(string streamName, string videoFormat = "1080p50", 
            string colorBitDepth = "8Bit", string videoSampleRate = "420")
        {
            var body = JsonConvert.SerializeObject(new
            {
                ChNum = "1",
                VideoFormat = videoFormat,
                VideoSampleRate = videoSampleRate,
                ColorBitDepth = colorBitDepth,
                StreamName = streamName,
                NDIAudio = "NDIAudioAnalog",
                ScreenSaverMode = "CaptureSS",
                BandwidthMode = "NDIManaged",
                BandwidthSelect = "120",
                LoopTally = "LoopTallyDis",
                TallyMode = "TallyOn",
                VideoCSC = "RGB",
                NDIGroup = "NDIGroupDis",
                NDIGroupName = "BirdDog"
            });

            var command = new BirddogApiCommand("encodesetup", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // Audio Settings
        public BirddogApiCommand GetAudioSettings() => new BirddogApiCommand("analogaudiosetup");

        public BirddogApiCommand SetAudioSettings(int audioInGain = 80, int audioOutGain = 80, 
            string outputSelect = "DecodeMain")
        {
            var body = JsonConvert.SerializeObject(new
            {
                AnalogAudioInGain = audioInGain.ToString(),
                AnalogAudioOutGain = audioOutGain.ToString(),
                AnalogAudiooutputselect = outputSelect
            });

            var command = new BirddogApiCommand("analogaudiosetup", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // Picture Settings
        public BirddogApiCommand GetPictureSettings() => new BirddogApiCommand("birddogpicsetup");

        public BirddogApiCommand SetPictureSettings(int brightness = 2, int contrast = 1, int color = 8,
            int hue = 7, int sharpness = 122, bool flip = false, bool mirror = false)
        {
            var body = JsonConvert.SerializeObject(new
            {
                BackLightCom = "On",
                ChromeSuppress = "OFF",
                Color = color.ToString(),
                Contrast = contrast.ToString(),
                Effect = "Off",
                Flip = flip ? "On" : "Off",
                Gamma = "1",
                HighlightComp = "OFF",
                HighlightCompMask = "3",
                Hue = hue.ToString(),
                IRCutFilter = "Auto",
                Mirror = mirror ? "On" : "Off",
                NoiseReduction = "Off",
                Sharpness = sharpness.ToString(),
                Stabilizer = "Off",
                TWODNR = "2",
                ThreeDNR = "2",
                WideDynamicRange = "Off",
                LowLatency = "Off",
                NDFilter = "0"
            });

            var command = new BirddogApiCommand("birddogpicsetup", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // Exposure Settings
        public BirddogApiCommand GetExposureSettings() => new BirddogApiCommand("birddogexpsetup");

        public BirddogApiCommand SetExposureSettings(string expMode = "FULL-AUTO", int brightLevel = 24,
            int gainLevel = 4, int gainLimit = 11, int shutterSpeed = 16)
        {
            var body = JsonConvert.SerializeObject(new
            {
                AeResponse = "1",
                BackLight = "Off",
                BrightLevel = brightLevel.ToString(),
                ExpCompEn = "Off",
                ExpCompLvl = "0",
                ExpMode = expMode,
                GainLevel = gainLevel.ToString(),
                GainLimit = gainLimit.ToString(),
                GainPoint = "Off",
                GainPointPosition = "10",
                HighSensitivity = "Off",
                IrisLevel = "21",
                ShutterControlOverwrite = "On",
                ShutterMaxSpeed = "29",
                ShutterMinSpeed = "16",
                ShutterSpeed = shutterSpeed.ToString(),
                ShutterSpeedOverwrite = "30",
                SlowShutterEn = "Off",
                SlowShutterLimit = "1",
                Spotlight = "Off"
            });

            var command = new BirddogApiCommand("birddogexpsetup", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // White Balance Settings
        public BirddogApiCommand GetWhiteBalanceSettings() => new BirddogApiCommand("birddogwbsetup");

        public BirddogApiCommand SetWhiteBalanceSettings(string wbMode = "AUTO", int colorTemp = 5600,
            int redGain = 179, int blueGain = 174)
        {
            var body = JsonConvert.SerializeObject(new
            {
                BG = "0",
                BR = "0",
                BlueGain = blueGain.ToString(),
                ColorTemp = colorTemp.ToString(),
                GB = "0",
                GR = "0",
                Level = "4",
                Matrix = "Off",
                Offset = "7",
                Phase = "7",
                RB = "0",
                RG = "0",
                RedGain = redGain.ToString(),
                Select = "OFF",
                Speed = "3",
                WbMode = wbMode
            });

            var command = new BirddogApiCommand("birddogwbsetup", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        // Network Settings
        public BirddogApiCommand ConnectToNdiSource(string sourceName)
        {
            var body = JsonConvert.SerializeObject(new { sourceName = sourceName });
            
            var command = new BirddogApiCommand("connectTo", "POST", body);
            command.Headers["Content-Type"] = "application/json";
            return command;
        }

        public BirddogApiCommand GetConnectedNdiSource() => new BirddogApiCommand("connectTo");
        public BirddogApiCommand GetNdiSourceList() => new BirddogApiCommand("List");
        public BirddogApiCommand RefreshNdiSources() => new BirddogApiCommand("refresh", "POST");

        #endregion

        #region Utility Methods

        /// <summary>
        /// Create a custom RESTful API command with optional body
        /// </summary>
        public BirddogApiCommand CustomRestCommand(string path, string method = "GET", object bodyObject = null)
        {
            var body = bodyObject != null ? JsonConvert.SerializeObject(bodyObject) : null;
            var command = new BirddogApiCommand(path, method, body);
            
            if (body != null)
            {
                command.Headers["Content-Type"] = "application/json";
            }
            
            return command;
        }

        /// <summary>
        /// Log command information for debugging
        /// </summary>
        public void LogCommand(BirddogApiCommand command, string context = "")
        {
            Debug.LogMessage(LogEventLevel.Debug, "BirddogCameraCommands", 
                "Command {0}: {1} {2}{3}", 
                context, 
                command.Method, 
                command.Path,
                !string.IsNullOrEmpty(command.Body) ? $" Body: {command.Body}" : "");
        }

        #endregion
    }
}