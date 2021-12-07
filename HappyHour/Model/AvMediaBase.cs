using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HappyHour.Interfaces;

namespace HappyHour.Model
{
    internal abstract class AvMediaBase : NotifyPropertyChanged, IAvMedia
    {
        private string _poster;
        private string _briefInfo;

        public string Pid { get; set; }
        public string Path { get; set; }
        public virtual DateTime Date { get; set; }
        public string Poster
        {
            get => _poster;
            set => Set(ref _poster, value);
        }
        public string BriefInfo
        {
            get => _briefInfo;
            set => Set(ref _briefInfo, value);
        }

        public int CompareTo(IAvMedia media)
        {
            var result = Date.CompareTo(media.Date);
            if (result == 0)
            {
                return Pid.CompareTo(media.Pid);
            }
            return result;
        }

        public abstract void Reload(string[] files);
    }
}
