using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;
using HappyHour.Interfaces;

namespace HappyHour.ScrapItems
{
    internal class ItemBase2
    {
        private readonly AvDbContext _context;
        protected AvItem _avItem;

        public ItemBase2(IAvMedia avm)
        {
            _avItem = App.DbContext.Items
                .Include("Studio")
                .Include("Actors")
                .Include("Genres")
                .Include("Series")
                .FirstOrDefault(av => av.Pid == avm.Pid);

            if (_avItem == null)
            {
                _avItem = new AvItem
                {
                    Pid = avm.Pid,
                    Path = avm.Path,
                    IsCensored = true,
                };
                _avItem.DateAdded = _avItem.DateModifed = DateTime.Now;
                _avItem = _context.Items.Add(_avItem).Entity;
            }
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
                    _avItem.Genres.Add(entity);
                }
                //_genres.Add(entity);
            }
        }

        private void UpdateStudio(string studio)
        {
            studio = NameMap.StudioName(studio);
            var entity = _context.Studios.FirstOrDefault(x => x.Name.ToLower() == studio.ToLower());
            if (entity == null)
            {
                entity = _context.Studios.Add(new AvStudio { Name = studio }).Entity;
                _avItem.Studio = entity;
            }
        }

        private void UpdateSeries(string series)
        {
            var entity = _context.Series.FirstOrDefault(
                 x => x.Name.ToLower() == series.ToLower());
            if (entity == null)
            {
                entity = _context.Series.Add(new AvSeries { Name = series }).Entity;
                _avItem.Series = entity;
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
                    List<AvActorName> ActorNames = new();
                    ForEachActor(actor, name =>
                    {
                        ActorNames.Add(new AvActorName { Name = name, Actor = dbActor });
                        return false;
                    });

                    dbActor.Names = ActorNames;
                    dbActor.DateAdded = DateTime.Now;

                    dbActor = _context.Actors.Add(dbActor).Entity;
                    _avItem.Actors.Add(dbActor);
                }
                if (actor.ContainsKey("thumb"))
                {
                    dbActor.PicturePath = actor["thumb"].ToString();
                }
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

            try
            {
                _context.SaveChanges();
            }
            catch (ValidationException e)
            {
                Log.Print(e.Message);
            }
        } 

#if false
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
            if (item == null || _actorChanged)
            {
                _avItem.Actors = _actors;
            }
            if (item == null || (_genres != null && item.Genres.Count == 0))
            {
                _avItem.Genres = _genres;
            }
            try
            {
                //if (item == null)
                //{
                //    _context.Items.Add(_avItem);
                //}
                _context.SaveChanges();
            }
            catch (ValidationException e)
            {
                Log.Print(e.Message);
            }
            finally
            {
                //_series = null;
                //_studio = null;
                //_actors = null;
                //_genres = null;
            }
        }
#endif
    }
}
