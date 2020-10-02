using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using SharpDX;
using System.Net;
using System.IO;
using System.Windows.Forms;
using ExileCore.PoEMemory;
using ExileCore.Shared.Helpers;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector4 = System.Numerics.Vector4;
using Newtonsoft.Json;
using ImGuiNET;
using TreeRoutine.TreeSharp;
using Action = System.Action;

namespace FollowerV2
{
    public class Follower : BaseSettingsPlugin<FollowerV2Settings>
    {
        private Coroutine _nearbyPlayersUpdateCoroutine;
        private Coroutine _networkRequestsCoroutine;
        private Coroutine _serverCoroutine;
        private Coroutine _followerCoroutine;

        public Composite Tree { get; set; }

        private readonly DelayHelper _delayHelper = new DelayHelper();

        private NetworkRequestStatus _networkRequestStatus = NetworkRequestStatus.Finished;
        private int _networkRequestStatusRetries = 0;

        private Server _server;

        private FollowerState _followerState = new FollowerState();
        private readonly DateTime _emptyDateTime = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override bool Initialise()
        {
            Tree = CreateTree();

            _nearbyPlayersUpdateCoroutine = new Coroutine(UpdateNearbyPlayersWork(), this, "Update nearby players", true);
            _networkRequestsCoroutine = new Coroutine(MainNetworkRequestsWork(), this, "Network requests coroutine", true);
            _serverCoroutine = new Coroutine(MainServerWork(), this, "Server coroutine", true);
            _followerCoroutine = new Coroutine(() => TickTree(Tree), new WaitTime(200), this, "Follower coroutine", true);

            // Fire all coroutines
            Core.ParallelRunner.Run(_nearbyPlayersUpdateCoroutine);
            Core.ParallelRunner.Run(_networkRequestsCoroutine);
            Core.ParallelRunner.Run(_serverCoroutine);
            Core.ParallelRunner.Run(_followerCoroutine);

            GameController.LeftPanel.WantUse(() => true);

            return true;
        }

        private void SetAllOnCallbacks()
        {
            Settings.Profiles.OnValueSelected += OnProfileChange;
            //Settings.NearbyPlayers.OnValueSelected += OnNearbyPlayerAsLeaderSelect;

            Settings.FollowerModeSettings.FollowerModes.OnValueSelected += OnFollowerModeChange; // local or network
            Settings.FollowerModeSettings.StartNetworkRequesting.OnValueChanged += OnStartNetworkRequestingValueChanged;
            Settings.FollowerModeSettings.StartNetworkRequestingHotkey.OnValueChanged +=
                OnStartNetworkRequestingHotkeyValueChanged;
            Settings.FollowerModeSettings.UseNearbyPlayerAsLeaderButton.OnPressed += OnNearbyPlayerAsLeaderSelect;

            Settings.LeaderModeSettings.SetMyselfAsLeader.OnPressed += OnSetMyselfAsLeaderToPropagateChanged;
            Settings.LeaderModeSettings.ServerStop.OnPressed += (() => _server.KillServer());
            Settings.LeaderModeSettings.ServerRestart.OnPressed += (() => _server.RestartServer());
            Settings.LeaderModeSettings.StartServer.OnValueChanged += OnStartServerValueChanged;
            Settings.LeaderModeSettings.PropagateWorkingOfFollowersHotkey.OnValueChanged +=
                OnPropagateWorkingOfFollowersHotkeyValueChanged;
        }

        public override void OnLoad()
        {
            Input.RegisterKey(Settings.LeaderModeSettings.PropagateWorkingOfFollowersHotkey);
            Input.RegisterKey(Settings.FollowerModeSettings.StartNetworkRequestingHotkey);

            _delayHelper.AddToDelayManager(nameof(OnPropagateWorkingOfFollowersHotkeyPressed), OnPropagateWorkingOfFollowersHotkeyPressed, 1000);
            _delayHelper.AddToDelayManager(nameof(DebugHoverToLeader), DebugHoverToLeader, 50);
            _delayHelper.AddToDelayManager(nameof(StartNetworkRequestingPressed), StartNetworkRequestingPressed, 1000);

            SetAllOnCallbacks();

            _server = new Server(Settings);
            _server.RestartServer();
        }

        public override void Render()
        {
            if (!Settings.Enable.Value || !GameController.InGame) return;

            // Debug related
            if (Settings.DebugShowRadius.Value)
            {
                Camera camera = GameController.Game.IngameState.Camera;
                Entity player = GameController.EntityListWrapper.Player;

                Entity leaderEntity = GetLeaderEntity();
                if (leaderEntity != null)
                {
                    DebugHelper.DrawEllipseToWorld(camera, Graphics, leaderEntity.Pos, Settings.FollowerModeSettings.LeaderProximityRadius.Value, 25, 2, Color.LawnGreen);
                }

                DebugHelper.DrawEllipseToWorld(camera, Graphics, player.Pos, Settings.LeaderModeSettings.LeaderProximityRadiusToPropagate.Value, 25, 2, Color.Yellow);

            }
            // Debug related ends

            if (Settings.LeaderModeSettings.PropagateWorkingOfFollowersHotkey.PressedOnce())
            {
                _delayHelper.CallFunction(nameof(OnPropagateWorkingOfFollowersHotkeyPressed));
            }

            if (Settings.FollowerModeSettings.StartNetworkRequestingHotkey.PressedOnce())
            {
                _delayHelper.CallFunction(nameof(StartNetworkRequestingPressed));
            }

            if (Settings.FollowerCommandsImguiSettings.ShowWindow.Value && Settings.Profiles.Value == ProfilesEnum.Leader)
            {
                RenderFollowerCommandImgui();
            }
        }

        public override Job Tick()
        {
            // Debug related
            if (Settings.Debug.Value)
            {
                if (Input.GetKeyState(Settings.DebugGenerateOnHoverEvents.Value))
                {
                    _delayHelper.CallFunction(nameof(DebugHoverToLeader));
                }
            }
            // Debug related ends

            return null;
        }
        public override void DrawSettings()
        {
            Settings.DrawSettings();
        }

        private Composite CreateTree()
        {
            LogMsgWithVerboseDebug($"{nameof(CreateTree)} called");

            return new Decorator(x => ShouldWork() && CanTick() && IsPlayerAlive(),
                new PrioritySelector(
                    CreateFollowingComposite()
                )
            );
        }

        private Composite CreateFollowingComposite()
        {
            LogMsgWithVerboseDebug($"{nameof(CreateFollowingComposite)} called");

            return new Decorator(x => ShouldFollowLeader(),
                new Sequence(
                    new TreeRoutine.TreeSharp.Action(x =>
                    {
                        IEnumerable<Entity> players = GameController.Entities.Where(e => e.Type == EntityType.Player);
                        Entity leaderPlayer = players.FirstOrDefault(e => e.GetComponent<Player>().PlayerName == Settings.FollowerModeSettings.LeaderName.Value);

                        LogMsgWithVerboseDebug($"leaderPlayer: {leaderPlayer}");

                        if (leaderPlayer != null)
                        {
                            LogMsgWithVerboseDebug("Hovering and clicking on leader");

                            HoverTo(leaderPlayer);
                            return TreeRoutine.TreeSharp.RunStatus.Success;
                        }

                        return TreeRoutine.TreeSharp.RunStatus.Failure;
                    }),
                    new TreeRoutine.TreeSharp.Action(x =>
                    {
                        LogMsgWithVerboseDebug("Pressing Keys.T");

                        Input.KeyPressRelease(Keys.T);
                        return TreeRoutine.TreeSharp.RunStatus.Success;
                    })
                )
            );
        }

        #region TreeSharp Related

        private bool ShouldFollowLeader()
        {
            LogMsgWithVerboseDebug($"{nameof(ShouldFollowLeader)} called");

            bool leaderNotEmpty = !string.IsNullOrEmpty(Settings.FollowerModeSettings.LeaderName.Value);
            Entity leaderEntity = GetLeaderEntity();
            if (leaderEntity == null) return leaderNotEmpty;

            var distance = leaderEntity.Distance(GameController.Player);
            LogMsgWithVerboseDebug($"  distance: {distance}");
            LogMsgWithVerboseDebug($"  proximity: {Settings.FollowerModeSettings.LeaderProximityRadius.Value}");
            bool outsideBorders = distance > Settings.FollowerModeSettings.LeaderProximityRadius.Value;

            return leaderNotEmpty && outsideBorders;
        }

        private bool CanTick()
        {
            //LogMsgWithVerboseDebug($"{nameof(CanTick)} called");

            if (GameController.IsLoading)
            {
                return false;
            }

            if (!GameController.Game.IngameState.ServerData.IsInGame)
            {
                return false;
            }
            if (!GameController.Game.IngameState.ServerData.IsInGame)
            {
                return false;
            }
            else if (GameController.Player == null || GameController.Player.Address == 0 || !GameController.Player.IsValid)
            {
                return false;
            }
            else if (!GameController.Window.IsForeground())
            {
                return false;
            }

            //LogMsgWithVerboseDebug("    CanTick returning true");

            return true;
        }

        private bool IsPlayerAlive()
        {
            return GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().CurHP > 0;
        }

        private bool ShouldWork()
        {
            //LogMsgWithVerboseDebug($"{nameof(ShouldWork)} called");

            if (Settings.Profiles.Value == ProfilesEnum.Follower)
            {
                //LogMsgWithVerboseDebug($"    returning {Settings.FollowerModeSettings.FollowerShouldWork}");

                return Settings.FollowerModeSettings.FollowerShouldWork;
            }

            //LogMsgWithVerboseDebug("    returning false");
            return false;
        }

        #endregion

        private void TickTree(Composite treeRoot)
        {
            treeRoot.Start(null);

            try
            {
                treeRoot.Tick(null);
            }
            catch (Exception e)
            {
                LogError($"{Name}: Exception! \nMessage: {e.Message} \n{e.StackTrace}", 30);
                throw e;
            }

            if (treeRoot.LastStatus != RunStatus.Running)
            {
                // Reset the tree, and begin the execution all over...
                treeRoot.Stop(null);
                treeRoot.Start(null);
            }
        }

        //private void TickTree(Composite treeRoot)
        //{
        //    try
        //    {
        //        if (!Settings.Enable)
        //            return;

        //        if (treeRoot == null)
        //        {
        //            LogError("Plugin is not initialized");
        //            return;
        //        }

        //        if (treeRoot.LastStatus != null)
        //        {
        //            treeRoot.Tick(null);

        //            // If the last status wasn't running, stop the tree, and restart it.
        //            if (treeRoot.LastStatus != RunStatus.Running)
        //            {
        //                //LogMsgWithVerboseDebug("Stopping and restarting the tree in TickTree");
        //                treeRoot.Stop(null);
        //                treeRoot.Start(null);
        //            }
        //        }
        //        else
        //        {
        //            treeRoot.Start(null);
        //            RunStatus status = treeRoot.Tick(null);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        LogError($"{Name}: Exception! \nMessage: {e.Message} \n{e.StackTrace}", 30);
        //        throw e;
        //    }
        //}

        private void OnProfileChange(string profile)
        {
            LogMsgWithVerboseDebug("OnProfileChange called");
            LogMsgWithDebug($"Profile changed to: {profile}");

            if (profile == ProfilesEnum.Follower)
            {
                //_server.KillServer();
                Settings.LeaderModeSettings.StartServer.Value = false;
            }
            else if (profile == ProfilesEnum.Leader)
            {
                Settings.FollowerModeSettings.StartNetworkRequesting.Value = false;
                //_server.RestartServer();
            }
            else if (profile == ProfilesEnum.Disable)
            {
                //_server.KillServer();
                Settings.LeaderModeSettings.StartServer.Value = false;
                Settings.FollowerModeSettings.StartNetworkRequesting.Value = false;
            }
            else
            {
                LogError($"Profile changed to unsupported value: {profile}. This should not have happened...");
            }
        }

        private void OnFollowerModeChange(string newFollowerMode)
        {
            LogMsgWithVerboseDebug("OnFollowerModeChange called");

            if (newFollowerMode == FollowerNetworkActivityModeEnum.Local)
            {
                //_server.KillServer();
                Settings.LeaderModeSettings.StartServer.Value = false;
                Settings.FollowerModeSettings.StartNetworkRequesting.Value = false;
            }
            else if (newFollowerMode == FollowerNetworkActivityModeEnum.Network)
            {
                //_server.RestartServer();
            }
        }

        private void OnSetMyselfAsLeaderToPropagateChanged()
        {
            LogMsgWithVerboseDebug("OnSetMyselfAsLeaderToPropagateChanged called");

            string name = GameController.Player.GetComponent<Player>().PlayerName;
            Settings.LeaderModeSettings.LeaderNameToPropagate.Value = name;
        }

        private void OnStartServerValueChanged(object obj, bool value)
        {
            LogMsgWithVerboseDebug("OnStartServerValueChanged called");

            //if (value) _server.RestartServer();
            //else _server.KillServer();
        }

        private void OnPropagateWorkingOfFollowersHotkeyValueChanged()
        {
            LogMsgWithVerboseDebug("OnPropagateWorkingOfFollowersHotkeyValueChanged called");

            Input.RegisterKey(Settings.LeaderModeSettings.PropagateWorkingOfFollowersHotkey);
        }
        private void OnStartNetworkRequestingHotkeyValueChanged()
        {
            LogMsgWithVerboseDebug("OnStartNetworkRequestingHotkeyValueChanged called");

            Input.RegisterKey(Settings.FollowerModeSettings.StartNetworkRequestingHotkey);
        }

        private void OnPropagateWorkingOfFollowersHotkeyPressed()
        {
            LogMsgWithVerboseDebug("OnPropagateWorkingOfFollowersHotkeyPressed called");

            Settings.LeaderModeSettings.PropagateWorkingOfFollowers.Value =
                !Settings.LeaderModeSettings.PropagateWorkingOfFollowers.Value;
        }

        private void OnNearbyPlayerAsLeaderSelect()
        {
            LogMsgWithVerboseDebug("OnNearbyPlayerAsLeaderSelect called");

            if (!String.IsNullOrEmpty(Settings.NearbyPlayers.Value)) Settings.FollowerModeSettings.LeaderName.Value = Settings.NearbyPlayers.Value;

            Settings.NearbyPlayers.Value = "";
        }

        private void OnStartNetworkRequestingValueChanged(object obj, bool value)
        {
            LogMsgWithVerboseDebug("OnStartNetworkRequestingValueChanged called");
            if (!value) Settings.FollowerModeSettings.FollowerShouldWork = false;
        }

        private IEnumerator MainNetworkRequestsWork()
        {
            LogMsgWithVerboseDebug("Starting MainNetworkRequestsWork function");

            while (true)
            {
                if (Settings.Profiles.Value != ProfilesEnum.Follower || !Settings.FollowerModeSettings.StartNetworkRequesting.Value)
                {
                    yield return new WaitTime(100);
                    continue;
                }

                yield return DoFollowerNetworkActivityWork();
                yield return new WaitTime(Settings.FollowerModeSettings.FollowerModeNetworkSettings.DelayBetweenRequests.Value);
            }
        }

        private IEnumerator MainServerWork()
        {
            LogMsgWithVerboseDebug("Starting MainServerWork function");

            while (true)
            {
                if (Settings.Profiles.Value != ProfilesEnum.Leader || !Settings.LeaderModeSettings.StartServer.Value)
                {
                    yield return new WaitTime(100);
                    continue;
                }

                LogMsgWithVerboseDebug("MainServerWork: Starting the server and listening");

                //_server.StartServer();
                _server.Listen();

                yield return new WaitTime(50);
            }
        }

        private IEnumerator UpdateNearbyPlayersWork()
        {
            LogMsgWithVerboseDebug("Starting UpdateNearbyPlayersWork function");
            while (true)
            {
                List<string> playerNames = GameController.EntityListWrapper
                    .Entities.Where(e => e.Type == EntityType.Player)
                    .Where(e =>
                    {
                        string playerName = e.GetComponent<Player>().PlayerName;
                        if (playerName == "") return false;
                        return playerName != GameController.Player.GetComponent<Player>().PlayerName;

                    })
                    .Select(e => e.GetComponent<Player>().PlayerName)
                    .ToList();

                Settings.NearbyPlayers.Values = playerNames;

                yield return new WaitTime(1000);
            }
        }

        private Entity GetLeaderEntity()
        {
            string leaderName = Settings.FollowerModeSettings.LeaderName.Value;

            IEnumerable<Entity> players = GameController.Entities.Where(x => x.Type == EntityType.Player);
            return players.FirstOrDefault(x => x.GetComponent<Player>().PlayerName == leaderName);
        }

        private void HoverTo(Entity entity)
        {
            LogMsgWithVerboseDebug("HoverTo called");

            if (entity == null) return;

            var worldCoords = entity.Pos;
            Camera camera = GameController.Game.IngameState.Camera;

            var result = camera.WorldToScreen(worldCoords);

            var randomXOffset = new Random().Next(0, Settings.RandomClickOffset.Value);
            var randomYOffset = new Random().Next(0, Settings.RandomClickOffset.Value);

            Vector2 finalPos = new Vector2(result.X + randomXOffset, result.Y + randomYOffset);

            Mouse.MoveCursorToPosition(finalPos);
        }

        private IEnumerator DoFollowerNetworkActivityWork()
        {
            LogMsgWithVerboseDebug("DoFollowerNetworkActivityWork called");

            string url = Settings.FollowerModeSettings.FollowerModeNetworkSettings.Url.Value;
            int timeoutMs = Settings.FollowerModeSettings.FollowerModeNetworkSettings.RequestTimeoutMs.Value;

            if (string.IsNullOrEmpty(url))
            {
                LogMsgWithVerboseDebug("    url in DoFollowerNetworkActivityWork was null or empty");
                yield break;
            }

            if (_networkRequestStatusRetries > 10)
            {
                _networkRequestStatus = NetworkRequestStatus.Finished;
            }

            if (_networkRequestStatus == NetworkRequestStatus.Working)
            {
                LogMsgWithVerboseDebug("    request has not been finished in DoFollowerNetworkActivityWork");
                _networkRequestStatusRetries++;
                yield break;
            }

            _networkRequestStatus = NetworkRequestStatus.Working;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeoutMs;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    string reply = reader.ReadToEnd();

                    NetworkActivityObject networkActivityObject = JsonConvert.DeserializeObject<NetworkActivityObject>(reply);
                    ProcessNetworkActivityResponse(networkActivityObject);
                }
                else
                {
                    LogMsgWithVerboseDebug(
                        $"Follower - tried to make a HTTP request to {url} but the return message was not successful");
                }
            }
            finally
            {
                _networkRequestStatus = NetworkRequestStatus.Finished;
            }

            yield break;
        }

        private void ProcessNetworkActivityResponse(NetworkActivityObject obj)
        {
            LogMsgWithVerboseDebug("ProcessNetworkActivityResponse called");

            if (obj == null)
            {
                return;
            }

            Settings.FollowerModeSettings.FollowerShouldWork = obj.FollowersShouldWork;
            Settings.FollowerModeSettings.LeaderName.Value = obj.LeaderName;
            Settings.FollowerModeSettings.LeaderProximityRadius.Value = obj.LeaderProximityRadius;

            string selfName = GameController.EntityListWrapper.Player.GetComponent<Player>().PlayerName;
            var follower = obj.FollowerCommandSettings.FollowerCommandsDataSet.First(f => f.FollowerName == selfName);

            if (follower == null) return;

            _followerState.LastTimeEntranceUsedDateTime = follower.LastTimeEntranceUsedDateTime;
            _followerState.LastTimePortalUsedDateTime = follower.LastTimePortalUsedDateTime;
        }

        private void StartNetworkRequestingPressed()
        {
            LogMsgWithVerboseDebug("StartNetworkRequestingPressed called");

            Settings.FollowerModeSettings.StartNetworkRequesting.Value =
                !Settings.FollowerModeSettings.StartNetworkRequesting.Value;
        }
        private void DebugHoverToLeader()
        {
            HoverTo(GetLeaderEntity());
        }

        private void LogMsgWithDebug(string message)
        {
            if (!Settings.Debug.Value) return;
            LogMessage(message);
        }

        private void LogMsgWithVerboseDebug(string message)
        {
            if (Settings.Debug.Value && Settings.VerboseDebug.Value)
                LogMessage(message);
        }

        private enum NetworkRequestStatus
        {
            Finished,
            Working,
        }

        private void RenderFollowerCommandImgui()
        {
            DateTime emptyDateTime = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var newWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground |
                                 ImGuiWindowFlags.NoScrollbar;
            string resizeButtonLabel = "Allowing resize";
            string lockButtonLabel = "Unlocked";

            if (Settings.FollowerCommandsImguiSettings.LockPanel.Value)
            {
                newWindowFlags |= ImGuiWindowFlags.NoMove;
                lockButtonLabel = "Locked";
            }

            if (Settings.FollowerCommandsImguiSettings.NoResize.Value)
            {
                newWindowFlags |= ImGuiWindowFlags.NoResize;
                resizeButtonLabel = "Restricting resizing";
            }

            ImGui.SetNextWindowBgAlpha(0.35f);
            ImGui.Begin("FollowerV2", newWindowFlags);

            ImGui.TextUnformatted("This window commands");
            ImGui.SameLine();
            if (ImGui.Button(lockButtonLabel))
            {
                Settings.FollowerCommandsImguiSettings.LockPanel.Value = !Settings.FollowerCommandsImguiSettings.LockPanel.Value;
            }
            ImGui.SameLine();
            if (ImGui.Button(resizeButtonLabel))
            {
                Settings.FollowerCommandsImguiSettings.NoResize.Value = !Settings.FollowerCommandsImguiSettings.NoResize.Value;
            }
            ImGui.Spacing();

            foreach (var follower in Settings.LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet)
            {
                ImGui.TextUnformatted($"User: {follower.FollowerName}:");
                ImGui.SameLine();
                if (follower.LastTimeEntranceUsedDateTime != emptyDateTime)
                {
                    ImGui.TextUnformatted("E");
                    ImGui.SameLine();
                }

                if (follower.LastTimePortalUsedDateTime != emptyDateTime)
                {
                    ImGui.TextUnformatted(" P");
                    ImGui.SameLine();
                }

                if (ImGui.Button($"Entrance##{follower.FollowerName}")) follower.SetToUseEntrance();

                ImGui.SameLine();
                if (ImGui.Button($"Portal##{follower.FollowerName}")) follower.SetToUsePortal();
            }
            ImGui.Spacing();

            List<FollowerCommandsDataClass> followers = Settings.LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet.ToList();

            ImGui.SameLine();
            ImGui.TextUnformatted($"All:  ");
            ImGui.SameLine();
            if (ImGui.Button("Entrance##AllEntrance")) followers.ForEach(f => f.SetToUseEntrance());
            ImGui.SameLine();
            if (ImGui.Button("Portal##AllPortal")) followers.ForEach(f => f.SetToUsePortal());
            ImGui.Spacing();

            ImGui.Spacing();
            ImGui.End();
        }

        private void RenderFollowerCommands(string name, Action useWaypoint, Action useEntrace, Action usePortal)
        {

        }
    }
}
