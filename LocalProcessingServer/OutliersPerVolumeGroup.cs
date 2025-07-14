using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


public static class OutliersPerVolumeGroup
{
    // Detects outliers in each cluster 
    public static DataTable DetectOutliersPerCluster(DataTable data, string clusterCol, string[] featureCols, string outlierCol = "ClusterOutlier")
    {
        List<DataTable> clusterTables = new List<DataTable>();

        var clusterIds = data.AsEnumerable()
                                .Select(row => Convert.ToInt32(row[clusterCol]))
                                .Distinct()
                                .ToList();

        foreach (var clusterId in clusterIds)
        {
            DataTable clusterData = data.AsEnumerable()
                                        .Where(row => Convert.ToInt32(row[clusterCol]) == clusterId)
                                        .CopyToDataTable();

            DataTable withOutliers = OutlierDetection.DetectOutliersMahalanobis(clusterData, featureCols, outlierCol);

            clusterTables.Add(withOutliers);
        }

        DataTable result = clusterTables[0].Clone();
        foreach (var dt in clusterTables)
        {
            foreach (DataRow row in dt.Rows)
            {
                result.ImportRow(row);
            }
        }

        return result;
    }
}

