using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;

namespace FollowerV2
{
    public class ImGuiExtension
    {
        // Int Sliders
        public static int IntSlider(string labelString, int value, int minValue, int maxValue)
        {
            var refValue = value;
            ImGui.SliderInt(labelString, ref refValue, minValue, maxValue);
            return refValue;
        }

        public static int IntSlider(string labelString, string sliderString, int value, int minValue, int maxValue)
        {
            var refValue = value;
            ImGui.SliderInt(labelString, ref refValue, minValue, maxValue);
            return refValue;
        }

        public static int IntSlider(string labelString, RangeNode<int> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderInt(labelString, ref refValue, setting.Min, setting.Max);
            return refValue;
        }

        public static int IntSlider(string labelString, string sliderString, RangeNode<int> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderInt(labelString, ref refValue, setting.Min, setting.Max);
            return refValue;
        }

        // float Sliders
        public static float FloatSlider(string labelString, float value, float minValue, float maxValue)
        {
            var refValue = value;
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue, "%.00f", 1f);
            return refValue;
        }

        public static float FloatSlider(string labelString, float value, float minValue, float maxValue, float power)
        {
            var refValue = value;
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue, "%.00f", power);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, float value, float minValue, float maxValue)
        {
            var refValue = value;
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue, $"{sliderString}: {value}", 1f);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, float value, float minValue, float maxValue, float power)
        {
            var refValue = value;
            ImGui.SliderFloat(labelString, ref refValue, minValue, maxValue, $"{sliderString}: {value}", power);
            return refValue;
        }

        public static float FloatSlider(string labelString, RangeNode<float> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max, "%.00f", 1f);
            return refValue;
        }

        public static float FloatSlider(string labelString, RangeNode<float> setting, float power)
        {
            var refValue = setting.Value;
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max, "%.00f", power);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, RangeNode<float> setting)
        {
            var refValue = setting.Value;
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max, $"{sliderString}: {setting.Value}", 1f);
            return refValue;
        }

        public static float FloatSlider(string labelString, string sliderString, RangeNode<float> setting, float power)
        {
            var refValue = setting.Value;
            ImGui.SliderFloat(labelString, ref refValue, setting.Min, setting.Max, $"{sliderString}: {setting.Value}", power);
            return refValue;
        }

        // Buttons
        public static Action Button(string label, Action callback)
        {
            ImGui.Button(label);
            return callback;
        }

        // Checkboxes
        public static bool Checkbox(string labelString, bool boolValue)
        {
            ImGui.Checkbox(labelString, ref boolValue);
            return boolValue;
        }

        public static bool Checkbox(string labelString, bool boolValue, out bool outBool)
        {
            ImGui.Checkbox(labelString, ref boolValue);
            outBool = boolValue;
            return boolValue;
        }

        // Hotkey Selector
        public static IEnumerable<Keys> KeyCodes() => Enum.GetValues(typeof(Keys)).Cast<Keys>();

        // Tooltip Hover
        public static void ToolTipWithText(string text, string desc)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(text);
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AnyWindow))
            {
                ImGui.SetTooltip(desc);
            }
        }

        public static int ComboBox(string sideLabel, int currentSelectedItem, List<string> objectList)
        {
            ImGui.Combo(sideLabel, ref currentSelectedItem, objectList.ToArray(), objectList.Count);

            return currentSelectedItem;
        }
        public static string ComboBox(string sideLabel, string currentSelectedItem, List<string> objectList, ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightRegular)
        {
            if (ImGui.BeginCombo(sideLabel, currentSelectedItem, comboFlags))
            {
                var refObject = currentSelectedItem;
                for (var n = 0; n < objectList.Count; n++)
                {
                    var isSelected = refObject == objectList[n];
                    if (ImGui.Selectable(objectList[n], isSelected)) return objectList[n];
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            return currentSelectedItem;
        }
        public static string ComboBox(string sideLabel, string currentSelectedItem, List<string> objectList, out bool didChange, ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightRegular)
        {
            if (ImGui.BeginCombo(sideLabel, currentSelectedItem, comboFlags))
            {
                var refObject = currentSelectedItem;
                for (var n = 0; n < objectList.Count; n++)
                {
                    var isSelected = refObject == objectList[n];
                    if (ImGui.Selectable(objectList[n], isSelected))
                    {
                        didChange = true;
                        return objectList[n];
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }

            didChange = false;
            return currentSelectedItem;
        }

        public static string InputText(string label, string currentValue, uint maxLength, ImGuiInputTextFlags flags)
        {
            byte[] buff = new byte[maxLength];
            if (!String.IsNullOrEmpty(currentValue))
            {
                byte[] currentValueBytes = Encoding.UTF8.GetBytes(currentValue);
                Array.Copy(currentValueBytes, buff, currentValueBytes.Length);
            }
            ImGui.InputText(label, buff, maxLength, flags);
            return Encoding.Default.GetString(buff).TrimEnd('\0');
        }

        public static Keys HotkeySelector(string buttonName, Keys currentKey)
        {
            string msg = $"{buttonName } - {currentKey}";

            var open = true;
            if (ImGui.Button(msg))
            {
                ImGui.OpenPopup(msg);
                open = true;
            }

            if (ImGui.BeginPopupModal(msg, ref open, (ImGuiWindowFlags)35))
            {
                if (Input.GetKeyState(Keys.Escape))
                {
                    ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                }
                else
                {
                    foreach (var key in Enum.GetValues(typeof(Keys)))
                    {
                        var keyState = Input.GetKeyState((Keys)key);
                        if (keyState)
                        {
                            currentKey = (Keys)key;
                            ImGui.CloseCurrentPopup();
                            break;
                        }
                    }
                }

                ImGui.Text($" Press new key to change '{currentKey}' or Esc for exit.");

                ImGui.EndPopup();
            }

            return currentKey;
        }

        public static void SpacedTextHeader(string text)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text(text);
            ImGui.Separator();
            ImGui.Spacing();
        }
    }
}