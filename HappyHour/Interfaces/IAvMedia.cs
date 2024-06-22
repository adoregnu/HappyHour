using System;

namespace HappyHour.Interfaces
{
    internal interface IAvMedia : IComparable<IAvMedia>
    {
        string Pid { get; set; }
        string Path { get; set; }
        string Poster { get; set; }
        string BriefInfo { get; }
        DateTime Date { get; set; }
        bool IsPlayable { get; }

        string GenPosterPath(string ext, bool isScreenshot = false);
        string GenActorThumbPath(string name, string ext);
        void Reload(string[] files = null);
    }
}
