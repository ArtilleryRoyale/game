using System.Collections.Generic;
using System.Linq;
using Jrmgx.Helpers;
using UnityEngine;

namespace CC
{
    // Note: this graph is meant to be bi-directional and its implementation is based on this predicate
    public class Graph : WeightedGraph<GraphPoint>
    {
        public enum SegmentType : int { Default = 0, Link = 1 }

        public HashSet<Vector2> Points { get; private set; } = new HashSet<Vector2>();
        private Dictionary<GraphPoint, HashSet<GraphPoint>> graphData = new Dictionary<GraphPoint, HashSet<GraphPoint>>();
        private Dictionary<Segment, SegmentType> segmentTypes = new Dictionary<Segment, SegmentType>();

        public static List<Graph> GraphsForPolygon(
            List<Vector2> pathPoints,
            float maxSlope,
            float clearanceHeight,
            float clearanceWidth,
            LayerMask maskClearance,
            float maskClearanceRadius = 1f
        )
        {
            var graphs = new List<Graph>();

            // Simplify collider here so they are all counter-clockwise
            pathPoints = MapHelpers.SimplifyPolygon(pathPoints.ToArray());
            if (pathPoints.Count == 0) return graphs;

            bool lastWasInvalid = true;
            Vector2 firstValidPointRight = Vector2.negativeInfinity;
            Graph currentGraph = null;

            // For each couple of points find valid points
            for (int pointIndex = 0, pointMax = pathPoints.Count - 1; pointIndex < pointMax; pointIndex++) {
                // Counter-clockwise means that for a given point at index i,
                // the point at index + 1 is on the left (starting on top)
                Vector2 pointLeft  = pointIndex == 0 ? pathPoints[0] : pathPoints[pointIndex + 1];
                Vector2 pointRight = pointIndex == 0 ? pathPoints[pointMax] : pathPoints[pointIndex];

                // Check if point left is on the right: means we are on the bottom of our collider
                if (pointLeft.x > pointRight.x) {
                    lastWasInvalid = true;
                    continue;
                }

                // Check angle
                float angle = Mathf.Abs(Vector2.SignedAngle(pointRight - pointLeft, Vector2.right));
                if (angle > maxSlope) {
                    // Debugging.DrawText(pointLeft, "" + Mathf.RoundToInt(angle), Color.yellow, 20f, 0.2f);
                    // Debugging.DrawLine(pointLeft, pointRight, Color.yellow, 20f); // Unvalid
                    lastWasInvalid = true;
                    continue;
                }

                // Check clearance height
                Vector2 deltaClearance = new Vector2(0, 0.1f);
                if (Physics2D.Raycast(pointLeft  + deltaClearance, Vector2.up, clearanceHeight, Layer.Terrain.Mask) ||
                    Physics2D.Raycast(pointRight + deltaClearance, Vector2.up, clearanceHeight, Layer.Terrain.Mask)) {
                    // Debugging.DrawLine(pointLeft + deltaClearance, pointLeft + deltaClearance + (Vector2.up * clearanceHeight), Color.red.WithA(.2f), 20f);
                    // Debugging.DrawLine(pointRight + deltaClearance, pointRight + deltaClearance + (Vector2.up * clearanceHeight), Color.red.WithA(.2f), 20f);
                    // Debugging.DrawLine(pointLeft, pointRight, Color.red, 20f); // Unvalid
                    lastWasInvalid = true;
                    continue;
                }

                // Check clearance width
                deltaClearance = new Vector2(0, clearanceHeight);
                clearanceWidth /= 2f;
                if (Physics2D.Raycast(pointLeft  + deltaClearance, Vector2.left,  clearanceWidth, Layer.Terrain.Mask) ||
                    Physics2D.Raycast(pointRight + deltaClearance, Vector2.right, clearanceWidth, Layer.Terrain.Mask)) {
                    // Debugging.DrawLine(pointLeft + deltaClearance, pointLeft + deltaClearance + (Vector2.left * clearanceWidth), Color.red.WithA(.2f), 20f);
                    // Debugging.DrawLine(pointRight + deltaClearance, pointRight + deltaClearance + (Vector2.right * clearanceWidth), Color.red.WithA(.2f), 20f);
                    // Debugging.DrawLine(pointLeft, pointRight, Color.red, 20f); // Unvalid
                    lastWasInvalid = true;
                    continue;
                }

                // Check if no object of type layer mask is close
                if (Physics2D.CircleCast(pointLeft,  maskClearanceRadius, Vector2.left,  0.1f, maskClearance) ||
                    Physics2D.CircleCast(pointRight, maskClearanceRadius, Vector2.right, 0.1f, maskClearance)) {
                    // Debugging.DrawLine(pointLeft,  pointLeft  + Vector2.left  * 0.1f, Color.red.WithA(.2f), 20f);
                    // Debugging.DrawLine(pointRight, pointRight + Vector2.right * 0.1f, Color.red.WithA(.2f), 20f);
                    // Debugging.DrawLine(pointLeft, pointRight, Color.red, 20f); // Unvalid
                    lastWasInvalid = true;
                    continue;
                }

                if (lastWasInvalid) {
                    lastWasInvalid = false;
                    currentGraph = new Graph();
                    graphs.Add(currentGraph);
                }

                // Debugging.DrawLine(pointLeft, pointRight, Color.magenta, 20f); // Valid
                currentGraph.Add(new GraphPoint(pointRight), new GraphPoint(pointLeft));

                if (firstValidPointRight == Vector2.negativeInfinity) {
                    firstValidPointRight = pointRight;
                }
            }

            // First graph and last graph can share a common point (right from first <> left from last)
            // Check if true and merge it (remove this endPoints from the list too)
            if (graphs.Count > 1) {
                int first = 0, last = graphs.Count - 1;
                if (graphs[last].ContainsPoint(firstValidPointRight)) {
                    graphs[first] = Graph.Merge(graphs[first], graphs[last]);
                    graphs.RemoveAt(last);
                }
            }

            return graphs;
        }

        public static List<GraphLinkInfo> GetLinksBetweenGraphs(List<Graph> graphs, float linkDistanceMax, float raycastElevation)
        {
            var graphLinksInfo = new List<GraphLinkInfo>();

            var points = new List<Vector2>();
            var ends = new List<Vector2>();

            foreach (var g in graphs) {
                points.AddRange(g.Points);
                ends.AddRange(g.GetEnds());
            }

            foreach (var endPoint in ends) {
                int countDistance = 0;
                float sumDistance = 0f;
                var linkCandidates = new Dictionary<float, Vector2>();
                foreach (var point in points) {
                    if (endPoint == point) continue; // same point, skip

                    var vectorBetween = Basics.VectorBetween(endPoint, point);
                    float distance = vectorBetween.magnitude;
                    if (distance > linkDistanceMax) continue; // points are too far

                    var deltaCast = 0.1f;
                    var direction = vectorBetween.normalized;
                    // We elevate the endPoint a bit to prevent hitting collider too fast and block important links
                    var hit = Physics2D.Raycast(endPoint + Vector2.up * raycastElevation + direction * deltaCast, direction, distance - (deltaCast * 2f));
                    if (!hit) {
                        // Debugging.DrawLine(endPoint + direction * deltaCast, endPoint + direction * deltaCast + direction * (distance - (deltaCast * 2f)), Color.cyan.WithA(.5f), 20f);
                        countDistance++;
                        sumDistance += distance;
                        linkCandidates[distance] = point;
                    }
                }
                // We most likely have multiple link candidates per endPoint,
                // let's filter by taking the one with average distance
                var avgDistance = sumDistance / (float)countDistance;
                foreach (KeyValuePair<float, Vector2> kv in linkCandidates.OrderBy(i => i.Key)) {
                    if (kv.Key >= avgDistance) {
                        graphLinksInfo.Add(new GraphLinkInfo(endPoint, kv.Value));
                        break;
                    }
                }
                linkCandidates.Clear();
            }

            return graphLinksInfo;
        }

        public static List<Graph> MergeByLinks(List<Graph> graphs, List<GraphLinkInfo> linksInfo)
        {
            var graphsIndex = 0;
            var graphsCandidates = new Dictionary<int, Graph>();

            foreach (var g in graphs) {
                graphsCandidates.Add(graphsIndex++, g);
            }

            foreach (var linkInfo in linksInfo) {
                Graph graphA = null, graphB = null;
                int indexA = 0, indexB = 0;
                foreach (var kv in graphsCandidates) {
                    if (kv.Value.ContainsPoint(linkInfo.LeftPoint)) {
                        indexA = kv.Key;
                        graphA = kv.Value;
                        break;
                    }
                }
                foreach (var kv in graphsCandidates) {
                    if (kv.Value.ContainsPoint(linkInfo.RightPoint)) {
                        indexB = kv.Key;
                        graphB = kv.Value;
                        break;
                    }
                }
                if (graphA == null || graphB == null) {
                    Log.Critical("AI Pathfinder", "Graph is null when trying to merge for link");
                    continue;
                }
                Graph merged = Graph.Merge(graphA, graphB);
                merged.Add(
                    new GraphPoint(linkInfo.LeftPoint),
                    new GraphPoint(linkInfo.RightPoint),
                    SegmentType.Link
                );
                graphsCandidates[indexA] = merged;
                if (indexA != indexB) {
                    graphsCandidates.Remove(indexB);
                }
            }

            return graphsCandidates.Values.ToList();
        }

        public static Graph Merge(Graph a, Graph b)
        {
            var graph = new Graph();
            foreach (var data in a.graphData) {
                foreach (var v in data.Value) {
                    graph.Add(data.Key, v);
                }
            }
            foreach (var data in b.graphData) {
                foreach (var v in data.Value) {
                    graph.Add(data.Key, v);
                }
            }
            graph.segmentTypes = Basics.Merge(a.segmentTypes, b.segmentTypes);
            return graph;
        }

        public static Graph GetClosestGraph(Vector2 point, IEnumerable<Graph> graphs)
        {
            float minimal = float.MaxValue;
            Graph found = null;
            foreach (var g in graphs) {
                foreach (var p in g.Points) {
                    var distance = (point - p).sqrMagnitude;
                    if (distance < minimal) {
                        minimal = distance;
                        found = g;
                    }
                }
            }
            return found;
        }

        public Vector2 GetClosestPoint(Vector2 point)
        {
            float minimal = float.MaxValue;
            Vector2 found = Vector2.negativeInfinity;
            foreach (var p in Points) {
                var distance = (point - p).sqrMagnitude;
                if (distance < minimal) {
                    minimal = distance;
                    found = p;
                }
            }
            return found;
        }

        public Vector2? PointIsNearGraph(Vector2 point, float distanceMax)
        {
            foreach (var p in Points) {
                if (Vector2.Distance(p, point) < distanceMax) {
                    return p;
                }
            }
            return null;
        }

        public SegmentType GetSegmentType(Segment segment)
        {
            if (segmentTypes.ContainsKey(segment)) {
                return segmentTypes[segment];
            }
            return SegmentType.Default;
        }

        /// <summary>
        /// Segments will only return segments points and not links
        /// </summary>
        public Dictionary<Vector2, Vector2> GetSegments()
        {
            var segments = new Dictionary<Vector2, Vector2>();
            foreach (var data in graphData) {
                foreach (var v in data.Value) {
                    if (segmentTypes.ContainsKey(new Segment(v.Point, data.Key.Point))) continue;
                    segments[data.Key.Point] = v.Point;
                }
            }
            return segments;
        }

        public List<Vector2> GetEnds()
        {
            var ends = new List<Vector2>();
            foreach (var p in graphData) {
                if (p.Value.Count == 1) {
                    ends.Add(p.Key.Point);
                }
            }
            return ends;
        }

        public bool ContainsPoint(Vector2 point)
        {
            foreach (var p in Points) {
                if (point == p) return true;
            }
            return false;
        }

        public void Add(GraphPoint a, GraphPoint b, SegmentType segmentType = SegmentType.Default)
        {
            if (a.Point == b.Point) return;
            Points.Add(a.Point);
            Points.Add(b.Point);
            if (segmentType != SegmentType.Default) {
                segmentTypes[new Segment(a.Point, b.Point)] = segmentType;
            }
            if (graphData.ContainsKey(a)) {
                graphData[a].Add(b);
            } else {
                graphData.Add(a, new HashSet<GraphPoint>{ b });
            }
            if (graphData.ContainsKey(b)) {
                graphData[b].Add(a);
            } else {
                graphData.Add(b, new HashSet<GraphPoint>{ a });
            }
        }

        public float Cost(GraphPoint a, GraphPoint b)
        {
            var segment = new Segment(a.Point, b.Point);
            if (segmentTypes.ContainsKey(segment)) {
                // So far we have only one type so, only one rule here
                return (a.Point - b.Point).magnitude * 1.1f;
            }
            return (a.Point - b.Point).magnitude;
        }

        public IEnumerable<GraphPoint> Neighbors(GraphPoint id)
        {
            return graphData[id];
        }

        public override string ToString()
        {
            var t = "Graph with " + Points.Count + " points:\n";
            foreach (var c in graphData) {
                t += "    - " + c.Key + "\n";
                foreach (var cc in c.Value) {
                    t += "      => " + cc + "\n";
                }
            }
            return t;
        }

        public static void DrawDebugGraph(Graph graph, float duration)
        {
#if CC_DEBUG
            foreach (var data in graph.graphData) {
                foreach (var v in data.Value) {
                    var segment = new Segment(data.Key.Point, v.Point);
                    // Debugging.DrawLine(data.Key.Point, v.Point, graph.segmentTypes.ContainsKey(segment) ? Color.blue : Color.magenta, duration);
                }
            }
#endif
        }

        public static void DrawDebugPath(IEnumerable<GraphPoint> path, Graph graph, float duration)
        {
#if CC_DEBUG
            // Debugging.DrawCircle(path.First().Point, 1f, Color.blue, duration, 4);
            // Debugging.DrawCircle(path.Last().Point, 1f, Color.green, duration, 4);
            var points = path.ToList();
            for (int i = 0, max = points.Count - 1; i < max; i++) {
                Vector2 a = points[i].Point, b = points[i + 1].Point;
                var segment = new Segment(a, b);
                var type = graph.GetSegmentType(segment);
                // Debugging.DrawLine(a + Vector2.up, b + Vector2.up, type == Graph.SegmentType.Link ? Color.red : Color.cyan, duration);
            }
#endif
        }
    }
}
