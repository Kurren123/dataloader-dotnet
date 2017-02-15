using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DataLoader.GraphQL.StarWars.Schema;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;

namespace DataLoader.GraphQL.StarWars.Controllers
{
    [Route("api/graphql")]
    public class GraphQLController : Controller
    {
        private static int _queryNumber;
        private readonly IDocumentExecuter _executer = new DocumentExecuter();
        private readonly StarWarsSchema _schema = new StarWarsSchema();

        [HttpPost]
        public async Task<ExecutionResult> Post([FromBody] GraphQLRequest request)
        {
            var queryNumber = Interlocked.Increment(ref _queryNumber);
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(2, ' ')} - Running query {queryNumber}...");
            var sw = Stopwatch.StartNew();

            var result = await DataLoaderContext.Run(loadCtx => _executer.ExecuteAsync(_ =>
            {
                var context = new GraphQLUserContext(loadCtx);
                _.Schema = _schema;
                _.Query = request.Query;
                _.UserContext = context;
                _.FieldMiddleware.Use(next => {
                    FieldType lastField = null;
                    return ctx =>
                    {
                        var thisField = ctx.FieldDefinition;
                        if (lastField == null) lastField = thisField;
                        if (lastField != thisField) context.LoadContext.Complete();
                        lastField = ctx.FieldDefinition;
                        return next(ctx);
                    };
                });
            }));

            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(2, ' ')} - Finished query {queryNumber} ({sw.ElapsedMilliseconds}ms)");
            sw.Stop();

            return result;
        }
    }

    public class GraphQLRequest
    {
        public string Query { get; set; }
        public object Variables { get; set; }
    }
}