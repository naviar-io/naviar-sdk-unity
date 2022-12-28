# naviar SDK (Unity3D)

This is **naviar** SDK for Unity3D engine. Main features are:
- High-precision global user position localization for your AR apps
- Easy to use public API and prefabs
- Supports Android and iOS target platforms
- Integration in ARFoundation (ARCore and ARKit)

## Requirements

- Unity 2021.1+
- ARKit or ARCore supported device

## Installation

Just clone this repository. Requires installed [Git-LFS](https://git-lfs.github.com).

You can also add git URL to the **Unity Package Manager** UI in your project dependencies:
```
https://github.com/naviar-io/naviar-sdk-unity.git?path=/Assets
```

## Examples

SDK includes an example scene with a basic VPS setup and graphics. Load project in Unity Editor and open `Scenes/TestScene`. 

You can run this scene in Editor or build it on your mobile device.

## Usage

### Testing your app

When you start VPS in Editor, it loads an image from `Mock Provider`. You can change this image by selecting `VPS/MockData/FakeCamera` component in Example scene hierarchy.

You can also enable `Mock Mode` for device builds. Just toggle `Use Mock` property in `VPS/VPSLocalisationService` and rebuild your app.

## Free flight mode

During testing in Unity Editor you can switch on a Free flight mode. The component of the Free flight mode named FreeFlightSimulation is located on VPS/FreeFlightSimulation in TestScene

Features:
* to switch on the Free flight mode press button ToggleFreeFlightMode (located on VPSLocalisationService, Tab by default);
* after switching on VPS will be stopped;
* after switching a canvas IsFreeFlightPrefab on will be loaded and enabled. It reports the information about this mode. You can replace it with your custom or set null;
* when you press SimulateLocalizationButton (E by default) you will be localized in FreeFlightSimalution's child (LocalizationPose) position;
* you can rotate camera with mouse drag; the cursor will be locked by default, but you can change this behavior in the field LockCursor in FreeFlightSimalution. Also you can tune
mouse sensitivity and maximum tilt angle;
* you can move camera in the scene using buttons W (forward), A (left), S (back), D (right) and Left Shift for acceleration. You can tune speed and acceleration ratio in FreeFlightSimalution;
* if you try StartVPS when Free flight mode is on, VPS will not start, but after LocalizationDelay (3 by default) seconds the success localisation event with current camera pose will be send;
* to switch off the Free flight mode press button ToggleFreeFlightMode (located on VPSLocalisationService, Tab by default) again. Notice that it will reset you tracking and you will need to call StartVPS to start VPS in default mode.

During testing in Unity Editor in TestScene you can control VPS manually. There are two buttons are defined for it in ManualControl on VPS object:
* StartVPSKeyCode - start VPS with default settings (I by default)
* ResetKeyCode - reset VPS tracking (T by default)

### VPS Settings

You can adjust VPS behaviour by changing public properties in the `VPSLocalisationService` component:

| Property Name | Description | Default |
| ------ | ------ | ------ |
| **Start On Awake** | Should VPS start on Awake or be activated manually. | true |
| **Use Mock** | Use mock provider when VPS service has started. Allows to test VPS in Editor. | false |
| **Force Mock in Editor** | Always use mock provider in Editor, even if UseMock is false .| true |
| **Localization Mode** | There are three modes available: FEATURES - process images with neural network before sending. Mandatory for production-ready apps; TEXTURE - send image without process with neural network; BOTH - process images with neural network and send result with original image | FEATURES |
| **Send GPS** | Send user GPS location. Recomended for outdoor locations. | true |
| **FailsCountToReset** | Number of fails to reset current VPS session (number of attempts to correct position taking into account previous localization result) | 5 |
| **Location Ids** | Ids of current location(s) | ... |
| **Save Images Localy** | Save sent images and meta in persistent data folder (used for debag) | false |

## License 

This project is licensed under [MIT License](LICENSE).

TensorFlow library is licensed under [Apache License 2.0.](https://github.com/tensorflow/tensorflow/blob/master/LICENSE)
