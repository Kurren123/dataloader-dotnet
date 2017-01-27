using DataLoader;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars
{
    public class GraphQLUserContext
    {
        public StarWarsContext DataContext { get; set; } = new StarWarsContext();
        public DataLoaderContext LoadContext { get; set; } = new DataLoaderContext();
    }

    public static class GraphQLUserContextExtensions
    {
        public static GraphQLUserContext GetUserContext<T>(this ResolveFieldContext<T> context)
        {
            return (GraphQLUserContext)context.UserContext;
        }

        public static StarWarsContext GetDataContext<T>(this ResolveFieldContext<T> context)
        {
            return context.GetUserContext().DataContext;
        }

        public static IDataLoader<int, TReturn> GetDataLoader<TSource, TReturn>(this ResolveFieldContext<TSource> context, FetchDelegate<int, TReturn> fetchDelegate)
        {
            return DataLoaderContext.Current.GetLoader(context.FieldDefinition, fetchDelegate);
        }
    }
}