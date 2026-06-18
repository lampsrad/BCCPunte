using BCC.Models;
using Microsoft.EntityFrameworkCore;

namespace BCC;

public interface IDbContextFactory
{
    BKKEntities CreateDbContext();
    Task<BKKEntities> CreateDbContextAsync();
}
public class DbContextFactory : IDbContextFactory
{
    private readonly string machineName;
    private readonly IConfiguration configuration;

    public DbContextFactory(string _machinename, IConfiguration _config)
    {
        machineName = _machinename;
        configuration = _config;
    }
    public BKKEntities CreateDbContext()
    {
        string con = string.Empty;
        if (gData.connectionKey == "Abshost")
            con = configuration.GetConnectionString("Abshost");
        else
            con = configuration.GetConnectionString(gData.connectionKey)
                ?.Replace("[ServerName]", $"{machineName}\\{gData.ServerName}")
                ?.Replace("[DatabaseName]", gData.dbName)
                ?? throw new InvalidOperationException($"Connection String for {gData.dbName} not Found.");
        var opsbuilder = new DbContextOptionsBuilder<BKKEntities>();
        opsbuilder.UseSqlServer(con).EnableSensitiveDataLogging();
        var context = new BKKEntities(opsbuilder.Options);
        return context;
    }
    public async Task<BKKEntities> CreateDbContextAsync()
    {
        string con = string.Empty;
        if (gData.connectionKey == "Abshost")
            con = configuration.GetConnectionString("Abshost");
        else
            con = configuration.GetConnectionString(gData.connectionKey)
                ?.Replace("[ServerName]", $"{machineName}\\{gData.ServerName}")
                ?.Replace("[DatabaseName]", gData.dbName)
                ?? throw new InvalidOperationException($"Connection String for {gData.dbName} not Found.");
        var opsbuilder = new DbContextOptionsBuilder<BKKEntities>();
        opsbuilder.UseSqlServer(con).EnableSensitiveDataLogging();
        var context = new BKKEntities(opsbuilder.Options);
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect is false)
            throw new InvalidOperationException("Cannot connect to the database.");
        return context;
    }
}
