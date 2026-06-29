using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
//using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Suparco.Api.Data;
using Suparco.Api.Services;
using Suparco.Api.Helpers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;





builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<JwtTokenHelper>();
builder.Services.AddControllers();

// Initialize the state with values from appsettings.json
var alertState = new AlertConfigState();
alertState.TankLevelThreshold = builder.Configuration.GetValue<double>("Alerting:TankLevelThreshold");
alertState.RecipientEmails.Add(builder.Configuration.GetValue<string>("Alerting:RecipientEmail"));

builder.Services.AddSingleton(alertState);

builder.Services.AddHostedService<AlertMonitorService>();

builder.Services.AddCors(options =>     // <--- ADD THIS
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3001") // The URL of your Next.js app
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// JWT configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });


builder.Services.AddAuthorization();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();

    db.Database.Migrate();
    userService.SeedUsersIfEmpty();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
