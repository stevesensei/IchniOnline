using System.Text;
using IchniOnline.Server.Models.Options;
using IchniOnline.Server.Service;
using IchniOnline.Server.Service.Interface;
using IchniOnline.Server.Service.Storage;
using IchniOnline.Server.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var preferredConnectionNames = new[] { "MainDB", "IchniOnline" };
var resolvedConnectionName = preferredConnectionNames.FirstOrDefault(name =>
    !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(name)));

if (resolvedConnectionName is null)
{
    throw new InvalidOperationException("No database connection string found. Expected one of: MainDB, IchniOnline.");
}

var resolvedConnectionString = builder.Configuration.GetConnectionString(resolvedConnectionName)!;

builder.Services.AddControllers();
builder.Services.AddNpgsqlDataSource(resolvedConnectionString);
builder.Services.AddDbContext(builder.Configuration, resolvedConnectionName);
builder.AddRedisClient("cache");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AliyunOssOptions>(builder.Configuration.GetSection(AliyunOssOptions.SectionName));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBeatmapService, BeatmapService>();
builder.Services.AddScoped<IFileStorageService, AliyunOssStorageService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseFileServer();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}