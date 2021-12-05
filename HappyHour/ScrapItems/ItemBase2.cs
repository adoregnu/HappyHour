using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;

namespace HappyHour.ScrapItems
{
    internal class ItemBase2
    {
        private readonly AvDbContext _context;
        private readonly MediaItem _mediaItem;

        protected AvItem _avItem;
        private AvStudio _studio;
        private AvSeries _series;
        private List<AvActor> _actors = new();
        private List<AvGenre> _genres;

        public ItemBase2(MediaItem mediaItem)
        {
            _mediaItem = mediaItem;
            _context = App.DbContext;
            _avItem = new AvItem
            {
                Pid = _mediaItem.Pid,
                Path = _mediaItem.MediaPath,
                IsCensored = true,
            };
        }

        private void UpdateTitle(string title)
        {
            _avItem.Title = title;
        }

        private void UpdatePlot(string plot)
        {
            _avItem.Plot = plot;
        }

        private void UpdateRating(string rating)
        {
            try
            {
                _avItem.Rating = float.Parse(rating, App.enUS);
            }
            catch (Exception ex)
            {
                Log.Print($"ItemBase2::UpdateRating: {ex.Message}");
            }
        }

        protected virtual void UpdateDate(string date)
        {
            try
            {
                string[] patterns = new string[] {
                    "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd", "MMM d yyyy"
                };
                foreach (string pattern in patterns)
                {
                    try
                    {
                        _avItem.DateReleased = DateTime.ParseExact(
                            date, pattern, App.enUS);
                        break;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Print($"ItemBase2::UpdateDate : {ex.Message}");
            }
        }

        private void UpdateGenre(List<object> genres)
        {
            _genres = new List<AvGenre>();
            foreach (string genre in genres)
            {
                if (NameMap.SkipGenre(genre))
                {
                    continue;
                }
                AvGenre entity = _context.Genres.FirstOrDefault(
                    x => x.Name.ToLower() == genre.ToLower());
                if (entity == null)
                {
                    entity = _context.Genres.Add(new AvGenre { Name = genre }).Entity;
                }
                _genres.Add(entity);
            }
        }

        private void UpdateStudio(string studio)
        {
            studio = NameMap.StudioName(studio);
            _studio = _context.Studios.FirstOrDefault(
                x => x.Name.ToLower() == studio.ToLower());
            if (_studio == null)
            {
                _studio = _context.Studios.Add(new AvStudio { Name = studio }).Entity;
            }
        }

        private void UpdateSeries(string series)
        {
            _series = _context.Series.FirstOrDefault(
                 x => x.Name.ToLower() == series.ToLower());
            if (_series == null)
            {
                _series = _context.Series.Add(new AvSeries { Name = series }).Entity;
            }
        }

        /// <summary>
        /// dic { name : string, thumb: string, alias: List<object> }
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="action"></param>
        private static void ForEachActor(IDictionary<string, object> dict,
            Func<string, bool> action)
        {
            foreach (var item in dict)
            {
                if (item.Value == null)
                {
                    continue;
                }
                if (item.Key == "alias" && item.Value is List<object> alias)
                {
                    bool exitLoop = false;
                    foreach (string a in alias)
                    {
                        if (action(a.ToString()))
                        {
                            exitLoop = true;
                            break;
                        }
                    }
                    if (exitLoop)
                    {
                        break;
                    }
                }
                else if (item.Key == "name")
                {
                    if (action(item.Value.ToString()))
                    {
                        break;
                    }
                }
            }
        }

        private void UpdateActor(List<object> actors)
        {
            foreach (IDictionary<string, object> actor in actors)
            {
                AvActorName dbName = null;
                ForEachActor(actor, val =>
                {
                    dbName = _context.ActorNames
                        .Include(n => n.Actor).ThenInclude(a => a.Names)
                        .Where(n => n.Name.ToLower() == val.ToLower())
                        .Where(n => n.Actor != null)
                        .FirstOrDefault();
                    return dbName != null;
                });

                AvActor dbActor;
                if (dbName != null)
                {
                    dbActor = dbName.Actor;
                    ForEachActor(actor, name =>
                    {
                        if (!dbActor.Names.Any(n => n.Name.ToLower() == name.ToLower()))
                        {
                            dbActor.Names.Add(new AvActorName { Name = name, Actor = dbActor });
                        }
                        return false;
                    });
                }
                else
                {
                    dbActor = new();
                    List<AvActorName> list = new();
                    ForEachActor(actor, name =>
                    {
                        list.Add(new AvActorName { Name = name, Actor = dbActor });
                        return false;
                    });

                    dbActor.Names = list;
                    dbActor.DateAdded = DateTime.Now;

                    _context.Actors.Add(dbActor);
                }
                if (actor.ContainsKey("thumb"))
                {
                    dbActor.PicturePath = actor["thumb"].ToString();
                }
                _actors.Add(dbActor);
            }
        }

        public void UpdateItems(IDictionary<string, object> items)
        {
            Dictionary<string, Delegate> updater = new()
            {
                { "title", (Action<string>)UpdateTitle },
                { "genre", (Action<List<object>>)UpdateGenre },
                { "studio", (Action<string>)UpdateStudio },
                { "series", (Action<string>)UpdateSeries },
                { "actor", (Action<List<object>>)UpdateActor },
                { "date", (Action<string>)UpdateDate },
                { "plot", (Action<string>)UpdatePlot },
                { "rating", (Action<string>)UpdateRating },

                //{ "cover",  (Action<string>)UpdateCover },
            };
 
            foreach (var item in items)
            {
                //Log.Print($"{item.Key} : {item.Value?.ToString()}");
                if (updater.ContainsKey(item.Key) && item.Value != null)
                {
                    updater[item.Key].DynamicInvoke(item.Value);
                }
            }

            UpdateDb();
        }

        private void UpdateDb()
        {
            AvItem item = _context.Items
                .Include("Studio")
                .Include("Actors")
                .Include("Genres")
                .Include("Series")
                .FirstOrDefault(i => i.Pid == _avItem.Pid);

            if (item != null)
            {
                _avItem = item;
                _avItem.DateModifed = DateTime.Now;
            }
            else
            {
                _avItem.DateAdded = _avItem.DateModifed = DateTime.Now;
            }

            if (item == null || (_series != null && item.Series == null))
            {
                _avItem.Series = _series;
            }
            if (item == null || (_studio != null && item.Studio == null))
            {
                _avItem.Studio = _studio;
            }
            if (item == null || (_actors != null && item.Actors.Count == 0))
            {
                _avItem.Actors = _actors;
            }
            if (item == null || (_genres != null && item.Genres.Count == 0))
            {
                _avItem.Genres = _genres;
            }

            try
            {
                if (item == null)
                {
                    _context.Items.Add(_avItem);
                }
                _context.SaveChanges();
            }
            catch (ValidationException e)
            {
                Log.Print(e.Message);
            }
            finally
            {
                _series = null;
                _studio = null;
                _actors = null;
                _genres = null;
            }
        }
    }
}
