using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;

namespace FollowerV2
{
    public class FollowerV2Settings : ISettings
    {
        #region Follower mode related settings

        public FollowerModeSetting FollowerModeSettings = new FollowerModeSetting();

        #endregion

        #region Leader mode related settings

        public LeaderModeSetting LeaderModeSettings = new LeaderModeSetting();

        #endregion

        public FollowerV2Settings()
        {
            ResetToDefaultsButton.OnPressed += ResetAllSettingsToDefaults;

            LeaderModeSettings.NewFollowerCommandClassSetting.UseNearbyPlayerNameButton.OnPressed += () =>
            {
                LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value = NearbyPlayers.Value;
                NearbyPlayers.Value = "";
            };
            LeaderModeSettings.NewFollowerCommandClassSetting.AddNewFollowerButton.OnPressed += () =>
            {
                var name = LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value;
                LeaderModeSettings.FollowerCommandSetting.AddNewFollower(name);
                LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value = "";
            };
        }

        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        private void ResetAllSettingsToDefaults()
        {
            Debug.Value = false;
            VerboseDebug.Value = false;
            DebugShowRadius.Value = false;
            DebugGenerateOnHoverEvents.Value = Keys.L;
            Profiles.Value = ProfilesEnum.Disable;
            RandomClickOffset.Value = 10;
            NearbyPlayers.Value = "";

            FollowerCommandsImguiSettings.ShowWindow.Value = true;
            FollowerCommandsImguiSettings.LockPanel.Value = false;
            FollowerCommandsImguiSettings.NoResize.Value = false;

            FollowerModeSettings.FollowerShouldWork.Value = false;
            FollowerModeSettings.LeaderName.Value = "";
            FollowerModeSettings.FollowerUseCombat.Value = false;
            FollowerModeSettings.FollowerModes.Value = FollowerNetworkActivityModeEnum.Local;
            FollowerModeSettings.LeaderProximityRadius.Value = 100;
            FollowerModeSettings.StartRequesting.Value = false;
            FollowerModeSettings.StartRequestingHotkey.Value = Keys.F3;
            FollowerModeSettings.MoveHotkey.Value = Keys.T;
            FollowerModeSettings.MoveLogicCooldown.Value = 50;
            FollowerModeSettings.MinimumFpsThreshold.Value = 5;

            FollowerModeSettings.FollowerModeNetworkSettings.Url.Value = "";
            FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests.Value = 1000;
            FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs.Value = 3000;

            FollowerModeSettings.FollowerModeFileSettings.FilePath.Value = "C:\\test.txt";

            LeaderModeSettings.LeaderNameToPropagate.Value = "";
            LeaderModeSettings.PropagateWorkingOfFollowers.Value = false;
            LeaderModeSettings.PropagateWorkingOfFollowersHotkey.Value = Keys.F4;
            LeaderModeSettings.LeaderProximityRadiusToPropagate.Value = 100;
            LeaderModeSettings.FollowerCommandSetting = new FollowerCommandSetting();
            LeaderModeSettings.MinimumFpsThresholdToPropagate.Value = 5;

            LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value = "";

            LeaderModeSettings.LeaderModeNetworkSettings.ServerHostname.Value = "localhost";
            LeaderModeSettings.LeaderModeNetworkSettings.ServerPort.Value = "4412";
            LeaderModeSettings.LeaderModeNetworkSettings.StartServer.Value = false;

            LeaderModeSettings.LeaderModeFileSettings.FilePath.Value = "C:\\test.txt";
            LeaderModeSettings.LeaderModeFileSettings.StartFileWriting.Value = false;
        }

        public void DrawSettings()
        {
            var collapsingHeaderFlags = ImGuiTreeNodeFlags.CollapsingHeader;
            var isNetworkMode =
                FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Network;
            var isFileMode = FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.File;

            Debug.Value = ImGuiExtension.Checkbox("Debug", Debug);
            ImGui.Spacing();
            if (Debug.Value)
            {
                VerboseDebug.Value = ImGuiExtension.Checkbox("Extra Verbose Debug", VerboseDebug);
                ImGui.Spacing();

                ImGui.TextDisabled("Hotkey to randomly generate On Hover events");
                ImGui.TextDisabled("This will help to see where follower will click");
                ImGui.TextDisabled("This takes \"Random click offset\" into account");
                DebugGenerateOnHoverEvents.Value =
                    ImGuiExtension.HotkeySelector("Generate OnHover", DebugGenerateOnHoverEvents);

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
                if (ImGui.TreeNodeEx("Follower Mode Settings", collapsingHeaderFlags))
                {
                    FollowerModeSettings.FollowerModes.Value = ImGuiExtension.ComboBox("Follower modes",
                        FollowerModeSettings.FollowerModes.Value, FollowerModeSettings.FollowerModes.Values);

                    ImGui.Spacing();
                    ImGui.TextDisabled(
                        "The minimum FPS threshold when the follower will do other actions than following");
                    FollowerModeSettings.MinimumFpsThreshold.Value = ImGuiExtension.IntSlider("Minimum FPS threshold",
                        FollowerModeSettings.MinimumFpsThreshold);

                    ImGui.Spacing();
                    ImGui.Spacing();

                    if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Local)
                    {
                        ImGui.TextDisabled(
                            "This mode will NOT do any network requests and will use ONLY settings values");
                        ImGui.Spacing();
                        ImGui.Spacing();

                        FollowerModeSettings.LeaderName.Value =
                            ImGuiExtension.InputText("Leader name", FollowerModeSettings.LeaderName);
                        ImGuiExtension.ToolTipWithText("(?)", "Provide character's name this player will follow");

                        if (NearbyPlayers.Values.Any())
                        {
                            NearbyPlayers.Value = ImGuiExtension.ComboBox("Use nearby member as leader",
                                NearbyPlayers.Value, NearbyPlayers.Values);
                            if (!string.IsNullOrEmpty(NearbyPlayers.Value))
                                if (ImGui.Button("Set as selected as leader"))
                                    FollowerModeSettings.UseNearbyPlayerAsLeaderButton.OnPressed();
                        }

                        FollowerModeSettings.FollowerShouldWork.Value =
                            ImGuiExtension.Checkbox("Start follower", FollowerModeSettings.FollowerShouldWork);

                        // TODO: Implement this later
                        //FollowerModeSettings.FollowerUseCombat.Value = ImGuiExtension.Checkbox("Use Combat", FollowerModeSettings.FollowerUseCombat);
                        //ImGuiExtension.ToolTipWithText("(?)", "This player will use combat routines");
                        ImGui.Spacing();
                        FollowerModeSettings.LeaderProximityRadius.Value =
                            ImGuiExtension.IntSlider("Leader prox. radius", FollowerModeSettings.LeaderProximityRadius);
                        ImGuiExtension.ToolTipWithText("(?)", "Set \"Debug: show radius\" on to see the radius");
                        ImGuiExtension.ToolTipWithText("(?)", "Color: Red");
                    }
                    else if (isNetworkMode || isFileMode)
                    {
                        ImGui.TextDisabled("This mode will make requests and use ONLY values from the server or file");
                        ImGui.TextDisabled("All local values are disabled and will not be used");
                        ImGui.Spacing();
                        ImGui.Spacing();

                        if (isNetworkMode)
                        {
                            ImGui.TextDisabled(
                                "P.S. On server you might want to use something such as \"ngrok\" or \"localtunnel\"");
                            ImGui.TextDisabled("    if your server is outside of localhost");
                            ImGui.Spacing();
                            ImGui.Spacing();

                            FollowerModeSettings.FollowerModeNetworkSettings.Url.Value =
                                ImGuiExtension.InputText("Server URL",
                                    FollowerModeSettings.FollowerModeNetworkSettings.Url);
                            ImGuiExtension.ToolTipWithText("(?)", "Provide the URL this follower will connect");

                            FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests.Value =
                                ImGuiExtension.IntSlider("Request delay",
                                    FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests);
                            ImGui.Spacing();
                            FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs.Value =
                                ImGuiExtension.IntSlider("Request timeout ms",
                                    FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs);
                        }

                        if (isFileMode)
                        {
                            FollowerModeSettings.FollowerModeFileSettings.FilePath.Value =
                                ImGuiExtension.InputText("File path",
                                    FollowerModeSettings.FollowerModeFileSettings.FilePath);

                            FollowerModeSettings.FollowerModeFileSettings.DelayBetweenReads.Value =
                                ImGuiExtension.IntSlider("Delay between reads",
                                    FollowerModeSettings.FollowerModeFileSettings.DelayBetweenReads);
                        }

                        ImGui.Spacing();
                        ImGui.Spacing();

                        FollowerModeSettings.StartRequesting.Value =
                            ImGuiExtension.Checkbox("Start requesting",
                                FollowerModeSettings.StartRequesting);
                        FollowerModeSettings.StartRequestingHotkey.Value = ImGuiExtension.HotkeySelector(
                            "Hotkey to start requesting", FollowerModeSettings.StartRequestingHotkey);
                        ImGui.Spacing();
                        ImGui.Spacing();

                        ImGui.TextDisabled(
                            "The next hotkey will be used for moving. Follower will click it after hovering");
                        FollowerModeSettings.MoveHotkey.Value =
                            ImGuiExtension.HotkeySelector("Move hotkey", FollowerModeSettings.MoveHotkey);
                        ImGui.Spacing();
                        ImGui.TextDisabled("The delay to \"sleep\" between following logic iterations");
                        FollowerModeSettings.MoveLogicCooldown.Value =
                            ImGuiExtension.IntSlider("Following logic cooldown",
                                FollowerModeSettings.MoveLogicCooldown);
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                }

            if (Profiles.Value == ProfilesEnum.Leader)
                if (ImGui.TreeNodeEx("Leader Mode Settings", collapsingHeaderFlags))
                {
                    FollowerModeSettings.FollowerModes.Value = ImGuiExtension.ComboBox("Follower modes",
                        FollowerModeSettings.FollowerModes.Value, FollowerModeSettings.FollowerModes.Values);

                    if (FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Local)
                    {
                        ImGui.TextDisabled("Local mode for leader does not contain any settings");
                    }
                    else if (isNetworkMode || isFileMode)
                    {
                        ImGui.TextDisabled("This is the network or file mode for LEADER");
                        if (isNetworkMode)
                        {
                            ImGui.TextDisabled(
                                $"Server will run on port \"{LeaderModeSettings.LeaderModeNetworkSettings.ServerPort.Value}\"");
                            ImGui.TextDisabled(
                                $"   hostname: {LeaderModeSettings.LeaderModeNetworkSettings.ServerHostname.Value}");
                        }

                        if (isFileMode)
                        {
                            ImGui.TextDisabled("File path to write:");
                            ImGui.TextDisabled($"   {LeaderModeSettings.LeaderModeFileSettings.FilePath.Value}");
                        }

                        ImGui.Spacing();
                        ImGui.Spacing();
                        LeaderModeSettings.LeaderNameToPropagate.Value =
                            ImGuiExtension.InputText("Leader FollowerName To Propagate",
                                LeaderModeSettings.LeaderNameToPropagate);
                        ImGui.Spacing();

                        if (ImGui.Button("Set myself as leader")) LeaderModeSettings.SetMyselfAsLeader.OnPressed();

                        if (isNetworkMode)
                        {
                            ImGui.Spacing();
                            LeaderModeSettings.LeaderModeNetworkSettings.StartServer.Value = ImGuiExtension.Checkbox(
                                "Start Server Listening",
                                LeaderModeSettings.LeaderModeNetworkSettings.StartServer);
                        }


                        if (isFileMode)
                        {
                            ImGui.Spacing();
                            LeaderModeSettings.LeaderModeFileSettings.StartFileWriting.Value = ImGuiExtension.Checkbox(
                                "Start File Writing",
                                LeaderModeSettings.LeaderModeFileSettings.StartFileWriting);
                        }

                        ImGui.Spacing();
                        LeaderModeSettings.PropagateWorkingOfFollowers.Value = ImGuiExtension.Checkbox(
                            "Propagate working of followers", LeaderModeSettings.PropagateWorkingOfFollowers);
                        LeaderModeSettings.PropagateWorkingOfFollowersHotkey.Value =
                            ImGuiExtension.HotkeySelector("Hotkey to propagate working of follower",
                                LeaderModeSettings.PropagateWorkingOfFollowersHotkey);
                        ImGui.Spacing();
                        ImGui.Spacing();
                        LeaderModeSettings.LeaderProximityRadiusToPropagate.Value =
                            ImGuiExtension.IntSlider("Leader proximity radius",
                                LeaderModeSettings.LeaderProximityRadiusToPropagate);
                        ImGuiExtension.ToolTipWithText("(?)", "Set \"Debug: show radius\" on to see the radius");
                        ImGuiExtension.ToolTipWithText("(?)", "Color: Yellow");

                        ImGui.Spacing();
                        ImGui.Spacing();
                        ImGui.TextDisabled("The minimum FPS threshold when followers will start other activities");
                        ImGui.TextDisabled("   than just following");
                        LeaderModeSettings.MinimumFpsThresholdToPropagate.Value =
                            ImGuiExtension.IntSlider("Minimum FPS threshold",
                                LeaderModeSettings.MinimumFpsThresholdToPropagate);
                        ImGuiExtension.ToolTipWithText("(?)", "Minimum FPS threshold to propagate");

                        ImGui.Spacing();
                        ImGui.Spacing();

                        if (ImGui.TreeNodeEx("Follower command settings"))
                        {
                            ImGui.Spacing();
                            ImGui.Spacing();
                            ImGui.TextDisabled("Add here new slaves to command them using the server");
                            ImGui.Spacing();
                            LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName.Value =
                                ImGuiExtension.InputText("Slave's name",
                                    LeaderModeSettings.NewFollowerCommandClassSetting.FollowerName);
                            ImGui.Spacing();
                            if (ImGui.Button("Add new slave"))
                                LeaderModeSettings.NewFollowerCommandClassSetting.AddNewFollowerButton.OnPressed();
                            ImGui.Spacing();
                            ImGui.Spacing();
                            NearbyPlayers.Value = ImGuiExtension.ComboBox("Use nearby player's name",
                                NearbyPlayers.Value, NearbyPlayers.Values);
                            ImGui.Spacing();
                            if (ImGui.Button("Set selected value"))
                                LeaderModeSettings.NewFollowerCommandClassSetting.UseNearbyPlayerNameButton.OnPressed();
                            ImGui.Spacing();
                        }

                        if (LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet.Any())
                            foreach (var follower in LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet)
                                if (ImGui.TreeNodeEx(
                                    $"Follower \"{follower.FollowerName}\" settings##{follower.FollowerName}"))
                                {
                                    var imguiId = follower.FollowerName;

                                    ImGui.TextDisabled("****** Other settings ******");
                                    ImGui.Spacing();
                                    ImGui.Spacing();
                                    follower.ShouldLevelUpGems = ImGuiExtension.Checkbox($"Level up gems##{imguiId}",
                                        follower.ShouldLevelUpGems);

                                    ImGui.TextDisabled("****** Skill settings ******");
                                    if (ImGui.Button($"Add new skill##{follower.FollowerName}"))
                                        follower.AddNewEmptySkill();
                                    ImGui.Spacing();
                                    ImGui.Spacing();

                                    foreach (var skill in follower.FollowerSkills)
                                    {
                                        var skillId = imguiId + skill.Id;

                                        ImGui.TextDisabled($"------ Skill (id: {skill.Id}) ------");

                                        skill.Enable = ImGuiExtension.Checkbox($"Enable##{skillId}", skill.Enable);
                                        ImGui.SameLine();
                                        ImGui.TextDisabled("    ");
                                        ImGui.SameLine();
                                        if (ImGui.Button($"Remove##{skillId}")) follower.RemoveSkill(skill.Id);

                                        ImGui.Spacing();
                                        skill.Hotkey =
                                            ImGuiExtension.HotkeySelector($"Hotkey: {skill.Hotkey}##{skillId}",
                                                skill.Hotkey);
                                        ImGui.Spacing();
                                        ImGui.SliderInt($"Priority##{skillId}", ref skill.Priority, 1, 5);
                                        ImGui.Spacing();
                                        ImGui.SliderInt($"Skill cooldown in ms##{skillId}", ref skill.CooldownMs, 100,
                                            10000);
                                        ImGui.Spacing();
                                        skill.IsMovingSkill = ImGuiExtension.Checkbox($"Is moving skill##{skillId}",
                                            skill.IsMovingSkill);
                                        ImGui.Spacing();

                                        if (!skill.IsMovingSkill)
                                        {
                                            skill.HoverEntityType = ImGuiExtension.ComboBox(
                                                $"Hover entity type##{skillId}",
                                                skill.HoverEntityType, FollowerSkillHoverEntityType.GetAllAsList());
                                            ImGui.Spacing();
                                            ImGui.SliderInt($"Max range##{skillId}",
                                                ref skill.MaxRange, 10, 200);
                                            ImGuiExtension.ToolTipWithText("(?)",
                                                "Range to monsters, range to corpse etc.");
                                            ImGui.Spacing();
                                        }
                                    }

                                    if (follower.FollowerSkills.Any()) ImGui.TextDisabled("-----------");

                                    ImGui.Spacing();
                                }

                        if ((isNetworkMode || isFileMode) && ImGui.TreeNodeEx("Advanced leader mode settings"))
                        {
                            if (isNetworkMode)
                            {
                                ImGui.TextDisabled(
                                    "Remember to restart the server if you have changed the port or the hostname");
                                ImGui.TextDisabled(
                                    "    run \"netsh http add urlacl url=http://HOSTNAME:PORT/\" user=YOUR_USER");
                                ImGui.TextDisabled(
                                    "    example \"netsh http add urlacl url=http://+:4412/\" user=YOUR_USER");
                                ImGui.TextDisabled("        if you have changed your hostname");
                                ImGui.TextDisabled("    allow the inbound connection on the port in firewall as well");
                                LeaderModeSettings.LeaderModeNetworkSettings.ServerHostname.Value =
                                    ImGuiExtension.InputText("Server Hostname",
                                        LeaderModeSettings.LeaderModeNetworkSettings.ServerHostname);
                                LeaderModeSettings.LeaderModeNetworkSettings.ServerPort.Value =
                                    ImGuiExtension.InputText("Server Port",
                                        LeaderModeSettings.LeaderModeNetworkSettings.ServerPort);
                                ImGui.Spacing();
                                ImGui.TextDisabled("Server management");
                                ImGui.Spacing();
                                ImGui.SameLine();
                                if (ImGui.Button("Restart Server"))
                                    LeaderModeSettings.LeaderModeNetworkSettings.ServerRestart.OnPressed();
                                ImGui.SameLine();
                                if (ImGui.Button("Stop Server"))
                                    LeaderModeSettings.LeaderModeNetworkSettings.ServerStop.OnPressed();
                                ImGui.Spacing();
                            }

                            if (isFileMode)
                            {
                                LeaderModeSettings.LeaderModeFileSettings.FilePath.Value = ImGuiExtension.InputText(
                                    "File path",
                                    LeaderModeSettings.LeaderModeFileSettings.FilePath);

                                ImGui.Spacing();
                            }
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    //ImGui.TreePop();
                }
        }

        #region Debug

        [Menu("Debug", 1000)] public EmptyNode EmptyDebug { get; set; } = new EmptyNode();
        [Menu("Debug", "", 2, 1000)] public ToggleNode Debug { get; set; } = new ToggleNode(false);
        public ToggleNode VerboseDebug { get; set; } = new ToggleNode(false);
        [Menu("Show Radius", "", 3, 1000)] public ToggleNode DebugShowRadius { get; set; } = new ToggleNode(false);
        public HotkeyNode DebugGenerateOnHoverEvents { get; set; } = Keys.L;

        public FollowerCommandsImguiSetting FollowerCommandsImguiSettings = new FollowerCommandsImguiSetting();

        #endregion

        #region Main

        [Menu("Main", 2000)] public EmptyNode EmptyMain { get; set; } = new EmptyNode();

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

        public ListNode NearbyPlayers { get; set; } = new ListNode {Values = new List<string>(), Value = ""};

        #endregion
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
            Values = new List<string>
            {
                FollowerNetworkActivityModeEnum.Local,
                FollowerNetworkActivityModeEnum.Network,
                FollowerNetworkActivityModeEnum.File
            },
            Value = FollowerNetworkActivityModeEnum.Local
        };

        public FollowerModeNetworkSetting FollowerModeNetworkSettings { get; set; } = new FollowerModeNetworkSetting();
        public FollowerModeFileSetting FollowerModeFileSettings { get; set; } = new FollowerModeFileSetting();
        public RangeNode<int> LeaderProximityRadius { get; set; } = new RangeNode<int>(100, 10, 300);
        public ToggleNode StartRequesting { get; set; } = new ToggleNode(false);
        public HotkeyNode StartRequestingHotkey { get; set; } = Keys.F3;
        public HotkeyNode MoveHotkey { get; set; } = Keys.T;
        public RangeNode<int> MoveLogicCooldown { get; set; } = new RangeNode<int>(50, 20, 300);
        public RangeNode<int> MinimumFpsThreshold { get; set; } = new RangeNode<int>(5, 1, 10);
    }

    public class LeaderModeSetting
    {
        public FollowerCommandSetting FollowerCommandSetting = new FollowerCommandSetting();
        public LeaderModeFileSetting LeaderModeFileSettings = new LeaderModeFileSetting();

        public LeaderModeNetworkSetting LeaderModeNetworkSettings = new LeaderModeNetworkSetting();

        public NewFollowerCommandClassSetting NewFollowerCommandClassSetting = new NewFollowerCommandClassSetting();
        public TextNode LeaderNameToPropagate { get; set; } = new TextNode("");
        public ButtonNode SetMyselfAsLeader { get; set; } = new ButtonNode();
        public ToggleNode PropagateWorkingOfFollowers { get; set; } = new ToggleNode(false);
        public HotkeyNode PropagateWorkingOfFollowersHotkey { get; set; } = Keys.F4;
        public RangeNode<int> LeaderProximityRadiusToPropagate { get; set; } = new RangeNode<int>(20, 1, 300);

        public RangeNode<int> MinimumFpsThresholdToPropagate { get; set; } = new RangeNode<int>(5, 1, 10);
    }

    public class LeaderModeFileSetting
    {
        public TextNode FilePath { get; set; } = new TextNode("C:\\test.txt");
        public ToggleNode StartFileWriting { get; set; } = new ToggleNode(false);
    }

    public class LeaderModeNetworkSetting
    {
        public TextNode ServerHostname { get; set; } = new TextNode("localhost");
        public TextNode ServerPort { get; set; } = new TextNode("4412");
        public ToggleNode StartServer { get; set; } = new ToggleNode(false);
        public ButtonNode ServerRestart { get; set; } = new ButtonNode();
        public ButtonNode ServerStop { get; set; } = new ButtonNode();
    }

    public class NewFollowerCommandClassSetting
    {
        public TextNode FollowerName { get; set; } = new TextNode("");

        public ButtonNode UseNearbyPlayerNameButton { get; set; } = new ButtonNode();

        public ButtonNode AddNewFollowerButton { get; set; } = new ButtonNode();
    }

    public class FollowerModeFileSetting
    {
        public TextNode FilePath { get; set; } = new TextNode("C:\\test.txt");

        public RangeNode<int> DelayBetweenReads { get; set; } = new RangeNode<int>(500, 300, 3000);
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
        public static string File = "file";
    }
}