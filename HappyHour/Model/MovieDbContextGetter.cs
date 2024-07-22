using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    public partial class MovieDbContext: DbContext
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
                else if(n.Priority == 0)
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
        public async ValueTask<List<Actor>> GetActors(string keyword = null)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return await Actors
                    .Include(a => a.Names)
                        .ThenInclude(n => n.Name)
                    .Include(a => a.Movies)
                    .Include(a => a.Thumb)
                    .Where(a => a.Movies.Count() >  0)
                    .OrderByDescending(a => a.Key)
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
                    .OrderBy(n => n.Name)
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
                .Include(m  => m.Title)
                .Where(m => m.PID == pid)
                .FirstOrDefaultAsync();
        }
        public async ValueTask<List<Movie>> GetMovies(string pid)
        {
            return await Movies
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                    .ThenInclude(name => name.Name)
                .Include(m => m.Cover)
                .Include(m  => m.Title)
                .Where(m => EF.Functions.Like(m.PID,  $"%{pid}%"))
                .ToListAsync();
        }

        public async ValueTask<List<Movie>> GetMovies(Actor actor)
        {
            return await Movies
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                    .ThenInclude(name => name.Name)
                .Include(m => m.Cover)
                .Include(m  => m.Title)
                .Where(m => m.Actors.Contains(actor))
                .ToListAsync();
        }

        public async ValueTask<List<Movie>> GetMovies(Genre genre)
        {
            return await Movies
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                    .ThenInclude(name => name.Name)
                .Include(m => m.Cover)
                .Include(m  => m.Title)
                .Where(m => m.Genres.Contains(genre))
                .ToListAsync();
        }

        public async ValueTask<List<Genre>> GetGenres(string keyword = null)
        {
            return await MovieGenres
                .Include(g => g.Name)
                .Include(g => g.Movies)
                .ToListAsync();
        }

        public async ValueTask<List<Series>> GetSeries(string keyword = null)
        {
            return await Series
                .Include(s => s.Movies)
                .Include(s => s.Name)
                .ToListAsync();
        }
    }
}
