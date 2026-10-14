using DogWalker.Core.Models;
using DogWalkerApi.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DogWalkerApi.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FWF_TEST_SQL_CONNECTION")))
            Skip = "Requires a disposable local SQL Server Developer instance; see scripts/start-test-sql.sh.";
    }
}

public class SqlServerAccountTests
{
    [SqlServerFact]
    public async Task Real_sql_server_preserves_legacy_data_and_enforces_identity_bindings()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FWF_TEST_SQL_CONNECTION"));
        // Never use caller's database name: this test owns only its random database.
        builder.InitialCatalog = "FwfM0Tests_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<DogWalkerDbContext>().UseSqlServer(builder.ConnectionString).Options;
        await using var db = new DogWalkerDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            var owner = new Client { FullName = "Legacy owner" };
            db.Clients.Add(owner); await db.SaveChangesAsync();
            // Rehearse the additive script against the same legacy tables, without touching them.
            await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.Accounts");
            var path = Path.Combine(AppContext.BaseDirectory, "M0_AddAccounts.sql");
            var script = await File.ReadAllTextAsync(path);
            await db.Database.ExecuteSqlRawAsync(script);
            await db.Database.ExecuteSqlRawAsync(script);
            Assert.Equal(owner.Id, (await db.Clients.SingleAsync()).Id);
            db.Accounts.Add(new Account { Issuer = ApiFactory.Issuer, Subject = "CaseSensitive", ClientId = owner.Id });
            db.Accounts.Add(new Account { Issuer = ApiFactory.Issuer, Subject = "casesensitive" });
            await db.SaveChangesAsync();
            Assert.Equal(2, await db.Accounts.CountAsync());
            var http = new Microsoft.AspNetCore.Http.HttpContextAccessor {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] {
                        new System.Security.Claims.Claim("iss", ApiFactory.Issuer),
                        new System.Security.Claims.Claim("sub", "CaseSensitive ") }, "test")) } };
            var access = new DogWalkerApi.Security.AccountAccess(db, http);
            var mismatch = await Assert.ThrowsAsync<DogWalkerApi.Security.ResourceAccessException>(() => access.FindAsync());
            Assert.Equal(401, mismatch.StatusCode);
            db.Accounts.Add(new Account { Issuer = ApiFactory.Issuer, Subject = "CaseSensitive" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.Accounts.Add(new Account { Issuer = ApiFactory.Issuer, Subject = "Other", ClientId = owner.Id });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.Accounts.Add(new Account { Issuer = ApiFactory.Issuer, Subject = "MissingOwner", ClientId = Guid.NewGuid() });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            var unclaimed = new Account { Issuer = ApiFactory.Issuer, Subject = "UnclaimedSubject" };
            var admin = new Account { Issuer = ApiFactory.Issuer, Subject = "sql-admin", IsAdministrator = true };
            var verifiedOwner = new Client { FullName = "Verified new owner" };
            db.Accounts.AddRange(unclaimed, admin); db.Clients.Add(verifiedOwner); await db.SaveChangesAsync();
            http.HttpContext!.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] {
                new System.Security.Claims.Claim("iss", ApiFactory.Issuer),
                new System.Security.Claims.Claim("sub", "sql-admin") }, "test"));
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["Authentication:Authority"] = ApiFactory.Issuer }).Build();
            var controller = new DogWalkerApi.Controllers.AccountsController(access, db, config,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DogWalkerApi.Controllers.AccountsController>.Instance);
            var binding = await controller.Bind(new DogWalkerApi.Controllers.BindAccountRequest(
                ApiFactory.Issuer, "UnclaimedSubject ", verifiedOwner.Id, null), CancellationToken.None);
            Assert.IsType<Microsoft.AspNetCore.Mvc.ConflictResult>(binding.Result);
            Assert.Null((await db.Accounts.SingleAsync(a => a.Id == unclaimed.Id)).ClientId);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
