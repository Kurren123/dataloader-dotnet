using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLoader.GraphQL.StarWars.Infrastructure;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace DataLoader.GraphQL.StarWars.Schema
{
    public class StarWarsQuery : ObjectGraphType
    {
        public StarWarsQuery()
        {
            Name = "Query";

            Field<ListGraphType<HumanType>>()
                .Name("humans")
                .Returns<Task<object>>()
                .Resolve(async ctx => new GraphNodeCollection<Human>(await ctx.GetDataContext().Humans.ToListAsync()));

            Field<ListGraphType<DroidType>>()
                .Name("droids")
                .Returns<Task<object>>()
                .Resolve(async ctx => new GraphNodeCollection<Droid>(await ctx.GetDataContext().Droids.ToListAsync()));

            Field<ListGraphType<EpisodeType>>()
                .Name("episodes")
                .Returns<Task<object>>()
                .Resolve(async ctx => new GraphNodeCollection<Episode>(await ctx.GetDataContext().Episodes.ToListAsync()));
        }
    }
}
