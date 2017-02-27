using System;
using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Types;

namespace DataLoader.GraphQL.StarWars.Infrastructure
{
    public class GraphNodeMiddleware
    {
        internal const string Disabled = "GraphNodeMiddleware.Disabled";

        public Task<object> Resolve(ResolveFieldContext context, FieldMiddlewareDelegate next)
        {
            if (!context.FieldDefinition.HasMetadata(Disabled))
            {
                var node = context.Source as IGraphNode;
                if (node != null)
                    context.Source = node.Value;
            }

            return next(context);
        }
    }
}