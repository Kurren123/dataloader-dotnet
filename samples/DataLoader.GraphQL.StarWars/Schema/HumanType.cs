using System.Linq;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace DataLoader.GraphQL.StarWars.Schema
{
    public class HumanType : ObjectGraphType<Human>
    {
        public HumanType()
        {
            Name = "Human";
            Field(h => h.Name);
            Field(h => h.HumanId);
            Field(h => h.HomePlanet);
            Interface<CharacterInterface>();

            Field<ListGraphType<CharacterInterface>>()
                .Name("friends")
                .Resolve(ctx => ctx.GetDataLoader(ids =>
                    {
                        var db = ctx.GetDataContext();
                        return db.Friendships
                            .Where(f => ids.Contains(f.HumanId))
                            .Select(f => new {Key = f.HumanId, f.Droid})
                            .ToLookup(x => x.Key, x => x.Droid);
                    }).LoadAsync(ctx.Source.HumanId));

            Field<ListGraphType<EpisodeType>>()
                .Name("appearsIn")
                .Resolve(ctx => ctx.GetDataLoader(ids =>
                    {
                        var db = ctx.GetDataContext();
                        return db.HumanAppearances
                            .Where(ha => ids.Contains(ha.HumanId))
                            .Select(ha => new { Key = ha.HumanId, ha.Episode })
                            .ToLookup(x => x.Key, x => x.Episode);
                    }).LoadAsync(ctx.Source.HumanId));
        }
    }
}
