using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics.Distributions.Multivariate;
using MathNet.Numerics.LinearAlgebra.Factorization;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;


public static class VolumeGrouping
{
    private static readonly int KMeansRandomSeed = 123; // Fixed seeding for Kmeans algorithm

    

    // Apply Kmeans clustering
    public static (DataTable clusteredData, List<double> boundaries, double[] sortedCentroids) ApplyKMeansClustering(DataTable data, string quantityColumn, string outlierColumn, string clusterColumn, int k)
    {
        
        DataTable processedData = data.Copy();

        double[][] input = processedData.AsEnumerable()
            .Select(row => new double[] { Convert.ToDouble(row[quantityColumn]) })
            .ToArray();

        // Ensure we have enough data points for the requested number of clusters
        if (input.Length < k)
        {
            throw new ArgumentException($"Cannot create {k} clusters with only {input.Length} data points. Reduce k or provide more data.");
        }

        var kmeans = new KMeans(k);
        kmeans.UseSeeding = Seeding.Uniform;
        var clusters = kmeans.Learn(input); // 'clusters' contains the centroids and other info
        int[] originalLabels = clusters.Decide(input); // Get the arbitrary labels assigned by KMeans

        // Every time we generate new clusters the clusterIDs are assigned randomly by algorithm so we need a sorting mechanism:

        // Get the original centroids and pair them with their original cluster IDs
        // clusters.Centroids is a double[][] where each inner array is a centroid.
        // Since we are clustering a single quantity column, each centroid is a double[1].
        // We want to sort by that single value.
        var centroidInfo = clusters.Centroids
            .Select((centroid, index) => new { CentroidValue = centroid[0], OriginalClusterId = index })
            .OrderBy(info => info.CentroidValue) // Sort by the actual quantity value of the centroid
            .ToList();

        // Create a mapping from original cluster ID to new, ordered cluster ID
        // The new cluster IDs will be 0, 1, 2, ... based on the sorted centroid values.
        var newClusterIdMap = new Dictionary<int, int>();
        for (int i = 0; i < centroidInfo.Count; i++)
        {
            newClusterIdMap[centroidInfo[i].OriginalClusterId] = i; // Map old ID to new ordered ID
        }

        // Apply the new, ordered labels to the data
        processedData.Columns.Add(clusterColumn, typeof(int));
        for (int i = 0; i < processedData.Rows.Count; i++)
        {
            int originalLabel = originalLabels[i];
            int newLabel = newClusterIdMap[originalLabel];
            processedData.Rows[i][clusterColumn] = newLabel;
        }

        // Extract the sorted centroids for return
        // This will be the centroids themselves, sorted by their value.
        double[] sortedCentroids = centroidInfo.Select(info => info.CentroidValue).ToArray();

        // Sort boundaries based on the new sorted centroids
        List<double> boundaries = new();
        for (int i = 0; i < sortedCentroids.Length - 1; i++)
        {
            boundaries.Add((sortedCentroids[i] + sortedCentroids[i + 1]) / 2.0);
        }


        return (processedData, boundaries, sortedCentroids);
    }


    // Extract boundaries of clusters
    public static List<double> ExtractBoundaries(DataTable data, Dictionary<string, (double Mean, double Std)> stats, List<double> StandardizedBoundaries)
    {
        List<double> DestandardizedBoundaries = new();

        double mean = stats["Quantity"].Mean;
        double std = stats["Quantity"].Std;

        foreach (var boundary in StandardizedBoundaries)
        {
            var value = Math.Round(boundary * (std == 0 ? 1 : std) + mean, 2);
            DestandardizedBoundaries.Add(value);
        }

        return DestandardizedBoundaries;
    }


    

    // Get cluster buffer zones
    public static (List<List<double>> BufferZones, List<double> PriceMeans) GetClusterBufferZone(DataTable data, string clusterColumn, List<double> boundaries)
    {
        var grouped = data.AsEnumerable()
            .GroupBy(row => Convert.ToInt32(row[clusterColumn]))
            .OrderBy(g => g.Key)
            .ToList();

        List<List<double>> BufferZones = new List<List<double>>();
        List<double> PriceMeans = new List<double>();

        foreach (var group in grouped)
        {
            var clusterId = group.Key;
            var clusterData = group;

            double[] quantities = clusterData.Select(r => Convert.ToDouble(r["Quantity"])).ToArray();
            double[] prices = clusterData.Select(r => Convert.ToDouble(r["Price"])).ToArray();

            Console.WriteLine($"Prices for cluster {clusterId} are:");
            foreach (var price in prices)
            {
                Console.WriteLine(price);
            }

            if (quantities.Length == 0 || prices.Length == 0)
                continue;

            // Compute stats
            double meanPrice = prices.Average();
            double q1 = GetPercentile(prices, 0.25);
            double q3 = GetPercentile(prices, 0.75);
            double clusterMeanQty = quantities.Average();

            Console.WriteLine($"Mean price for cluster {clusterId} is:");
            Console.WriteLine(meanPrice);

            List<double> temp = [ q1, q3 ];
            BufferZones.Add(temp);
            PriceMeans.Add(meanPrice);
        }
       
        return (BufferZones, PriceMeans);
    }

    private static double GetPercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues == null || sortedValues.Length == 0)
            return 0;

        var sorted = sortedValues.OrderBy(x => x).ToArray();
        double index = percentile * (sorted.Length - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return sorted[lower];
        return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower]);
    }

   
}
