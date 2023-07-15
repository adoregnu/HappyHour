using AvalonDock.Converters;
using CefSharp.DevTools.CSS;
using CefSharp.DevTools.Target;
using HappyHour.Extension;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Scriban.Runtime.Accessors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace HappyHour.Model
{
    public class AvDbContextPool : IDisposable
    {
        static readonly DbContextOptions<AvDbContext> _options;
        static readonly PooledDbContextFactory<AvDbContext> _pool;
        readonly AvDbContext _context;
        readonly string DbPath;

        public static AvDbContext CreateContext()
        { 
            return _pool.CreateDbContext(); 
        }
        static AvDbContextPool()
        {
            _options = new DbContextOptionsBuilder<AvDbContext>()
                .UseSqlite($@"Data Source={App.Current.LocalAppData}\db\AvDb.db")
                .Options;

            _pool = new PooledDbContextFactory<AvDbContext>(_options);
        }
        public AvDbContextPool()
        {
            _context = _pool.CreateDbContext();
            DbPath = $"{App.Current.LocalAppData}\\db\\";
        }

        public List<AvItem> GetAvMovies(string search, bool allOnEmpty = false)
        {
            if (string.IsNullOrEmpty(search) && !allOnEmpty)
            {
                return null;
            }
            else
            {
                return _context.Items
                    .Where(i => EF.Functions.Like(i.Pid, $"%{search}%"))
                    .ToList();
            }
        }

        public List<AvItem> GetAvMovies(AvActorName name)
        {
            _context.Entry(name)
                .Reference(n => n.Actor).Load();
            _context.Entry(name.Actor)
                .Collection(a => a.Items).Load();

            return name.Actor.Items.ToList();
        }

        public List<AvItem> GetAvMovies(AvSeries series)
        {
            return _context.Items
                .Where(i => i.Series.Name == series.Name)
                .ToList();
        }

        public List<AvItem> GetAvMovies(AvStudio studio)
        {
            return _context.Items
                .Where(i => i.Studio.Name == studio.Name)
                .ToList();
        }

        public async ValueTask<AvItem> GetMovie(string pid)
        {
            return await _context.Items
                .Include(av => av.Studio)
                .Include(av => av.Actors)
                    .ThenInclude(ac => ac.Names)
                .Include(av => av.Genres)
                .FirstOrDefaultAsync(av => av.Pid == pid);
        }

        public void UpdateMovie(AvItem movie, Action<AvItem> updater)
        {
            _context.Items.Attach(movie);
            updater(movie);
            _context.SaveChanges();
        }
        public void DeleteMovie(AvItem movie)
        { 
            _context.Items.Attach(movie);
            _context.Items.Remove(movie);
            _context.SaveChanges();
        }

        public List<AvActorName> GetActorNames(string search)
        {
            return _context.ActorNames
                .Where(i => EF.Functions.Like(i.Name, $"%{search}%"))
                .OrderBy(a => a.Name)
                .ToList();
        }

        public List<AvStudio> GetAvStudios(string search)
        {
            return _context.Studios
                .Where(s => EF.Functions.Like(s.Name, $"{search}%"))
                .OrderBy(s => s.Name)
                .ToList();
        }

        public List<AvSeries> GetAvSeries(string search)
        {
            return _context.Series
                .Where(s => EF.Functions.Like(s.Name, $"%{search}%"))
                .ToList();
        }

        string CheckAnCopyPicture(string path)
        {
            if (!path.StartsWith(DbPath, StringComparison.OrdinalIgnoreCase))
            {
                var dst = DbPath + Path.GetFileName(path);
                File.Move(path, dst);
                path = dst;
            }
            return path;
        }
        public AvActor AddActor(string name, string picturePath)
        {
            if (_context.ActorNames.Any(i => i.Name == name))
            {
                Log.Print($"{name} is already exists.");
                return null;
            }

            var names = new List<AvActorName> {
                new AvActorName { Name = name}
            };

            var actor = new AvActor
            {
                PicturePath = CheckAnCopyPicture(picturePath),
                Names = names
            };
            names.ForEach(n => n.Actor = actor);

            _context.Actors.Add(actor);
            _context.SaveChanges();
            return actor;
        }

        public void DeleteActor(AvActor actor)
        {
            _context.Actors.Attach(actor);
            var names = actor.Names.ToList();
            foreach (var name in names)
            {
                _context.ActorNames.Remove(name);
            }
            _context.Actors.Remove(actor);
            _context.SaveChanges();
        }
        public List<AvActorName> SearchActorName(string search)
        {
            return string.IsNullOrEmpty(search) ? null :
                _context.ActorNames
                    .Where(n => EF.Functions.Like(n.Name, $"%{search}%"))
                    .ToList();
        }
        public void UpdateActor(AvActor actor, Action<AvActor> updater)
        {
            _context.Actors.Attach(actor);
            updater(actor);
            _context.SaveChanges();
        }
        public bool AddActorName(AvActor actor, string name)
        {
            if (_context.ActorNames.Any(i => i.Name == name))
            {
                Log.Print($"{name} is already exists.");
                return false;
            }

            var actorName = _context.ActorNames.Add(new AvActorName { Name = name });
            _context.Actors.Attach(actor);
            actor.Names.Add(actorName.Entity);

            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
                return false;
            }
        }

        public void DeleteActorName(AvActorName name)
        {
            _context.ActorNames.Attach(name).Reference(n => n.Actor).Load();
            _context.Actors.Entry(name.Actor).Collection(a => a.Names).Load();
            if (name.Actor.Names.Count < 2)
            {
                Log.Print("only one name left in this actor!");
            }
            else
            {
                name.Actor.Names.Remove(name);
                _context.ActorNames.Remove(name);
            }
            _context.SaveChanges();
        }

        public void MergeActors(List<AvActor> actors, Action<AvActor> onDelete = null)
        {
            AvActor target = null;
            foreach (var actor in actors)
            {
                var actorEntry = _context.Actors.Attach(actor);
                if (target == null)
                {
                    target = actor;
                    continue;
                }

                actorEntry.Collection(a => a.Items).Load();
                var avItems = actor.Items.ToList();
                foreach (var item in avItems)
                {
                    _ = item.Actors.Remove(actor);
#if false
                    if (!item.Actors.Any(a => a.Id == tgtActor.Id))
                    {
                        item.Actors.Add(tgtActor);
                    }
#endif
                }
                foreach (var name in actor.Names)
                {
                    if (!target.Names.Any(n => n.Name.Equals(name.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        name.Actor = target;
                        target.Names.Add(name);
                    }
                }
                onDelete?.Invoke(actor);
                _context.Actors.Remove(actor);
            }
            _context.SaveChanges();
        }

        public AvActor GetActorByName(AvActorName name)
        {
            _context.Attach(name)
                .Reference(n => n.Actor).Load();
            if (name.Actor == null)
            {
                Log.Print($"No actor found for {name.Name}");
                return null;
            }
            return name.Actor;
        }

        public async ValueTask<List<AvItem>> GetMoviesByActor(AvActor actor)
        {
            await _context.Actors.Attach(actor)
                .Collection(a => a.Items)
                .LoadAsync();
            return actor.Items.ToList();
        }

        public async ValueTask<List<AvActor>> GetActorsByInitial(string initial)
        {
            if (initial == "All")
            {
                return await _context.Actors
                  .Include(a => a.Names)
                  .Where(a => a.Items.Count > 0)
                  .OrderByDescending(a => a.DateAdded)
                  .ToListAsync();
            }
            else
            {
                var names = await _context.ActorNames
                    .Include(name => name.Actor)
                        .ThenInclude(a => a.Names)
                    .Where(n => EF.Functions.Like(n.Name, $"{initial}%"))
                    .Where(n => n.Actor != null)
                    .Where(n => n.Actor.Items.Count >  0)
                    .OrderBy(n => n.Name)
                    .ToListAsync();

                var actors = names.Select(n => n.Actor).Distinct();
                if (!actors.Any()) return new List<AvActor>();
                return actors.ToList();
            }
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            _context.Dispose();
            //Log.Print("AvDbContextPool::Dispose");
        }
    }
}
