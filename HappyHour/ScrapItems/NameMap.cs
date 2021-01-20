using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.ScrapItems
{
    static class NameMap
    {
        static Dictionary<string, string> _studioMap
            = new Dictionary<string, string>
            {
                //{ "Emmanuelle", "Emmanuelle"},
                { "h.m.p", "h.m.p"},
                //{ "Daydream", "Mousouzoku"},
                //{ "Daydreamers", "Mousouzoku"},
                //{ "Mousouzoku", "Mousouzoku"},
                //{ "Mousozoku", "Mousouzoku"},
                { "scute", "S-Cute"},
                { "sodcreate", "SOD Create"},
                { "Tameike Goro-", "Tameike Goro" },
                { "Kira ★ Kira", "kira*kira"},
                { "JUICY HONEY", "Orustak Pictures" },
                { "Honnaka", "Hon Naka" },
                { "K.M.Produce", "K M Produce" },
                { "Uchu Kikaku", "K M Produce" },
                //{ "Real Works", "K M Produce" },
                { "Crystal Eizou", "Crystal Eizo"},
                { "Deeps", "Deep's"},
                { "Kyandei", "Candy"},
                { "MAX-A", "Max A"},
                { "Saidobi-", "Nagae Style" },
                { "Das !", "Das"},
                { "Bi", "Chijo Heaven"},
            };
        public static string StudioName(string name)
        {
            if (_studioMap.Any(i => name.Contains(i.Key)))
            {
                var found = _studioMap.First(i => name.Contains(i.Key));
                return found.Value;
            }
            else
                return name;
        }

        static Dictionary<string, string> _actorMap
            = new Dictionary<string, string>
            {
                { "Oohashi Miku", "Ohashi Miku" },
                { "Mariya Nagai", "Maria Nagai" },
                { "Yui Ooba", "Yui Oba" },
                { "Yuu Shinoda", "Yu Shinoda" },
                { "Hibiki Ootsuki", "Hibiki Otsuki"},
                { "Haruki Satou", "Haruki Sato"},
                { "Erika Momodani", "Erika Momotani" },
                { "Sari Kousaka", "Sari Kosaka"},
                { "Miyuu Amano", "Miyu Amano"},
                { "Yuuri Arakawa", "Yuri Arakawa"},
                { "Nozomi Asou", "Nozomi Aso"},
                { "Maasa Matsushima", "Masa Matsushima"}
            };

        public static string ActorName(string name)
        {
            if (_actorMap.ContainsKey(name))
                return _actorMap[name];
            else
                return name;
        }
    }
}
