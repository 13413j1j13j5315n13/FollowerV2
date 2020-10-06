using System.Windows.Forms;

namespace FollowerV2
{
    public class FollowerSkill
    {
        public int Id { get; set; }
        public bool Enable { get; set; } = false;
        public Keys Hotkey { get; set; } = Keys.Q;
        public bool IsMovingSkill { get; set; } = false;
        public int MaxRangeToMonsters { get; set; } = 70;
        public int CooldownMs = 3000;
        public int Priority = 5;

        public FollowerSkill(int id)
        {
            Id = id;
        }

    }
}
