using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Nutrix.Database.Procedures;
using Nutrix.Downloading;
using Nutrix.Importing;
using Nutrix.Web.Dtos;
using Nutrix.Web.IoC;

var builder = WebApplication.CreateBuilder(args);

builder
    .SetupLogging()
    .SetupHangfire()
    .SetupETL();

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

app.MapGet("/search", async ([FromQuery] string q) => 
{
    var results = await new SearchProductProcedure().Execute(new SearchProductInput(q));
    return results
    .Products
    .Select(x => new FoodProductDto(
        x.Id, 
        x.Name, 
        x.Kcal1000g / 10, 
        x.Proteins1000g / 10, 
        x.Fats1000g / 10, 
        x.Carbs1000g / 10, 
        x.Fiber1000g / 10));
});

var jobsClient = app!.Services.GetService<IRecurringJobManagerV2>()!;

jobsClient.AddOrUpdate<ETLManager>(
    $"Download_{nameof(IleWazyDownloader)}", x => x.RunDownloader(nameof(IleWazyDownloader), CancellationToken.None),
    "0 3 * * 2,4"   /*At    03:00          on Tuesday and Thursday.*/);

jobsClient.AddOrUpdate<ETLManager>(
    $"Import_{nameof(IleWazyImporter)}", x => x.RunImporter(nameof(IleWazyImporter), CancellationToken.None),
    "0 4,6 * * 2,4" /*At    04:00, 06:00   on Tuesday and Thursday.*/);

app.Run();
