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
using System.Threading;
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
            _followerCoroutine = new Coroutine(MainFollowerWork(), this, "Follower coroutine", true);

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
            _delayHelper.AddToDelayManager(nameof(SendPickupItemSignal), SendPickupItemSignal, 1000);

            SetAllOnCallbacks();

            try
            {
                _server = new Server(Settings);
                _server.RestartServer();
            }
            catch (Exception e)
            {
                LogMsgWithDebug($"Initializing Server failed.\n{e.Message}\n{e.StackTrace}");
            }
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

            if (Input.GetKeyState(Keys.ControlKey) && Settings.Profiles.Value == ProfilesEnum.Leader && Settings.FollowerCommandsImguiSettings.ShowWindow.Value)
            {
                RenderFollowerCommandImgui();
            }

            WriteLeftPanelTexts();
        }

        private void WriteLeftPanelTexts()
        {
            int fontHeight = 20;
            Vector2 startDrawPoint = GameController.LeftPanel.StartDrawPoint;

            bool isLocalMode = Settings.FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Local;
            bool isNetworkMode = Settings.FollowerModeSettings.FollowerModes.Value == FollowerNetworkActivityModeEnum.Network;
            bool isLeaderProfile = Settings.Profiles.Value == ProfilesEnum.Leader;
            bool isFollowerProfile = Settings.Profiles.Value == ProfilesEnum.Follower;

            NumericsVector2 firstLine = Graphics.DrawText("FollowerV2    ", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
            startDrawPoint.Y += firstLine.Y;

            firstLine = Graphics.DrawText($"Profile: {Settings.Profiles.Value}", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
            startDrawPoint.Y += firstLine.Y;

            firstLine = Graphics.DrawText($"FollowerMode: {Settings.FollowerModeSettings.FollowerModes.Value}", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
            startDrawPoint.Y += firstLine.Y;

            if (isFollowerProfile)
            {
                if (isNetworkMode)
                {
                    firstLine = Graphics.DrawText($"Network requesting: {Settings.FollowerModeSettings.StartNetworkRequesting.Value}", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                    startDrawPoint.Y += firstLine.Y;
                }

                firstLine = Graphics.DrawText($"Follower working: {Settings.FollowerModeSettings.FollowerShouldWork.Value}", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                startDrawPoint.Y += firstLine.Y;
            }

            if (isLeaderProfile && isNetworkMode)
            {
                firstLine = Graphics.DrawText($"Propagate working: {Settings.LeaderModeSettings.PropagateWorkingOfFollowers.Value}", startDrawPoint, Color.Yellow, fontHeight, FontAlign.Right);
                startDrawPoint.Y += firstLine.Y;
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

            if (Settings.Profiles.Value == ProfilesEnum.Leader)
            {
                if (Input.GetKeyState(Keys.ControlKey))
                {
                    Keys[] numberKeys = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0 };
                    bool anyNumberKeyPressed = numberKeys.Any(Input.GetKeyState);
                    if (anyNumberKeyPressed)
                    {
                        _delayHelper.CallFunction(nameof(SendPickupItemSignal));
                    }
                }
            }

            return null;
        }

        private void SendPickupItemSignal()
        {
            int index;
            if (Input.GetKeyState(Keys.D1)) index = 0;
            else if (Input.GetKeyState(Keys.D2)) index = 1;
            else if (Input.GetKeyState(Keys.D3)) index = 2;
            else if (Input.GetKeyState(Keys.D4)) index = 3;
            else if (Input.GetKeyState(Keys.D5)) index = 4;
            else if (Input.GetKeyState(Keys.D6)) index = 5;
            else if (Input.GetKeyState(Keys.D7)) index = 6;
            else if (Input.GetKeyState(Keys.D8)) index = 7;
            else if (Input.GetKeyState(Keys.D9)) index = 8;
            else if (Input.GetKeyState(Keys.D0)) index = 9;
            else
            {
                LogMsgWithVerboseDebug("*** No proper number key pressed found");
                return;
            }

            int len = Settings.LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet.Count;
            if (index > len - 1)
            {
                LogMsgWithVerboseDebug("*** index was larger than length");
                return;
            }

            FollowerCommandsDataClass follower = Settings.LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet.ElementAt(index);

            Entity targetedEntity = GameController.EntityListWrapper.Entities
                .Where(e => e.GetComponent<Targetable>() != null)
                .Where(e => e.Type != EntityType.Player)
                .Where(e => e.Type != EntityType.Monster)
                .FirstOrDefault(e => e.GetComponent<Targetable>().isTargeted);

            if (targetedEntity == null)
            {
                LogMsgWithVerboseDebug("*** No targeted item found");
                return;
            }

            int entityId = (int)targetedEntity.Id;
            LogMsgWithVerboseDebug($"*** Setting follower {follower.FollowerName} to pick item id {entityId}");

            follower.SetPickupNormalItem(entityId);
        }

        public override void DrawSettings()
        {
            Settings.DrawSettings();
        }

        private Composite CreateTree()
        {
            LogMsgWithVerboseDebug($"{nameof(CreateTree)} called");

            return new Decorator(x => ShouldWork() && BtCanTick() && IsPlayerAlive(),
                new PrioritySelector(
                    CreatePickingTargetedItemComposite(),
                    CreatePickingQuestItemComposite(),
                    CreateUsingPortalCompositeV2(),
                    CreateUsingEntranceCompositeV2(),

                    // Following has the lowest priority
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
                        Entity leaderPlayer;
                        try
                        {
                            leaderPlayer = players.FirstOrDefault(e =>
                                e.GetComponent<Player>().PlayerName == Settings.FollowerModeSettings.LeaderName.Value);
                        }
                        // Sometimes we can get "Collection was modified; enumeration operation may not execute" exception
                        catch (Exception e)
                        {
                            return;
                        }

                        //LogMsgWithVerboseDebug($"leaderPlayer: {leaderPlayer}");

                        if (leaderPlayer != null)
                        {
                            //LogMsgWithVerboseDebug("Hovering and clicking on leader");

                            HoverTo(leaderPlayer);

                            Input.KeyDown(Keys.T);
                            Thread.Sleep(5);
                            Input.KeyUp(Keys.T);
                        }
                    })
                )
            );
        }

        private Composite CreatePickingQuestItemComposite()
        {
            return new Decorator(x => ShouldPickupQuestItem(),
                new Sequence(
                    new TreeRoutine.TreeSharp.Action(x =>
                        {
                            LogMsgWithVerboseDebug("Picking quest item");

                            Input.KeyUp(Keys.T);

                            _followerState.LastTimeQuestItemPickupDateTime = _emptyDateTime;
                            _followerState.SavedLastTimeQuestItemPickupDateTime = _emptyDateTime;

                            // Take only quest items
                            Entity entity = GameController.EntityListWrapper.Entities
                                .Where(e => e.Type == EntityType.WorldItem)
                                .Where(e => e.IsTargetable)
                                .Where(e => e.GetComponent<WorldItem>() != null)
                                .FirstOrDefault(e =>
                                {
                                    Entity itemEntity = e.GetComponent<WorldItem>().ItemEntity;
                                    return GameController.Files.BaseItemTypes.Translate(itemEntity.Path).ClassName == "QuestItem";
                                });

                            if (entity == null) return TreeRoutine.TreeSharp.RunStatus.Failure;

                            Input.KeyDown(Keys.F);
                            var hovered = HoverToEntityAction(entity);
                            Input.KeyUp(Keys.F);

                            if (!hovered) return TreeRoutine.TreeSharp.RunStatus.Failure;

                            Input.KeyDown(Keys.F);
                            Mouse.LeftClick(10);
                            Thread.Sleep(2000);
                            Input.KeyUp(Keys.F);

                            return TreeRoutine.TreeSharp.RunStatus.Success;
                        })
                )
            );
        }

        private Composite CreatePickingTargetedItemComposite()
        {
            return new Decorator(x => ShouldPickupNormalItem(),
                new Sequence(
                    new TreeRoutine.TreeSharp.Action(x =>
                    {
                        LogMsgWithVerboseDebug($"Picking targeted item with id {_followerState.NormalItemId}");

                        Input.KeyUp(Keys.T);

                        _followerState.SavedLastTimeNormalItemPickupDateTime = _emptyDateTime;
                        _followerState.LastTimeNormalItemPickupDateTime = _followerState.LastTimeNormalItemPickupDateTime;

                        Entity entity = GameController.EntityListWrapper.Entities.FirstOrDefault(e => e.Id == _followerState.NormalItemId);

                        if (entity == null) return TreeRoutine.TreeSharp.RunStatus.Success;

                        Input.KeyDown(Keys.F);
                        Thread.Sleep(20);
                        Input.KeyDown(Keys.Alt);
                        Thread.Sleep(20);
                        var hovered = HoverToEntityAction(entity);
                        Thread.Sleep(20);
                        Input.KeyUp(Keys.F);
                        Thread.Sleep(20);
                        Input.KeyUp(Keys.Alt);

                        if (!hovered) return TreeRoutine.TreeSharp.RunStatus.Failure;

                        Input.KeyDown(Keys.F);
                        Input.KeyDown(Keys.Alt);
                        Thread.Sleep(100);
                        Mouse.LeftClick(10);
                        Thread.Sleep(2000);
                        Input.KeyUp(Keys.F);
                        Input.KeyUp(Keys.Alt);

                        return TreeRoutine.TreeSharp.RunStatus.Success;
                    })
                )
            );
        }

        private Composite CreateUsingPortalCompositeV2()
        {
            LogMsgWithVerboseDebug($"{nameof(CreateUsingPortalCompositeV2)} called");
            return new Decorator(x => _followerState.CurrentAction == ActionsEnum.UsingPortal,
                new Sequence(
                    new TreeRoutine.TreeSharp.Action(x =>
                    {
                        Input.KeyUp(Keys.T);

                        _followerState.PortalLogicIterationCount++;

                        // Allow only 3 portal logic iterations
                        if (_followerState.PortalLogicIterationCount > 3)
                        {
                            _followerState.PortalLogicIterationCount = 0;
                            _followerState.CurrentAction = ActionsEnum.Nothing;

                            return TreeRoutine.TreeSharp.RunStatus.Failure;
                        }

                        Entity portalEntity = GetEntitiesByEntityTypeAndSortByDistance(EntityType.TownPortal, GameController.Player).FirstOrDefault();
                        if (portalEntity == null) return TreeRoutine.TreeSharp.RunStatus.Failure;

                        // If portal entity is too far away stop the whole logic
                        if (portalEntity.Distance(GameController.Player) > 70)
                        {
                            return TreeRoutine.TreeSharp.RunStatus.Failure;
                        }

                        bool hovered = HoverToEntityAction(portalEntity);

                        if (!hovered) return TreeRoutine.TreeSharp.RunStatus.Failure;

                        Mouse.LeftClick(10);
                        Thread.Sleep(2000);

                        _followerState.PortalLogicIterationCount = 0;
                        _followerState.CurrentAction = ActionsEnum.Nothing;

                        return TreeRoutine.TreeSharp.RunStatus.Success;
                    })
                )
            );
        }

        private Composite CreateUsingEntranceCompositeV2()
        {
            LogMsgWithVerboseDebug($"{nameof(CreateUsingEntranceCompositeV2)} called");
            return new Decorator(x => _followerState.CurrentAction == ActionsEnum.UsingEntrance,
                new Sequence(
                    new TreeRoutine.TreeSharp.Action(x =>
                    {
                        Input.KeyUp(Keys.T);

                        _followerState.EntranceLogicIterationCount++;

                        // Allow only 3 entrance logic iterations
                        if (_followerState.EntranceLogicIterationCount > 3)
                        {
                            _followerState.EntranceLogicIterationCount = 0;
                            _followerState.CurrentAction = ActionsEnum.Nothing;

                            return TreeRoutine.TreeSharp.RunStatus.Failure;
                        }

                        Entity entranceEntity = GetEntitiesByEntityTypeAndSortByDistance(EntityType.AreaTransition, GameController.Player).FirstOrDefault();
                        if (entranceEntity == null) return TreeRoutine.TreeSharp.RunStatus.Failure;

                        // If portal entity is too far away stop the whole logic
                        if (entranceEntity.Distance(GameController.Player) > 70)
                        {
                            return TreeRoutine.TreeSharp.RunStatus.Failure;
                        }

                        bool hovered = HoverToEntityAction(entranceEntity);

                        if (!hovered) return TreeRoutine.TreeSharp.RunStatus.Failure;

                        Mouse.LeftClick(10);
                        Thread.Sleep(2000);

                        _followerState.EntranceLogicIterationCount = 0;
                        _followerState.CurrentAction = ActionsEnum.Nothing;

                        return TreeRoutine.TreeSharp.RunStatus.Success;
                    })
                )
            );
        }

        private bool HoverToEntityAction(Entity entity)
        {
            Random rnd = new Random();
            int offsetValue = 10;

            // Matrix of offsets as vectors. Try each offset and see whether the entity's isTargeted is true
            List<Vector2> offsets = new List<Vector2>();

            foreach (int yOffset in Enumerable.Range(-5, 5))
            {
                foreach (int xOffset in Enumerable.Range(-5, 5))
                {
                    offsets.Add(new Vector2(xOffset * offsetValue, yOffset * offsetValue));
                }
            }

            bool targeted = false;

            HoverTo(entity);

            while (offsets.Any())
            {
                if (entity.GetComponent<Targetable>().isTargeted)
                {
                    targeted = true;
                    break;
                }

                if (!Settings.FollowerModeSettings.FollowerShouldWork.Value) break;

                // If entity is not present anymore (e.g. map portal is used by another player) stop hovering
                if (!IsEntityPresent(entity.Id)) break;

                int elem = rnd.Next(offsets.Count);
                Vector2 offset = offsets[elem];
                offsets.Remove(offset);

                HoverTo(entity, (int)offset.X, (int)offset.Y);
                Thread.Sleep(50);
            }

            Thread.Sleep(50);

            return targeted;
        }

        private bool IsEntityPresent(uint entityId)
        {
            bool isEntityPresent = false;
            try
            {
                isEntityPresent = GameController.Entities.Any(e => e.Id == entityId);

            }
            catch (Exception e) { }

            return isEntityPresent;
        }

        private TreeRoutine.TreeSharp.Action SleepAction(int timeoutMs)
        {
            return new TreeRoutine.TreeSharp.Action(x =>
            {
                Thread.Sleep(2000);
                return TreeRoutine.TreeSharp.RunStatus.Success;
            });
        }

        #region TreeSharp Related

        private bool ShouldPickupNormalItem()
        {
            return _followerState.LastTimeNormalItemPickupDateTime != _emptyDateTime &&
                   _followerState.LastTimeNormalItemPickupDateTime != _followerState.SavedLastTimeNormalItemPickupDateTime;
        }

        private bool ShouldPickupQuestItem()
        {
            return _followerState.LastTimeQuestItemPickupDateTime != _emptyDateTime &&
                   _followerState.LastTimeQuestItemPickupDateTime != _followerState.SavedLastTimeQuestItemPickupDateTime;
        }

        private bool ShouldFollowLeader()
        {
            //LogMsgWithVerboseDebug($"{nameof(ShouldFollowLeader)} called");

            bool leaderNotEmpty = !string.IsNullOrEmpty(Settings.FollowerModeSettings.LeaderName.Value);
            Entity leaderEntity = GetLeaderEntity();
            if (leaderEntity == null) return leaderNotEmpty;

            var distance = leaderEntity.Distance(GameController.Player);
            //LogMsgWithVerboseDebug($"  distance: {distance}");
            //LogMsgWithVerboseDebug($"  proximity: {Settings.FollowerModeSettings.LeaderProximityRadius.Value}");
            bool outsideBorders = distance > Settings.FollowerModeSettings.LeaderProximityRadius.Value;

            return leaderNotEmpty && outsideBorders;
        }

        private bool BtCanTick()
        {
            //LogMsgWithVerboseDebug($"{nameof(BtCanTick)} called");

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

            //LogMsgWithVerboseDebug("    BtCanTick returning true");

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
                //LogMsgWithVerboseDebug($"    returning {Settings.FollowerModeSettings.FollowerShouldWork.Value}");

                return Settings.FollowerModeSettings.FollowerShouldWork.Value;
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
            if (!value) Settings.FollowerModeSettings.FollowerShouldWork.Value = false;
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

        private IEnumerator MainFollowerWork()
        {
            LogMsgWithVerboseDebug($"Starting {nameof(MainFollowerWork)} function");

            while (true)
            {
                Tree.Start(null);

                try
                {
                    Tree.Tick(null);
                }
                catch (Exception e)
                {
                    LogError($"{Name}: Exception! \nMessage: {e.Message} \n{e.StackTrace}", 30);
                    throw e;
                }

                if (Tree.LastStatus != RunStatus.Running)
                {
                    // Reset the tree, and begin the execution all over...
                    Tree.Stop(null);
                    Tree.Start(null);
                }

                yield return new WaitTime(50);
            }
        }

        private IEnumerator MainServerWork()
        {
            LogMsgWithVerboseDebug($"Starting {nameof(MainServerWork)} function");

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

            try
            {
                return GameController.Entities
                    .Where(x => x.Type == EntityType.Player)
                    .FirstOrDefault(x => x.GetComponent<Player>().PlayerName == leaderName);
            }
            // Sometimes we can get "Collection was modified; enumeration operation may not execute" exception
            catch (Exception e)
            {
                return null;
            }
        }

        private void HoverTo(Entity entity, int xOffset = 0, int yOffset = 0)
        {
            //LogMsgWithVerboseDebug("HoverTo called");

            if (entity == null) return;

            Camera camera = GameController.Game.IngameState.Camera;
            Vector2 windowOffset = GameController.Window.GetWindowRectangle().TopLeft;

            var result = camera.WorldToScreen(entity.Pos);

            var randomXOffset = new Random().Next(0, Settings.RandomClickOffset.Value);
            var randomYOffset = new Random().Next(0, Settings.RandomClickOffset.Value);

            Vector2 finalPos = new Vector2(result.X + randomXOffset + xOffset + windowOffset.X, result.Y + randomYOffset + yOffset + windowOffset.Y);

            Mouse.SetCursorPosHuman2(finalPos);
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

            if (_networkRequestStatusRetries > 5)
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

        private List<Entity> GetEntitiesByEntityTypeAndSortByDistance(EntityType entityType, Entity entity)
        {
            return GameController.EntityListWrapper.ValidEntitiesByType[entityType]
                .OrderBy(o => FollowerHelpers.EntityDistance(o, entity))
                .ToList();
        }

        private void ProcessNetworkActivityResponse(NetworkActivityObject obj)
        {
            LogMsgWithVerboseDebug("ProcessNetworkActivityResponse called");

            if (obj == null)
            {
                return;
            }

            Settings.FollowerModeSettings.FollowerShouldWork.Value = obj.FollowersShouldWork;
            Settings.FollowerModeSettings.LeaderName.Value = obj.LeaderName;
            Settings.FollowerModeSettings.LeaderProximityRadius.Value = obj.LeaderProximityRadius;

            string selfName = GameController.EntityListWrapper.Player.GetComponent<Player>().PlayerName;
            var follower = obj.FollowerCommandSettings.FollowerCommandsDataSet.First(f => f.FollowerName == selfName);

            if (follower == null) return;

            _followerState.LastTimeEntranceUsedDateTime = follower.LastTimeEntranceUsedDateTime;
            _followerState.LastTimePortalUsedDateTime = follower.LastTimePortalUsedDateTime;
            _followerState.LastTimeQuestItemPickupDateTime = follower.LastTimeQuestItemPickupDateTime;
            _followerState.LastTimeNormalItemPickupDateTime = follower.LastTimeNormalItemPickupDateTime;
            _followerState.LastTimeNormalItemPickupDateTime = follower.LastTimeNormalItemPickupDateTime;
            _followerState.NormalItemId = follower.NormalItemId;

            if (_followerState.LastTimePortalUsedDateTime != _emptyDateTime && _followerState.LastTimePortalUsedDateTime != _followerState.SavedLastTimePortalUsedDateTime)
            {
                _followerState.SavedLastTimePortalUsedDateTime = _followerState.LastTimePortalUsedDateTime;
                _followerState.CurrentAction = ActionsEnum.UsingPortal;
            }

            if (_followerState.LastTimeEntranceUsedDateTime != _emptyDateTime && _followerState.LastTimeEntranceUsedDateTime != _followerState.SavedLastTimeEntranceUsedDateTime)
            {
                _followerState.SavedLastTimeEntranceUsedDateTime = _followerState.LastTimeEntranceUsedDateTime;
                _followerState.CurrentAction = ActionsEnum.UsingEntrance;
            }

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

            int userNumber = 1;

            foreach (var follower in Settings.LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet)
            {
                ImGui.TextUnformatted($"User {userNumber}: {follower.FollowerName}:");
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
                if (follower.LastTimeQuestItemPickupDateTime != emptyDateTime)
                {
                    ImGui.TextUnformatted(" Q");
                    ImGui.SameLine();
                }
                if (follower.LastTimeNormalItemPickupDateTime != emptyDateTime)
                {
                    ImGui.TextUnformatted(" I");
                    ImGui.SameLine();
                }

                if (ImGui.Button($"E##{follower.FollowerName}")) follower.SetToUseEntrance();

                ImGui.SameLine();
                if (ImGui.Button($"P##{follower.FollowerName}")) follower.SetToUsePortal();

                ImGui.SameLine();
                if (ImGui.Button($"QIPick##{follower.FollowerName}")) follower.SetPickupQuestItem();

                ImGui.SameLine();
                if (ImGui.Button($"Del##{follower.FollowerName}")) Settings.LeaderModeSettings.FollowerCommandSetting.RemoveFollower(follower.FollowerName);

                ImGui.SameLine();
                ImGui.TextUnformatted($"I: Ctrl+{userNumber}");

                userNumber++;
            }
            ImGui.Spacing();

            List<FollowerCommandsDataClass> followers = Settings.LeaderModeSettings.FollowerCommandSetting.FollowerCommandsDataSet.ToList();

            ImGui.SameLine();
            ImGui.TextUnformatted($"All:  ");
            ImGui.SameLine();
            if (ImGui.Button("Entrance##AllEntrance")) followers.ForEach(f => f.SetToUseEntrance());
            ImGui.SameLine();
            if (ImGui.Button("Portal##AllPortal")) followers.ForEach(f => f.SetToUsePortal());
            ImGui.SameLine();
            if (ImGui.Button("PickQuestItem##AllPickQuestItem")) followers.ForEach(f => f.SetPickupQuestItem());
            ImGui.Spacing();

            ImGui.Spacing();
            ImGui.End();
        }
    }
}
