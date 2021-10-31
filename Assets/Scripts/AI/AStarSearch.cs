using System.Collections.Generic;

/// https://www.redblobgames.com/pathfinding/a-star/implementation.html#csharp
namespace CC
{
    public class AStarSearch<T> where T : WithHeuristic<T>
    {
        public Dictionary<T, T> cameFrom = new Dictionary<T, T>();
        private Dictionary<T, double> costSoFar = new Dictionary<T, double>();
        private T Goal;
        public bool Found { get; private set; }

        public AStarSearch() {}

        public bool Search(WeightedGraph<T> graph, T start, T goal)
        {
            Goal = goal;
            var frontier = new PriorityQueue<T>();
            frontier.Enqueue(start, 0);

            costSoFar[start] = 0;

            while (frontier.Count > 0) {
                var current = frontier.Dequeue();

                if (current.Equals(goal)) {
                    Found = true;
                    return true;
                }

                foreach (var neighbor in graph.Neighbors(current)) {
                    double newCost = costSoFar[current] + graph.Cost(current, neighbor);
                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]) {
                        costSoFar[neighbor] = newCost;
                        double priority = newCost + neighbor.Heuristic(goal);
                        frontier.Enqueue(neighbor, priority);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            Found = false;
            return false;
        }

        public List<T> GetPath()
        {
            var path = new List<T>();
            if (!Found) return path;
            path.Add(Goal);
            var current = Goal;
            while (cameFrom.ContainsKey(current)) {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
