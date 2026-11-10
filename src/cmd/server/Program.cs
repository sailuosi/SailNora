using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SailNora.App.Auth;
using SailNora.App.Scheduling;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Bootstrap;
using SailNora.Foundation.Configuration;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Models;
using SailNora.Foundation.Options;
using SailNora.Platform;
using SailNora.Platform.Auth.Controller;
using SailNora.Platform.Auth.Service;
using SailNora.Platform.Bootstrap;
using SailNora.Platform.Probe.Controller;
using SailNora.Product;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddMooJsonShards(builder.Environment);
builder.Services.AddSailNoraProjectOptions(builder.Configuration);
builder.Services.AddMooSql(builder.Configuration);
builder.Services.AddSailNoraPlatform();
builder.Services.AddSailNoraHangfire(builder.Configuration);

builder.Services.AddSailNoraServiceDiscovery(
    typeof(AuthService),
    typeof(DbInitializer),
    typeof(GlobalExceptionFilter),
    typeof(PermissionAuthorizationHandler),
    typeof(RefreshTokenCleanupJob),
    typeof(ProductAssemblyMarker));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt configuration is required.");

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                    context.HttpContext.Items["TokenExpired"] = true;
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted)
                    return;

                var expired = context.HttpContext.Items.ContainsKey("TokenExpired");
                var code = expired ? ApiResultCodes.TokenExpired : ApiResultCodes.Unauthorized;
                var msg = expired ? "Token 已过期" : "未授权";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResult.Fail(msg, code));
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthRoles.SuperAdmin, policy =>
        policy.RequireAssertion(ctx => AuthPrincipalHelper.BypassesPermissionCheck(ctx.User)));
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    })
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(ProbeController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sail Nora API", Version = "v0.1-skeleton" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Json(new { status = "ok" }));
app.MapControllers();
app.UseSailNoraHangfire();

app.Run();

public partial class Program;
