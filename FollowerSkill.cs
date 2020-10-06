﻿using System;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FollowerV2
{
    public class FollowerSkill
    {
        public int Id { get; set; }
        public bool Enable { get; set; } = false;
        public Keys Hotkey { get; set; } = Keys.Q;
        public bool IsMovingSkill { get; set; } = false;
        public int MaxRangeToMonsters { get; set; } = 70;

        // Because ImGui uses "ref"
        public int CooldownMs = 3000;
        public int Priority = 5;

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
            MaxRangeToMonsters = s.MaxRangeToMonsters;
            CooldownMs = s.CooldownMs;
            Priority = s.Priority;
        }
    }
}
