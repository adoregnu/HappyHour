using System;

namespace HappyHour.Interfaces
{
    internal interface IAvMedia
    {
        string Pid { get; set; }
        string Path { get; set; }
        string Poster { get; set; }
        string BriefInfo { get; }
        DateTime Date { get; set; }

        void Reload(string[] files = null);
    }
}
