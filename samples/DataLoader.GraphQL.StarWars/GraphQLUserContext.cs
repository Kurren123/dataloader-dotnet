using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars
{
    public class GraphQLUserContext
    {
        public StarWarsContext DataContext { get; set; }
        public DataLoaderContext LoadContext { get; set; }

        public GraphQLUserContext(DataLoaderContext loadContext) : this(loadContext, new StarWarsContext())
        {
        }

        public GraphQLUserContext(DataLoaderContext loadContext, StarWarsContext dataContext)
        {
            DataContext = dataContext;
            LoadContext = loadContext;
        }
    }

    public static class GraphQLUserContextExtensions
    {
        public static StarWarsContext GetDataContext<T>(this ResolveFieldContext<T> context)
        {
            return ((GraphQLUserContext)context.UserContext).DataContext;
        }

        // public static IDataLoader<int, TReturn> GetDependentLoader<TDataLoader, TSource, TReturn>(
        //     this ResolveFieldContext<TSource> context, Func<IEnumerable<int>, ILookup<int, TReturn>> fetch)
        // {
        //     return ((GraphQLUserContext)context.UserContext).LoadContext.GetDependentLoader(context.FieldDefinition, fetch);
        // }

        public static AsyncLocal<KeyValuePair<FieldType, IDataLoader>> KeysFromField = new AsyncLocal<KeyValuePair<FieldType, IDataLoader>>();

        public static IDataLoader<int, TReturn> GetDataLoader<TSource, TReturn>(this ResolveFieldContext<TSource> context, Func<IEnumerable<int>, Task<ILookup<int, TReturn>>> fetch)
        {
            var loader = ((GraphQLUserContext)context.UserContext).LoadContext.GetLoader(context.FieldDefinition, fetch);
            if (context.ParentType == KeysFromField.Value.Key)
            {
                var last = KeysFromField.Value.Value;
                last.GetResult().SelectMany(x => x);
                KeysFromField.Value = context.FieldDefinition;
            }
            return loader;
        }
    }
}