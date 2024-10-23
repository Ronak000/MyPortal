
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using MyPortal;
using MyPortal.Data;
using MyPortal.Services;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class Program
{
    public Program(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var Configuration = builder.Configuration;

        builder.Services.AddAuthentication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllersWithViews();
        builder.Services.AddControllers();
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddSingleton<IConfiguration>(Configuration);
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Set the session timeout
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddScoped<UserServices>();
        builder.Services.AddScoped<TokenManager>();
        builder.Services.AddScoped<AccountServices>();

        var conn = Configuration.GetConnectionString("DefaultConnection");
        // sql server connection string
        builder.Services.AddDbContext<DatabaseContext>(option =>
            option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Add services to the container.
        var Serverlocal = new OpenApiServer();
        Serverlocal.Url = "https://localhost:7003";
        //builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAD"));
        builder.Services.AddAuthorization(config =>
       {
           config.AddPolicy("AuthZPolicy", policyBuilder =>
               policyBuilder.Requirements.Add(new ScopeAuthorizationRequirement() { RequiredScopesConfigurationKey = $"AzureAd:Scopes" }));
       });
        

        builder.Services.AddSwaggerGen(options =>
        {
            options.AddServer(Serverlocal);
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "My Portal",
                Description = "An ASP.NET Core Web API for managing My Portal",

            });
            var Instance = Configuration.GetSection("AzureAD").GetValue<string>("Instance");
            var TenantId = Configuration.GetSection("AzureAD").GetValue<string>("TenantId");
            var ClientId = Configuration.GetSection("AzureAD").GetValue<string>("ClientId");
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri($"{Instance}{TenantId}/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri($"{Instance}{TenantId}/oauth2/v2.0/token"),
                        Scopes = { { $"api://{ClientId}/access_as_user", "Access as user" } }
                    }
                }
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                { new OpenApiSecurityScheme
                        {
                         Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2"}
                        },
                    new string[] {}
                }
                });
        }
        );



        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<UserServices>();
            var tokenManager = scope.ServiceProvider.GetRequiredService<TokenManager>();

            // Call methods to fetch tokens or initialize
            userService.InitializeAsync(); // Assuming you have these methods in the service classes

            var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            try
            {
                // Test the connection
                dbContext.Database.CanConnect();
                Console.WriteLine("Database connection successful.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
            }
        }
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.

        }

        app.UseHsts();
        app.UseCors(
                options => options.WithOrigins(
                "https://localhost:7003/")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Disposition")
            );
        app.UseSwagger();
        app.UseSwaggerUI(option =>
        {
            option.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            option.RoutePrefix = "swagger";
        });
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseSession();

        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
        //app.MapControllers();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}