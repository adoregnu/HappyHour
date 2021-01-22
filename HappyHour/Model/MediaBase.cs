using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    class MediaBase : NotifyPropertyChanged
    {
        public string Pid { get; set; }
        public string Path { get; set; }

        public DateTime DateCreated { get; set; }

        public virtual string Info
        {
            get => $"{Pid}\n" + DateCreated.ToString("u");
        }

        public static MediaBase Create(string path)
        {
            return new MediaBase
            {
                Pid = path.Split('\\').Last(),
                Path = path,
            };
        }
    }
}
