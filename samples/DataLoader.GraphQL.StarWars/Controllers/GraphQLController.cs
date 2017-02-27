using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DataLoader.GraphQL.StarWars.Schema;
using DataLoader.GraphQL.StarWars.Infrastructure;
using GraphQL;
using GraphQL.Instrumentation;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(2, ' ')} - Running query {queryNumber}");
            var sw = Stopwatch.StartNew();

            var result = await _executer.ExecuteAsync(_ =>
            {
                _.Schema = _schema;
                _.Query = request.Query;
                _.UserContext = new GraphContext();
                _.FieldMiddleware
                    .Use<GraphNodeMiddleware>()
                    .ApplyTo(_schema);
                    
                GraphNodeCollectionBootstrapper.Bootstrap(_schema);
            });

            sw.Stop();
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(2, ' ')} - Finished query {queryNumber} ({sw.ElapsedMilliseconds}ms)");

            return result;
        }
    }

    public class GraphQLRequest
    {
        public string Query { get; set; }
        public object Variables { get; set; }
    }
}