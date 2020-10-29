using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FollowerV2
{
    public class FollowerSkill
    {
        // Because ImGui uses "ref"
        public int CooldownMs = 3000;

        [JsonIgnore] public DateTime LastTimeUsed = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public int MaxRange = 70;
        public int Priority = 5;

        public FollowerSkill(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
        public bool Enable { get; set; }
        public Keys Hotkey { get; set; } = Keys.Q;
        public bool IsMovingSkill { get; set; }
        public string HoverEntityType { get; set; } = FollowerSkillHoverEntityType.Monster;

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