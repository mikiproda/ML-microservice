using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Data;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/process", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    // Deserialize input JSON
    var inputData = DataHandling.DeserializeStandardizedInput(body);

    // Process
    var (clustered, Boundaries, centroids) = VolumeGrouping.ApplyKMeansClustering(inputData,"Quantity", "MahalanobisOutlier", "VolumeCluster",4); // your function
    var clusteredWithOutliers = OutliersPerVolumeGroup.DetectOutliersPerCluster(clustered, "VolumeCluster", new[] { "Quantity", "Price" }); // your function

    // Return JSON
    string jsonResponse = DataHandling.SerializeProcessedClusterData(clusteredWithOutliers.Data, clusteredWithOutliers.Boundaries);
    return Results.Ok(jsonResponse);
});

app.Run();
