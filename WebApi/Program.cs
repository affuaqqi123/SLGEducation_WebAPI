using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Configuration;
using WebApi.DAL;
using System.IO;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using WebApi.Model;
using Microsoft.Extensions.Options;
using System.Security.Principal;
using Microsoft.AspNetCore.ResponseCompression;
using WebApi.Middlewares;
using WebAPI.Middlewares;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Serilog.Sinks.ApplicationInsights.Formatters;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using WebApi.BackGroundTask;


//private static string[]? _compressMimeTypes;
//string[] _compressMimeTypes = new string[] {};
var builder = WebApplication.CreateBuilder(args);
//builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));


//Serilog For Development
IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true).Build();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
builder.Host.UseSerilog();

//Serilog For Production
//IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
//Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
//builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CourseDbConnection")));

builder.Services.Configure<WebApi.Model.EmailModel>(builder.Configuration.GetSection("EConfiguration"));

//Background task 
builder.Services.AddHostedService<BackgroundTask>();


builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["ApplicationInsights:InstrumentationKey"]);

//Configure services for localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");


// For Identity
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//    .AddDefaultTokenProviders();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
    options =>
    {
        options.Password.RequireUppercase = false;
        // Customize allowed characters for the username
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ "; // Add space as an allowed character
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        //ClockSkew = TimeSpan.FromSeconds(0),
        ValidIssuer = builder.Configuration["JwtToken:Issuer"],
        ValidAudience = builder.Configuration["JwtToken:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtToken:SecretKey"]))
    };
});


// Add services to the container.

builder.Services.AddControllers();
//var logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .Enrich.FromLogContext()
//    .CreateLogger();
//builder.Logging.ClearProviders();
//builder.Logging.AddSerilog(logger);
//builder.Services.AddSingleton(Log.Logger);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SLG Education API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

var staticFilesConfig = builder.Configuration.GetSection("AssetFolder");

var fileProviderPath = staticFilesConfig.GetValue<string>("AssetFolderPath");
var requestPath = staticFilesConfig.GetValue<string>("FolderName");

var fileProvider = new PhysicalFileProvider(fileProviderPath);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder =>
        {
            builder.AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowAnyOrigin()
                   .WithExposedHeaders("Content-Disposition");

        });
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\AppData\Keys\"))
    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    });
var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ErrorLoggingMiddleware>();

app.UseStaticFiles(new StaticFileOptions
{
    //FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "assets")),
    FileProvider = fileProvider,
    RequestPath = requestPath
});

//Configure localization
var supportedCultures = new[] {
    new CultureInfo("en-US"),
    //new CultureInfo("fr-FR"),
    new CultureInfo("nb-NO"),
    //new CultureInfo("nn-NO")
    // Add more cultures as needed
};
//app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});


app.UseDefaultFiles();
//app.UsePathBase("/slgeducationapp");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseSerilogRequestLogging();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

//app.MapGet("/localization/{key}", (IStringLocalizer<Program> localizer, string key) =>
//{
//    var localizedString = localizer[key];
//    return Results.Ok(localizedString);
//}).AllowAnonymous();


app.MapControllers();

app.Run();

