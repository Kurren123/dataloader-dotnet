using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars
{
    public class GraphQLUserContext : IDisposable
    {
        private bool _isDisposed;
        public StarWarsContext DataContext { get; }
        public DataLoaderContext LoadContext { get; }

        public GraphQLUserContext(DataLoaderContext loadContext) : this(loadContext, new StarWarsContext())
        {
        }

        public GraphQLUserContext(DataLoaderContext loadContext, StarWarsContext dataContext)
        {
            DataContext = dataContext;
            LoadContext = loadContext;
        }

        public IDataLoader<int, TReturn> GetDataLoader<TSource, TReturn>(object key, Func<StarWarsContext, IEnumerable<int>, Task<ILookup<int, TReturn>>> fetcher)
        {
            return LoadContext.GetLoader<int, TReturn>(key, ids => fetcher(DataContext, ids));
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                DataContext.Dispose();
            }
        }
    }

    public static class GraphQLResolveContextExtensions
    {
        public static IDataLoader<int, TReturn> GetDataLoader<TSource, TReturn>(this ResolveFieldContext<TSource> context, Func<StarWarsContext, IEnumerable<int>, Task<ILookup<int, TReturn>>> fetcher)
        {
            return ((GraphQLUserContext)context.UserContext).GetDataLoader<int, TReturn>(context.FieldDefinition, fetcher);
        }

        public static StarWarsContext GetDataContext<TSource>(this ResolveFieldContext<TSource> context)
        {
            return ((GraphQLUserContext)context.UserContext).DataContext;
        }
    }
}