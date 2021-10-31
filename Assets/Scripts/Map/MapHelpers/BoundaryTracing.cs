using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This is a conversion to C# of the algorithm which is implemented at:
// https://www.eriksmistad.no/moore-neighbor-contour-tracing-algorithm-in-c/
// http://www.imageprocessingplace.com/downloads_V3/root_downloads/tutorials/contour_tracing_Abeer_George_Ghuneim/moore.html
namespace BoundaryTracing
{
    class Size
    {
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
    }

    class BoundaryTracing
    {
        public static List<List<Vector2Int>> Find(Pixels pixels, int factor = 1)
        {
            Size size = pixels.size;
            HashSet<Vector2Int> found = new HashSet<Vector2Int>();
            List<Vector2Int> list = null;
            List<List<Vector2Int>> lists = new List<List<Vector2Int>>();
            bool inside = false;

            // Defines the neighborhood offset position from current position and the neighborhood
            // position we want to check next if we find a new border at checkLocationNr.
            int width = size.Width;
            Tuple<Func<Vector2Int, Vector2Int>, int>[] neighborhood = new Tuple<Func<Vector2Int, Vector2Int>, int>[]
            {
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x - 1, point.y    ), 7),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x - 1, point.y - 1), 7),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x    , point.y - 1), 1),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x + 1, point.y - 1), 1),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x + 1, point.y    ), 3),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x + 1, point.y + 1), 3),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x    , point.y + 1), 5),
                new Tuple<Func<Vector2Int, Vector2Int>, int>(point => new Vector2Int(point.x - 1, point.y + 1), 5)
            };

            for (int y = 0; y < size.Height; ++y) {
                for (int x = 0; x < size.Width; ++x) {

                    Vector2Int point = new Vector2Int(x, y);

                    // Scan for non-transparent pixel
                    if (found.Contains(point) && !inside) {
                        inside = true;
                        continue;
                    }

                    bool isTransparent = pixels.IsEmpty(point);

                    if (!isTransparent && inside) {
                        continue;
                    }

                    if (isTransparent && inside) {
                        inside = false;
                        continue;
                    }

                    if (!isTransparent && !inside) {

                        lists.Add(list = new List<Vector2Int>());

                        found.Add(point);
                        list.Add(point * factor); // Mark the start pixel
                        int checkLocationNr = 1; // The neighbor number of the location we want to check for a new border point
                        Vector2Int startPos = point; // Set start position
                        int counter = 0; // Counter is used for the jacobi stop criterion
                        int counter2 = 0; // Counter2 is used to determine if the point we have discovered is one single point

                        // Trace around the neighborhood
                        while (true) {

                            // The corresponding absolute array address of checkLocationNr
                            Vector2Int checkPosition = neighborhood[checkLocationNr - 1].Item1(point);
                            // Variable that holds the neighborhood position we want to check if we find a new border at checkLocationNr
                            int newCheckLocationNr = neighborhood[checkLocationNr - 1].Item2;

                            // Beware that the point might be outside the bitmap.
                            // The isTransparent method contains the safety check.
                            if (!pixels.IsEmpty(checkPosition)) {
                                // Next border point found
                                if (checkPosition == startPos) {
                                    counter++;

                                    // Stopping criterion (jacob)
                                    if (newCheckLocationNr == 1 || counter >= 3) {
                                        // Close loop
                                        // Since we are starting the search at were we first started we must set inside to true
                                        inside = true;
                                        break;
                                    }
                                }

                                // Update which neighborhood position we should check next
                                checkLocationNr = newCheckLocationNr;
                                point = checkPosition;
                                // Reset the counter that keeps track of how many neighbors we have visited
                                counter2 = 0;
                                found.Add(point);
                                list.Add(point * factor); // Set the border pixel
                            } else {
                                // Rotate clockwise in the neighborhood
                                checkLocationNr = 1 + (checkLocationNr % 8);
                                if (counter2 > 8) {
                                    // If counter2 is above 8 we have traced around the neighborhood and
                                    // therefor the border is a single black pixel and we can exit
                                    counter2 = 0;
                                    list = null;
                                    break;
                                } else {
                                    counter2++;
                                }
                            }
                        }

                    }
                }
            }
            return lists;
        }

        // This gets the longest boundary (i.e. list of points), if you don't want all boundaries.
        public static List<Vector2Int> Longest(List<List<Vector2Int>> lists)
        {
            lists.Sort((x, y) => x.Count.CompareTo(y.Count));
            return lists.Last();
        }
    }

    class Pixels
    {
        internal Size size;
        protected readonly int nPixels;
        protected readonly int[] pixels;

        internal Pixels(int[,] data)
        {
            size = new Size(data.GetLength(0), data.GetLength(1));
            nPixels = size.Width * size.Height;
            pixels = new int[nPixels];
            for (int x = 0; x < data.GetLength(0); x++) {
                for (int y = 0; y < data.GetLength(1); y++) {
                    pixels[y * size.Width + x] = data[x, y];
                }
            }
        }

        public int this[Vector2Int point] {
            get {
                int n = (point.y * size.Width) + point.x;
                return pixels[n];
            }
            protected set {
                int n = (point.y * size.Width) + point.x;
                pixels[n] = value;
            }
        }

        internal bool Contains(Vector2Int point)
        {
            return !((point.x < 0) || (point.x >= size.Width) || (point.y < 0) || (point.y >= size.Height));
        }

        internal bool IsEmpty(Vector2Int point)
        {
            if (!Contains(point)) {
                return true;
            }
            return this[point] == 0;
        }
    }
}
