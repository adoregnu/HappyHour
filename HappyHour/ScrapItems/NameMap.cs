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
        private readonly static Dictionary<string, string> _studioMap = new()
        {
            //{ "Emmanuelle", "Emmanuelle"},
            { "h.m.p", "h.m.p" },
            //{ "Daydream", "Mousouzoku"},
            //{ "Daydreamers", "Mousouzoku"},
            //{ "Mousouzoku", "Mousouzoku"},
            //{ "Mousozoku", "Mousouzoku"},
            { "scute", "S-Cute" },
            { "sodcreate", "SOD Create" },
            { "Tameike Goro-", "Tameike Goro" },
            { "Kira ★ Kira", "kira*kira" },
            { "JUICY HONEY", "Orustak Pictures" },
            { "Honnaka", "Hon Naka" },
            { "K.M.Produce", "K M Produce" },
            { "Uchu Kikaku", "K M Produce" },
            //{ "Real Works", "K M Produce" },
            { "Crystal Eizou", "Crystal Eizo" },
            { "Deeps", "Deep's" },
            { "Kyandei", "Candy" },
            { "MAX-A", "Max A" },
            { "Saidobi-", "Nagae Style" },
            { "Das !", "Das" },
            //{ "一本道", "1pondo" },
            { "Bi", "Chijo Heaven" },
        };
        public static string StudioName(string name)
        {
            if (_studioMap.Any(i => name.Contains(i.Key, StringComparison.OrdinalIgnoreCase)))
            {
                return _studioMap.First(i => name.Contains(i.Key, StringComparison.OrdinalIgnoreCase)).Value;
            }
            else
            {
                return name;
            }
        }

        private static readonly Dictionary<string, string> _actorMap = new()
        {
            { "Oohashi Miku", "Ohashi Miku" },
            { "Mariya Nagai", "Maria Nagai" },
            { "Yui Ooba", "Yui Oba" },
            { "Yuu Shinoda", "Yu Shinoda" },
            { "Hibiki Ootsuki", "Hibiki Otsuki" },
            { "Haruki Satou", "Haruki Sato" },
            { "Erika Momodani", "Erika Momotani" },
            { "Sari Kousaka", "Sari Kosaka" },
            { "Miyuu Amano", "Miyu Amano" },
            { "Yuuri Arakawa", "Yuri Arakawa" },
            { "Nozomi Asou", "Nozomi Aso" },
            { "Maasa Matsushima", "Masa Matsushima" }
        };

        public static string ActorName(string name)
        {
            return _actorMap.ContainsKey(name) ? _actorMap[name] : name;
        }

        private readonly static List<string> _skipGenre = new()
        {
            "1080p",
            "60fps",
            "Hi-Def",
            "AV Idol",
            "SALE",
            "Sample",
            "POV",
            "****",
            "Blu-ray",
            "4K",
            "Featured",
            "Editor",
            "Famous"
        };
        public static bool SkipGenre(string name)
        {
            if (_skipGenre.Any(g => name.Contains(g, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }
    }
}
