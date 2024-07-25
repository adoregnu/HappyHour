using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    public partial class MovieDbContext : DbContext
    {
        public List<ActorName> GetActorNames(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return null;

            return [.. ActorNames
                .Include(n => n.Name)
                .Where(n => EF.Functions.Like(n.Name.Text, $"%{keyword}%"))];
        }

        public static string GetLable(Movie movie, string lang = null)
        {
            if (movie.Label == null) return null;

            var label = movie.Label;
            string lbl_name = null;
            foreach (var name in label.Name)
            {
                if (lang != null && name.Lang == lang)
                {
                    lbl_name = name.Text;
                    break;
                }
                else if (lang == null)
                {
                    lbl_name = name.Text;
                    break;
                }
            }
            return lbl_name;
        }
        public static string GetActorName(Actor actor, string lang = null)
        {
            string name = null;
            foreach (var n in actor.Names)
            {
                name = n.Name.Text;
                if (lang != null && n.Name.Lang == lang)
                {
                    name = n.Name.Text;
                    break;
                }
                else if (n.Priority == 0)
                {
                    name = n.Name.Text;
                    break;
                }
            }
            return name;
        }

        public static List<string> GetActorsNames(Movie movie, string lang = null)
        {
            if (movie.Actors == null || movie.Actors.Count == 0) return null;
            List<string> names = [];

            foreach (var actor in movie.Actors)
            {
                names.Add(GetActorName(actor, lang));
            }
            return names.Count > 0 ? names : null;
        }

        public Actor GetActor(ActorName aname)
        {
            return Actors
                .Include(a => a.Names)
                    .ThenInclude(n => n.Name)
                .Include(a => a.Movies)
                .Include(a => a.Thumb)
                .Where(a => a.Names.Contains(aname))
                .FirstOrDefault();
        }

        public List<Actor> GetActors(Movie movie)
        {
            return [.. Actors
                .Include(a => a.Names)
                    .ThenInclude(n => n.Name)
                .Include(a => a.Movies)
                .Include(a => a.Thumb)
                .Where(a => a.Movies.Contains(movie))
            ];
        }

        public async ValueTask<List<Actor>> GetActors(string keyword = null, int limit = 30)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return await Actors
                    .Include(a => a.Names)
                        .ThenInclude(n => n.Name)
                    .Include(a => a.Movies)
                    .Include(a => a.Thumb)
                    .Where(a => a.Movies.Count() > 0)
                    .OrderByDescending(a => a.Key)
                    .Take(limit)
                    .ToListAsync();
            }
            else
            {
                var names = await ActorNames
                    .Include(n => n.Actor)
                        .ThenInclude(a => a.Names)
                            .ThenInclude(n => n.Name)
                    .Include(n => n.Actor)
                        .ThenInclude(a => a.Movies)
                    .Include(n => n.Actor)
                        .ThenInclude(a => a.Thumb)
                    .Where(n => EF.Functions.Like(n.Name.Text, $"{keyword}%"))
                    .Where(n => n.Actor.Movies.Count() > 0)
                    .OrderBy(n => n.Name.Text)
                    .ToListAsync();

                var actors = names.Select(n => n.Actor).Distinct();
                if (!actors.Any()) return [];
                return actors.ToList();
            }
        }


        public void RemoveMovie(Movie movie)
        {
            Movies.Remove(movie);
        }

        public void RemoveActor(Actor actor)
        {
            Actors.Remove(actor);
        }

        private static readonly char[] separator = ['\\'];
        public async ValueTask<List<string>> GetMovieUrls(string ppath)
        {
            //string escaped = string.Join("\\", ppath.Split(separator));
            return await Movies
                .Where(m => EF.Functions.Like(m.VideoUrl, $"{ppath}%"))
                .OrderBy(m => m.VideoUrl)
                .Select(m => m.VideoUrl)
                .ToListAsync();
        }

        public async ValueTask<Movie> GetMovie(string pid)
        {
            return await Movies
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                        .ThenInclude(name => name.Name)
                .Include(m => m.Cover)
                .Include(m => m.Title)
                .Where(m => m.PID == pid)
                .FirstOrDefaultAsync();
        }

        public async ValueTask<List<Movie>> GetMovies(Expression<Func<Movie, bool>> exp, int limit = 0)
        {
            var query = Movies
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                        .ThenInclude(name => name.Name)
                .Include(m => m.Cover)
                .Include(m => m.Title);

            if (exp != null)
            {
                query.Where(exp);
            }
            if (limit > 0)
            {
                query.OrderByDescending(m => m.Key)
                    .Take(limit);
            }
            return await query.ToListAsync();
        }

        public async ValueTask<List<Movie>> GetMovies(string pid)
        {
            return await GetMovies((Movie m) => EF.Functions.Like(m.PID, $"%{pid}%"));
        }
        public async ValueTask<List<Movie>> GetMovies(Actor actor)
        {
            return await GetMovies((Movie m) => m.Actors.Contains(actor));
        }
        public async ValueTask<List<Movie>> GetMovies(Genre genre)
        {
            return await GetMovies((Movie m) => m.Genres.Contains(genre));
        }
        public async ValueTask<List<Movie>> GetMovies(Maker maker)
        {
            return await GetMovies((Movie m) => m.Maker == maker);
        }
        public async ValueTask<List<Movie>> GetMovies(Label label)
        {
            return await GetMovies((Movie m) => m.Label == label);
        }
        public async ValueTask<List<Movie>> GetMovies(Series series)
        {
            return await GetMovies((Movie m) => m.Series == series);
        }

        public List<Genre> GetGenres(Movie movie)
        {
            return [.. MovieGenres
                .Include(g => g.Name)
                .Include(g => g.Movies)
                .Where(g => g.Movies.Contains(movie))
            ];
        }

        public async ValueTask<List<Genre>> GetGenres(string keyword = null)
        {
            var query = MovieGenres
                .Include(g => g.Name)
                .Include(g => g.Movies);

            if (keyword != null)
            {
                query.Where(g => g.Name.Any(n => EF.Functions.Like(n.Text, $"%{keyword}%")));
            }
                
            return await query.ToListAsync();
        }

        public async ValueTask<List<Series>> GetSeries(string keyword = null)
        {
            var query = Series
                .Include(s => s.Movies)
                .Include(s => s.Name);
            if (keyword != null)
            {
                query.Where(s => s.Name.Any(n => EF.Functions.Like(n.Text, $"%{keyword}%")));
            }

            return await query.ToListAsync();
        }
        public Maker GetMaker(string keyword)
        {
            return Makers
                .Include(m => m.Name)
                .Include(m => m.Logo)
                .Include(m => m.Labels)
                    .ThenInclude(lb => lb.Name)
                .FirstOrDefault(m =>  m.Name.Any(n => n.Text == keyword));
        }
        public async ValueTask<List<Maker>> GetMakers(string keyword = null)
        {
            return await Makers
                .Include(m => m.Name)
                .Include(m => m.Logo)
                .Include(m => m.Labels)
                .ToListAsync();
        }
        public async ValueTask<List<Label>> GetLabels(string keyword = null)
        {
            return await Labels
                .Include(l => l.Name)
                .Include(l => l.Logo)
                .Include(l => l.Movies)
                .ToListAsync();
        }
        public List<Label> GetLabels(Maker maker)
        {
            var tmp = Makers
                .Include(m => m.Labels)
                    .ThenInclude(lb =>  lb.Name)
                .Include(m => m.Labels)
                    .ThenInclude(lb =>  lb.Movies)
                .FirstOrDefault(m => m == maker);

            return [.. tmp.Labels];
        }
    }
}
