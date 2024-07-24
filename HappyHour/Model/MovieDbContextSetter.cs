using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;

namespace HappyHour.Model
{
    public partial class MovieDbContext : DbContext
    {
        public void MergeActors(List<Actor> actors, Action<Actor> onDelete = null)
        {
            var target = actors.First(a => a.Thumb != null);
            foreach (var actor in actors)
            {
                if (actor == target) continue;

                foreach (var name in actor.Names)
                {
                    target.Names.Add(name);
                }
                foreach (var movie in actor.Movies)
                {
                    if (!target.Movies.Any(m => m.PID == movie.PID))
                    {
                        target.Movies.Add(movie);
                    }
                }
                Actors.Remove(actor);
                onDelete?.Invoke(actor);
            }
            SaveChanges();
        }

        public void MergeGenres(List<Genre> genres, Action<Genre> onDelete = null)
        {
            var target = genres[0];
            genres.RemoveAt(0);
            foreach (var genre in genres)
            {
                foreach (var name in genre.Name)
                {
                    target.Name.Add(name);
                }
                foreach (var movie in genre.Movies)
                {
                    target.Movies.Add(movie);
                }
                MovieGenres.Remove(genre);
                onDelete?.Invoke(genre);
            }
            SaveChanges();
        }

        public void MergeMakers(List<Maker> makers, Action<Maker> onDelete = null)
        {
            var target = makers[0];
            makers.RemoveAt(0);
            foreach (var maker in makers)
            {
                foreach (var name in maker.Name)
                {
                    target.Name.Add(name);
                }
                foreach (var label in maker.Labels)
                {
                    target.Labels.Add(label);
                }
                if (maker.Logo != null)
                {
                    Images.Remove(maker.Logo);
                }
                Makers.Remove(maker);
                onDelete?.Invoke(maker);
            }
        }

        public void MergeSeries(List<Series> series, Action<Series> onDelete = null)
        {
            var target = series[0];
            series.RemoveAt(0);
            foreach (var serie in series)
            {
                foreach (var name in serie.Name)
                {
                    target.Name.Add(name);
                }
                foreach (var movie in serie.Movies)
                {
                    target.Movies.Add(movie);
                }
                Series.Remove(serie);
                onDelete.Invoke(serie);
            }
        }

        public void MargeLabels(List<Label> labels, Action<Label> OnDelete = null)
        {
            var target = labels[0];
            labels.RemoveAt(0);
            foreach (var label in labels)
            {
                if (target.Maker != label.Maker)
                {
                    Log.Print($"{target} and {label} are not name maker!");
                    continue;
                }
                foreach (var name in label.Name)
                {
                    target.Name.Add(name);
                }
                foreach (var movie in Movies)
                {
                    target.Movies.Add(movie);
                }
                if (label.Logo != null)
                {
                    Images.Remove(label.Logo);
                }
                Lables.Remove(label);
                OnDelete?.Invoke(label);
            }
        }

        static string GetLang(IDictionary<string, object> data)
        {
            return data.TryGetValue("lang", out object _lang) ? _lang.ToString() : "no";
        }

        /*
        public bool SetActor(string pid, IDictionary<string, object> data)
        {
            return true;
        }
        static void SetMText<T>(ICollection<T> list, string text, string lang)
            where T : MText, new()
        {
            var org = list.FirstOrDefault(t => t.Lang == lang);
            if (org != null)
            {
                org.Text = text;
            }
            else
            {
               list.Add(new T() { Lang = lang, Text = text });
            }
        }
        */
        static string GenHash(byte[] blob)
        {
            int i;
            var hash = MD5.HashData(blob);
            StringBuilder sOutput = new StringBuilder(hash.Length);
            for (i = 0; i < hash.Length; i++)
            {
                sOutput.Append(hash[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
        static void SetMText<T>(ICollection<T> list, IDictionary<string, object> data, string field)
            where T : MText, new()
        {
            if (!data.TryGetValue(field, out object value) || value == null)
            {
                return;
            }

            string lang = GetLang(data);
            string text = value.ToString();
            var org = list.FirstOrDefault(t => t.Lang == lang);
            if (org != null)
            {
                org.Text = text;
                Log.Print($"Overwiting {field} ");
            }
            else
            {
                list.Add(new T() { Lang = lang, Text = text });
            }
        }

        static void SetRating(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("rating", out object value) || value == null)
            {
                return;
            }
            string url = "default";
            if (data.TryGetValue("url", out object var_url))
            {
                url = var_url.ToString();
            }
            var rating = movie.Ratings.FirstOrDefault(r => r.SiteUrl == url);
            _ = float.TryParse(value.ToString(), out float result);
            if (rating == null)
            {
                movie.Ratings.Add(new Rating() { Rate = result, SiteUrl = url, Movie = movie });
            }
            else
            {
                Log.Print($"Overwriting user rating value for {movie.PID} : {result}");
                rating.Rate = result;
            }
        }

        //TODO: pop-up new genre window 
        bool SetGenre(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("genre", out object value) || value == null)
            {
                return false;
            }

            string lang = GetLang(data);
            var genres = value as IList<object>;
            foreach (string genre in genres.Cast<string>())
            {
                var dbGenre = MovieGenres
                    .Include(g => g.Name)
                    .FirstOrDefault(g => g.Name.Any(n => n.Text == genre));
                if (dbGenre != null)
                {
                    Entry(dbGenre).Collection(g => g.Movies).Load();
                    if (!dbGenre.Movies.Any(m => m.PID == movie.PID))
                    {
                        dbGenre.Movies.Add(movie);
                    }
                    else
                    {
                        Log.Print($"{genre} already has {movie.PID}");
                    }
                }
                else
                {
                    MovieGenres.Add(new Genre()
                    {
                        Name = [new ShortText() { Lang = lang, Text = genre }],
                        Movies = [movie]
                    });
                }
            }
            return true;
        }

        Actor SetActorName(string lang, IDictionary<string, object> data)
        {
            List<string> names = null;
            if (data.TryGetValue("name", out object name) && name != null)
            {
                names = [name.ToString()];
            }
            if (data.TryGetValue("alias", out object alias) && alias != null)
            {
                if (alias is List<object> obj_list)
                {
                    names.AddRange(obj_list.OfType<string>());
                }
            }
            if (names == null || names.Count == 0) return null;

            static Tuple<string, string> GetNameLang(string name, string lang)
            {
                var name_lang = name.Split(';');
                if (name_lang.Length > 1)
                {
                    lang = name_lang[1];
                }
                return new Tuple<string, string>(name_lang[0], lang);
            }

            ActorName dbName = null;
            foreach (var aname in names)
            {
                var sname = GetNameLang(aname, lang);
                dbName = ActorNames
                        .Include(an => an.Name)
                        .Include(an => an.Actor)
                        .FirstOrDefault(an => an.Name.Text == sname.Item1);
                if (dbName != null) break;
            }

            Actor dbActor = null;
            if (dbName == null)
            {
                dbActor = new Actor() { Names = [], Movies = [] };
                foreach (var aname in names)
                {
                    var sname = GetNameLang(aname, lang);
                    dbName = new ActorName()
                    {
                        Priority = (sname.Item2 == "ko") ? 0 : 1,
                        Name = new ShortText() { Lang = sname.Item2, Text = sname.Item1 },
                        Actor = dbActor
                    };
                    dbActor.Names.Add(dbName);
                }
                Actors.Add(dbActor);
            }
            else
            {
                dbActor = dbName.Actor;
                Entry(dbActor).Collection(a => a.Names).Load();
                Entry(dbActor).Collection(a => a.Movies).Load();
                foreach (var aname in names)
                {
                    var sname = GetNameLang(aname, lang);
                    if (ActorNames.Any(an => an.Name.Text == sname.Item1))
                    {
                        Log.Print($"{aname} alread exists!");
                        continue;
                    }

                    ActorNames.Add(new ActorName()
                    {
                        Name = new ShortText() { Lang = sname.Item2, Text = sname.Item1 },
                        Actor = dbActor
                    });
                }
            }

            if (data.TryGetValue("thumb", out object thumb) && thumb != null)
            {
                var path = $"{App.Current.LocalAppData}\\db\\{thumb}";
                if (SetImageBlob(path) is ImageBlob blob)
                {
                    dbActor.Thumb = blob;
                }
            }

            return dbActor;
        }

        ImageBlob SetImageBlob(string path)
        {
            var exts = new Dictionary<string, int>(){
                { "jpeg", 1 }, { "jpg", 1 }, { "png", 2 } , { "webp", 3 }
            };
            var ext = path.Split('.').Last();
            if (exts.TryGetValue(ext.ToLower(), out int type) && Path.Exists(path))
            {
                var blob = File.ReadAllBytes(path);
                var hash = GenHash(blob);
                if (!Images.Any(i => i.Hash == hash))
                {
                    return new ImageBlob() { Data = blob, Type = type, Hash = hash };
                }
            }
            return null;
        }

        bool SetActor(Movie movie, IDictionary<string, object> data)
        {
            /*
            actor : [ { name : name, alias = [ name, ...]}, ... ]
             */
            if (!data.TryGetValue("actor", out object actorList) || actorList == null)
            {
                return false;
            }
            string lang = GetLang(data);
            var actors = actorList as List<object>;
            foreach (var actor in actors.Cast<IDictionary<string, object>>())
            {
                var dbActor = SetActorName(lang, actor);
                if (dbActor == null)
                {
                    continue;
                }
                if (!dbActor.Movies.Any(m => m.PID == movie.PID))
                {
                    dbActor.Movies.Add(movie);
                }
                else
                {
                    Log.Print($"{actor} alreay has PID:{movie.PID}!");
                }
            }
            return true;
        }

        void SetMaker(Label lable, IDictionary<string, object> data)
        {
            var newMaker = new Maker() { Name = [], Labels = [lable] };
            string lang = GetLang(data);
            if (data.TryGetValue("maker", out object maker) && maker != null)
            {
                newMaker.Name.Add(new ShortText() { Lang = lang, Text = maker.ToString() });
            }
            else
            {
                foreach (var name in lable.Name)
                {
                    newMaker.Name.Add(name);
                }
            }
            Makers.Add(newMaker);
        }

        void SetLable(Movie movie, IDictionary<string, object> data)
        {
            data.TryGetValue("label", out object label);
            data.TryGetValue("maker", out object maker);
            if (maker == null && label == null) return;

            label ??= maker;
            maker ??= label;

            string lang = GetLang(data);
            var dbLable = Lables
                .Include(l => l.Name)
                .Include(l => l.Movies)
                .FirstOrDefault(lb => lb.Name.Any(n => n.Text.Equals(label.ToString())));
            if (dbLable == null)
            {
                dbLable = new Label()
                {
                    Name = [new ShortText() { Lang = lang, Text = label.ToString() }],
                    Movies = [movie]
                };
                SetMaker(dbLable, data);
            }
            else
            {
                dbLable.Movies.Add(movie);
            }
        }

        static void SetReleaseDate(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("date", out object date) || date == null)
            {
                return;
            }

            string[] patterns = ["yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd", "MMM d yyyy"];
            foreach (string pattern in patterns)
            {
                try
                {
                    movie.DateReleased = DateTime.ParseExact(
                        date.ToString(), pattern, App.Current.enUS);
                    break;
                }
                catch { }
            }
        }

        void SetSeries(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("series", out object series) || series == null)
            {
                return;
            }
            var tmp = series.ToString().Split(';');
            string lang = GetLang(data);
            if (tmp.Length > 1) lang = tmp[^1];

            string pid = data["pid"] as string;
            var db_ser = Series
                .Include(s => s.Movies)
                .Include(s => s.Name)
                .Where(s => s.Name.Any(n => n.Text == tmp[0]))
                .FirstOrDefault();

            if (db_ser == null)
            {
                db_ser = new Series()
                {
                    Movies = [movie],
                    Name = [new ShortText() { Lang = lang, Text = tmp[0] }]
                };
                Series.Add(db_ser);
            }
            else
            {
                if (!db_ser.Movies.Any(m => m.PID == pid))
                {
                    db_ser.Movies.Add(movie);
                }
            }
        }
        void SetCover(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("cover", out object cover) || cover == null)
            {
                return;
            }
            var path = cover.ToString();
            if (SetImageBlob(path) is ImageBlob blob)
            {
                movie.Cover = blob;
            }

            movie.VideoUrl = data["path"].ToString();
        }

        public void DeleteMovie(Movie movie)
        {
            Images.Remove(movie.Cover);
            Movies.Remove(movie);
            SaveChanges();
        }

        public bool SetMovie(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("pid", out object pid) || pid == null)
            {
                return false;
            }

            var movie = Movies
                .Include(m => m.Title)
                .Include(m => m.Plot)
                .Include(m => m.Ratings)
                .FirstOrDefault(m => m.PID == pid.ToString());
            movie ??= new Movie()
            {
                PID = pid.ToString(),
                DateAdded = DateTime.Now,
                VideoUrl = data["path"].ToString(),
                Title = [],
                Plot = [],
                Actors = [],
                Genres = [],
                Ratings = []
            };

            SetMText(movie.Title, data, "title");
            SetMText(movie.Plot, data, "plot");
            SetRating(movie, data);
            SetGenre(movie, data);
            SetActor(movie, data);
            SetLable(movie, data);
            SetReleaseDate(movie, data);

            SetSeries(movie, data);
            SetCover(movie, data);

            if (movie.Key == 0)
            {
                Movies.Add(movie);
            }

            SaveChanges();
            return true;
        }
    }
}
