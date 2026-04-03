using AspNetCore.Identity.CosmosDb.Contracts;
using AspNetCore.Identity.CosmosDb.Extensions;
using AspNetCore.Identity.CosmosDb.Passkeys;
using AspNetCore.Identity.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCore.Identity.CosmosDb.Demo;

namespace AspNetCore.Identity.CosmosDb.Demo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Get configuration
            var configuration = builder.Configuration;
            
            // Get Cosmos DB connection string
            var cosmosConnectionString = configuration.GetConnectionString("CosmosDb") 
                ?? throw new InvalidOperationException("Cosmos DB connection string not configured");
            var cosmosDatabaseName = configuration["CosmosDb:DatabaseName"] 
                ?? "AspNetCoreIdentity";

            // Add DbContext
            builder.Services.AddDbContext<CosmosIdentityDbContext>(options =>
            {
                options.UseCosmos(cosmosConnectionString, cosmosDatabaseName);
            });

            // Get passkey server domain
            var passkeyServerDomain = configuration["Passkeys:ServerDomain"];
            if (string.IsNullOrWhiteSpace(passkeyServerDomain))
            {
                if (builder.Environment.IsDevelopment())
                {
                    passkeyServerDomain = "localhost";
                }
                else
                {
                    throw new InvalidOperationException("Passkeys:ServerDomain must be configured outside Development.");
                }
            }

            // Configure passkey behavior
            builder.Services.Configure<IdentityPasskeyOptions>(options =>
            {
                options.ServerDomain = passkeyServerDomain;
                options.AuthenticatorTimeout = TimeSpan.FromMinutes(3);
                options.ChallengeSize = 32;
            });

            // Add Identity services (includes authentication, user/role management, and stores)
            builder.Services
                .AddCosmosIdentity<CosmosIdentityDbContext, IdentityUser, IdentityRole, string>(options =>
                {
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 8;

                    // User settings
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedAccount = true; // Moved from AddDefaultIdentity
                }, TimeSpan.FromHours(8), slidingExpiration: true)
                .AddDefaultTokenProviders()
                .AddDefaultUI();

            // Add repository for manual store operations
            builder.Services.AddScoped<IRepository>(provider =>
            {
                var dbContext = provider.GetRequiredService<CosmosIdentityDbContext>();
                return new CosmosIdentityRepository<CosmosIdentityDbContext, IdentityUser, IdentityRole, string>(dbContext);
            });

            // Add controllers and pages
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // Add CORS if needed
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Register passkey UI integration (provides client script and API options configuration)
            builder.Services.AddCosmosPasskeyUiIntegration(options =>
            {
                options.RoutePrefix = "/identity/passkeys";
                options.ClientScriptPath = "/identity/passkeys/client.js";
                options.RequireAntiforgery = true;
                options.MaxPasskeysPerUser = 100;
                options.MaxPasskeyNameLength = 200;
            });

            var app = builder.Build();

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CosmosIdentityDbContext>();
                await dbContext.Database.EnsureCreatedAsync();
            }

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            // Maps passkey API endpoints (/identity/passkeys/list, /register, /remove, etc.)
            app.MapCosmosPasskeyUiEndpoints<IdentityUser>();
            app.MapControllers();
            app.MapRazorPages();
            app.MapDefaultControllerRoute();

            await app.RunAsync();
        }
    }
}
