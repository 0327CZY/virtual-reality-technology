using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace QuestCounter
{
    /// <summary>
    /// 读取头显和右手柄的位姿并应用到相机和手的 Transform 上，同时提供扳机读取、震动，
    /// 以及没有头显时的桌面预览（右键转视角，WASD 移动）。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class XRTrackedRig : MonoBehaviour
    {
        private const float DesktopEyeHeight = 1.65f;

        public XRNode ControllerNode = XRNode.RightHand;

        private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
        private InputDevice controller;
        private float originHeight = DesktopEyeHeight;
        private float nextOriginCheckTime;
        private float yaw;
        private float pitch;
        private bool wasXRActive;

        public Camera View { get; private set; }
        public Transform Hand { get; private set; }
        public bool HeadTracked { get; private set; }
        public bool HandTracked { get; private set; }
        public bool IsXRActive { get { return XRSettings.isDeviceActive; } }

        public void Initialize(Camera view, Transform hand)
        {
            View = view;
            Hand = hand;
        }

        // 在渲染前再更新一次位姿，减少画面相对头部的延迟。
        private void OnEnable()
        {
            Application.onBeforeRender += UpdatePoses;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= UpdatePoses;
        }

        private void Update()
        {
            if (View == null)
            {
                return;
            }

            if (IsXRActive)
            {
                RefreshTrackingOriginOncePerSecond();
                UpdatePoses();
            }
            else
            {
                HeadTracked = false;
                HandTracked = false;
                if (wasXRActive)
                {
                    ResetDesktopCamera();
                }
                UpdateDesktopCamera();
            }

            wasXRActive = IsXRActive;
        }

        // 优先使用地面原点（位姿以地板为 0 高度）；设备不支持时用固定眼高估算。
        private void RefreshTrackingOriginOncePerSecond()
        {
            if (Time.unscaledTime < nextOriginCheckTime)
            {
                return;
            }
            nextOriginCheckTime = Time.unscaledTime + 1f;

            SubsystemManager.GetInstances(inputSubsystems);
            foreach (XRInputSubsystem subsystem in inputSubsystems)
            {
                if (!subsystem.running)
                {
                    continue;
                }

                if ((subsystem.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0)
                {
                    subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                }

                bool isFloorOrigin = subsystem.GetTrackingOriginMode() == TrackingOriginModeFlags.Floor;
                originHeight = isFloorOrigin ? 0f : DesktopEyeHeight;
            }
        }

        private void UpdatePoses()
        {
            if (View == null || !IsXRActive)
            {
                return;
            }

            Vector3 position;
            Quaternion rotation;

            HeadTracked = TryGetPose(InputDevices.GetDeviceAtXRNode(XRNode.Head), out position, out rotation);
            if (HeadTracked)
            {
                View.transform.SetLocalPositionAndRotation(position + Vector3.up * originHeight, rotation);
            }

            controller = InputDevices.GetDeviceAtXRNode(ControllerNode);
            HandTracked = TryGetPose(controller, out position, out rotation);
            if (HandTracked)
            {
                Hand.SetLocalPositionAndRotation(position + Vector3.up * originHeight, rotation);
            }
        }

        private static bool TryGetPose(InputDevice device, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            bool isTracked;
            return device.isValid
                && device.TryGetFeatureValue(CommonUsages.isTracked, out isTracked) && isTracked
                && device.TryGetFeatureValue(CommonUsages.devicePosition, out position)
                && device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
        }

        // 读取扳机是否按下。头部或手柄没有追踪到时返回 false，调用方应当忽略输入。
        public bool TryReadTriggerPressed(out bool pressed)
        {
            pressed = false;
            if (!IsXRActive || !HeadTracked || !HandTracked)
            {
                return false;
            }

            if (controller.TryGetFeatureValue(CommonUsages.triggerButton, out pressed))
            {
                return true;
            }

            // 个别设备没有 triggerButton，退而求其次，用模拟量超过一半算按下。
            float amount;
            if (controller.TryGetFeatureValue(CommonUsages.trigger, out amount))
            {
                pressed = amount > 0.5f;
                return true;
            }
            return false;
        }

        // 选中目标时给手柄一个很短的震动。
        public void Pulse()
        {
            HapticCapabilities capabilities;
            if (controller.isValid && controller.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                controller.SendHapticImpulse(0, 0.25f, 0.035f);
            }
        }

        private void ResetDesktopCamera()
        {
            View.transform.localPosition = new Vector3(0f, DesktopEyeHeight, 0f);
            View.transform.localRotation = Quaternion.identity;
            yaw = 0f;
            pitch = 0f;
        }

        private void UpdateDesktopCamera()
        {
            if (!Application.isFocused)
            {
                return;
            }

            // 按住右键拖动：转动视角，俯仰限制在 +-65 度。
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * 2f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2f, -65f, 65f);
                View.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // WASD 沿相机的左右和前后方向平移。
            float sideways = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 move = View.transform.right * sideways + View.transform.forward * forward;
            View.transform.position += move * (Time.unscaledDeltaTime * 0.7f);
        }
    }
}
