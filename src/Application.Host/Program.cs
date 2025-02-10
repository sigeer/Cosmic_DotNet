using Application.Core;
using Application.EF;
using Application.Host;
using Application.Host.Middlewares;
using Application.Host.Models;
using Application.Host.Services;
using Application.Utility.Configs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Text;

// Environment.SetEnvironmentVariable("ms-wz", "D:\\Cosmic\\wz");

var builder = WebApplication.CreateBuilder(args);

// 支持GBK
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
GlobalTools.Encoding = Encoding.GetEncoding("GBK");

// 日志配置
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
#if !DEBUG
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
#endif
    .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Map(
        keySelector: logEvent =>
            logEvent.Properties.TryGetValue("Category", out var category) ? category?.ToString()?.Trim('"') : "Default",
            configure: (category, writeTo) =>
                writeTo.Logger(
                    lg => lg.Filter.ByIncludingOnly(p => p.Level == LogEventLevel.Error).WriteTo.Async(a => a.File($"logs/AllError/Error-.txt", rollingInterval: RollingInterval.Day))
                )
                .WriteTo.Logger(lg => lg.WriteTo.Async(a => a.File($"logs/{category}/All-.txt", rollingInterval: RollingInterval.Day)))
         )
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// 数据库配置
builder.Services.AddDbContext<DBContext>();


//builder.Services.AddQuartz(o => o.AddJobListener(new JobCompleteListener()));
//builder.Services.AddSingleton<TimerManager>();

// 游戏服务
builder.Services.AddSingleton<GameHost>();
builder.Services.AddHostedService<GameHost>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DropdataService>();
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<ServerService>();
builder.Services.AddAutoMapper(typeof(DtoMapper).Assembly);

builder.Services.AddAuthentication(s =>
{
    s.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    s.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    s.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidateIssuer = true,
        ValidIssuer = "cosmic_dotnet",

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthService.SecretKey)),

        ValidateAudience = false
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            //Token expired
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers["Token-Expired"] = "true";
            }

            return Task.CompletedTask;
        },
    };
});

// Api
builder.Services.AddControllers(o =>
    o.Filters.Add<DataWrapperFilter>()
);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddOpenApi();

var app = builder.Build();

var authCode = AuthService.GetAuthCode();
Log.Logger.Information("授权码>>：[" + authCode + "]");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
