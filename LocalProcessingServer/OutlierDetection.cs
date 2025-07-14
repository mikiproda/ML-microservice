using Accord.MachineLearning;
using Accord.Math;
using Accord.Statistics;
using Accord.Statistics.Models.Regression.Linear;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

public static class OutlierDetection
{
    
    // Regression-Based Outlier Detection
    public static DataTable DetectOutliersRegression(DataTable data, string xCol, string yCol, string resultCol)
    {
        DataTable result = data.Copy();
        result.Columns.Add(resultCol, typeof(int));

        double[][] inputs = data.AsEnumerable()
            .Select(r => new double[] { Convert.ToDouble(r[xCol]) })
            .ToArray();
        double[] outputs = data.AsEnumerable()
            .Select(r => Convert.ToDouble(r[yCol]))
            .ToArray();

        var ols = new OrdinaryLeastSquares();
        var regression = ols.Learn(inputs, outputs);

        double[] predicted = inputs.Select(x => regression.Transform(x)).ToArray();
        double[] residuals = predicted.Zip(outputs, (p, y) => Math.Abs(p - y)).ToArray();
        double mean = residuals.Average();
        double std = Math.Sqrt(residuals.Sum(x => Math.Pow(x - mean, 2)) / residuals.Length);

        for (int i = 0; i < result.Rows.Count; i++)
        {
            result.Rows[i][resultCol] = residuals[i] > 2 * std ? 1 : 0;
        }

        return result;
    }


    // Mahalonobis distance - IN USE
    public static DataTable DetectOutliersMahalanobis(DataTable data, string[] columns, string outlierColumnName, double threshold = 2)
    {
        DataTable result = data.Copy();

        double[][] jagged = data.AsEnumerable()
                                .Select(row => columns.Select(col => Convert.ToDouble(row[col])).ToArray())
                                .ToArray();

        double[,] observations = jagged.ToMatrix();

        double[] mean = observations.Mean(0); 
        double[,] cov = observations.Covariance();
        double[,] covInv = cov.PseudoInverse();

        List<double> distances = new List<double>();

        for (int i = 0; i < observations.GetLength(0); i++)
        {
            double[] x = observations.GetRow(i);
            double[] diff = x.Subtract(mean);
            double distSq = diff.Dot(covInv).Dot(diff);
            distances.Add(Math.Sqrt(distSq));
        }

        result.Columns.Add(outlierColumnName, typeof(int));

        for (int i = 0; i < result.Rows.Count; i++)
        {
            result.Rows[i][outlierColumnName] = distances[i] > threshold ? 1 : 0;
        }

        return result;
    }

    // Distance to k-nearest neighbors (average distance to k neighbors)
    public static DataTable DetectOutliersKNN(DataTable data, string[] columns, string outlierColumnName, int k = 5, double threshold = 1)
    {
        DataTable result = data.Copy();

        double[][] points = result.AsEnumerable()
            .Select(row => columns.Select(col => Convert.ToDouble(row[col])).ToArray())
            .ToArray();

        int n = points.Length;
        List<double> avgDistances = new List<double>();

        for (int i = 0; i < n; i++)
        {
            List<double> distances = new List<double>();
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                double dist = EuclideanDistance(points[i], points[j]);
                distances.Add(dist);
            }
            distances.Sort();
            avgDistances.Add(distances.Take(k).Average());
        }

        double mean = avgDistances.Average();
        double std = Math.Sqrt(avgDistances.Sum(d => Math.Pow(d - mean, 2)) / n);

        result.Columns.Add(outlierColumnName, typeof(int));
        for (int i = 0; i < result.Rows.Count; i++)
        {
            double score = (avgDistances[i] - mean) / std;
            result.Rows[i][outlierColumnName] = score > threshold ? 1 : 0;
        }

        return result;
    }

    private static double EuclideanDistance(double[] p1, double[] p2)
    {
        double sum = 0;
        for (int i = 0; i < p1.Length; i++)
        {
            sum += Math.Pow(p1[i] - p2[i], 2);
        }
        return Math.Sqrt(sum);
    }


    // Plotting function
    public static void PlotOutliersOxyPlot(DataTable data, string xColumn, string yColumn, string outlierColumn, string plotTitle)
    {
        var model = new PlotModel { Title = plotTitle };

        var normalSeries = new ScatterSeries
        {
            Title = "Normal",
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColors.SkyBlue,
            MarkerSize = 5
        };

        var outlierSeries = new ScatterSeries
        {
            Title = "Outlier",
            MarkerType = MarkerType.Triangle,
            MarkerFill = OxyColors.Red,
            MarkerSize = 6
        };

        foreach (DataRow row in data.Rows)
        {
            double x = Convert.ToDouble(row[xColumn]);
            double y = Convert.ToDouble(row[yColumn]);
            bool isOutlier = Convert.ToInt32(row[outlierColumn]) == 1;

            if (isOutlier)
                outlierSeries.Points.Add(new ScatterPoint(x, y));
            else
                normalSeries.Points.Add(new ScatterPoint(x, y));
        }

        model.Series.Add(normalSeries);
        model.Series.Add(outlierSeries);

        ShowPlot(model);
    }

        private static void ShowPlot(PlotModel model)
    {
        var form = new Form
        {
            Text = model.Title,
            Width = 800,
            Height = 600
        };

        var plotView = new PlotView
        {
            Dock = DockStyle.Fill,
            Model = model
        };

        form.Controls.Add(plotView);
        Application.Run(form);
    }

    
}
