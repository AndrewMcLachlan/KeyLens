using System.Reflection;
using Asm.AspNetCore.Authentication;
using Asm.OAuth;
using Asp.Versioning;
using KeyLens;
using KeyLens.Api.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

const string ApiPrefix = "/api";

return Asm.AspNetCore.WebApplicationStart.Run(args, "Asm.KeyLens.Api", AddServices, AddApp, AddHealthChecks);

static void AddServices(WebApplicationBuilder builder)
{
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
        builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
    }

    builder.Services.AddHttpContextAccessor();

    //builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

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

    builder.Services.AddKeyVaultCredentialProviders();
    builder.Services.AddEntraIdCredentialProviders();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddStandardJwtBearer(options =>
    {
        options.Events.OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
            return Task.CompletedTask;
        };

        AzureOAuthOptions oAuthOptions = builder.Configuration.GetSection("OAuth").Get<AzureOAuthOptions>() ?? throw new InvalidOperationException("OAuth config not defined");
        options.OAuthOptions = oAuthOptions;
    });

    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());
}
static void AddApp(WebApplication app)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();

    var api = app.NewVersionedApi().MapGroup($"{ApiPrefix}/v{{version:apiVersion}}").WithOpenApi();

    var v1 = api.HasApiVersion(1.0);

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
