using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars.Infrastructure
{
    public static class FieldBuilderExtensions
    {
        public static DeferredFieldBuilder<TSource, TKey> Defer<TSource, TKey>(this FieldBuilder<TSource, object> builder, Func<TSource, TKey> keySelector)
        {

            return new DeferredFieldBuilder<TSource, TKey>(builder.FieldType, keySelector);
        }
    }

    public class DeferredFieldBuilder<TSource, TKey>
    {
        private readonly FieldType _field;
        private readonly Func<TSource, TKey> _keySelector;

        public DeferredFieldBuilder(FieldType field, Func<TSource, TKey> keySelector)
        {
            _field = field;
            _field.Resolver = null;
            _field.Metadata[GraphNodeMiddleware.Disabled] = true;
            // _field.ResolvedType.ApplyGraphNodePatch();
            _keySelector = keySelector;
        }
        
        public void Resolve<TReturn>(Func<ResolveFieldContext<IEnumerable<TKey>>, Task<ILookup<TKey, TReturn>>> fetch)
        {
            _field.Resolver = new GraphNodeResolver<TSource, TKey, TReturn>(_keySelector, fetch);
        }
    }
}