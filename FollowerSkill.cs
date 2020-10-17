using System;
using System.Windows.Forms;
using ExileCore.Shared.Enums;
using Newtonsoft.Json;

namespace FollowerV2
{
    public class FollowerSkill
    {
        public int Id { get; set; }
        public bool Enable { get; set; } = false;
        public Keys Hotkey { get; set; } = Keys.Q;
        public bool IsMovingSkill { get; set; } = false;
        public string HoverEntityType { get; set; } = FollowerSkillHoverEntityType.Monster;

        // Because ImGui uses "ref"
        public int CooldownMs = 3000;
        public int Priority = 5;
        public int MaxRange = 70;

        [JsonIgnore] public DateTime LastTimeUsed = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public FollowerSkill(int id)
        {
            Id = id;
        }

        public void OverwriteValues(FollowerSkill s)
        {
            Enable = s.Enable;
            Hotkey = s.Hotkey;
            IsMovingSkill = s.IsMovingSkill;
            MaxRange = s.MaxRange;
            CooldownMs = s.CooldownMs;
            Priority = s.Priority;
            HoverEntityType = s.HoverEntityType;
        }
    }
}
