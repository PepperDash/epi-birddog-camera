using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugins.Birddog.Camera
{
    public class BirddogCameraController : EssentialsBridgeableDevice, ICommunicationMonitor, IRoutingSource,
        IHasCameraOff, IHasCameraPtzControl, IHasCameraFocusControl, IHasCameraPresets
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2),
        };

        private readonly EssentialsControlPropertiesConfig _control;
        private readonly BirddogCameraConfig _properties;
        private readonly IBasicCommunication _comm;
        private readonly BirddogCameraCommands _commands;
        private readonly CommandQueueManager _queueManager;


        public StatusMonitorBase CommunicationMonitor { get; private set; }
        public BoolFeedback IsOnlineFeedback { get { return CommunicationMonitor.IsOnlineFeedback; } }

        public RoutingPortCollection<RoutingOutputPort> OutputPorts => new RoutingPortCollection<RoutingOutputPort>();


        private bool powerIsOff;
        public bool PowerIsOff
        {
            get { return powerIsOff; }
            set
            {
                powerIsOff = value;
                CameraIsOffFeedback.FireUpdate();
            }
        }
        public BoolFeedback CameraIsOffFeedback => new BoolFeedback("cameraIsOff", () => PowerIsOff);

        public bool CanPan => true;
        public bool CanTilt => true;
        public bool CanZoom => true;
        public bool CanFocus => true;

        public IntFeedback PanSpeedFeedback => new IntFeedback("panSpeed", () => _commands.PanSpeed);
        public IntFeedback TiltSpeedFeedback => new IntFeedback("tiltSpeed", () => _commands.TiltSpeed);
        public IntFeedback ZoomSpeedFeedback => new IntFeedback("zoomSpeed", () => _commands.ZoomSpeed);

        public List<CameraPreset> Presets { get; set; } = new List<CameraPreset>();

        public IntFeedback NumberOfPresetsFeedback => new IntFeedback("numberOfPresets", () => (ushort)Presets.Count);
        public Dictionary<uint, StringFeedback> PresetNamesFeedbacks
        {
            get
            {
                var dict = new Dictionary<uint, StringFeedback>();
                if (Presets == null)
                    return dict;
                    
                for (uint i = 1; i <= Presets.Count; i++)
                {
                    var preset = Presets[(int)i - 1];
                    if (preset == null)
                        continue;
                        
                    dict[i] = new StringFeedback(string.Format("presetName{0}", i), () => preset?.Description ?? string.Empty);
                }
                return dict;
            }
        }

        public BoolFeedback PresetSavedFeedback => new BoolFeedback("presetSaved", () => false);
        public event EventHandler<EventArgs> PresetsListHasChanged;

        /// <summary>
        /// Raises the PresetsListHasChanged event
        /// </summary>
        protected void OnPresetsListHasChanged()
        {
            var handler = PresetsListHasChanged;
            if (handler == null)
                return;

            handler.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">device key</param>
        /// <param name="name">device name</param>
        /// <param name="config">device config</param>
        public BirddogCameraController(string key, string name, EssentialsControlPropertiesConfig control, IBasicCommunication comm, BirddogCameraConfig config)
            : base(key, name)
        {
            this._control = control;
            this._properties = config;
            this._comm = comm;

            // Initialize command builder and configure speeds from properties
            _commands = new BirddogCameraCommands();

            // Initialize queue system
            _queueManager = new CommandQueueManager(
                deviceKey: key,
                getBaseUri: () => $"http://{_control.TcpSshProperties.Address}:{_control.TcpSshProperties.Port}",
                httpClient: _httpClient
            );

            // Subscribe to queue events
            _queueManager.ResponseReceived += OnResponseReceived;
            _queueManager.RequestFailed += OnRequestFailed;

            // Set PTZ speeds using Birddog API ranges
            if (config != null)
            {
                // Map from config values to Birddog API ranges if provided
                if (config.PanSpeed > 0) _commands.PanSpeed = Math.Min(21, (int)config.PanSpeed);
                if (config.TiltSpeed > 0) _commands.TiltSpeed = Math.Min(18, (int)config.TiltSpeed);
                if (config.ZoomSpeed > 0) _commands.ZoomSpeed = Math.Min(7, (int)config.ZoomSpeed);

                Presets = config.Presets ?? new List<CameraPreset>();
            }
            else
            {
                // Ensure Presets is initialized even if config is null
                Presets = new List<CameraPreset>();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _queueManager.StartProcessing();
        }

        /// <summary>
        /// Links the plugin device to the EISC bridge
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        /// <exception cref="NotImplementedException"></exception>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new BirddogCameraBridgeJoinMap(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.LogInformation("BirddogCamera", "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.LogInformation("BirddogCamera", "Linking to Bridge Type {0}", GetType().Name);

            // links to bridge
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            LinkPowerControlsToApi(trilist, joinMap);
            LinkPtzControlsToApi(trilist, joinMap);
            LinkPresetControlsToApi(trilist, joinMap);

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

                UpdateFeedbacks();
            };
        }

        public void LinkPowerControlsToApi(BasicTriList trilist, BirddogCameraBridgeJoinMap joinMap)
        {
            trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, CameraOn);
            trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, CameraOff);

            CameraIsOffFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOff.JoinNumber]);
            CameraIsOffFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);
        }

        public void LinkPtzControlsToApi(BasicTriList trilist, BirddogCameraBridgeJoinMap joinMap)
        {
            PanSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.PanSpeed.JoinNumber]);
            TiltSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.TiltSpeed.JoinNumber]);
            ZoomSpeedFeedback.LinkInputSig(trilist.UShortInput[joinMap.ZoomSpeed.JoinNumber]);

            trilist.SetUShortSigAction(joinMap.PanSpeed.JoinNumber, panSpeed => _commands.PanSpeed = panSpeed);
            trilist.SetUShortSigAction(joinMap.TiltSpeed.JoinNumber, tiltSpeed => _commands.TiltSpeed = tiltSpeed);
            trilist.SetUShortSigAction(joinMap.ZoomSpeed.JoinNumber, zoomSpeed => _commands.ZoomSpeed = zoomSpeed);

            trilist.SetBoolSigAction(joinMap.PanLeft.JoinNumber, sig =>
            {
                if (sig) PanLeft();
                else PanStop();
            });

            trilist.SetBoolSigAction(joinMap.PanRight.JoinNumber, sig =>
            {
                if (sig) PanRight();
                else PanStop();
            });

            trilist.SetBoolSigAction(joinMap.TiltUp.JoinNumber, sig =>
            {
                if (sig) TiltUp();
                else TiltStop();
            });

            trilist.SetBoolSigAction(joinMap.TiltDown.JoinNumber, sig =>
            {
                if (sig) TiltDown();
                else TiltStop();
            });

            trilist.SetBoolSigAction(joinMap.ZoomIn.JoinNumber, sig =>
            {
                if (sig) ZoomIn();
                else ZoomStop();
            });

            trilist.SetBoolSigAction(joinMap.ZoomOut.JoinNumber, sig =>
            {
                if (sig) ZoomOut();
                else ZoomStop();
            });
        }

        public void LinkPresetControlsToApi(BasicTriList trilist, BirddogCameraBridgeJoinMap joinMap)
        {
            NumberOfPresetsFeedback.LinkInputSig(trilist.UShortInput[joinMap.NumberOfPresets.JoinNumber]);
            PresetSavedFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PresetSavedFeedback.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.PrivacyOn.JoinNumber, PositionPrivacy);
            trilist.SetSigTrueAction(joinMap.PrivacyOff.JoinNumber, () => PresetSelect(1));
            trilist.SetSigTrueAction(joinMap.Home.JoinNumber, PositionHome);

            foreach (var preset in PresetNamesFeedbacks)
            {
                Debug.LogDebug("BirddogCamera", "foreach: preset.Key: {0} preset.Value: {1}", preset.Key, preset.Value);
                var presetNumber = preset.Key;
                var nameJoin = joinMap.PresetNames.JoinNumber + presetNumber - 1;

                preset.Value.LinkInputSig(trilist.StringInput[nameJoin]);
                preset.Value.FireUpdate();

                var recallJoin = joinMap.PresetRecall.JoinNumber + presetNumber - 1;
                var saveJoin = joinMap.PresetSave.JoinNumber + presetNumber - 1;

                trilist.SetSigHeldAction(recallJoin, 5000, () => PresetStore((int)presetNumber, ""), () => PresetSelect((int)presetNumber));
                trilist.SetSigTrueAction(saveJoin, () => PresetStore((int)presetNumber, ""));
            }
        }

        public void UpdateFeedbacks()
        {
            IsOnlineFeedback.FireUpdate();
            CameraIsOffFeedback.FireUpdate();
            PanSpeedFeedback.FireUpdate();
            TiltSpeedFeedback.FireUpdate();
            ZoomSpeedFeedback.FireUpdate();
            NumberOfPresetsFeedback.FireUpdate();
            PresetSavedFeedback.FireUpdate();

            foreach (var preset in PresetNamesFeedbacks)
            {
                preset.Value.FireUpdate();
            }
        }


        /// <summary>
        /// Handles responses received from the queue manager
        /// </summary>
        private void OnResponseReceived(object sender, CameraResponse response)
        {
            try
            {
                // Parse response content and update device state accordingly
                ParseResponseContent(response);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "Error handling response: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handles failed requests from the queue manager
        /// </summary>
        private void OnRequestFailed(object sender, QueuedRequest failedRequest)
        {
            Debug.LogError(this, "Request failed after retries: {0} {1}",
                failedRequest.Command.Method, failedRequest.Command.Path);

            // Update communication monitor or other status indicators if needed
            // This could be used to set device offline status after multiple failures
        }


        /// <summary>
        /// Parses response content and updates device state accordingly
        /// </summary>
        private void ParseResponseContent(CameraResponse response)
        {
            try
            {
                if (string.IsNullOrEmpty(response.Content))
                    return;

                // Add response parsing logic here based on your needs
                // Examples:
                // - Parse PTZ settings responses to update speed feedbacks
                // - Parse preset responses to update preset status
                // - Parse device info responses to update online status

                Debug.LogVerbose(this, $"ParseResponseContent: requestId {response.RequestId}");
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"ParseResponseContent: Exception message\n{ex.Message}");
                Debug.LogError(this, $"ParseResponseContent: Exception content\n: {response.Content}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"ParseResponseContent: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"ParseResponseContent: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        #region Queue Management Methods

        /// <summary>
        /// Gets the current queue status
        /// </summary>
        public (int RequestCount, int ResponseCount) GetQueueStatus()
        {
            return _queueManager.GetQueueStatus();
        }

        /// <summary>
        /// Clears all pending requests from the queue
        /// </summary>
        public void ClearRequestQueue()
        {
            _queueManager.ClearRequestQueue();
        }

        /// <summary>
        /// Clears all pending responses from the queue
        /// </summary>
        public void ClearResponseQueue()
        {
            _queueManager.ClearResponseQueue();
        }

        /// <summary>
        /// Gets whether queue processing is currently enabled
        /// </summary>
        public bool IsQueueProcessingEnabled => _queueManager.IsProcessingEnabled;

        /// <summary>
        /// Sends a high-priority request that bypasses the queue (use sparingly)
        /// </summary>
        /// <param name="command">The command to send immediately</param>
        /// <param name="onSuccess">Optional callback for successful response</param>
        /// <param name="onError">Optional callback for error response</param>
        public async Task SendHighPriorityRequest(BirddogApiCommand command, Action<string> onSuccess = null, Action<string> onError = null)
        {
            await _queueManager.SendHighPriorityRequest(command, onSuccess, onError);
        }

        /// <summary>
        /// Logs current queue status for debugging
        /// </summary>
        public void LogQueueStatus()
        {
            _queueManager.LogQueueStatus();
        }

        #endregion


        /// <summary>
        /// Sends a request using the queue system (recommended)
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <param name="onSuccess">Optional callback for successful response</param>
        /// <param name="onError">Optional callback for error response</param>
        public void SendRequest(BirddogApiCommand command, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                if (!_queueManager.IsProcessingEnabled)
                {
                    Debug.LogWarning(this, "Queue processing is not enabled. Starting queue processing.");
                    _queueManager.StartProcessing();
                }

                _queueManager.EnqueueRequest(command, onSuccess, onError);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"SendRequest: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"SendRequest: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"SendRequest: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"SendRequest: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Sends a request synchronously (legacy method - use SendRequest instead)
        /// </summary>
        /// <param name="command">The command to send</param>
        [Obsolete("Use SendRequest() with queue system instead")]
        public void SendRequestDirect(BirddogApiCommand command)
        {
            try
            {
                var baseUri = $"http://{_control.TcpSshProperties.Address}:{_control.TcpSshProperties.Port}";
                var requestUri = new Uri(new Uri(baseUri), command.Path);

                using (var request = new HttpRequestMessage())
                {
                    request.RequestUri = requestUri;
                    request.Method = command.Method.ToUpper() == "POST" ? HttpMethod.Post : HttpMethod.Get;

                    // Add headers to the request
                    foreach (var header in command.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    // Add content for POST requests
                    if (command.Method.ToUpper() == "POST")
                    {
                        request.Content = !string.IsNullOrEmpty(command.Body)
                            ? new StringContent(command.Body, System.Text.Encoding.UTF8, "application/json")
                            : new StringContent("");
                    }

                    var response = _httpClient.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        Debug.LogWarning(this, "Response: {0}", responseContent);
                    }
                    else
                    {
                        Debug.LogError(this, "Error: {0} - {1}", response.StatusCode, response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"SendRequestDirect: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"SendRequestDirect: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"SendRequestDirect: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"SendRequestDirect: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }


        public void CameraOn()
        {
            try
            {
                var command = _commands.PowerOn();
                SendRequest(command,
                    onSuccess: (response) =>
                    {
                        Debug.LogInformation(this, "Camera power on successful");
                        // Update power state feedback if needed
                        CameraIsOffFeedback.FireUpdate();
                    },
                    onError: (error) =>
                    {
                        Debug.LogError(this, "Camera power on failed: {0}", error);
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"CameraOn: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"CameraOn: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"CameraOn: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"CameraOn: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        public void CameraOff()
        {
            try
            {
                var command = _commands.PowerOff();
                SendRequest(command,
                    onSuccess: (response) =>
                    {
                        Debug.LogInformation(this, "Camera power off successful");
                        // Update power state feedback if needed
                        CameraIsOffFeedback.FireUpdate();
                    },
                    onError: (error) =>
                    {
                        Debug.LogError(this, "Camera power on failed: {0}", error);
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"CameraOff: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"CameraOff: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"CameraOff: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"CameraOff: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        public void PositionHome()
        {
            try
            {
                var command = _commands.PositionHome();
                SendRequest(command);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"PositionHome: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"PositionHome: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"PositionHome: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"PositionHome: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        public void PositionPrivacy()
        {
            try
            {
                var command = _commands.PositionPrivacy();
                SendRequest(command);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"PositionPrivacy: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"PositionPrivacy: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"PositionPrivacy: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"PositionPrivacy: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        public void PanLeft()
        {
            var command = _commands.PanLeft();
            SendRequest(command);
        }

        public void PanRight()
        {
            var command = _commands.PanRight();
            SendRequest(command);
        }

        public void PanStop()
        {
            var command = _commands.PanTiltStop();
            SendRequest(command);
        }

        public void TiltDown()
        {
            var command = _commands.TiltDown();
            SendRequest(command);
        }

        public void TiltUp()
        {
            var command = _commands.TiltUp();
            SendRequest(command);
        }

        public void TiltStop()
        {
            var command = _commands.PanTiltStop();
            SendRequest(command);
        }

        public void ZoomIn()
        {
            var command = _commands.ZoomIn();
            SendRequest(command);
        }

        public void ZoomOut()
        {
            var command = _commands.ZoomOut();
            SendRequest(command);
        }

        public void ZoomStop()
        {
            var command = _commands.ZoomStop();
            SendRequest(command);
        }

        public void FocusNear()
        {
            var command = _commands.FocusNear();
            SendRequest(command);
        }

        public void FocusFar()
        {
            var command = _commands.FocusFar();
            SendRequest(command);
        }

        public void FocusStop()
        {
            var command = _commands.FocusStop();
            SendRequest(command);
        }

        public void TriggerAutoFocus()
        {
            Debug.LogError(this, "TriggerAutoFocus not implemented - Use exposure settings to control focus");
        }

        public void PresetSelect(int preset)
        {
            try
            {
                var command = _commands.RecallPresetRest(preset);
                SendRequest(command,
                    onSuccess: (response) =>
                    {
                        Debug.LogInformation(this, "Preset {0} recalled successfully", preset);
                        // You could parse the response to confirm preset position
                    },
                    onError: (error) =>
                    {
                        Debug.LogError(this, "Preset {0} recall failed: {1}", preset, error);
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "PresetSelect failed: {0}", ex.Message);
            }
        }

        public void PresetStore(int preset, string description)
        {
            try
            {
                var command = _commands.SavePresetRest(preset);
                SendRequest(command,
                    onSuccess: (response) =>
                    {
                        Debug.LogInformation(this, "Preset {0} saved successfully", preset);
                        PresetSavedFeedback.FireUpdate();
                    },
                    onError: (error) =>
                    {
                        Debug.LogError(this, "Preset {0} save failed: {1}", preset, error);
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "PresetStore failed: {0}", ex.Message);
            }
        }

        #region Additional Birddog Camera Methods

        /// <summary>
        /// Get device information from the camera
        /// </summary>
        public void GetDeviceInfo()
        {
            try
            {
                var command = _commands.GetAbout();
                SendRequest(command,
                    onSuccess: (response) =>
                    {
                        Debug.LogInformation(this, "Device info received: {0}", response);
                        // Parse device info here and update relevant feedbacks
                        // Example: Parse version, model, status, etc.
                        ParseDeviceInfo(response);
                    },
                    onError: (error) =>
                    {
                        Debug.LogError(this, "Failed to get device info: {0}", error);
                    });
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "GetDeviceInfo failed: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Parses device information response
        /// </summary>
        private void ParseDeviceInfo(string response)
        {
            try
            {
                // TODO - Add parsing logic h
                if (!string.IsNullOrEmpty(response))
                {
                    Debug.LogVerbose(this, $"ParseDeviceInfo: response >>>>\n{response}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(this, $"ParseDeviceInfo: Exception Message\n{ex.Message}");
                Debug.LogError(this, $"ParseDeviceInfo: Exception StackTrace\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.LogError(this, $"ParseDeviceInfo: Inner Exception Message\n{ex.InnerException.Message}");
                    Debug.LogError(this, $"ParseDeviceInfo: Inner Exception StackTrace\n{ex.InnerException.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Set PTZ speeds on the camera
        /// </summary>
        public void SetPtzSpeeds(int panSpeed, int tiltSpeed, int zoomSpeed)
        {
            try
            {
                var command = _commands.SetPtzSettings(panSpeed, tiltSpeed, zoomSpeed);
                SendRequest(command);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "SetPtzSpeeds failed: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Get current PTZ settings from the camera
        /// </summary>
        public void GetPtzSettings()
        {
            try
            {
                var command = _commands.GetPtzSettings();
                SendRequest(command);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "GetPtzSettings failed: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Set picture settings on the camera
        /// </summary>
        public void SetPictureSettings(int brightness = 2, int contrast = 1, int color = 8,
            int hue = 7, int sharpness = 122, bool flip = false, bool mirror = false)
        {
            try
            {
                var command = _commands.SetPictureSettings(brightness, contrast, color, hue, sharpness, flip, mirror);
                SendRequest(command);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "SetPictureSettings failed: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Set audio settings on the camera
        /// </summary>
        public void SetAudioSettings(int audioInGain = 80, int audioOutGain = 80, string outputSelect = "DecodeMain")
        {
            try
            {
                var command = _commands.SetAudioSettings(audioInGain, audioOutGain, outputSelect);
                SendRequest(command);
            }
            catch (Exception ex)
            {
                Debug.LogError(this, "SetAudioSettings failed: {0}", ex.Message);
            }
        }

        #endregion
    }
}