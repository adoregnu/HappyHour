using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using System.Linq.Expressions;
using MvvmDialogs.FrameworkDialogs.SaveFile;

namespace HappyHour.Model
{
    public partial class MovieDbContext : DbContext
    {
        public void MergeActors(List<Actor> actors, Action<Actor> onDelete = null)
        {
            Actor target = null;
            try
            {
                target = actors.First(a => a.Thumb != null);
            }
            catch (InvalidOperationException ex)
            {
                target = actors.First();
            }

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
        public void MergeSeries(List<Series> series, Action<Series> onDelete = null)
        {
            var target = series[0];
            series.RemoveAt(0);
            foreach (var serie in series)
            {
                foreach (var name in serie.Name)
                {
                    if (!target.Name.Any(n => n.Text == name.Text))
                    {
                        target.Name.Add(name);
                    }
                }
                foreach (var movie in serie.Movies)
                {
                    target.Movies.Add(movie);
                }
                Series.Remove(serie);
                onDelete.Invoke(serie);
            }
            SaveChanges();
        }

        public async void MergeMakers(List<Maker> makers, Action<Maker> onDelete = null)
        {
            var target = makers[0];
            makers.RemoveAt(0);
            foreach (var maker in makers)
            {
                foreach (var name in maker.Name)
                {
                    if (!target.Name.Any(n => n.Text == name.Text))
                    {
                        target.Name.Add(name);
                    }
                }
                foreach (var label in maker.Labels)
                {
                    if (!target.Labels.Any(lb => lb == label))
                    {
                        target.Labels.Add(label);
                    }
                }
                if (maker.Logo != null)
                {
                    Images.Remove(maker.Logo);
                }
                var movies = await GetMovies(maker);
                foreach (var movie in movies)
                {
                    movie.Maker = target;
                }

                Makers.Remove(maker);
                onDelete?.Invoke(maker);
            }
            SaveChanges();
        }

        public async void MargeLabels(List<Label> labels, Action<Label> OnDelete = null)
        {
            var target = labels[0];
            labels.RemoveAt(0);
            foreach (var label in labels)
            {
                foreach (var name in label.Name)
                {
                    target.Name.Add(name);
                }
                foreach (var maker in label.Makers)
                {
                    if (!target.Makers.Any(m => m == maker))
                    {
                        target.Makers.Add(maker);
                    }
                }
                if (label.Logo != null)
                {
                    Images.Remove(label.Logo);
                }
                var movies = await GetMovies(label);
                foreach (var movie in movies)
                {
                    movie.Label = target;
                    target.Movies.Add(movie);
                }

                Labels.Remove(label);
                OnDelete?.Invoke(label);
            }
            SaveChanges();
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

            string strBirth = null, strDebut = null;
            if (data.TryGetValue("birth", out object birth) && birth != null)
            {
                strBirth = birth.ToString().Trim();
            }

            if (data.TryGetValue("debut", out object debut) && debut != null)
            {
                strDebut = debut.ToString().Trim();
            }

            static Tuple<string, string> GetNameLang(string name_lang, string lang)
            {
                var name = name_lang.Split(';');
                if (name.Length > 1)
                {
                    lang = name[1].Trim();
                }
                return new Tuple<string, string>(name[0], lang);
            }

            ActorName dbName = null;
            foreach (var aname in names)
            {
                var sname = GetNameLang(aname, lang);
                dbName = ActorNames
                        .Include(an => an.Name)
                        .Include(an => an.Actor)
                            .ThenInclude(a => a.Names)
                                .ThenInclude(n => n.Name)
                        .Include(an => an.Actor)
                            .ThenInclude(a => a.Movies)
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
                foreach (var aname in names)
                {
                    var sname = GetNameLang(aname, lang);
                    if (dbActor.Names.Any(an => an.Name.Text == sname.Item1))
                    {
                        Log.Print($"{aname} alread exists!");
                        continue;
                    }

                    dbActor.Names.Add(new ActorName()
                    {
                        Name = new ShortText() { Lang = sname.Item2, Text = sname.Item1 },
                        Actor = dbActor
                    });
                }

            }
            if (strBirth != null) dbActor.DateBirth = ParseDate(strBirth);
            if (strDebut != null) dbActor.DateDebut = ParseDate(strDebut);

            if (data.TryGetValue("thumb", out object thumb) && thumb != null)
            {
                if (SetImageBlob(thumb.ToString()) is ImageBlob blob)
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
                File.Delete(path);
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
            actor : [ { name : name, alias = [ name, ...], birth = '', debut = ''}, ... ]
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

        Maker SetMaker(string maker, Label label, IDictionary<string, object> data)
        {
            string lang = GetLang(data);
            var dbMaker = GetMaker(maker);
            if (dbMaker == null)
            {
                dbMaker = new Maker()
                {
                    Name = [new ShortText() { Lang = lang, Text = maker }],
                    Labels = [label]
                };
                Makers.Add(dbMaker);
            }

            if (!dbMaker.Labels.Any(lb => lb == label))
            {
                dbMaker.Labels.Add(label);
            }
            return dbMaker;
        }

        void SetLable(Movie movie, IDictionary<string, object> data)
        {
            data.TryGetValue("label", out object label);
            data.TryGetValue("maker", out object maker);
            if (maker == null && label == null) return;

            label ??= maker;
            maker ??= label;

            string lang = GetLang(data);
            var dbLable = Labels
                .Include(l => l.Name)
                .Include(l => l.Movies)
                .FirstOrDefault(lb => lb.Name.Any(n => n.Text.Equals(label.ToString())));
            if (dbLable == null)
            {
                dbLable = new Label()
                {
                    Name = [new ShortText() { Lang = lang, Text = label.ToString() }],
                    Movies = []
                };
                Labels.Add(dbLable);
            }
            movie.Maker = SetMaker(maker.ToString(), dbLable, data);
            dbLable.Movies.Add(movie);
        }

        static DateTime ParseDate(string strDate)
        {
            DateTime dt = DateTime.MinValue;
            if (string.IsNullOrEmpty(strDate)) return dt;

            string[] patterns = ["yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd", "MMM d yyyy"];
            foreach (string pattern in patterns)
            {
                try
                {
                    return DateTime.ParseExact(strDate, pattern, App.Current.enUS);
                }
                catch { }
            }
            Log.Print($"SetReleaseDate:: Failed to parse \"{strDate}\"");
            return dt;
        }

        static void SetReleaseDate(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("date", out object date) || date == null)
            {
                return;
            }
            movie.DateReleased = ParseDate(date.ToString().Trim());
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
            if (new FileInfo(cover.ToString()).Length < 10 * 1024)
            {
                File.Delete(cover.ToString());
                return;
            }
            if (SetImageBlob(cover.ToString()) is ImageBlob blob)
            {
                movie.Cover = blob;
            }
        }


        public async Task SetMovie(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("pid", out object pid) || pid == null)
            {
                return;
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

            await SaveChangesAsync();
        }

        public void RemoveMovie(Movie movie)
        {
            void CountAndRun(Expression<Func<Movie, bool>> exp, Action run)
            {
                var count = Movies.Where(exp).Count();
                if (count == 1) run();
            }

            //CountAndRun((Movie m) => m.Maker == movie.Maker, () => Makers.Remove(movie.Maker));
            //CountAndRun((Movie m) => m.Label == movie.Label, () => Labels.Remove(movie.Label));
            //CountAndRun((Movie m) => m.Series == movie.Series, () => Series.Remove(movie.Series));

            Ratings.Where(r => r.Movie == movie).ExecuteDelete();
            if (movie.Cover != null)
            {
                Images.Remove(movie.Cover);
            }
            Movies.Remove(movie);
            SaveChanges();
        }

        public void RemoveMaker(Maker maker)
        {
            foreach (var label in maker.Labels)
            {
                if (Makers.Where(m => m.Labels.Contains(label)).Count() == 1)
                {
                    Labels.Remove(label);
                }
            }
            Makers.Remove(maker);
            SaveChanges();
        }

        public void RemoveLabel(Label label)
        {
            Labels.Remove(label);
            SaveChanges();
        }
        public void RemoveSeries(Series series)
        {
            Series.Remove(series);
            SaveChanges();
        }

        public void RemoveActor(Actor actor)
        {
            foreach (var name in actor.Names)
            {
                ActorNames.Remove(name);
            }
            if (actor.Thumb != null)
            {
                Images.Remove(actor.Thumb);
            }
            Actors.Remove(actor);
            SaveChanges();
        }
        public void UpdateMaker(Movie movie, Maker maker)
        {
            movie.Maker = maker;
            if (!maker.Labels.Any(lb => lb == movie.Label))
            {
                maker.Labels.Add(movie.Label);
            }
            SaveChanges();
        }

        public void UpdateLabel(Movie movie, Label label)
        {
            movie.Label = label;
            label.Movies.Add(movie);
            SaveChanges();
        }

        public void UpdateActor(Movie movie, Actor actor)
        {
            movie.Actors.Add(actor);
            actor.Movies.Add(movie);
            SaveChanges();
        }
    }

}
