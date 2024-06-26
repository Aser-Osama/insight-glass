using InsightGlassTest.Server.Data;
using InsightGlassTest.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Security.Claims;

namespace InsightGlassTest.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve);

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var liveDB = Environment.GetEnvironmentVariable("MYSQLCONNSTR_DBLiveConn");
            var connectionString = liveDB;
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 34));

            builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseMySql(connectionString, serverVersion), ServiceLifetime.Scoped);

            builder.Services.AddDbContextFactory<idbcontext>(options =>
                options.UseMySql(connectionString, ServerVersion.Parse("8.0.34-mysql"),
                options => options.EnableRetryOnFailure()), ServiceLifetime.Scoped);

            builder.Services.AddAuthorization();
            builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Lockout.MaxFailedAccessAttempts = 999;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            builder.Services.AddTransient<UserManager<ApplicationUser>>(); // Seeding users

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.MapIdentityApi<ApplicationUser>();

            app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Ok();
            }).RequireAuthorization();

            app.MapGet("/pingauth", (ClaimsPrincipal user) =>
            {
                var email = user.FindFirstValue(ClaimTypes.Email); // Get the user's email from the claim
                var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
                return Results.Json(new { Email = email, Id = id }); // Return the email and id as a JSON response
            }).RequireAuthorization();

            // Configure the HTTP request pipeline
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}
            app.UseSwagger();
            app.UseSwaggerUI();


            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.MapFallbackToFile("/index.html");
            app.UseCors();

            // Minimal await to satisfy compiler
            await Task.CompletedTask;

            app.Run();
        }
    }
}
