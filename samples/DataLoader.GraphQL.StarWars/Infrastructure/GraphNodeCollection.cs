using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataLoader.GraphQL.StarWars.Infrastructure
{
    public class GraphNodeCollection<T> : IEnumerable<GraphNode<T>>
    {
        private readonly IList<T> _items;

        public GraphNodeCollection(IList<T> list)
        {
            _items = list;
        }

        public GraphNodeCollection(IEnumerable<T> enumerable)
        {
            _items = enumerable.ToList();
        }

        public IEnumerator<GraphNode<T>> GetEnumerator()
        {
            foreach (var value in _items)
                yield return new GraphNode<T>(value, _items);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}