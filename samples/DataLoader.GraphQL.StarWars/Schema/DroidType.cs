/*
 *  Example 1: One loader instance for each HumanType instance.
 *
 *    If the schema is created once on application startup and reused
 *    for every request, then the same loader will be used by
 *    multiple requests/threads. This is probably unsafe.
 */

using System.Linq;
using DataLoader.GraphQL.StarWars.Infrastructure;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace DataLoader.GraphQL.StarWars.Schema
{
    public class DroidType : ObjectGraphType<Droid>
    {
        public DroidType()
        {
            Name = "Droid";
            Field(d => d.Name);
            Field(d => d.DroidId);
            Field(d => d.PrimaryFunction);

            Field<ListGraphType<CharacterInterface>>()
                .Name("friends")
                .Defer(d => d.DroidId)
                .Resolve(async ctx =>
                {
                    var ids = ctx.Source;
                    var db = ctx.GetDataContext();
                    return (await db.Friendships
                            .Where(f => ids.Contains(f.DroidId))
                            .Select(f => new { Key = f.DroidId, f.Human })
                            .ToListAsync())
                        .ToLookup(x => x.Key, x => x.Human);
                });

            Field<ListGraphType<EpisodeType>>()
                .Name("appearsIn")
                .Defer(d => d.DroidId)
                .Resolve(async ctx =>
                {
                    var ids = ctx.Source;
                    var db = ctx.GetDataContext();
                    return (await db.DroidAppearances
                            .Where(da => ids.Contains(da.DroidId))
                            .Select(da => new { Key = da.DroidId, da.Episode })
                            .ToListAsync())
                        .ToLookup(x => x.Key, x => x.Episode);
                });
                
            Interface<CharacterInterface>();
        }
    }
}
