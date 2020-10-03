using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

    public enum ActionsEnum
    {
        Nothing,
        UsingEntrance,
        UsingPortal,
        PickingQuestItem,
        PickingNormalItem,
        Moving,
    }
}
