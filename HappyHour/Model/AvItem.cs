using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HappyHour.Model
{
    public class AvGenre
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
        public virtual ICollection<AvItem> Items { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class AvActorName
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
        public AvActor Actor { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class AvActor : NotifyPropertyChanged
    {
        private string picturePath;
        [Key]
        public int Id { get; set; }

        public string PicturePath
        {
            get => string.IsNullOrEmpty(picturePath) ? "" : picturePath;
            set => Set(ref picturePath, value);
        }
        public DateTime DateAdded { get; set; }
        public virtual ICollection<AvActorName> Names { get; set; }
        public virtual ICollection<AvItem> Items { get; set; }
        public override string ToString()
        {
            int idx = 0;
            if (Names.Count == 0)
            {
                return "Actor-Name relation has broken!";
            }

            string ret = "";
            foreach (var name in Names)
            {
                if (idx == 0) { ret = name.Name; }
                else if (idx == 1) { ret += "(" + name.Name; }
                else if (idx > 1) { ret += ", " + name.Name; }
                idx++;
            }
            if (idx > 1) { ret += ")"; }
            return ret;
        }
    }

    public class AvStudio
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class AvSeries
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(512)]
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class AvItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Pid { get; set; }

        [MaxLength(1024)]
        public string Title { get; set; }
        [MaxLength(1024)]
        public string OrigTitle { get; set; }

        public bool IsCensored { get; set; }
        public string LeakedPid { get; set; }
        public DateTime DateReleased { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModifed { get; set; }
        public AvStudio Studio { get; set; }
        public AvSeries Series { get; set; }

        [Required]
        public string Path { get; set; }

        [MaxLength(2048)]
        public string Plot { get; set; }
        public float Rating { get; set; }
        public virtual ICollection<AvGenre> Genres { get; set; }
        public virtual ICollection<AvActor> Actors { get; set; }
        public string ActorsName()
        {
            if (Actors.Count == 0) { return "No Actor Info"; }

            int aidx = 0;
            string ret = "";
            foreach (var actor in Actors)
            {
                if (aidx > 0) { ret += "\n"; }
                ret += actor.ToString();
                aidx++;
            }
            return ret;
        }

        public override string ToString()
        {
            return Pid;
        }
    }
}
