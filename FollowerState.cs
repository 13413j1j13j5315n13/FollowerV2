using System;
using System.Collections.Generic;
using SharpDX;

namespace FollowerV2
{
    class FollowerState
    {
        public DateTime LastTimeEntranceUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime SavedLastTimeEntranceUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimePortalUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime SavedLastTimePortalUsedDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeQuestItemPickupDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime SavedLastTimeQuestItemPickupDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime LastTimeNormalItemPickupDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public DateTime SavedLastTimeNormalItemPickupDateTime { get; set; } = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public int NormalItemId = 0;

        public ActionsEnum CurrentAction = ActionsEnum.Nothing;

        public int PortalLogicIterationCount = 0;

        public int EntranceLogicIterationCount = 0;
        public Vector3 SavedCurrentPos = Vector3.Zero;
        public uint SavedCurrentAreaHash = 0;

        public List<FollowerSkill> FollowerSkills { get; set; } = new List<FollowerSkill>();

        public void ResetAreaChangingValues()
        {
            EntranceLogicIterationCount = 0;
            SavedCurrentPos = Vector3.Zero;
            SavedCurrentAreaHash = 0;
            PortalLogicIterationCount = 0;
        }
    }

    public enum ActionsEnum
    {
        Nothing,
        UsingEntrance,
        UsingPortal,
        PickingQuestItem,
        PickingNormalItem,
    }
}
