using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    public abstract class MText
    {
        [Key]
        public int Key { get; set; }
        public string Lang { get; set; }
        public abstract string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }
    public class ShortText : MText
    {
        [MaxLength(512)]
        public override string Text { set; get; }
    }


    public class LongText : MText
    {
        [MaxLength(4906)]
        public override string Text { set; get; }
    }


    public class ImageBlob
    {
        [Key]
        public int Key {  set; get; }
        public int Type { set; get; }
        [Column(TypeName = "MediumBlob")]
        public byte[] Data { get; set; }
        [MaxLength(32)]
        public string Hash { get; set; }
    }

    public class Genre
    {
        [Key]
        public int Key { get; set; }
        public virtual ICollection<ShortText> Name { get; set; }
        public virtual ICollection<Movie> Movies { get; set; }

        public override string ToString()
        {
            string str = "";
            foreach (var name in Name)
            {
                str += $"{name.Text}({name.Lang}), ";
            }
            return str;
        }
    }

    public class ActorName
    {
        [Key]
        public int Key { get; set; }
        public int Priority { get; set; }
        public int Alias { get; set; }
        public ShortText Name { get; set; }
        public Actor Actor { get; set; }
        public override string ToString()
        {
            return Name.Text;
        }
    }

    public class Actor
    {
        [Key]
        public int Key { get; set; }
        public ImageBlob Thumb { get; set; }
        public virtual ICollection<ImageBlob> Pictures { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateBirth { get; set; }
        public virtual ICollection<ActorName> Names { get; set; }
        public virtual ICollection<Movie> Movies { get; set; }

        public override string ToString()
        {
            if (Names == null || Names.Count == 0)
            {
                return "NotLoaded";
            }
            ActorName aname = null;
            foreach (var name in Names)
            {
                aname = name;
                if (name.Priority == 0)
                {
                    aname = name;
                    break;
                }
            }
            return aname.ToString();
        }
    }

    public class Label
    {
        [Key]
        public int Key { get; set; }
        public virtual ImageBlob Logo { get; set; }
        public virtual ICollection<ShortText> Name { get; set; }
        public Maker Maker { get; set; }
        public ICollection<Movie> Movies { get; set; }

    }

    public class Maker
    {
        [Key]
        public int Key { set; get; }
        public virtual ImageBlob Logo { get; set; }
        public virtual ICollection<ShortText> Name { get; set; }
        public virtual ICollection<Label> Labels { get; set; }
    }

    public class Series
    {
        [Key]
        public int Key { set; get; }
        public virtual ICollection<ShortText> Name { set; get; }
        public virtual ICollection<Movie> Movies { set; get; }
        public override string ToString()
        {
            return Name.First().ToString();
        }
    }

    public class Rating
    {
        [Key]
        public int Key { get; set; }
        public float Rate { set; get; }
        public string SiteUrl { set; get; }
        public Movie Movie { set; get; }
    }

    public class Movie
    {
        [Key]
        public int Key { set; get; }
        [Required]
        public string PID { set; get; }
        public string AltPID { set; get; }
        public bool Censored { set; get; }
        public DateTime DateReleased { set; get; }
        public DateTime DateAdded { set; get; }
        public DateTime DateDeleted { set; get; }
        public virtual ICollection<ShortText> Title { set; get; }
        public virtual ICollection<LongText> Plot { set; get; }
        public virtual ICollection<Rating> Ratings { set; get; }
        public virtual ICollection<Genre> Genres { set; get; }
        public virtual ICollection<Actor> Actors { set; get; }
        public ImageBlob Cover { set; get; }
        public virtual ICollection<ImageBlob> Screenshots { set; get; }
        public Label Label { set; get; }
        public Series Series { set; get; }

        public string VideoUrl { set; get; }

        public override string ToString()
        {
            return PID;
        }
    }
}
