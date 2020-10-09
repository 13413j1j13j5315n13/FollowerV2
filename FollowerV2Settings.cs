using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using ImGuiNET;

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
        public HotkeyNode DebugGenerateOnHoverEvents { get; set; } = Keys.L;

        public FollowerCommandsImguiSetting FollowerCommandsImguiSettings = new FollowerCommandsImguiSetting();

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
        public ButtonNode ResetToDefaultsButton { get; set; } = new ButtonNode();

        public ListNode NearbyPlayers { get; set; } = new ListNode { Values = new List<string>(), Value = "" };

        #endregion

        #region Follower mode related settings

        public FollowerModeSetting FollowerModeSettings = new FollowerModeSetting();

        #endregion

        #region Leader mode related settings

        public LeaderModeSetting LeaderModeSettings = new LeaderModeSetting();

        #endregion

        public FollowerV2Settings()
        {
            ResetToDefaultsButton.OnPressed += ResetAllSettingsToDefaults;

            LeaderModeSettings.NewFollowerCommandClassSetting.UseNearbyPlayerNameButton.OnPressed += (() =>
                {
                    LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value = NearbyPlayers.Value;
                    NearbyPlayers.Value = "";
                });
            LeaderModeSettings.NewFollowerCommandClassSetting.AddNewFollowerButton.OnPressed += (() =>
            {
                string name = LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value;
                LeaderModeSettings.FollowerCommandSetting.AddNewFollower(name);
                LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value = "";
            });
        }

        private void ResetAllSettingsToDefaults()
        {
            Debug.Value = false;
            VerboseDebug.Value = false;
            DebugShowRadius.Value = false;
            DebugGenerateOnHoverEvents.Value = Keys.L;
            Profiles.Value = ProfilesEnum.Disable;
            RandomClickOffset.Value = 10;
            NearbyPlayers.Value = "";

            FollowerModeSettings.LeaderName.Value = "";
            FollowerModeSettings.FollowerUseCombat.Value = false;
            FollowerModeSettings.FollowerModes.Value = FollowerNetworkActivityModeEnum.Local;
            FollowerModeSettings.LeaderProximityRadius.Value = 100;
            FollowerModeSettings.StartNetworkRequesting.Value = false;
            FollowerModeSettings.StartNetworkRequestingHotkey.Value = Keys.F3;
            FollowerModeSettings.FollowerModeNetworkSettings.Url.Value = "";
            FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests.Value = 1000;
            FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs.Value = 3000;

            LeaderModeSettings.LeaderNameToPropagate.Value = "";
            LeaderModeSettings.ServerHostname.Value = "localhost";
            LeaderModeSettings.ServerPort.Value = "4412";
            LeaderModeSettings.PropagateWorkingOfFollowers.Value = false;
            LeaderModeSettings.PropagateWorkingOfFollowersHotkey.Value = Keys.F4;
            LeaderModeSettings.LeaderProximityRadiusToPropagate.Value = 100;
        }

        public void DrawSettings()
        {
            ImGuiTreeNodeFlags collapsingHeaderFlags = ImGuiTreeNodeFlags.CollapsingHeader;

            Debug.Value = ImGuiExtension.Checkbox("Debug", Debug);
            ImGui.Spacing();
            if (Debug.Value)
            {
                VerboseDebug.Value = ImGuiExtension.Checkbox("Extra Verbose Debug", VerboseDebug);
                ImGui.Spacing();

                ImGui.TextDisabled("Hotkey to randomly generate On Hover events");
                ImGui.TextDisabled("This will help to see where follower will click");
                ImGui.TextDisabled("This takes \"Random click offset\" into account");
                DebugGenerateOnHoverEvents.Value = ImGuiExtension.HotkeySelector("Generate OnHover", DebugGenerateOnHoverEvents);

                ImGui.Spacing();
                DebugShowRadius.Value = ImGuiExtension.Checkbox("Debug: show radius", DebugShowRadius);
            }
            ImGui.Spacing();
            Profiles.Value = ImGuiExtension.ComboBox("Profiles", Profiles.Value, Profiles.Values);
            ImGui.Spacing();
            ImGui.Spacing();
            RandomClickOffset.Value = ImGuiExtension.IntSlider("Random click offset", RandomClickOffset);
            ImGuiExtension.ToolTipWithText("(?)", "Will randomly offset X and Y coords by - or + of this value");

            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.SameLine();
            ImGui.TextDisabled("***** ");
            ImGui.SameLine();
            if (ImGui.Button("Reset settings to defaults")) ResetToDefaultsButton.OnPressed();
            ImGui.SameLine();
            ImGui.TextDisabled(" *****");

            ImGui.Spacing();
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

                        FollowerModeSettings.LeaderName.Value = ImGuiExtension.InputText("Leader name", FollowerModeSettings.LeaderName);
                        ImGuiExtension.ToolTipWithText("(?)", "Provide character's name this player will follow");

                        if (NearbyPlayers.Values.Any())
                        {
                            NearbyPlayers.Value = ImGuiExtension.ComboBox("Use nearby member as leader", NearbyPlayers.Value, NearbyPlayers.Values);
                            if (!String.IsNullOrEmpty(NearbyPlayers.Value))
                            {
                                if (ImGui.Button("Set as selected as leader")) FollowerModeSettings.UseNearbyPlayerAsLeaderButton.OnPressed();
                            }
                        }

                        FollowerModeSettings.FollowerShouldWork.Value = ImGuiExtension.Checkbox("Start follower", FollowerModeSettings.FollowerShouldWork);

                        // TODO: Implement this later
                        //FollowerModeSettings.FollowerUseCombat.Value = ImGuiExtension.Checkbox("Use Combat", FollowerModeSettings.FollowerUseCombat);
                        //ImGuiExtension.ToolTipWithText("(?)", "This player will use combat routines");
                        ImGui.Spacing();
                        FollowerModeSettings.LeaderProximityRadius.Value = ImGuiExtension.IntSlider("Leader prox. radius", FollowerModeSettings.LeaderProximityRadius);
                        ImGuiExtension.ToolTipWithText("(?)", "Set \"Debug: show radius\" on to see the radius");
                        ImGuiExtension.ToolTipWithText("(?)", "Color: Red");
                    }
                    else if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Network)
                    {
                        ImGui.TextDisabled("This mode will make network requests and use ONLY values from the server");
                        ImGui.TextDisabled("All local values are disabled and will not be used");
                        ImGui.TextDisabled("P.S. On server you might want to use something such as \"ngrok\" or \"localtunnel\"");
                        ImGui.TextDisabled("    if your server is outside of localhost");
                        ImGui.Spacing();
                        ImGui.Spacing();

                        FollowerModeSettings.FollowerModeNetworkSettings.Url.Value = ImGuiExtension.InputText("Server URL", FollowerModeSettings.FollowerModeNetworkSettings.Url);
                        ImGuiExtension.ToolTipWithText("(?)", "Provide the URL this follower will connect");

                        FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests.Value = ImGuiExtension.IntSlider("Request delay", FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests);
                        ImGui.Spacing();
                        FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs.Value = ImGuiExtension.IntSlider("Request timeout ms", FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        FollowerModeSettings.StartNetworkRequesting.Value = ImGuiExtension.Checkbox("Start network requesting", FollowerModeSettings.StartNetworkRequesting);
                        FollowerModeSettings.StartNetworkRequestingHotkey.Value = ImGuiExtension.HotkeySelector("Hotkey to start network requesting", FollowerModeSettings.StartNetworkRequestingHotkey);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.TextDisabled("The next hotkey will be used for moving. Follower will click it after hovering");
                        FollowerModeSettings.MoveHotkey.Value = ImGuiExtension.HotkeySelector("Move hotkey", FollowerModeSettings.MoveHotkey);
                        ImGui.Spacing();
                        ImGui.TextDisabled("The delay to \"sleep\" between following logic iterations");
                        FollowerModeSettings.MoveLogicCooldown.Value = ImGuiExtension.IntSlider("Following logic cooldown", FollowerModeSettings.MoveLogicCooldown);
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    //ImGui.TreePop();
                }
            }

            if (Profiles.Value == ProfilesEnum.Leader)
            {
                if (ImGui.TreeNodeEx("Leader Mode Settings", collapsingHeaderFlags))
                {
                    FollowerModeSettings.FollowerModes.Value = ImGuiExtension.ComboBox("Follower modes", FollowerModeSettings.FollowerModes.Value, FollowerModeSettings.FollowerModes.Values);

                    if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Local)
                    {
                        ImGui.TextDisabled("Local mode for leader does not contain any settings");
                    }
                    else if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Network)
                    {
                        ImGui.TextDisabled("This is the network mode for LEADER");
                        ImGui.TextDisabled($"Server will run on port \"{LeaderModeSettings.ServerPort.Value}\"");
                        ImGui.TextDisabled($"   hostname: {LeaderModeSettings.ServerHostname.Value}");
                        ImGui.Spacing();
                        ImGui.Spacing();
                        LeaderModeSettings.LeaderNameToPropagate.Value = ImGuiExtension.InputText("Leader FollowerName To Propagate", LeaderModeSettings.LeaderNameToPropagate);
                        ImGui.Spacing();

                        if (ImGui.Button("Set myself as leader")) LeaderModeSettings.SetMyselfAsLeader.OnPressed();

                        ImGui.Spacing();
                        LeaderModeSettings.StartServer.Value = ImGuiExtension.Checkbox("Start Server Listening", LeaderModeSettings.StartServer);

                        ImGui.Spacing();
                        LeaderModeSettings.PropagateWorkingOfFollowers.Value = ImGuiExtension.Checkbox("Propagate working of followers", LeaderModeSettings.PropagateWorkingOfFollowers);
                        LeaderModeSettings.PropagateWorkingOfFollowersHotkey.Value = ImGuiExtension.HotkeySelector("Hotkey to propagate working of follower", LeaderModeSettings.PropagateWorkingOfFollowersHotkey);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        LeaderModeSettings.LeaderProximityRadiusToPropagate.Value = ImGuiExtension.IntSlider("Leader proximity radius", LeaderModeSettings.LeaderProximityRadiusToPropagate);
                        ImGuiExtension.ToolTipWithText("(?)", "Set \"Debug: show radius\" on to see the radius");
                        ImGuiExtension.ToolTipWithText("(?)", "Color: Yellow");

                        if (ImGui.TreeNodeEx("Follower command settings"))
                        {
                            ImGui.Spacing();
                            ImGui.Spacing();
                            ImGui.TextDisabled("Add here new slaves to command them using the server");
                            ImGui.Spacing();
                            LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value = ImGuiExtension.InputText("Slave's name", LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName);
                            ImGui.Spacing();
                            NearbyPlayers.Value = ImGuiExtension.ComboBox("Use nearby player's name", NearbyPlayers.Value, NearbyPlayers.Values);
                            ImGui.Spacing();
                            if (ImGui.Button("Set selected value")) LeaderModeSettings.NewFollowerCommandClassSetting.UseNearbyPlayerNameButton.OnPressed();
                            ImGui.Spacing();
                            ImGui.Spacing();
                            if (ImGui.Button("Add new slave")) LeaderModeSettings.NewFollowerCommandClassSetting.AddNewFollowerButton.OnPressed();
                            ImGui.Spacing();
                        }

                        if (LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet.Any())
                        {
                            foreach (var follower in LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet)
                            {
                                if (ImGui.TreeNodeEx($"Follower \"{follower.FollowerName}\" settings##{follower.FollowerName}"))
                                {
                                    string imguiId = follower.FollowerName;

                                    ImGui.TextDisabled($"****** Other settings ******");
                                    ImGui.Spacing();
                                    ImGui.Spacing();
                                    follower.ShouldLevelUpGems = ImGuiExtension.Checkbox($"Level up gems##{imguiId}", follower.ShouldLevelUpGems);

                                    ImGui.TextDisabled($"****** Skill settings ******");
                                    if (ImGui.Button($"Add new skill##{follower.FollowerName}")) follower.AddNewEmptySkill();
                                    ImGui.Spacing();
                                    ImGui.Spacing();

                                    foreach (FollowerSkill skill in follower.FollowerSkills)
                                    {
                                        ImGui.TextDisabled($"------ Skill (id: {skill.Id}) ------");

                                        skill.Enable = ImGuiExtension.Checkbox($"Enable##{imguiId}", skill.Enable);
                                        ImGui.SameLine();
                                        ImGui.TextDisabled("    ");
                                        ImGui.SameLine();
                                        if (ImGui.Button($"Remove##{imguiId}")) follower.RemoveSkill(skill.Id);

                                        ImGui.Spacing();
                                        skill.Hotkey = ImGuiExtension.HotkeySelector($"Hotkey: {skill.Hotkey}##{imguiId}", skill.Hotkey);
                                        ImGui.Spacing();
                                        ImGui.SliderInt($"Priority##{imguiId}", ref skill.Priority, 1, 5);
                                        ImGui.Spacing();
                                        skill.IsMovingSkill = ImGuiExtension.Checkbox($"Is moving skill##{imguiId}", skill.IsMovingSkill);
                                        ImGui.Spacing();
                                        ImGui.SliderInt($"Skill cooldown in ms##{imguiId}", ref skill.CooldownMs, 100, 10000);
                                        ImGui.Spacing();

                                        if (!skill.IsMovingSkill)
                                        {
                                            ImGui.SliderInt($"Max range to monsters##{imguiId}", ref skill.MaxRangeToMonsters, 10, 200);
                                            ImGui.Spacing();
                                        }
                                    }

                                    if (follower.FollowerSkills.Any()) ImGui.TextDisabled("-----------");

                                    ImGui.Spacing();
                                }
                                
                            }
                        }
                        
                        if (ImGui.TreeNodeEx("Advanced leader mode settings"))
                        {
                            ImGui.TextDisabled("Remember to restart the server if you have changed the port or the hostname");
                            ImGui.TextDisabled("    run \"netsh http add urlacl url=http://HOSTNAME:PORT/\" user=YOUR_USER");
                            ImGui.TextDisabled("    example \"netsh http add urlacl url=http://+:4412/\" user=YOUR_USER");
                            ImGui.TextDisabled("        if you have changed your hostname");
                            ImGui.TextDisabled("    allow the inbound connection on the port in firewall as well");
                            LeaderModeSettings.ServerHostname.Value = ImGuiExtension.InputText("Server Hostname", LeaderModeSettings.ServerHostname);
                            LeaderModeSettings.ServerPort.Value = ImGuiExtension.InputText("Server Port", LeaderModeSettings.ServerPort);
                            ImGui.Spacing();
                            ImGui.TextDisabled("Server management");
                            ImGui.Spacing();
                            ImGui.SameLine();
                            if (ImGui.Button("Restart Server")) LeaderModeSettings.ServerRestart.OnPressed();
                            ImGui.SameLine();
                            if (ImGui.Button("Stop Server")) LeaderModeSettings.ServerStop.OnPressed();
                            ImGui.Spacing();
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

        public ToggleNode FollowerShouldWork { get; set; } = new ToggleNode(false);

        public TextNode LeaderName { get; set; } = new TextNode("");
        public ToggleNode FollowerUseCombat { get; set; } = new ToggleNode(false);

        public ButtonNode UseNearbyPlayerAsLeaderButton { get; set; } = new ButtonNode();

        public ListNode FollowerModes { get; set; } = new ListNode
        {
            Values = new List<string> {
                FollowerNetworkActivityModeEnum.Local,
                FollowerNetworkActivityModeEnum.Network
            },
            Value = FollowerNetworkActivityModeEnum.Local
        };

        public FollowerModeNetworkSetting FollowerModeNetworkSettings { get; set; } = new FollowerModeNetworkSetting();
        public RangeNode<int> LeaderProximityRadius { get; set; } = new RangeNode<int>(100, 10, 300);
        public ToggleNode StartNetworkRequesting { get; set; } = new ToggleNode(false);
        public HotkeyNode StartNetworkRequestingHotkey { get; set; } = Keys.F3;
        public HotkeyNode MoveHotkey { get; set; } = Keys.T;
        public RangeNode<int> MoveLogicCooldown { get; set; } = new RangeNode<int>(50, 20, 300);
    }

    public class LeaderModeSetting
    {
        public TextNode LeaderNameToPropagate { get; set; } = new TextNode("");
        public ButtonNode SetMyselfAsLeader { get; set; } = new ButtonNode();
        public TextNode ServerHostname { get; set; } = new TextNode("localhost");
        public TextNode ServerPort { get; set; } = new TextNode("4412");
        public ToggleNode StartServer { get; set; } = new ToggleNode(false);
        public ButtonNode ServerRestart { get; set; } = new ButtonNode();
        public ButtonNode ServerStop { get; set; } = new ButtonNode();
        public ToggleNode PropagateWorkingOfFollowers { get; set; } = new ToggleNode(false);
        public HotkeyNode PropagateWorkingOfFollowersHotkey { get; set; } = Keys.F4;
        public RangeNode<int> LeaderProximityRadiusToPropagate { get; set; } = new RangeNode<int>(20, 1, 300);

        public FollowerCommandSetting FollowerCommandSetting = new FollowerCommandSetting();

        public NewFollowerCommandClassSetting NewFollowerCommandClassSetting = new NewFollowerCommandClassSetting();
    }

    public class NewFollowerCommandClassSetting
    {
        public TextNode FollowerName { get; set; } = new TextNode("");

        public ButtonNode UseNearbyPlayerNameButton { get; set; } = new ButtonNode();

        public ButtonNode AddNewFollowerButton { get; set; } = new ButtonNode();
    }

    public class FollowerModeNetworkSetting
    {
        public TextNode Url { get; set; } = new TextNode("");

        public RangeNode<int> DelayBetweenRequests { get; set; } = new RangeNode<int>(1000, 300, 3000);
        public RangeNode<int> RequestTimeoutMs { get; set; } = new RangeNode<int>(3000, 1000, 10000);
    }

    public class FollowerCommandsImguiSetting
    {
        public ToggleNode ShowWindow { get; set; } = new ToggleNode(true);
        public ToggleNode LockPanel { get; set; } = new ToggleNode(false);
        public ToggleNode NoResize { get; set; } = new ToggleNode(false);
    }



}
