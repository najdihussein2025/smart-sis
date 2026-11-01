namespace SmartSis.Api.Features.Courses.Rules;

/// <summary>
/// Detects cycles in the prerequisite graph. An edge (CourseId, PrerequisiteId) means
/// "CourseId requires PrerequisiteId". Pure: give it the edges as they would be after the change.
/// </summary>
public static class PrerequisiteCycleCheck
{
    /// <summary>
    /// The first cycle found, as a closed path of course IDs following "requires" edges
    /// (e.g. [A, B, C, A] = A requires B, B requires C, C requires A), or null when there is none.
    /// </summary>
    public static IReadOnlyList<int>? FindCycle(IEnumerable<(int CourseId, int PrerequisiteId)> edges)
    {
        var graph = BuildGraph(edges);
        var state = new Dictionary<int, VisitState>();
        var path = new List<int>();

        foreach (var start in graph.Keys.Order())
        {
            if (state.ContainsKey(start)) continue;
            var cycle = Visit(start);
            if (cycle is not null) return cycle;
        }
        return null;

        IReadOnlyList<int>? Visit(int node)
        {
            state[node] = VisitState.OnPath;
            path.Add(node);
            foreach (var next in Neighbours(graph, node))
            {
                if (state.GetValueOrDefault(next) == VisitState.OnPath)
                    return [.. path.Skip(path.IndexOf(next)), next];
                if (state.ContainsKey(next)) continue;
                var cycle = Visit(next);
                if (cycle is not null) return cycle;
            }
            path.RemoveAt(path.Count - 1);
            state[node] = VisitState.Done;
            return null;
        }
    }

    /// <summary>
    /// A cycle that passes through <paramref name="courseId"/>, as a closed path starting and ending there
    /// (e.g. [A, B, C, A]), or null. Use this after changing one course's prerequisites: cycles elsewhere
    /// in the graph aren't caused by that change.
    /// </summary>
    public static IReadOnlyList<int>? FindCycleThrough(IEnumerable<(int CourseId, int PrerequisiteId)> edges, int courseId)
    {
        var graph = BuildGraph(edges);
        var visited = new HashSet<int> { courseId };
        var path = new List<int>();
        return Visit(courseId);

        IReadOnlyList<int>? Visit(int node)
        {
            path.Add(node);
            foreach (var next in Neighbours(graph, node))
            {
                if (next == courseId) return [.. path, courseId];
                if (!visited.Add(next)) continue; // already explored: no way back from there
                var cycle = Visit(next);
                if (cycle is not null) return cycle;
            }
            path.RemoveAt(path.Count - 1);
            return null;
        }
    }

    private enum VisitState { OnPath = 1, Done = 2 }

    private static Dictionary<int, SortedSet<int>> BuildGraph(IEnumerable<(int CourseId, int PrerequisiteId)> edges)
    {
        var graph = new Dictionary<int, SortedSet<int>>();
        foreach (var (course, prerequisite) in edges)
        {
            if (!graph.TryGetValue(course, out var set)) graph[course] = set = [];
            set.Add(prerequisite);
        }
        return graph;
    }

    private static IEnumerable<int> Neighbours(Dictionary<int, SortedSet<int>> graph, int node) =>
        graph.TryGetValue(node, out var set) ? set : [];
}
