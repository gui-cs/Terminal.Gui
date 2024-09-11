using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui.Drawing.Quant;

    /// <summary>
    /// <see cref="IPaletteBuilder"/> that works well for images with high contrast images
    /// </summary>
    public class KMeansPaletteBuilder : IPaletteBuilder
    {
        private readonly int maxIterations;
        private readonly Random random = new Random ();
        private readonly IColorDistance colorDistance;

        public KMeansPaletteBuilder (IColorDistance distanceAlgorithm, int maxIterations = 100)
        {
            colorDistance = distanceAlgorithm;
            this.maxIterations = maxIterations;
        }

        public List<Color> BuildPalette (List<Color> colors, int maxColors)
        {
            // Convert colors to vectors
            List<ColorVector> colorVectors = colors.Select (c => new ColorVector (c.R, c.G, c.B)).ToList ();

            // Perform K-Means Clustering
            List<ColorVector> centroids = KMeans (colorVectors, maxColors);

            // Convert centroids back to colors
            return centroids.Select (v => new Color ((int)v.R, (int)v.G, (int)v.B)).ToList ();
        }

        private List<ColorVector> KMeans (List<ColorVector> colors, int k)
        {
            // Randomly initialize k centroids
            List<ColorVector> centroids = InitializeCentroids (colors, k);

            List<ColorVector> previousCentroids = new List<ColorVector> ();
            int iterations = 0;

            // Repeat until convergence or max iterations
            while (!HasConverged (centroids, previousCentroids) && iterations < maxIterations)
            {
                previousCentroids = centroids.Select (c => new ColorVector (c.R, c.G, c.B)).ToList ();

                // Assign each color to the nearest centroid
                var clusters = AssignColorsToClusters (colors, centroids);

                // Recompute centroids
                centroids = RecomputeCentroids (clusters);

                iterations++;
            }

            return centroids;
        }

        private List<ColorVector> InitializeCentroids (List<ColorVector> colors, int k)
        {
            return colors.OrderBy (c => random.Next ()).Take (k).ToList (); // Randomly select k initial centroids
        }

        private Dictionary<ColorVector, List<ColorVector>> AssignColorsToClusters (List<ColorVector> colors, List<ColorVector> centroids)
        {
            var clusters = centroids.ToDictionary (c => c, c => new List<ColorVector> ());

            foreach (var color in colors)
            {
                // Find the nearest centroid using the injected IColorDistance implementation
                var nearestCentroid = centroids.OrderBy (c => colorDistance.CalculateDistance (c.ToColor (), color.ToColor ())).First ();
                clusters [nearestCentroid].Add (color);
            }

            return clusters;
        }

        private List<ColorVector> RecomputeCentroids (Dictionary<ColorVector, List<ColorVector>> clusters)
        {
            var newCentroids = new List<ColorVector> ();

            foreach (var cluster in clusters)
            {
                if (cluster.Value.Count == 0)
                {
                    // Reinitialize the centroid with a random color if the cluster is empty
                    newCentroids.Add (InitializeRandomCentroid ());
                }
                else
                {
                    // Recompute the centroid as the mean of the cluster's points
                    double avgR = cluster.Value.Average (c => c.R);
                    double avgG = cluster.Value.Average (c => c.G);
                    double avgB = cluster.Value.Average (c => c.B);

                    newCentroids.Add (new ColorVector (avgR, avgG, avgB));
                }
            }

            return newCentroids;
        }

        private bool HasConverged (List<ColorVector> currentCentroids, List<ColorVector> previousCentroids)
        {
            // Skip convergence check for the first iteration
            if (previousCentroids.Count == 0)
            {
                return false; // Can't check for convergence in the first iteration
            }

            // Check if the length of current and previous centroids are different
            if (currentCentroids.Count != previousCentroids.Count)
            {
                return false; // They haven't converged if they don't have the same number of centroids
            }

            // Check if the centroids have changed between iterations using the injected distance algorithm
            for (int i = 0; i < currentCentroids.Count; i++)
            {
                if (colorDistance.CalculateDistance (currentCentroids [i].ToColor (), previousCentroids [i].ToColor ()) > 1.0) // Use a larger threshold
                {
                    return false; // Centroids haven't converged yet if any of them have moved significantly
                }
            }

            return true; // Centroids have converged if all distances are below the threshold
        }

        private ColorVector InitializeRandomCentroid ()
        {
            // Initialize a random centroid by picking random color values
            return new ColorVector (random.Next (0, 256), random.Next (0, 256), random.Next (0, 256));
        }

        private class ColorVector
        {
            public double R { get; }
            public double G { get; }
            public double B { get; }

            public ColorVector (double r, double g, double b)
            {
                R = r;
                G = g;
                B = b;
            }

            // Convert ColorVector back to Color for use with the IColorDistance interface
            public Color ToColor ()
            {
                return new Color ((int)R, (int)G, (int)B);
            }
        }
}
