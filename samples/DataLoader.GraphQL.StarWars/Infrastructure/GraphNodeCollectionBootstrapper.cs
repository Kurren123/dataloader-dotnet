using System;
using System.Linq;
using System.Reflection;
using GraphQL;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars.Infrastructure
{
    public static class GraphNodeCollectionBootstrapper
    {
        private const string MarkerMetadataId = "GraphNode.IsPatched";

        /// <summary>
        /// Prepares a GraphQL schema to support GraphNode/NodeCollection.
        /// </summary>
        /// <remarks>
        /// This method goes through each object type in the schema and ensures <see cref="GraphNode{T}" />
        /// is considered a valid equivalent for an <code>ObjectGraphType{T}</code>. Specifically,
        /// it replaces the <code>IsTypeOf</code> property with a wrapped version that checks if the
        /// given object is a compatible <see cref="GraphNode{T}"/>.
        /// </remarks>
        /// <seealso cref="GraphNodeCollection{T}"/>
        public static void Bootstrap(global::GraphQL.Types.Schema schema)
        {
            schema.AllTypes.Apply(ApplyGraphNodePatch);
        }

        public static void ApplyGraphNodePatch(this IGraphType type)
        {
            // For an `ObjectGraphType`...
            var objectType = type as IObjectGraphType;
            if (objectType == null) return;

            // if we haven't already...
            if (objectType.HasMetadata(MarkerMetadataId)) return;

            // with the `IsTypeOf` property set...
            var isTypeOf = objectType.IsTypeOf;
            if (isTypeOf == null) return;

            // and an underlying type `T`...
            var innerType = FindObjectType(objectType.GetType());
            if (innerType == null) return;

            // replace the `IsTypeOf` property to support `GraphNode<T>`.
            var nodeType = typeof(GraphNode<>).MakeGenericType(innerType);
            objectType.IsTypeOf = value => isTypeOf((value as IGraphNode)?.Value ?? value);
            objectType.Metadata[MarkerMetadataId] = true;
        }

        private static Type FindObjectType(Type type)
        {
            while (type != null)
            {
                if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(ObjectGraphType<>))
                    return type.GenericTypeArguments[0];
                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }
    }
}