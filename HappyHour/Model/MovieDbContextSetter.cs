using CefSharp;
using CefSharp.DevTools.Autofill;
using Microsoft.EntityFrameworkCore;
using MvvmDialogs.FrameworkDialogs.SaveFile;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Converters;

namespace HappyHour.Model
{
    public partial class MovieDbContext : DbContext
    {
        public async ValueTask<Actor> MergeActors(List<Actor> actors, Action<Actor> onDelete = null)
        {

            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].Thumb != null) continue;
                actors[i] = await LoadActor(actors[i], true);
            }
            Actor target = null;
            try
            {
                target = actors.First(a => a.Thumb != null);
            }
            catch (InvalidOperationException)
            {
                target = actors.First();
            }

            List<ActorName> names = [];
            foreach (var actor in actors)
            {
                if (actor == target) continue;

                foreach (var name in actor.Names)
                {
                    if (!target.Names.Any(n => n.Name.Text == name.Name.Text))
                    {
                        target.Names.Add(name);
                    }
                    else
                    {
                        names.Add(name);
                    }
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
            foreach (var n in names)
            {
                ActorNames.Remove(n);
            }
            await SaveChangesAsync();
            return target;
        }

        public async Task MergeGenres(List<Genre> genres, Action<Genre> onDelete = null)
        {
            var target = genres[0];
            genres.RemoveAt(0);
            await Entry(target).Collection(g => g.Movies).LoadAsync();
            foreach (var genre in genres)
            {
                await Entry(genre).Collection(g => g.Movies).LoadAsync();
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
            await SaveChangesAsync();
        }
        public async Task MergeSeries(List<Series> series, Action<Series> onDelete = null)
        {
            var target = series[0];
            series.RemoveAt(0);
            await Entry(target).Collection(s => s.Movies).LoadAsync();
            foreach (var serie in series)
            {
                await Entry(serie).Collection(s => s.Movies).LoadAsync();
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
            await SaveChangesAsync();
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

        public async Task MargeLabels(List<Label> labels, Action<Label> OnDelete = null)
        {
            var target = labels[0];
            labels.RemoveAt(0);
            await Entry(target).Collection(l => l.Movies).LoadAsync();
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
                await SaveChangesAsync();

                Labels.Remove(label);
                OnDelete?.Invoke(label);
            }
            await SaveChangesAsync();
        }

        static string GetLang(IDictionary<string, object> data)
        {
            return data.TryGetValue("lang", out object _lang) ? _lang.ToString() : "no";
        }

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
            //string text = value.ToString();
            var namelang = GetNameLang(value.ToString(), lang);
            var org = list.FirstOrDefault(t => t.Lang == namelang.Item2);
            if (org != null)
            {
                org.Text = namelang.Item1;
                Log.Print($"Overwiting {field} ");
            }
            else
            {
                Log.Print($"Append {field}");
                list.Add(new T() { Lang = lang, Text = namelang.Item1 });
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

        async ValueTask<bool> SetGenre(Movie movie, IDictionary<string, object> data)
        {
            string[] skip_genres = {
                "4K", "Digital Mosaic", "Hi - Def", "Featured Actress", "DMM Exclusive",
                "配信専用", "フルハイビジョン(FHD)"};

            if (!data.TryGetValue("genre", out object value) || value == null)
            {
                return false;
            }

            string lang = GetLang(data);
            var genres = value as IList<object>;
            foreach (string genre in genres.Cast<string>())
            {
                if (skip_genres.Contains(genre)) continue;

                var dbGenre = await MovieGenres
                    .Include(g => g.Name)
                    .FirstOrDefaultAsync(g => g.Name.Any(n => n.Text == genre));
                if (dbGenre != null)
                {
                    await Entry(dbGenre).Collection(g => g.Movies).LoadAsync();
                    if (!dbGenre.Movies.Any(m => m.PID == movie.PID))
                    {
                        dbGenre.Movies.Add(movie);
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
        static Tuple<string, string> GetNameLang(string name_lang, string lang)
        {
            var name = name_lang.Split(';');
            if (name.Length > 1 && name[1].Length == 2)
            {
                return new Tuple<string, string>(name[0].Trim(), name[1]);
            }
            return new Tuple<string, string>(name_lang.Trim(), lang);
        }

        async ValueTask<Actor> SetActorName(string lang, IDictionary<string, object> data)
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

            List<Actor> dbActors = [];
            foreach (var aname in names)
            {
                var sname = GetNameLang(aname, lang);
                var dbName = await ActorNames
                        .Include(an => an.Name)
                        .Include(an => an.Actor)
/*
                            .ThenInclude(a => a.Names)
                                .ThenInclude(n => n.Name)
                        .Include(an => an.Actor)
                            .ThenInclude(a => a.Movies)
                        .Include(an => an.Actor)
                            .ThenInclude(a => a.Thumb)
*/
                        .FirstOrDefaultAsync(an => an.Name.Text == sname.Item1);
                if (dbName != null)
                {
                    if (dbName.Actor != null)
                    {
                        dbActors.Add(dbName.Actor);
                    }
                    else
                    {
                        Log.Print($"{dbName.Name} has no actor, Remove!");
                        ActorNames.Remove(dbName);
                    }
                }
            }

            dbActors = dbActors.Distinct().ToList();
            Log.Print($"actor distinct count: {dbActors.Count}");

            Actor dbActor = null;
            if (dbActors == null || dbActors.Count == 0)
            {
                int prio = 0;
                dbActor = new Actor() { Names = [], Movies = [] };
                foreach (var aname in names)
                {
                    var sname = GetNameLang(aname, lang);
                    var newName = new ActorName()
                    {
                        Priority = (sname.Item2 == "ko") ? prio++ : 10,
                        Name = new ShortText() { Lang = sname.Item2, Text = sname.Item1 },
                        Actor = dbActor
                    };
                    dbActor.Names.Add(newName);
                }
                Actors.Add(dbActor);
            }
            else
            {
                if (dbActors.Count > 1)
                {
                     dbActor = await MergeActors(dbActors);
                }
                else
                {
                    dbActor = await LoadActor(dbActors[0]);
                }

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
                if (data.TryGetValue("link", out object url) && url != null)
                {
                    Log.Print($"{string.Join(",", names)} is(are) known actor(s)");
                    data.Remove("link");
                }
            }
            if (strBirth != null) dbActor.DateBirth = ParseDate(strBirth);
            if (strDebut != null) dbActor.DateDebut = ParseDate(strDebut);

            if (data.TryGetValue("thumb", out object thumb) && thumb != null)
            {
                (ImageBlob blob, bool fromdb) = await SetImageBlob(thumb.ToString());
                if (blob != null)
                {
                    dbActor.Thumb = blob;
                }
            }

            return dbActor;
        }

        async ValueTask<(ImageBlob, bool)> SetImageBlob(string path)
        {
            var exts = new Dictionary<string, int>(){
                { "jpeg", 1 }, { "jpg", 1 }, { "png", 2 } , { "webp", 3 }
            };
            var ext = path.Split('.').Last();
            if (exts.TryGetValue(ext.ToLower(), out int type) && Path.Exists(path))
            {
                var blob = await File.ReadAllBytesAsync(path);
                var hash = GenHash(blob);
                File.Delete(path);
                if (!Images.Any(i => i.Hash == hash))
                {
                    return (new ImageBlob() { Data = blob, Type = type, Hash = hash }, false);
                }
                else// if (updatefromdb)
                {
                    var movie = Movies.Where(m => m.Cover.Hash == hash).FirstOrDefault();
                    if (movie != null)
                    {
                        Log.Print($"hash {hash} already exists! pid: {movie.PID}, {movie.VideoUrl}");
                    }
                    else 
                    {
                        Log.Print($"hash {hash} already exists! movie null!");
                        return (await Images.Where(i => i.Hash == hash).FirstOrDefaultAsync(), false);
                    }
                }
            }
            return (null, false);
        }

        async ValueTask<bool> SetActor(Movie movie, IDictionary<string, object> data)
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
                var dbActor = await SetActorName(lang, actor);
                if (dbActor == null)
                {
                    continue;
                }
                if (!dbActor.Movies.Any(m => m.PID == movie.PID))
                {
                    dbActor.Movies.Add(movie);
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

        async Task SetLable(Movie movie, IDictionary<string, object> data)
        {
            data.TryGetValue("label", out object label);
            data.TryGetValue("maker", out object maker);
            if (maker == null && label == null) return;

            label ??= maker;
            maker ??= label;

            string lang = GetLang(data);
            var dbLable = await Labels
                .Include(l => l.Name)
                .Include(l => l.Movies)
                .FirstOrDefaultAsync(lb => lb.Name.Any(n => n.Text.Equals(label.ToString())));
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
            if (!data.TryGetValue("date", out object date) || date == null
                || string.IsNullOrEmpty(date.ToString()))
            {
                return;
            }
            movie.DateReleased = ParseDate(date.ToString().Trim());
        }

        async Task SetSeries(Movie movie, IDictionary<string, object> data)
        {
            if (!data.TryGetValue("series", out object series) || series == null
                || string.IsNullOrEmpty(series.ToString()))
            {
                return;
            }
            string lang = GetLang(data);

            var nameLang = GetNameLang(series.ToString(), lang);

            var db_ser = await Series
                .Include(s => s.Movies)
                .Include(s => s.Name)
                .Where(s => s.Name.Any(n => n.Text == nameLang.Item1))
                .FirstOrDefaultAsync();

            if (db_ser == null)
            {
                db_ser = new Series()
                {
                    Movies = [movie],
                    Name = [new ShortText() { Lang = nameLang.Item2, Text = nameLang.Item1 }]
                };
                Series.Add(db_ser);
            }
            else
            {
                if (!db_ser.Movies.Any(m => m.PID == movie.PID))
                {
                    db_ser.Movies.Add(movie);
                }
            }
        }
        async Task SetCover(Movie movie, IDictionary<string, object> data)
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

            (ImageBlob blob, bool fromdb) = await SetImageBlob(cover.ToString());
            if (blob != null)
            {
                if (fromdb)
                {
                    var movies = Movies
                        //.Include(m => m.Cover)
                        .Where(m => m.Cover == blob)
                        .ToList();
                    foreach (var m in movies)
                    {
                        Log.Print($"same cover in {m.PID}, {m.VideoUrl}");
                    }
                }
                else
                {
                    movie.Cover = blob;
                }
            }
        }


        public async Task SetMovie(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("pid", out object pid) || pid == null)
            {
                return;
            }

            var movie = await Movies
                .Include(m => m.Title)
                .Include(m => m.Plot)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.PID == pid.ToString());
            movie ??= new Movie()
            {
                PID = pid.ToString(),
                DateAdded = DateTime.Now,
                DateDeleted = default,
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
            await SetGenre(movie, data);
            await SetActor(movie, data);
            await SetLable(movie, data);
            SetReleaseDate(movie, data);

            await SetSeries(movie, data);
            await SetCover(movie, data);

            if (movie.Key == 0)
            {
                Movies.Add(movie);
            }

            await SaveChangesAsync();
        }

        public async Task RemoveMovie(Movie movie, bool realClear)
        {
            if (realClear)
            {
                movie = await GetMovie(movie.PID, true);
                if (movie.Cover != null)
                {
                    Images.Remove(movie.Cover);
                }
                Movies.Remove(movie);
            }
            else
            {
                movie.DateDeleted = DateTime.Now;
            }
            await SaveChangesAsync();
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

        public void RemoveActorName(ActorName name)
        {
            ShortTexts.Remove(name.Name);
            ActorNames.Remove(name);
            SaveChanges();
        }

        public void RemoveActor(Movie movie, Actor actor)
        {
            Entry(movie).Collection(m => m.Actors).Load();
            Entry(actor).Collection(a => a.Movies).Load();

            movie.Actors.Remove(actor);
            actor.Movies.Remove(movie);
            SaveChanges();
        }

        public async Task RemoveGenre(Genre genre)
        {
            await Entry(genre).Collection(g => g.Movies).LoadAsync();
            foreach (var movie in genre.Movies)
            {
                await Entry(movie).Collection(m => m.Genres).LoadAsync();
                movie.Genres.Remove(genre);
            }
            MovieGenres.Remove(genre);
            await SaveChangesAsync();
        }

        public void UpdateMaker(Movie movie, Maker maker)
        {
            movie.Maker = maker;
            if (movie.Label != null && !maker.Labels.Any(lb => lb == movie.Label))
            {
                maker.Labels.Add(movie.Label);
            }
            SaveChanges();
        }

        public async Task UpdateLabel(Movie movie, Label label)
        {
            await Entry(label).Collection(l => l.Movies).LoadAsync();
            movie.Label = label;
            label.Movies.Add(movie);
            await SaveChangesAsync();
        }

        public async Task UpdateActor(Movie movie, Actor actor)
        {
            if (actor.Movies == null)
            {
                await Entry(actor).Collection(a => a.Movies).LoadAsync();
            }
            if (movie.Actors == null)
            {
                await Entry(movie).Collection(m => m.Actors).LoadAsync();
            }
            movie.Actors.Add(actor);
            actor.Movies.Add(movie);
            await SaveChangesAsync();
        }

        public async Task UpdateSeries(Movie movie, Series series)
        {
            if (series.Movies == null || series.Movies.Count == 0)
            {
                await Entry(series).Collection(s => s.Movies).LoadAsync();
            }
            movie.Series = series;
            series.Movies.Add(movie);
            await SaveChangesAsync();
        }
    }

}
