using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;
using HappyHour.Interfaces;

namespace HappyHour.Model
{
    internal static class DbHelper
    {
        //private readonly AvDbContext _context;
        //protected AvItem _avInfo;

        public static bool OverwriteActorPicture { get; set; }

        private static AvItem GetAvItem(AvDbContext context, IAvMedia avm)
        {
            var _avInfo = context.Items
                .Include("Studio")
                .Include("Actors")
                .Include("Genres")
                .Include("Series")
                .FirstOrDefault(av => av.Pid == avm.Pid);

            if (_avInfo == null)
            {
                _avInfo = new AvItem
                {
                    Pid = avm.Pid,
                    Path = avm.Path,
                    IsCensored = true,
                    Genres = new List<AvGenre>(),
                    Actors = new List<AvActor>(),
                    DateAdded = DateTime.Now,
                    DateModifed = DateTime.Now,
                };
                _avInfo = context.Items.Add(_avInfo).Entity;
            }
            return _avInfo;
        }

        private static void UpdateRating(AvDbContext context, AvItem _avInfo, string rating)
        {
            try
            {
                _avInfo.Rating = float.Parse(rating, App.Current.enUS);
            }
            catch (Exception ex)
            {
                Log.Print($"ItemBase2::UpdateRating: {ex.Message}");
            }
        }

        private static void UpdateDate(AvDbContext context, AvItem _avInfo, string date)
        {
            string[] patterns = new string[]
            {
                "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd", "MMM d yyyy"
            };
            foreach (string pattern in patterns)
            {
                try
                {
                    _avInfo.DateReleased = DateTime.ParseExact(
                        date, pattern, App.Current.enUS);
                    break;
                }
                catch { }
            }
        }

        private static void UpdateGenre(AvDbContext context, AvItem _avInfo, List<object> genres)
        {
            foreach (string genre in genres.Cast<string>())
            {
                if (NameMap.SkipGenre(genre))
                {
                    continue;
                }
                var entity = context.Genres.FirstOrDefault(
                    x => x.Name.ToLower() == genre.ToLower());
                entity ??= context.Genres.Add(new AvGenre { Name = genre }).Entity;
                if (!_avInfo.Genres.Any(g => g.Name.ToLower() == entity.Name.ToLower()))
                {
                    _avInfo.Genres.Add(entity);
                }
            }
        }

        private static void UpdateStudio(AvDbContext context, AvItem _avInfo, string studio)
        {
            studio = NameMap.StudioName(studio);
            var entity = context.Studios.FirstOrDefault(x => x.Name.ToLower() == studio.ToLower());
            entity ??= context.Studios.Add(new AvStudio { Name = studio }).Entity;
            _avInfo.Studio = entity;
        }

        private static void UpdateSeries(AvDbContext context, AvItem _avInfo, string series)
        {
            var entity = context.Series.FirstOrDefault(
                 x => x.Name.ToLower() == series.ToLower());
            entity ??= context.Series.Add(new AvSeries { Name = series }).Entity;
            _avInfo.Series = entity;
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
                    foreach (string a in alias.Cast<string>())
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
        private static void UpdateActor(AvDbContext context, AvItem _avInfo, List<object> actors)
        {
            foreach (var actor in actors.Cast<IDictionary<string, object>>())
            {
                AvActorName dbName = null;
                ForEachActor(actor, val =>
                {
                    dbName = context.ActorNames
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
                    dbActor = context.Actors.Add(dbActor).Entity;
                }
                if (_avInfo.Actors.Count == 0 ||
                    !_avInfo.Actors.Any(a => a.Id != 0 && a.Id == dbActor.Id))
                {
                    _avInfo.Actors.Add(dbActor);
                }
                if (actor.ContainsKey("thumb"))
                {
                    if (string.IsNullOrEmpty(dbActor.PicturePath) || OverwriteActorPicture)
                    {
                        //dbActor.PicturePath = actor["thumb"].ToString();
                        dbActor.PicturePath = $"{App.Current.LocalAppData}\\db\\{actor["thumb"]}";
                    }
                }
            }
        }

        public static void UpdateItems(IAvMedia avm, IDictionary<string, object> items)
        {
            using var context = AvDbContextPool.CreateContext();

            var avitem = GetAvItem(context, avm);
            Dictionary<string, Action<AvItem, object>> updater = new()
            {
                { "title", (_item, title) => _item.Title = title as string },
                { "genre", (_item, list) => UpdateGenre(context, _item, list as List<object>) }, //(Action<List<object>>)UpdateGenre },
                { "studio", (_item, studio) => UpdateStudio(context, _item, studio as string) }, //(Action<string>)UpdateStudio },
                { "series", (_item, series) => UpdateSeries(context, _item, series as string) }, //(Action<string>)UpdateSeries },
                { "actor", (_item, list) => UpdateActor(context, _item, list as List<object>) }, //(Action<List<object>>)UpdateActor },
                { "date", (_item, date) => UpdateDate(context, _item, date as string) }, //(Action<string>)UpdateDate },
                { "plot", (_item, plot) => _item.Plot = plot as string}, //(Action<string>)UpdatePlot },
                { "rating", (_item, rating) => UpdateRating(context, _item, rating as string) } //(Action<string>)UpdateRating },

                //{ "cover",  (Action<string>)UpdateCover },
            };

            foreach (var item in items)
            {
                //Log.Print($"{item.Key} : {item.Value?.ToString()}");
                if (updater.TryGetValue(item.Key, out var value) && item.Value != null)
                {
                    value(avitem, item.Value);
                }
            }

            _ = context.SaveChanges();
        }
    }
}
