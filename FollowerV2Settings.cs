using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ExileCore;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using ImGuiNET;
using SharpDX;

namespace FollowerV2
{
    public class FollowerV2Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        #region Debug

        [Menu("Debug", 1000)]
        public EmptyNode EmptyDebug { get; set; } = new EmptyNode();
        [Menu("Debug", "", 2, 1000)]
        public ToggleNode Debug { get; set; } = new ToggleNode(false);
        public ToggleNode VerboseDebug { get; set; } = new ToggleNode(false);
        [Menu("Show Radius", "", 3, 1000)]
        public ToggleNode DebugShowRadius { get; set; } = new ToggleNode(false);

        #endregion

        #region Main

        [Menu("Main", 2000)]
        public EmptyNode EmptyMain { get; set; } = new EmptyNode();
        [Menu("Profiles", "", 2, 2000)]
        public ListNode Profiles { get; set; } = new ListNode
        {
            Values = new List<string>
        {
            ProfilesEnum.Disable, ProfilesEnum.Follower, ProfilesEnum.Leader
        },
            Value = ProfilesEnum.Disable
        };

        public RangeNode<int> RandomClickOffset { get; set; } = new RangeNode<int>(10, 5, 100);

        #endregion

        #region Follower mode related settings

        public FollowerModeSetting FollowerModeSettings = new FollowerModeSetting();

        #endregion

        #region Leader mode related settings

        public LeaderModeSetting LeaderModeSettings = new LeaderModeSetting();

        #endregion

        public void DrawSettings()
        {
            ImGuiTreeNodeFlags collapsingHeaderFlags = ImGuiTreeNodeFlags.CollapsingHeader;

            Debug.Value = ImGuiExtension.Checkbox("Debug", Debug);
            ImGui.Spacing();
            if (Debug.Value)
            {
                VerboseDebug.Value = ImGuiExtension.Checkbox("Extra Verbose Debug", VerboseDebug);
                ImGui.Spacing();
            }
            DebugShowRadius.Value = ImGuiExtension.Checkbox("Debug: show radius", DebugShowRadius);
            ImGui.Spacing();
            Profiles.Value = ImGuiExtension.ComboBox("Profiles", Profiles.Value, Profiles.Values);
            ImGui.Spacing();
            ImGui.Spacing();
            RandomClickOffset.Value = ImGuiExtension.IntSlider("Random click offset", RandomClickOffset);
            ImGuiExtension.ToolTipWithText("(?)", "Will randomly offset X and Y coords by - or + of this value");

            ImGui.Separator();
            ImGui.Spacing();

            if (Profiles.Value == ProfilesEnum.Follower)
            {
                if (ImGui.TreeNodeEx("Follower Mode Settings", collapsingHeaderFlags))
                {
                    FollowerModeSettings.FollowerModes.Value = ImGuiExtension.ComboBox("Follower modes", FollowerModeSettings.FollowerModes.Value, FollowerModeSettings.FollowerModes.Values);

                    if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Local)
                    {
                        ImGui.TextDisabled("This mode will NOT do any network requests and will use ONLY settings values");
                        ImGui.Spacing();
                        ImGui.Spacing();

                        FollowerModeSettings.LeaderName.Value = ImGuiExtension.InputText("Leader name", FollowerModeSettings.LeaderName, 100, ImGuiInputTextFlags.AlwaysInsertMode);
                        ImGuiExtension.ToolTipWithText("(?)", "Provide character's name this player will follow");

                        if (FollowerModeSettings.NearbyPlayers.Values.Any())
                        {
                            FollowerModeSettings.NearbyPlayers.Value = ImGuiExtension.ComboBox("Use party member as leader", FollowerModeSettings.NearbyPlayers.Value, FollowerModeSettings.NearbyPlayers.Values);
                        }

                        FollowerModeSettings.FollowerUseCombat.Value = ImGuiExtension.Checkbox("Use Combat", FollowerModeSettings.FollowerUseCombat);
                        ImGuiExtension.ToolTipWithText("(?)", "This player will use combat routines");
                    }
                    else if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Network)
                    {
                        ImGui.TextDisabled("This mode will make network requests and use ONLY values from the server");
                        ImGui.TextDisabled("All local values are disabled and will not be used");
                        ImGui.TextDisabled("P.S. On server you might want to use something such as \"ngrok\" or \"localtunnel\"");
                        ImGui.TextDisabled("    if your server is outside of localhost");
                        ImGui.Spacing();
                        ImGui.Spacing();

                        FollowerModeSettings.FollowerModeNetworkSettings.Url.Value = ImGuiExtension.InputText("Server URL", FollowerModeSettings.FollowerModeNetworkSettings.Url, 100, ImGuiInputTextFlags.AlwaysInsertMode);
                        ImGuiExtension.ToolTipWithText("(?)", "Provide the URL this follower will connect");

                        FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests.Value = ImGuiExtension.IntSlider("Request delay", FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests);
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    //ImGui.TreePop();
                }
            }

            if (Profiles.Value == ProfilesEnum.Leader)
            {
                FollowerModeSettings.FollowerModes.Value = ImGuiExtension.ComboBox("Follower modes", FollowerModeSettings.FollowerModes.Value, FollowerModeSettings.FollowerModes.Values);

                if (ImGui.TreeNodeEx("Leader Mode Settings", collapsingHeaderFlags))
                {
                    if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Local)
                    {
                        ImGui.TextDisabled("Local mode for leader does not contain any settings");
                    }
                    else if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Network)
                    {
                        ImGui.TextDisabled("This is the network mode for LEADER");
                        ImGui.TextDisabled($"Server will run on port {LeaderModeSettings.ServerPort.Value}");
                        ImGui.Spacing();
                        ImGui.Spacing();
                        LeaderModeSettings.LeaderNameToPropagate.Value = ImGuiExtension.InputText("Leader Name To Propagate", LeaderModeSettings.LeaderNameToPropagate, 50, ImGuiInputTextFlags.AlwaysInsertMode);
                        ImGui.Spacing();

                        if (ImGui.Button("Set myself as leader")) LeaderModeSettings.SetMyselfAsLeader.OnPressed();
                        ImGui.Spacing();

                        ImGui.Spacing();
                        LeaderModeSettings.PropagateWorkingOfFollowers.Value = ImGuiExtension.Checkbox("Propagate working of followers", LeaderModeSettings.PropagateWorkingOfFollowers);
                        ImGui.Spacing();

                        if (ImGui.TreeNodeEx("Advanced leader mode settings", collapsingHeaderFlags))
                        {
                            ImGui.TextDisabled("Port the server will be listening on. Must be a number.");
                            ImGui.TextDisabled("Any other value will result in a crash");
                            ImGui.TextDisabled("Remember to restart the server if you have changed the port");
                            LeaderModeSettings.ServerPort.Value = ImGuiExtension.InputText("Server Port", LeaderModeSettings.ServerPort, 5, ImGuiInputTextFlags.AlwaysInsertMode);
                            ImGui.Spacing();
                            ImGui.TextDisabled("Server management");
                            ImGui.Spacing();
                            ImGui.SameLine();
                            if (ImGui.Button("Restart Server")) LeaderModeSettings.ServerRestart.OnPressed();
                            ImGui.SameLine();
                            if (ImGui.Button("Stop Server")) LeaderModeSettings.ServerStop.OnPressed();
                            ImGui.TextDisabled("TEST");
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    //ImGui.TreePop();
                }
            }
        }
    }

    public class ProfilesEnum
    {
        public static string Disable = "disable";
        public static string Follower = "follower";
        public static string Leader = "leader";
    }

    public class FollowerNetworkActivityModeEnum
    {
        public static string Local = "local";
        public static string Network = "network";
    }

    public class FollowerModeSetting
    {
        public EmptyNode EmptyFollower { get; set; } = new EmptyNode();
        public TextNode LeaderName { get; set; } = new TextNode("");
        public ToggleNode FollowerUseCombat { get; set; } = new ToggleNode(false);

        public ListNode FollowerModes { get; set; } = new ListNode
        {
            Values = new List<string> {
                FollowerNetworkActivityModeEnum.Local,
                FollowerNetworkActivityModeEnum.Network
            },
            Value = FollowerNetworkActivityModeEnum.Local
        };

        public ListNode NearbyPlayers { get; set; } = new ListNode { Values = new List<string>(), Value = "" };

        public FollowerModeNetworkSetting FollowerModeNetworkSettings { get; set; } = new FollowerModeNetworkSetting();
    }

    public class LeaderModeSetting
    {
        public TextNode LeaderNameToPropagate { get; set; } = new TextNode("");
        public ButtonNode SetMyselfAsLeader { get; set; } = new ButtonNode();
        public TextNode ServerPort { get; set; } = new TextNode("4412");
        public ButtonNode ServerRestart { get; set; } = new ButtonNode();
        public ButtonNode ServerStop { get; set; } = new ButtonNode();
        public ToggleNode PropagateWorkingOfFollowers { get; set; } = new ToggleNode(false);
    }

    public class FollowerModeNetworkSetting
    {
        public TextNode Url { get; set; } = new TextNode("");

        public RangeNode<int> DelayBetweenRequests { get; set; } = new RangeNode<int>(1000, 300, 3000);
    }

}
