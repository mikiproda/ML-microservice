// File: General/DataHandling.cs
using CsvHelper; // For CsvWriter
using Newtonsoft.Json; // For JsonConverter
using Newtonsoft.Json.Linq;
using System;
using System.Data; // For DataTable
using System.Globalization; // For CultureInfo.InvariantCulture
using System.IO;  // For StreamWriter
using System.Text.Json; // For Json Serialization
using System.Xml;



public static class DataHandling
{
    // Saving to a .csv file
    public static void SaveDataTableToCsv(DataTable dataTable, string filePath)
    {
        try
        {
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // Write headers
                foreach (DataColumn column in dataTable.Columns)
                {
                    csv.WriteField(column.ColumnName);
                }
                csv.NextRecord();

                // Write rows
                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        csv.WriteField(item?.ToString()); 
                    }
                    csv.NextRecord();
                }
            }
            Console.WriteLine($"Successfully saved DataTable to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving DataTable to CSV: {ex.Message}");
        }
    }

    // Load data from .csv
    public static DataTable LoadCsv(string filepath)
    {
        DataTable dataTable;
        using (var reader = new Accord.IO.CsvReader(filepath, hasHeaders: true))
        {
            dataTable = reader.ToTable();
        }
        return dataTable;
    }

    // Helper:Convert DataTable to a List of dictionaries
    public static List<Dictionary<string, object>> DataTableToList(DataTable table)
    {
        var list = new List<Dictionary<string, object>>();

        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in table.Columns)
            {
                dict[col.ColumnName] = row[col];
            }
            list.Add(dict);
        }

        return list;
    }

    // Saving outliers to .json file
    public static void ExportOutliersToJson(DataTable outliers, string outputPath)
    {
        var outlierList = DataTableToList(outliers);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = System.Text.Json.JsonSerializer.Serialize(outlierList, options);

        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Successfully saved .json file to: {outputPath}");
    }

    // Saving .json data needed for price analysis by user
    public static void ExportPlotDataToJson(
        DataTable data,
        List<double> clusterBoundaries,
        List<List<double>> bufferZones,
        List<double> priceMeans,
        List<(double Quantity, double PredictedPrice)> curvePoints,
        string outputPath)
    {
        var exportObject = new
        {
            dataPoints = data.AsEnumerable().Select(row => new
            {
                id = row["CustomerID"],
                quantity = Convert.ToDouble(row["Quantity"]),
                price = Convert.ToDouble(row["Price"])
            }).ToList(),

            clusterBoundaries = clusterBoundaries,
            bufferZones = bufferZones,
            priceMeans = priceMeans,
            elasticityCurve = curvePoints.Select(p => new
            {
                quantity = p.Quantity,
                predictedPrice = p.PredictedPrice
            }).ToList()
        };

        string json = JsonConvert.SerializeObject(exportObject, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(outputPath, json);
        Console.WriteLine($"Successfully saved .json file to: {outputPath}");
    }

    // Getting .json data needed for price analysis
    public static (DataTable DataPoints, List<double> ClusterBoundaries, List<List<double>> BufferZones,
                   List<double> PriceMeans, List<(double Quantity, double PredictedPrice)> CurvePoints)
    ImportPlotDataFromJson(string filePath)
    {
        string json = File.ReadAllText(filePath);
        JObject obj = JObject.Parse(json);

        // Parse dataPoints into DataTable
        DataTable dataTable = new DataTable();
        dataTable.Columns.Add("id", typeof(int));
        dataTable.Columns.Add("Quantity", typeof(double));
        dataTable.Columns.Add("Price", typeof(double));

        foreach (var row in obj["dataPoints"])
        {
            int id = row["id"].Value<int>();
            double qty = row["quantity"].Value<double>();
            double price = row["price"].Value<double>();
            dataTable.Rows.Add(id, qty, price);
        }

        // Parse clusterBoundaries
        List<double> clusterBoundaries = obj["clusterBoundaries"].ToObject<List<double>>();

        // Parse bufferZones
        List<List<double>> bufferZones = obj["bufferZones"].ToObject<List<List<double>>>();

        // Parse priceMeans
        List<double> priceMeans = obj["priceMeans"].ToObject<List<double>>();

        // Parse elasticityCurve
        List<(double Quantity, double PredictedPrice)> curvePoints = new();
        foreach (var point in obj["elasticityCurve"])
        {
            double qty = point["quantity"].Value<double>();
            double pred = point["predictedPrice"].Value<double>();
            curvePoints.Add((qty, pred));
        }

        return (dataTable, clusterBoundaries, bufferZones, priceMeans, curvePoints);
    }
}

