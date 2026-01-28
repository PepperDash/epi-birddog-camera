![PepperDash Logo](/images/logo_pdt_no_tagline_600.png)
# Birddog Camera Plugin

[Birddog X1/MAX API - PTZ](https://documenter.getpostman.com/view/29602224/2sAYHxn45i#a0f7b5e1-25bb-4a2d-bb4c-79f5dcf29d8f)

[Birddog API - PTZ](https://birddog.tv/AV/API/#api-PTZ)

## Essentials Device Configuration

```json
{
    "key": "camera-1",
    "name": "Camera 1",
    "type": "birddogCamera",
    "group": "plugin",
    "properties": {
        "control": {
            "method": "http",
            "tcpSshProperties": {
                "address": "192.168.1.101",
                "port": 8080
            }
        },
        "presets": [
            {
                "Name": "Preset 1",
                "Id": 1
            },
            {
                "Name": "Preset 2",
                "Id": 2
            },
            {
                "Name": "Preset 3",
                "Id": 3
            }
        ],
        "panSpeed": 25,
        "titlSpeed": 25,
        "zoomSpeed": 25
    }
}
```

## Essentials Bridging

```json
{
    "key": "plugin-bridge-1",
    "name": "Plugin Bridge",
    "group": "api",
    "type": "eiscApiAdvanced",
    "properties": {
        "control": {
            "tcpSshProperties": {
                "address": "127.0.0.2",
                "port": 0
            },
            "ipid": "B2",
            "method": "ipidTcp"
        },
        "devices": [
            {
                "deviceKey": "camera-1",
                "joinStart": 1
            }
        ]
    }
}
```

## Essentials Bridge Join Map

The join map below documents the commands implemented in this plugin.

### Digitals

| Input            | I/O | Output       |
| ---------------- | --- | ------------ |
| Tilt up          | 1   |              |
| Tilt down        | 2   |              |
| Pan left         | 3   |              |
| Pan right        | 4   |              |
| Zoom in          | 5   |              |
| Zoom out         | 6   |              |
| Power on         | 7   | Power on Fb  |
| Power off        | 8   | Power off fb |
|                  | 9   | Is online fb |
| Home             | 10  |              |
| Preset 1 recall  | 11  |              |
| Preset 2 recall  | 12  |              |
| Preset 15 recall | 25  |              |
| Preset 16 recall | 26  |              |
| Preset 1 save    | 31  |              |
| Preset 2 save    | 32  |              |
| Preset 15 save   | 45  |              |
| Preset 16 save   | 46  |              |
| Privacy on       | 48  |              |
| Privacy off      | 49  |              |

### Analogs

| Input      | I/O | Output               |
| ---------- | --- | -------------------- |
| Pan speed  | 1   | Pan speed fb         |
| Tilt speed | 2   | Tilt speed fb        |
| Zoom speed | 3   | Zoom speed fb        |
|            | 11  | Number of presets fb |

### Serials

| Input                | I/O | Output                  |
| -------------------- | --- | ----------------------- |
|                      | 1   | Device name fb          |
| IP address           | 2   | IP address fb           |
|                      | 11  | Preset 1 name fb        |
|                      | 12  | Preset 2 name fb        |
|                      | 25  | Preset 15 name fb       |
|                      | 26  | Preset 16 name fb       |
| Device communication | 50  | Device communication fb |


## PTZ Control Implementation

This plugin implements PTZ (Pan, Tilt, Zoom) control using Birddog's CGI-based API commands. The implementation provides:

### Supported PTZ Functions
- **Pan Control**: PanLeft(), PanRight(), PanStop()
- **Tilt Control**: TiltUp(), TiltDown(), TiltStop()  
- **Zoom Control**: ZoomIn(), ZoomOut(), ZoomStop()
- **Position Control**: PositionHome(), PositionPrivacy()
- **Preset Management**: Recall and Save presets (1-16)

### Error Handling
All PTZ methods include comprehensive error handling:
- HTTP request failures are caught and logged
- Network communication errors are reported via `Debug.LogError`
- Individual operation failures include descriptive error messages

### Speed Configuration
PTZ speeds can be configured through the device properties:
- **panSpeed**: Range 1-49 (default: 25) - Controls pan movement speed
- **tiltSpeed**: Range 1-49 (default: 25) - Controls tilt movement speed
- **zoomSpeed**: Range 1-49 (default: 25) - Controls zoom speed

*Note: The RESTful API v2.0 uses different ranges (Pan: 0-21, Tilt: 0-18, Zoom: 0-7), but this plugin uses the CGI-based API with its own speed mapping.*

### API Compatibility
This plugin uses **CGI-based commands** (`cgi-bin/aw_ptz`) for maximum compatibility with existing Birddog camera firmware. While Birddog also offers a newer RESTful API v2.0, the CGI approach ensures backward compatibility across different camera models and firmware versions.

## DEVJSON Commands

When using DEVJSON commands update the program index `devjson:{programIndex}` and `deviceKey` values to match the testing environment.

```json
devjson:1 {"deviceKey":"camera-1", "methodName":"CameraOn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"CameraOff", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"PanLeft", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PanRight", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PanStop", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"TiltUp", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"TiltDown", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"TiltStop", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"ZoomIn", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"ZoomOut", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"ZoomStop", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"PositionHome", "params":[]}
devjson:1 {"deviceKey":"camera-1", "methodName":"PositionPrivacy", "params":[]}

devjson:1 {"deviceKey":"camera-1", "methodName":"RecallPreset", "params":[4]}
devjson:1 {"deviceKey":"camera-1", "methodName":"SavePreset", "params":[9]}

devjson:1 {"deviceKey":"camera-1", "methodName":"SendCustomCommand", "params":["customCommandString"]}
```
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.16.2
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "BirddogCamera",
    "group": "Group",
    "properties": {
        "panSpeed": "SampleValue",
        "tiltSpeed": "SampleValue",
        "ZoomSpeed": "SampleValue",
        "FocusSpeed": "SampleValue",
        "PrivacyOnPreset": "SampleValue",
        "PrivacyOffPreset": "SampleValue",
        "pollTimeMs": 0
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera tilt up |
| 2 | R | Camera tilt down |
| 3 | R | Camera pan left |
| 4 | R | Camera pan right |
| 5 | R | Camera zoom in |
| 6 | R | Camera zoom out |
| 7 | R | Camera power on |
| 8 | R | Camera power off |
| 9 | R | Camera is online |
| 10 | R | Camera home |
| 11 | R | Camera preset recall |
| 30 | R | Camera preset saved Feedback |
| 31 | R | Camera preset save |
| 48 | R | Camera privacy on |
| 49 | R | Camera privacy off |

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera pan speed |
| 2 | R | Camera tilt speed |
| 3 | R | Camera zoom speed |
| 11 | R | Camera number of preset |
| 13 | R | Preset select by number |
| 14 | R | Preset store by number |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Camera device name |
| 2 | R | Camera IP Address |
| 11 | R | Camera preset names |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ICommunicationMonitor
- IRoutingSource
- IHasCameraOff
- IHasCameraPtzControl
- IHasCameraFocusControl
- IHasCameraPresets
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- JoinMapBaseAdvanced
- EssentialsBridgeableDevice
- CameraPropertiesConfig
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public BirddogApiCommand PositionHome()
- public BirddogApiCommand PositionPrivacy()
- public BirddogApiCommand PowerOn()
- public BirddogApiCommand PowerOff()
- public BirddogApiCommand GetAbout()
- public BirddogApiCommand GetHostname()
- public BirddogApiCommand GetVersion()
- public BirddogApiCommand Reboot()
- public BirddogApiCommand Restart()
- public BirddogApiCommand GetPtzSettings()
- public BirddogApiCommand SetPtzSettings(int panSpeed, int tiltSpeed, int zoomSpeed)
- public BirddogApiCommand PanLeft()
- public BirddogApiCommand PanRight()
- public BirddogApiCommand TiltUp()
- public BirddogApiCommand TiltDown()
- public BirddogApiCommand TiltUpLeft()
- public BirddogApiCommand TiltUpRight()
- public BirddogApiCommand TiltDownLeft()
- public BirddogApiCommand TiltDownRight()
- public BirddogApiCommand PanTiltStop()
- public BirddogApiCommand ZoomIn()
- public BirddogApiCommand ZoomOut()
- public BirddogApiCommand ZoomStop()
- public BirddogApiCommand FocusNear()
- public BirddogApiCommand FocusFar()
- public BirddogApiCommand FocusStop()
- public BirddogApiCommand RecallPresetRest(int preset)
- public BirddogApiCommand SavePresetRest(int preset)
- public BirddogApiCommand GetEncodeSettings()
- public BirddogApiCommand SetEncodeSettings(string streamName, string videoFormat = "1080p50", 
            string colorBitDepth = "8Bit", string videoSampleRate = "420")
- public BirddogApiCommand GetAudioSettings()
- public BirddogApiCommand SetAudioSettings(int audioInGain = 80, int audioOutGain = 80, 
            string outputSelect = "DecodeMain")
- public BirddogApiCommand GetPictureSettings()
- public BirddogApiCommand SetPictureSettings(int brightness = 2, int contrast = 1, int color = 8,
            int hue = 7, int sharpness = 122, bool flip = false, bool mirror = false)
- public BirddogApiCommand GetExposureSettings()
- public BirddogApiCommand SetExposureSettings(string expMode = "FULL-AUTO", int brightLevel = 24,
            int gainLevel = 4, int gainLimit = 11, int shutterSpeed = 16)
- public BirddogApiCommand GetWhiteBalanceSettings()
- public BirddogApiCommand SetWhiteBalanceSettings(string wbMode = "AUTO", int colorTemp = 5600,
            int redGain = 179, int blueGain = 174)
- public BirddogApiCommand ConnectToNdiSource(string sourceName)
- public BirddogApiCommand GetConnectedNdiSource()
- public BirddogApiCommand GetNdiSourceList()
- public BirddogApiCommand RefreshNdiSources()
- public BirddogApiCommand CustomRestCommand(string path, string method = "GET", object bodyObject = null)
- public void LogCommand(BirddogApiCommand command, string context = "")
- public void LinkPowerControlsToApi(BasicTriList trilist, BirddogCameraBridgeJoinMap joinMap)
- public void LinkPtzControlsToApi(BasicTriList trilist, BirddogCameraBridgeJoinMap joinMap)
- public void LinkPresetControlsToApi(BasicTriList trilist, BirddogCameraBridgeJoinMap joinMap)
- public void UpdateFeedbacks()
- public void SendRequest(BirddogApiCommand command)
- public void CameraOn()
- public void CameraOff()
- public void PositionHome()
- public void PositionPrivacy()
- public void PanLeft()
- public void PanRight()
- public void PanStop()
- public void TiltDown()
- public void TiltUp()
- public void TiltStop()
- public void ZoomIn()
- public void ZoomOut()
- public void ZoomStop()
- public void FocusNear()
- public void FocusFar()
- public void FocusStop()
- public void TriggerAutoFocus()
- public void PresetSelect(int preset)
- public void PresetStore(int preset, string description)
- public void GetDeviceInfo()
- public void SetPtzSpeeds(int panSpeed, int tiltSpeed, int zoomSpeed)
- public void GetPtzSettings()
- public void SetPictureSettings(int brightness = 2, int contrast = 1, int color = 8,
            int hue = 7, int sharpness = 122, bool flip = false, bool mirror = false)
- public void SetAudioSettings(int audioInGain = 80, int audioOutGain = 80, string outputSelect = "DecodeMain")
- public EssentialsControlPropertiesConfig GetControlFromDeviceConfig(DeviceConfig dc)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IsOnlineFeedback
- CameraIsOffFeedback
- PresetSavedFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- PanSpeedFeedback
- TiltSpeedFeedback
- ZoomSpeedFeedback
- NumberOfPresetsFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
