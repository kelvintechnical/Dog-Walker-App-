using DogWalker.Core.Abstractions;
using DogWalker.Core.Configuration;
using DogWalkerApi.Data;
using DogWalkerApi.Hubs;
using DogWalkerApi.Mapping;
using DogWalkerApi.Options;
using DogWalkerApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using DogWalkerApi.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, AccountUserIdProvider>();
builder.Services.AddScoped<BookingBroadcaster>();
builder.Services.AddAutoMapper(typeof(MauiDomainProfile));

builder.Services.Configure<AppEnvironmentOptions>(builder.Configuration.GetSection(AppEnvironmentOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<DogWalkerApi.Validators.CreateBookingRequestValidator>();

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IDogService, DogService>();

var connectionString = builder.Configuration.GetConnectionString("SqlServer");
if (string.IsNullOrWhiteSpace(connectionString))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Configure SQL Server outside Development.");
    builder.Services.AddDbContext<DogWalkerDbContext>(options => options.UseInMemoryDatabase("DogWalker"));
}
else
{
    builder.Services.AddDbContext<DogWalkerDbContext>(options => options.UseSqlServer(connectionString));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddAccountAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (ResourceAccessException exception)
    {
        context.Response.StatusCode = exception.StatusCode;
        await Results.Problem(statusCode: exception.StatusCode).ExecuteAsync(context);
    }
    catch (KeyNotFoundException)
    {
        await Results.Problem(statusCode: StatusCodes.Status404NotFound).ExecuteAsync(context);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateException)
    {
        await Results.Problem(statusCode: StatusCodes.Status409Conflict).ExecuteAsync(context);
    }
});
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<WalkHub>("/hubs/walks", options => options.CloseOnAuthenticationExpiration = true);

app.Run();

// Exposes the entry point for the real HTTP integration-test host.
public partial class Program { }
