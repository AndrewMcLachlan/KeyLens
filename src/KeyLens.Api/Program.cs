using System.Text.Json.Serialization;
using Asm.AspNetCore.Authentication;
using Asp.Versioning;
using KeyLens;
using KeyLens.Api;
using KeyLens.Api.Handlers;
using KeyLens.Api.Models;
using KeyLens.Api.Services;
using KeyLens.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

const string ApiPrefix = "/api";

return Asm.AspNetCore.WebApplicationStart.Run(args, "Asm.KeyLens.Api", AddServices, AddApp, AddHealthChecks);

static void AddServices(WebApplicationBuilder builder)
{
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IAccessTokenProvider, HttpContextAccessTokenProvider>();

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddOpenApi("v1", options =>
    {
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new(1.0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddNotificationBroker();
    builder.Services.AddHostedService<MonitorService>();

    builder.Services.AddKeyVaultCredentialProviders();
    builder.Services.AddEntraIdCredentialProviders();

    builder.Services.Configure<OAuthOptions>(options => builder.Configuration.Bind("OAuth", options));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddStandardJwtBearer(options =>
    {
        options.Events.OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
            return Task.CompletedTask;
        };

        Asm.OAuth.AzureOAuthOptions oAuthOptions = builder.Configuration.GetSection("OAuth").Get<Asm.OAuth.AzureOAuthOptions>() ?? throw new InvalidOperationException("OAuth config not defined");
        options.OAuthOptions = oAuthOptions;
    });

    builder.Services.AddAuthorization();
}
static void AddApp(WebApplication app)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.MapOpenApi();

    app.UseAuthentication();
    app.UseAuthorization();

    var api = app.NewVersionedApi().MapGroup($"{ApiPrefix}/v{{version:apiVersion}}").WithOpenApi();

    var v1 = api.HasApiVersion(1.0);

    v1.MapGet("/config", GetConfigHandler.Handle)
        .WithName("GetConfig")
        .WithSummary("Get API configuration")
        .WithDescription("Get configuration information for the API.")
        .Produces<Config>(StatusCodes.Status200OK)
        .AllowAnonymous();

    v1.MapGet("/credentials", GetCredentialsHandler.HandleAsync)
        .WithName("GetCredentials")
        .WithSummary("Get credentials")
        .WithDescription("Get a list of credentials from all configured providers.")
        .Produces<List<CredentialRecord>>(StatusCodes.Status200OK)
        .RequireAuthorization();

    app.UseSecurityHeaders();

    app.MapFallbackToFile("/index.html");
}

static void AddHealthChecks(IHealthChecksBuilder healthChecks, WebApplicationBuilder builder)
{
}
