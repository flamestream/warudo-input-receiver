using System.Linq;
using Warudo.Core;

namespace FlameStream
{
    public partial class InputReceiverAsset : ReceiverAsset {

        public const float DEFAULT_DEADZONE_RADIUS = 0.05f;
        public const int MAX_BUTTON_COUNT = 128;
        public const int MAX_SWITCH_COUNT = 8;
        public const int MAX_AXIS_COUNT = 8;

        // Buttons
        public bool[,] ButtonDownHistoryRegistry = new bool[MAX_BUTTON_COUNT, 2];
        public bool[] ButtonJustDownRegistry = new bool[MAX_BUTTON_COUNT];
        public bool[] ButtonJustUpDiffRegistry = new bool[MAX_BUTTON_COUNT];

        // Switches
        public int[,] SwitchSubIndexHistoryRegistry = new int[MAX_SWITCH_COUNT, 2];
        public bool[] SwitchSubIndexJustChangedRegistry = new bool[MAX_SWITCH_COUNT];
        public bool[] SwitchJustActiveRegistry = new bool[MAX_SWITCH_COUNT];
        public bool[] SwitchJustInactiveRegistry = new bool[MAX_SWITCH_COUNT];
        public int[,] SwitchSubIndexChangeHistoryRegistry = new int[MAX_SWITCH_COUNT, 3];

        // Axes
        public float[,] AxisValueRegistry = new float[MAX_AXIS_COUNT, 2];
        public float[] AxisAdjustedValueRegistry = new float[MAX_AXIS_COUNT];
        public bool[] AxisActiveRegistry = new bool[MAX_AXIS_COUNT];
        public bool[] AxisJustActiveRegistry = new bool[MAX_AXIS_COUNT];
        public bool[] AxisJustInactiveRegistry = new bool[MAX_AXIS_COUNT];

        void OnUpdateState() {
            if (lastState == null) return;

            var parts = lastState.Split(';');
            byte.TryParse(parts[0], out byte protocolVersion);
            if (protocolVersion != PROTOCOL_VERSION) {
                StopReceiver();
                SetMessage($"Invalid GameInput protocol '{protocolVersion}'. Expected '{PROTOCOL_VERSION}'\n\nPlease download compatible version of emitter at https://github.com/flamestream/input-device-emitter/releases");
                return;
            }
            if (parts.Length < 4) {
                StopReceiver();
                SetMessage("Invalid GameInput data format. Are you listening to the right port?");
                return;
            }

            // Part 1: Button flags
            var part1 = parts[1];
            var buttonDetectCount = part1.Length;
            for (var i = buttonDetectCount - 1; i >= 0; --i) {
                var idx = buttonDetectCount - i - 1;
                var isButtonLastDown = ButtonDownHistoryRegistry[idx,1] = ButtonDownHistoryRegistry[idx,0];

                var isButtonDown = part1[i] == '1';
                ButtonDownHistoryRegistry[idx,0] = part1[i] == '1';

                if (isButtonDown) {
                    // Compute just-down
                    ButtonJustDownRegistry[idx] = !isButtonLastDown && isButtonDown;
                    if (IsWaitingForButtonPress) {
                        // Display toast for identified button
                        var button = ButtonDefinitions.FirstOrDefault(d => d.Index == idx);
                        var buttonHeader = (button != null) ? button.GetHeader() : "Yet to be defined. Please add new definition";
                        Context.Service?.Toast(
                            Warudo.Core.Server.ToastSeverity.Info,
                            $"Button [{idx}] Identified",
                            $"Header: {buttonHeader}"
                        );
                        TriggerCancelIdentifyButton();
                    }
                } else {
                    // Compute just-up
                    ButtonJustUpDiffRegistry[idx] = isButtonLastDown && !isButtonDown;
                }
            }

            // Part 2: Switch states
            var part2 = parts[2];
            var switchCount = part2.Length;
            for (var i = 0; i < switchCount; ++i) {
                SwitchSubIndexHistoryRegistry[i,1] = SwitchSubIndexHistoryRegistry[i,0];

                // Set current state
                var currentValue = part2[i] - '0';
                SwitchSubIndexHistoryRegistry[i,0] = currentValue;

                var isJustChanged = SwitchSubIndexJustChangedRegistry[i] = SwitchSubIndexHistoryRegistry[i,1] != SwitchSubIndexHistoryRegistry[i,0];
                if (isJustChanged) {
                    SwitchSubIndexChangeHistoryRegistry[i,2] = SwitchSubIndexChangeHistoryRegistry[i,1];
                    SwitchSubIndexChangeHistoryRegistry[i,1] = SwitchSubIndexChangeHistoryRegistry[i,0];
                    SwitchSubIndexChangeHistoryRegistry[i,0] = currentValue;
                }
                SwitchJustActiveRegistry[i] = isJustChanged && SwitchSubIndexHistoryRegistry[i,0] != 0;
                SwitchJustInactiveRegistry[i] = isJustChanged && SwitchSubIndexHistoryRegistry[i,0] == 0;
            }

            // Part 3: Axes states
            var part3 = parts[3];
            var axisValues = part3.Split('|');
            var axisCount = axisValues.Length;
            for (var i = 0; i < axisCount; ++i) {
                AxisValueRegistry[i,1] = AxisValueRegistry[i,0];
                float.TryParse(axisValues[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value);
                AxisValueRegistry[i,0] = value;

                var isLastActive = AxisActiveRegistry[i];
                var isCurrentlyActive = false;
                var definition = AxisDefinitionsReference[i];
                if (definition != null) {
                    AxisAdjustedValueRegistry[i] = definition.CalculateAdjustedValue(value);
                    isCurrentlyActive = AxisActiveRegistry[i] = definition.IsValueActive(value);
                } else {
                    AxisAdjustedValueRegistry[i] = value;
                    isCurrentlyActive = AxisActiveRegistry[i] = value > 0f;
                }
                AxisJustActiveRegistry[i] = isCurrentlyActive && !isLastActive;
                AxisJustInactiveRegistry[i] = !isCurrentlyActive && isLastActive;
            }
        }

        public bool IsButtonDown(int idx) {
            if (idx < 0 || idx >= MAX_BUTTON_COUNT) return false;

            return ButtonDownHistoryRegistry[idx,0];
        }

        public bool IsButtonJustDown(int idx) {
            if (idx < 0 || idx >= MAX_BUTTON_COUNT) return false;

            return ButtonJustDownRegistry[idx];
        }

        public bool IsButtonJustUp(int idx) {
            if (idx < 0 || idx >= MAX_BUTTON_COUNT) return false;

            return ButtonJustUpDiffRegistry[idx];
        }

        public bool IsSwitchActive(int idx) {
            if (idx < 0 || idx >= MAX_SWITCH_COUNT) return false;

            return SwitchSubIndexHistoryRegistry[idx,0] > 0;
        }

        public bool IsSwitchJustActive(int idx) {
            if (idx < 0 || idx >= MAX_SWITCH_COUNT) return false;

            return SwitchJustActiveRegistry[idx];
        }

        public bool IsSwitchJustInactive(int idx) {
            if (idx < 0 || idx >= MAX_SWITCH_COUNT) return false;

            return SwitchJustInactiveRegistry[idx];
        }

        public bool IsSwitchJustChanged(int idx) {
            if (idx < 0 || idx >= MAX_SWITCH_COUNT) return false;

            return SwitchSubIndexJustChangedRegistry[idx];
        }

        public int GetSwitchSubIndex(int idx, int timeOffset = 0) {
            if (idx < 0 || idx >= MAX_SWITCH_COUNT) return 0;

            return SwitchSubIndexHistoryRegistry[idx, timeOffset];
        }

        public int GetSwitchLastChangeSubIndex(int idx, int timeOffset = 0) {
            if (idx < 0 || idx >= MAX_SWITCH_COUNT) return 0;

            return SwitchSubIndexChangeHistoryRegistry[idx, timeOffset];
        }

        public bool IsAxisActive(int idx) {
            if (idx < 0 || idx >= MAX_AXIS_COUNT) return false;

            return AxisActiveRegistry[idx];
        }

        public bool IsAxisJustActive(int idx) {
            if (idx < 0 || idx >= MAX_AXIS_COUNT) return false;

            return AxisJustActiveRegistry[idx];
        }

        public bool IsAxisJustInactive(int idx) {
            if (idx < 0 || idx >= MAX_AXIS_COUNT) return false;

            return AxisJustInactiveRegistry[idx];
        }

        public float GetAxisValue(int idx, int timeOffset = 0) {
            if (idx < 0 || idx >= MAX_AXIS_COUNT) return 0f;

            return AxisValueRegistry[idx, timeOffset];
        }

        public float GetAxisAdjustedValue(int idx) {
            if (idx < 0 || idx >= MAX_AXIS_COUNT) return 0f;

            return AxisAdjustedValueRegistry[idx];
        }
    }
}
