using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars
{
    public class GraphContext
    {
        public StarWarsContext DataContext { get; set; } = new StarWarsContext();
        public DataLoaderContext LoadContext { get; set; } = new DataLoaderContext();
    }

    public static class ResolveFieldContextExtensions
    {
        public static StarWarsContext GetDataContext<T>(this ResolveFieldContext<T> context)
        {
            return ((GraphContext)context.UserContext).DataContext;
        }

        public static IDataLoader<int, TReturn> GetDataLoader<TSource, TReturn>(this ResolveFieldContext<TSource> context, Func<IEnumerable<int>, Task<ILookup<int, TReturn>>> fetch)
        {
            return ((GraphContext)context.UserContext).LoadContext.GetLoader(context.FieldDefinition, fetch);
        }
    }
}