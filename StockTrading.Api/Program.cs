using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockTrading.Data;
using StockTrading.Models.Domain;
using StockTrading.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models; // For Swagger JWT button
using Serilog; // For Serilog
using StockTradingApi.Hubs; // For SignalR Hub
using StockTradingApi.Middlewares; // For custom exception handling
using StockTrading.Repositories;
using StockTrading.Repository.Interfaces; // Added for repository pattern
using StockTrading.Service.Interfaces; // Added for service interfaces
using StockTrading.Services;
using Microsoft.AspNetCore.SignalR;
using StockTradingApi.Filters;
using Microsoft.AspNetCore.Mvc; // Added for service implementations


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
try
{
    Log.Information("Starting up the application");

    builder.Host.UseSerilog(); // Use Serilog for ASP.NET Core logging

    // Add services to the container.

    // --- Database Configuration (Single DbContext) ---
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


    // --- Identity Configuration ---
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>() // Specify which DbContext Identity uses
        .AddDefaultTokenProviders(); // For password reset, email confirmation tokens

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

    // --- JWT Configuration ---
    var jwtSettings = new JwtSettings();
    builder.Configuration.Bind(nameof(JwtSettings), jwtSettings);
    builder.Services.AddSingleton(jwtSettings); // Register as singleton so it can be injected

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // Only for dev, set to true in production
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Add 5 minutes tolerance
        };
        // For SignalR authentication:
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Get the token from the Authorization header
                var accessToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                // If the request is for our Hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/signalr"))) // Match your SignalR hub path
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // Add Authorization policies if needed (e.g., for roles)
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
        // Add other policies here
    });


    // --- CORS Configuration ---
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin",
            builder => builder.WithOrigins("http://localhost:4200") // Your Angular app URL
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()); // Required for SignalR
    });

    // --- Services/Dependency Injection ---
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IStockService, StockService>();
    builder.Services.AddScoped<ITradeService, TradeService>();
    builder.Services.AddScoped<IPortfolioService, PortfolioService>();
    builder.Services.AddScoped<ITokenValidationService, TokenValidationService>(); // <-- Added this line

    // --- Register Repositories for Dependency Injection ---
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Generic repository
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IStockRepository, StockRepository>();
    builder.Services.AddScoped<ITradeRepository, TradeRepository>();
    builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
    builder.Services.AddScoped<IPortfolioItemRepository, PortfolioItemRepository>();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();

    // --- Swagger/OpenAPI Configuration (for API documentation and testing) ---
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Stock Trading Api", Version = "v1" });
        c.SwaggerDoc("v2", new OpenApiInfo { Title = "Stock Trading Api", Version = "v2" });
        
        // Configure Swagger to use JWT Bearer authentication
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
        });
    });

    // --- Add SignalR ---
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IHubFilter, DbContextHubFilter>();
    builder.Services.AddSingleton(MarketDataCache.Instance);

    // API Versioning Configuration - This allows you to version your API endpoints.
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });


    var app = builder.Build();

    // Configure the HTTP request pipeline.

    // --- Global Exception Handling Middleware ---
    app.UseGlobalExceptionHandling();

    // In development, use Swagger UI for API testing.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Trading API V1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "Stock Trading API V2");
        });
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowSpecificOrigin"); // Use the defined CORS policy

    app.UseAuthentication(); // Must be before UseAuthorization
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<StockHub>("/signalr"); // Map your SignalR Hub to a URL
    app.MapHub<StockHub>("/SubscribeToPortfolio"); // Map your SignalR Hub to a URL

    // --- Apply Migrations and Seed Data on Startup ---
    // This is suitable for development environments. For production, consider
    // running migrations as part of your CI/CD pipeline.
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            using var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply migrations
            //context.Database.Migrate();

            // Seed roles and admin user
            //await new SeedData().Initialize(services, userManager, roleManager);
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database or applying migrations.");
    }


    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
    throw;
}
finally
{
    // Ensures all logs are written before shutdown
    Log.CloseAndFlush();
}
