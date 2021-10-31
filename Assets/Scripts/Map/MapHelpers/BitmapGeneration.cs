using System.Collections.Generic;
using Jrmgx.Helpers;
using UnityEngine;

// https://blogs.unity3d.com/2018/05/29/procedural-patterns-you-can-use-with-tilemaps-part-i/
// https://blogs.unity3d.com/2018/06/07/procedural-patterns-to-use-with-tilemaps-part-ii/
/// <summary>
/// Generate Maps
/// WARNING: maps are bottom-up / left-right based, don't be fooled by the bit array
/// [0, 0] is the BOTTOM left
/// [max, max] is the TOP right
/// </summary>
public static class BitmapGeneration
{
    #region Public

    public static int[,] Ground(int width, int height)
    {
        return Array(width, Mathf.Min(5, height), empty: false);
    }

    public static int[,] RandomWalk(int width, int height, int MinSectionWidth)
    {
        var empty = Array(width, height, empty: true);
        return Add(
            empty,
            RandomWalkTopSmoothed(empty, Random.value, MinSectionWidth)
        );
    }

    public static int[,] Cellular(int width, int height, int FillPercent, int SmoothCount)
    {
        return Add(
            Array(width, height, empty: true),
            Invert(SmoothMooreCellularAutomata(CellularAutomata(
                width, height,
                Random.value,
                FillPercent,
                edgesAreWalls: true
            ),
            edgesAreWalls: true,
            SmoothCount
        )));
    }

    public static int[,] Perlin(int width, int height, int MinSectionWidth)
    {
        int[,] empty = Array(width, height, empty: true);
        int[,] perlin = PerlinNoiseSmooth(
            empty,
            Random.value * Time.realtimeSinceStartup,
            MinSectionWidth
        );
        return Add(empty, perlin);
    }

    #endregion

    #region Partial Generators

    private static int[,] Array(int width, int height, bool empty = true)
    {
        int[,] map = new int[width, height];
        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                map[x, y] = empty ? 0 : 1;
            }
        }
        return map;
    }

    private static int[,] PerlinNoise(int[,] map, float seed)
    {
        int newPoint;
        // Used to reduced the position of the Perlin point
        float reduction = 0.5f;
        // Create the Perlin
        for (int x = 0; x < map.GetLength(0); x++) {
            newPoint = Mathf.FloorToInt((Mathf.PerlinNoise(x, seed) - reduction) * map.GetUpperBound(1));
            // Make sure the noise starts near the halfway point of the height
            newPoint += map.GetUpperBound(1) / 2;
            for (int y = newPoint; y >= 0; y--) {
                map[x, y] = 1;
            }
        }
        return map;
    }

    private static int[,] PerlinNoiseSmooth(int[,] map, float seed, int interval)
    {
        // Smooth the noise and store it in the int array
        if (interval > 1) {
            int newPoint, points;
            // Used to reduced the position of the Perlin point
            float reduction = 0.5f;

            // Used in the smoothing process
            Vector2Int currentPos, lastPos;
            // The corresponding points of the smoothing. One list for x and one for y
            List<int> noiseX = new List<int>();
            List<int> noiseY = new List<int>();

            // Generate the noise
            for (int x = 0; x < map.GetLength(0); x += interval) {
                newPoint = Mathf.FloorToInt(Mathf.PerlinNoise(x, seed * reduction) * map.GetUpperBound(1));
                noiseY.Add(newPoint);
                noiseX.Add(x);
            }

            points = noiseY.Count;

            // Start at 1 so we have a previous position already
            for (int i = 1; i < points; i++) {
                currentPos = new Vector2Int(noiseX[i], noiseY[i]);
                lastPos = new Vector2Int(noiseX[i - 1], noiseY[i - 1]);

                Vector2 diff = currentPos - lastPos;

                // Set up what the height change value will be
                float heightChange = diff.y / interval;
                // Determine the current height
                float currHeight = lastPos.y;

                // Work our way through from the last x to the current x
                for (int x = lastPos.x; x < currentPos.x; x++) {
                    for (int y = Mathf.FloorToInt(currHeight); y > 0; y--) {
                        map[x, y] = 1;
                    }
                    currHeight += heightChange;
                }
            }
        } else {
            // Defaults to a normal Perlin gen
            map = PerlinNoise(map, seed);
        }
        return map;
    }

    private static int[,] RandomWalkTopSmoothed(int[,] map, float seed, int minSectionWidth)
    {
        System.Random rand = new System.Random(seed.GetHashCode());

        // Determine the start position
        int lastHeight = Random.Range(0, map.GetLength(1));

        // Used to determine which direction to go
        int nextMove;
        int sectionWidth = 0;

        // Work through the array width
        for (int x = 0; x < map.GetLength(0); x++) {
            // Determine the next move
            nextMove = rand.Next(2);

            // Only change the height if we have used the current height more than the minimum required section width
            if (nextMove == 0 && lastHeight > 0 && sectionWidth > minSectionWidth) {
                lastHeight--;
                sectionWidth = 0;
            } else if (nextMove == 1 && lastHeight < map.GetUpperBound(1) && sectionWidth > minSectionWidth) {
                lastHeight++;
                sectionWidth = 0;
            }
            // Increment the section width
            sectionWidth++;

            // Work our way from the height down to 0
            for (int y = lastHeight; y >= 0; y--) {
                map[x, y] = 1;
            }
        }
        return map;
    }

    private static int[,] SmoothMooreCellularAutomata(int[,] map, bool edgesAreWalls, int smoothCount)
    {
        for (int i = 0; i < smoothCount; i++) {
            for (int x = 0; x < map.GetLength(0); x++) {
                for (int y = 0; y < map.GetLength(1); y++) {
                    int surroundingTiles = GetMooreSurroundingTilesCount(map, x, y);

                    if (edgesAreWalls && (
                        x == 0 || x == map.GetUpperBound(0) ||
                        y == 0 || y == map.GetUpperBound(1)
                    )) {
                        map[x, y] = 1; // Keep the edges as walls
                    }
                    // The default moore rule requires more than 4 neighbors
                    else if (surroundingTiles > 4) {
                        map[x, y] = 1;
                    } else if (surroundingTiles < 4) {
                        map[x, y] = 0;
                    }
                }
            }
        }
        return map;
    }

    private static int[,] CellularAutomata(int width, int height, float seed, int fillPercent, bool edgesAreWalls)
    {
        System.Random rand = new System.Random(seed.GetHashCode());
        int[,] map = new int[width, height];

        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                // If we have the edges set to be walls, ensure the cell is set to on (1)
                if (edgesAreWalls && (x == 0 || x == map.GetUpperBound(0) || y == 0 || y == map.GetUpperBound(1))) {
                    map[x, y] = 1;
                } else {
                    // Randomly generate the grid
                    map[x, y] = (rand.Next(0, 100) < fillPercent) ? 1 : 0;
                }
            }
        }
        return map;
    }

    #endregion

    #region Operations

    public static int[,] Stretch(int[,] map, int factor)
    {
        if (factor <= 1) return map;

        int width = map.GetLength(0);
        int[,] result = Array(width * factor, map.GetLength(1));

        for (int y = 0; y < map.GetLength(1); y++) {
            int x2 = 0;
            for (int x = 0; x < map.GetLength(0); x++) {
                for (int f = 0; f < factor; f++) {
                    result[x2, y] = map[x, y];
                    x2++;
                }
            }
        }

        return result;
    }

    public static int[,] Trim(int[,] map, int keep = 1)
    {
        if (keep <= 0) return map;

        int fullLine = 0;
        for (int y = 0; y < map.GetLength(1); y++) {
            for (int x = 0; x < map.GetLength(0); x++) {
                if (map[x, y] == 0) {
                    goto break2; // Simulate break 2;
                }
            }
            fullLine++;
        }

        break2:

        if (fullLine == 0) return map;

        int[,] result = Array(map.GetLength(0), map.GetLength(1));

        int yStart = Mathf.Max(0, fullLine - keep);
        for (int y = yStart; y < map.GetLength(1); y++) {
            for (int x = 0; x < map.GetLength(0); x++) {
                result[x, y - yStart] = map[x, y];
            }
        }

        return result;
    }

    private static int[,] Invert(int[,] map)
    {
        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                map[x, y] = map[x, y] == 1 ? 0 : 1;
            }
        }
        return map;
    }

    private static int[,] Add(int[,] map, int[,] addeeMap)
    {
        if (map.Length != addeeMap.Length) {
            Log.Error("BitmapGeneration", "Map have different size");
        }

        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                map[x, y] = map[x, y] == 1 || addeeMap[x, y] == 1 ? 1 : 0;
            }
        }
        return map;
    }

    #endregion

    #region Helpers

    private static int GetMooreSurroundingTilesCount(int[,] map, int x, int y)
    {
        int tileCount = 0;

        for (int neighborX = x - 1; neighborX <= x + 1; neighborX++) {
            for (int neighborY = y - 1; neighborY <= y + 1; neighborY++) {
                if (
                    neighborX >= 0 && neighborX < map.GetLength(0) &&
                    neighborY >= 0 && neighborY < map.GetLength(1)
                ) {
                    // We don't want to count the tile we are checking the surroundings of
                    if (neighborX != x || neighborY != y) {
                        tileCount += map[neighborX, neighborY];
                    }
                }
            }
        }
        return tileCount;
    }

    #endregion
}
