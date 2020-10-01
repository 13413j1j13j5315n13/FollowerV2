using System;
using System.Collections;
using System.Collections.Generic;
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
using ExileCore.PoEMemory;
using ExileCore.Shared.Helpers;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector4 = System.Numerics.Vector4;
using Newtonsoft.Json;
using ImGuiNET;

namespace FollowerV2
{
    public class Follower : BaseSettingsPlugin<FollowerV2Settings>
    {
        private Coroutine _nearbyPlayersUpdateCoroutine;
        private Coroutine _networkRequestsCoroutine;
        private Coroutine _serverCoroutine;
        private readonly DelayHelper _delayHelper = new DelayHelper();

        private NetworkRequestStatus _networkRequestStatus = NetworkRequestStatus.Finished;
        private Server _server;

        //private readonly TestClass _testClass = new TestClass();

        public override bool Initialise()
        {
            _nearbyPlayersUpdateCoroutine = new Coroutine(UpdateNearbyPlayersWork(), this, "Update nearby players", true);
            _networkRequestsCoroutine = new Coroutine(MainNetworkRequestsWork(), this, "Network requests coroutine", true);
            _serverCoroutine = new Coroutine(MainServerWork(), this, "Server coroutine", true);

            // Fire all coroutines
            Core.ParallelRunner.Run(_nearbyPlayersUpdateCoroutine);
            Core.ParallelRunner.Run(_networkRequestsCoroutine);
            Core.ParallelRunner.Run(_serverCoroutine);

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

            //_testClass.Render();
            RenderAdditionalImgui();


            //// Just draw some test things
            //Vector2 leftPanelStartDrawPoint = GameController.LeftPanel.StartDrawPoint;
            //NumericsVector2 firstLineLeft = Graphics.DrawText("leftPanelStartDrawPoint", leftPanelStartDrawPoint, Color.Red, 20, FontAlign.Right);

            //Element stashElement = GameController.IngameState.IngameUi.StashElement;
            //Vector2 aaa = new Vector2(stashElement.GetClientRectCache.Width, stashElement.GetClientRectCache.Y + 20);

            //Element uiRoot = GameController.IngameState.UIRoot;
            //RectangleF rect = uiRoot.GetClientRect();
            //Vector2 a = new Vector2(uiRoot.Width / 2f, 50);
            //NumericsVector2 uiRootVector2 = Graphics.DrawText("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", aaa, Color.Yellow, 20, FontAlign.Top);
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
        }

        private IEnumerator MainNetworkRequestsWork()
        {
            LogMsgWithVerboseDebug("Starting MainNetworkRequestsWork function");

            while (true)
            {
                if (Settings.Profiles.Value != ProfilesEnum.Follower || !Settings.LeaderModeSettings.StartServer.Value || !Settings.FollowerModeSettings.StartNetworkRequesting.Value)
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

            if (_networkRequestStatus == NetworkRequestStatus.Working)
            {
                LogMsgWithVerboseDebug("    request has not been finished in DoFollowerNetworkActivityWork");
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

            Settings.FollowerModeSettings.LeaderName.Value = obj.LeaderName;
            Settings.FollowerModeSettings.LeaderProximityRadius.Value = obj.LeaderProximityRadius;
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

        private void RenderAdditionalImgui()
        {
            DateTime emptyDateTime = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var newWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground |
                                 ImGuiWindowFlags.NoScrollbar;
            string resizeButtonLabel = "Allowing resize";
            string lockButtonLabel = "Unlocked";

            if (Settings.FollowerCommandsImguiSettings.LockPanel.Value)
            {
                newWindowFlags |= ImGuiWindowFlags.NoMove;
                lockButtonLabel = "Lock";
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
                Settings.FollowerCommandsImguiSettings.LockPanel.Value =
                    !Settings.FollowerCommandsImguiSettings.LockPanel.Value;
            ImGui.SameLine();
            if (ImGui.Button(resizeButtonLabel))
                Settings.FollowerCommandsImguiSettings.NoResize.Value = 
                    !Settings.FollowerCommandsImguiSettings.NoResize.Value;

            ImGui.Spacing();

            foreach (var follower in Settings.LeaderModeSettings.FollowerCommandSettings.FollowerCommandsDataSet)
            {
                NumericsVector4 aaa = NumericsVector4.One;

                ImGui.SameLine();
                ImGui.TextUnformatted($"{follower.FollowerName}:  ");
                ImGui.SameLine();

                if (follower.LastTimeWaypointUsedDateTime != emptyDateTime)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.ToImgui());
                }
                if (ImGui.Button("Waypoint")) Settings.LeaderModeSettings.FollowerCommandSettings.UseWaypoint(follower.FollowerName);
                ImGui.PopStyleColor();

                if (follower.LastTimeEntranceUsedDateTime != emptyDateTime)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.ToImgui());
                }
                ImGui.SameLine();
                if (ImGui.Button("Entrance")) Settings.LeaderModeSettings.FollowerCommandSettings.UseEntrance(follower.FollowerName);
            }

            ImGui.Spacing();
            ImGui.End();
        }
    }
}
