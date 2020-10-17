using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerV2
{
    public class FollowerSkillHoverEntityType
    {
        public static string Monster = "Monster";
        public static string Player = "Player";
        public static string Leader = "Leader";
        public static string Corpse = "Corpse";

        public static List<string> GetAllAsList()
        {
            return new List<string>()
            {
                Monster,
                Player,
                Leader,
                Corpse
            };
        }
    }
}
