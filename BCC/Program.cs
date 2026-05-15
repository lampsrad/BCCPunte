
using BCC;
using BCC.Services;
using BKK.Services;
using Microsoft.Extensions.FileProviders;


gData.connectionKey = Environment.MachineName switch
{
    "XPS" or "ROG" => "Local",
    _ => "Abshost"
};
//gData.connectionKey = "Abshost"; //Uses Absolute Host Server DB
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IDbContextFactory, DbContextFactory>(sp => new DbContextFactory(Environment.MachineName, builder.Configuration));
builder.Services.AddSingleton<State>();
builder.Services.AddScoped<Repo>();
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<SalonImport>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    string url = app.Configuration["Kestrel:Endpoints:Http:Url"];
    //url = $"--start-fullscreen {url}";
    url = $"--start {url}";
    gData.StartBrowser(url);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(gData.photosLocal),
    RequestPath = "/photos"
});
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();
app.Run();
