using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    public enum ActorOrderType
    {
        Key, NumMovies, NumNames
    };
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

        public static string GetLable(Movie movie, string prefered_lang = "ko")
        {
            if (movie.Label == null) return null;

            var label = movie.Label;
            return label.Name.OrderByDescending(n => n.Lang == prefered_lang).First().Text;
        }
        public static string GetActorName(Actor actor, string lang = "ko")
        {
            string name = null;
            int priority = -1;
            foreach (var n in actor.Names)
            {
                name = n.Name.Text;
                if (lang != null && n.Name.Lang == lang)
                {
                    name = n.Name.Text;
                    priority = 0;
                }
                else if (n.Priority == 0)
                {
                    name = n.Name.Text;
                    priority = 1;
                }
                if (priority == 0) break;
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

        public  Actor GetActor(ActorName aname)
        {
            return Actors
                .Include(a => a.Names)
                    .ThenInclude(n => n.Name)
                .Include(a => a.Movies)
                .Include(a => a.Thumb)
                .Where(a => a.Names.Contains(aname))
                .FirstOrDefault();
        }

        public async ValueTask<Actor> LoadActor(Actor actor, bool includeThumb = false)
        {
            var query = Actors
                .Include(a => a.Names)
                    .ThenInclude(n => n.Name)
                .Include(a => a.Movies)
                .Where(a => a == actor);

            if (includeThumb)
            {
                query = query.Include(a => a.Thumb);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task LoadActorMovie(Actor actor)
        {
            await Entry(actor).Collection(a => a.Movies).LoadAsync();
        }

        public async ValueTask<List<Actor>> GetActors(Movie movie)
        {
            return await Actors
                .Include(a => a.Names)
                    .ThenInclude(n => n.Name)
                //.Include(a => a.Movies)
                .Include(a => a.Thumb)
                .Where(a => a.Movies.Contains(movie))
                .ToListAsync();
        }

        public async ValueTask<List<Actor>> GetActors()
        {
            return await Actors
               .Include(a => a.Names)
                   .ThenInclude(n => n.Name)
                .Include(a => a.Movies)
                .Where(a => a.Thumb == null)
                .ToListAsync();
        }

        public async ValueTask<List<Actor>> GetActors(string keyword = null,
            ActorOrderType order = ActorOrderType.Key, int limit = 0)
        {
            var query = Actors
               .Include(a => a.Names)
                   .ThenInclude(n => n.Name)
               //.Include(a => a.Movies)
               .Include(a => a.Thumb)
               .Where(a => a.Movies.Count() > 0);

            IQueryable<Actor> where = null;
            if (!string.IsNullOrEmpty(keyword))
            {
                where = query.Where(a => a.Names.Any(n => EF.Functions.Like(n.Name.Text, keyword)));
            }

            IOrderedQueryable<Actor> OrderBy(IQueryable<Actor> query)
            {
                return order switch
                {
                    ActorOrderType.NumMovies => query.OrderByDescending(a => a.Movies.Count()),
                    ActorOrderType.NumNames => query.OrderByDescending(a => a.Names.Count()),
                    _ => query.OrderByDescending(a => a.Key),
                };
            }

            IOrderedQueryable<Actor> orderBy = OrderBy(where ?? query);
            if (limit > 0)
            {
                return await orderBy.Take(limit).ToListAsync();
            }
            else
            {
                return await orderBy.ToListAsync();
            }
        }

        async ValueTask<List<string>> GetMovieUrls(Expression<Func<Movie, bool>> exp)
        {
            UiServices.WaitCursor(true);
            var urls = await Movies
                .Where(exp)
                .OrderBy(m => m.VideoUrl)
                .Select(m => m.VideoUrl)
                .ToListAsync();
            UiServices.WaitCursor(false);
            return urls;
        }
        public async ValueTask<List<string>> GetMovieUrls(string ppath)
        {
            return await GetMovieUrls(m => EF.Functions.Like(m.VideoUrl, $"{ppath}%", "|"));
        }
        public async ValueTask<List<string>> GetMovieUrls(Actor actor)
        {
            return await GetMovieUrls(m => m.Actors.Contains(actor));
        }

        public async ValueTask<Movie> GetMovie(Expression<Func<Movie, bool>> exp,  bool bAll = true, bool excludeCover = false)
        {
            var query = Movies
                .Include(m => m.Maker)
                    .ThenInclude(m => m.Name)
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                        .ThenInclude(name => name.Name);

            IQueryable<Movie> iquery;
            if (bAll)
            {
                iquery = query.Include(m => m.Series)
                    .ThenInclude(s => s.Name)
               .Include(m => m.Title)
               .Include(m => m.Plot)
               .Include(m => m.Genres)
               .Include(m => m.Ratings);

               if (!excludeCover)
               {
                   iquery = iquery.Include(m => m.Cover);
               }
               iquery = iquery.Where(exp);
            }
            else
            {
                if (!excludeCover)
                {
                    iquery = query.Include(m => m.Cover).Where(exp);
                }
                else
                {
                    iquery = query.Where(exp);
                }
            }
            return await iquery.FirstOrDefaultAsync();
        }
        public async ValueTask<Movie> GetMovie(string pid, bool bAll = true, bool excludeCover = false)
        {
            return await GetMovie(m => EF.Functions.Like(m.PID, $"%{pid}%"), bAll, excludeCover);
        }

        public async ValueTask<List<Movie>> GetMoviesFast(Expression<Func<Movie, bool>> exp)
        {
            return await Movies.Where(exp).ToListAsync();
        }

        public async ValueTask<List<Movie>> GetMoviesFast(string text, string searchType = "PID")
        {
            Expression<Func<Movie, bool>> exp = searchType switch
            {
                "PID" => m => !string.IsNullOrEmpty(text) && EF.Functions.Like(m.PID, $"%{text}%"),
                "Title" => m => !string.IsNullOrEmpty(text) && m.Title.Any(t => EF.Functions.Like(t.Text, $"%{text}%")),
                "Plot" => m => !string.IsNullOrEmpty(text) && m.Plot.Any(p => EF.Functions.Like(p.Text, $"%{text}%")),
                // text가 비어있으면 항목이 비어 있는 영화를 찾음, 비어있지 않으면 PID로 검색한 영화중에서 항목이 비었는 영화 찾음
                "Empty Rating" => m => string.IsNullOrEmpty(text) ? !m.Ratings.Any() : (EF.Functions.Like(m.PID, $"%{text}%") && !m.Ratings.Any()),
                "Empty Genre" => m => string.IsNullOrEmpty(text) ? !m.Genres.Any() : (EF.Functions.Like(m.PID, $"%{text}%") && !m.Genres.Any()),
                "Empty Actor" => m => string.IsNullOrEmpty(text) ? !m.Actors.Any() : (EF.Functions.Like(m.PID, $"%{text}%") && !m.Actors.Any()),
                "Empty Label" => m => string.IsNullOrEmpty(text) ? m.Label == null : (EF.Functions.Like(m.PID, $"%{text}%") && m.Label == null),
                "Empty Maker" => m => string.IsNullOrEmpty(text) ? m.Maker == null : (EF.Functions.Like(m.PID, $"%{text}%") && m.Maker == null),
                "Empty Plot" => m => string.IsNullOrEmpty(text) ? !m.Plot.Any() : (EF.Functions.Like(m.PID, $"%{text}%") && !m.Plot.Any()),
                _ => null,
            };
            return await GetMoviesFast(exp);
        }

        public async ValueTask<List<Movie>> GetMovies(Expression<Func<Movie, bool>> exp, int limit = 0)
        {
            var query = Movies
                .Include(m => m.Label)
                    .ThenInclude(l => l.Name)
                .Include(m => m.Actors)
                    .ThenInclude(actor => actor.Names)
                        .ThenInclude(name => name.Name)
                .Include(m => m.Cover);
                //.Include(m => m.Series)
                //.Include(m => m.Plot)
                //.Include(m => m.Ratings)
                //.Include(m => m.Title.OrderByDescending(t => t.Lang));

            IQueryable<Movie> mquery = null;
            if (exp != null)
            {
                mquery = query.Where(exp);
            }

            if (limit > 0)
            {
                if (mquery != null)
                {
                    mquery = mquery.OrderByDescending(m => m.Key).Take(limit);
                }
                else
                {
                    mquery = query.OrderByDescending(m => m.Key).Take(limit);
                }
            }
            return await mquery.ToListAsync();
        }

        public async ValueTask<List<Movie>> GetMovies(string pid)
        {
            return await GetMovies(m => EF.Functions.Like(m.PID, $"%{pid}%"));
        }
        public async ValueTask<List<Movie>> GetMovies(Actor actor)
        {
            return await GetMovies(m => m.Actors.Contains(actor));
        }
        public async ValueTask<List<Movie>> GetMovies(Genre genre)
        {
            return await GetMovies(m => m.Genres.Contains(genre));
        }
        public async ValueTask<List<Movie>> GetMovies(Maker maker)
        {
            return await GetMovies(m => m.Maker == maker);
        }
        public async ValueTask<List<Movie>> GetMovies(Label label)
        {
            return await GetMovies(m => m.Label == label);
        }
        public async ValueTask<List<Movie>> GetMovies(Series series)
        {
            return await GetMovies(m => m.Series == series);
        }
        public async ValueTask<List<Movie>> GetMoviesEmptyFieldOf(string name)
        {
            return await GetMovies(m => !m.Title.Any(t => t.Lang == "ko"));
        }

        public async ValueTask<List<Genre>> GetGenres(Movie movie)
        {
            return await MovieGenres
                .Include(g => g.Name.OrderByDescending(n => n.Lang))
                //.Include(g => g.Movies)
                .Where(g => g.Movies.Contains(movie))
                .ToListAsync();
        }

        public async ValueTask<List<Genre>> GetGenres(string keyword = null)
        {
            var query = MovieGenres
                .Include(g => g.Name.OrderByDescending(n => n.Lang));

            IQueryable<Genre> where = null;
            if (keyword != null)
            {
                where = query.Where(g => g.Name.Any(n => EF.Functions.Like(n.Text, $"%{keyword}%")));
            }
            if (where != null)
            {
                return await where.ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public async ValueTask<List<Series>> GetSeries(string keyword = null)
        {
            var query = Series
                //.Include(s => s.Movies)
                .Include(s => s.Name);

            IQueryable<Series> where = null;
            if (keyword != null)
            {
                where = query.Where(s => s.Name.Any(n => EF.Functions.Like(n.Text, $"%{keyword}%")));
            }
            if (where != null)
            {
                return await where.ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }
        public Maker GetMaker(string keyword)
        {
            return Makers
                .Include(m => m.Name)
                .Include(m => m.Logo)
                .Include(m => m.Labels)
                    .ThenInclude(lb => lb.Name)
                .FirstOrDefault(m => m.Name.Any(n => n.Text == keyword));
        }

        public async ValueTask<List<Maker>> GetMakers(string keyword = null)
        {
            var query = Makers
                .Include(m => m.Name.OrderByDescending(n => n.Lang))
                .Include(m => m.Logo)
                .Include(m => m.Labels);
                    //.ThenInclude(lb => lb.Movies);

            IQueryable<Maker> where = null;
            if (!string.IsNullOrEmpty(keyword))
            {
                where = query.Where(m => m.Name.Any(n => EF.Functions.Like(n.Text, $"%{keyword}%")));
            }
            if (where != null)
            {
                return await where.ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }
        public async ValueTask<List<Label>> GetLabels(string keyword = null)
        {
            var query = Labels
                .Include(l => l.Name.OrderByDescending(n => n.Lang))
                .Include(l => l.Makers);

            IQueryable<Label> where = null;
            if (!string.IsNullOrEmpty(keyword))
            {
                where = query.Where(l => l.Name.Any(n => EF.Functions.Like(n.Text, $"%{keyword}%")));
            }
            if (where != null)
            {
                return await where.ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public List<Label> GetLabels(Maker maker)
        {
            var tmp = Makers
                .Include(m => m.Labels)
                    .ThenInclude(lb => lb.Name.OrderByDescending(n => n.Lang))
                .Include(m => m.Labels)
                    //.ThenInclude(lb => lb.Movies)
                .FirstOrDefault(m => m == maker);

            return [.. tmp.Labels];
        }

        public async ValueTask<List<Label>> GetLabels(Movie movie)
        {
            return await Labels
                .Include(l => l.Name)
                .Include(l => l.Logo)
                //.Include(l => l.Movies)
                .Where(l => l.Movies.Contains(movie))
                .ToListAsync();
        }

        public List<string> GetFoldersStartsWithPid(string pidPrefix, string excludePath)
        {
            var folders = Movies
                .Where(m => EF.Functions.Like(m.PID, $"{pidPrefix}%"))
                .OrderBy(m => m.VideoUrl)
                .Select(m => m.VideoUrl.Substring(0, m.VideoUrl.LastIndexOf('\\')))
                .AsEnumerable() // EF에서 LINQ to Objects로 전환
                .Distinct(StringComparer.OrdinalIgnoreCase) // 대소문자 무시하고 중복 제거
                .Where(folder => string.IsNullOrEmpty(excludePath) || 
                                !folder.Contains(excludePath, StringComparison.OrdinalIgnoreCase)) // excludePath 제외
                .OrderBy(folder => folder) // 정렬
                .ToList();
            return folders;
        }

        public List<string> GetFoldersByMaker(Maker maker, string excludePath)
        {
            return Movies
                .Where(m => m.Maker == maker)
                .OrderBy(m => m.VideoUrl)
                .Select(m => m.VideoUrl.Substring(0, m.VideoUrl.LastIndexOf('\\')))
                .AsEnumerable() // EF에서 LINQ to Objects로 전환
                .Distinct(StringComparer.OrdinalIgnoreCase) // 대소문자 무시하고 중복 제거
                .Where(folder => string.IsNullOrEmpty(excludePath) || 
                                !folder.Contains(excludePath, StringComparison.OrdinalIgnoreCase)) // excludePath 제외
                .OrderBy(folder => folder) // 정렬
                .ToList();
        }
    }
}
