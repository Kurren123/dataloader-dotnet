using System.Linq;
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
            Interface<CharacterInterface>();

            Field<ListGraphType<CharacterInterface>>()
                .Name("friends")
                .Resolve(ctx => ctx.GetDataLoader(ids =>
                    {
                        var db = ctx.GetDataContext();
                        return db.Friendships
                            .Where(f => ids.Contains(f.DroidId))
                            .Select(f => new {Key = f.DroidId, f.Human})
                            .ToLookup(x => x.Key, x => x.Human);
                    }).LoadAsync(ctx.Source.DroidId));

            Field<ListGraphType<EpisodeType>>()
                .Name("appearsIn")
                .Resolve(ctx => ctx.GetDataLoader(ids =>
                    {
                        var db = ctx.GetDataContext();
                        return db.DroidAppearances
                            .Where(da => ids.Contains(da.DroidId))
                            .Select(da => new {Key = da.DroidId, da.Episode})
                            .ToLookup(x => x.Key, x => x.Episode);
                    }).LoadAsync(ctx.Source.DroidId));
        }
    }
}
