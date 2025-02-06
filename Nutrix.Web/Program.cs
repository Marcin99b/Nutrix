using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nutrix.Database;
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
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseHangfireDashboard();

app.MapGet("/search", async ([FromQuery] string q, SearchProductProcedure procedure, CancellationToken ct) => 
    (await procedure.Execute(new SearchProductInput(q), ct))
    .Products
    .Select(x => new FoodProductDto(
        x.Id, 
        x.Name, 
        Convert.ToDecimal(x.Kcal1000g) / 10,
        Convert.ToDecimal(x.Proteins1000g) / 10,
        Convert.ToDecimal(x.Fats1000g) / 10,
        Convert.ToDecimal(x.Carbs1000g) / 10,
        Convert.ToDecimal(x.Fiber1000g) / 10)));

var jobsClient = app!.Services.GetService<IRecurringJobManagerV2>()!;

jobsClient.AddOrUpdate<ETLManager>(
    $"Download_{nameof(IleWazyDownloader)}", x => x.RunDownloader(nameof(IleWazyDownloader), CancellationToken.None),
    "0 3 * * 2,4"   /*At    03:00          on Tuesday and Thursday.*/);

jobsClient.AddOrUpdate<ETLManager>(
    $"Import_{nameof(IleWazyImporter)}", x => x.RunImporter(nameof(IleWazyImporter), CancellationToken.None),
    "0 4,6 * * 2,4" /*At    04:00, 06:00   on Tuesday and Thursday.*/);

app.Run();
