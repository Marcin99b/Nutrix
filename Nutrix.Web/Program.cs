using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Nutrix.Commons.ETL;
using Nutrix.Database.Procedures;
using Nutrix.Downloading;
using Nutrix.Importing;
using Nutrix.Web.Dtos;
using Nutrix.Web.IoC;

var builder = WebApplication.CreateBuilder(args);

builder
    .SetupLogging()
    .SetupHangfire()
    .SetupETL()
    .SetupDatabase();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger().UseSwaggerUI();
    app.UseHangfireDashboard();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/search", async ([FromQuery] string q, SearchProductProcedure procedure, CancellationToken ct) =>
    (await procedure.Execute(new SearchProductInput(q), ct))
    .Products.Select(FoodProductDto.FromModel));

var jobsClient = app!.Services.GetService<IRecurringJobManagerV2>()!;

jobsClient.AddOrUpdate<ETLManager>(
    $"Download_{DownloaderSources.IleWazy}", x => x.RunDownloader(DownloaderSources.IleWazy, CancellationToken.None),
    "0 3 * * 2,4"   /*At    03:00          on Tuesday and Thursday.*/);

app.Run();
