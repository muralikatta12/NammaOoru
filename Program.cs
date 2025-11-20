using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NammaOoru.Data;
using NammaOoru.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Moodly API",
        Version = "v1",
        Description = "Secure login with Email and OTP verification"
    });

    // Add JWT Bearer Authentication to Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter the JWT token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new[] { "Bearer" } }
    });

    // Support multipart/form-data file uploads in Swagger
    c.OperationFilter<NammaOoru.Swagger.FormFileOperationFilter>();

    // Enable XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});


builder.Services.AddScoped<IAuthService,AuthService>();
// Note: IReportService registration is postponed until we verify type resolution in service layer
// Add Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Custom Services
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger and Swagger UI unconditionally for local testing.
// In production you might want to restrict this to Development only.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Moodly API v1");
    c.RoutePrefix = string.Empty; // Serve at root
});

// Serve static files from wwwroot (uploads will be available under /uploads)
app.UseStaticFiles();

app.UseHttpsRedirection();

// Apply CORS
app.UseCors("AllowAll");

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking database schema...");
        
        // Check if Users table exists
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'Users' AND TABLE_SCHEMA = 'dbo'";
            var result = await command.ExecuteScalarAsync();
            
            if (result != null && (int)result == 0)
            {
                logger.LogInformation("Users table does not exist. Creating tables...");
                
                // Create tables manually
                command.CommandText = @"
                    CREATE TABLE [Users] (
                        [Id] INT PRIMARY KEY IDENTITY(1,1),
                        [Email] NVARCHAR(255) NOT NULL UNIQUE,
                        [FirstName] NVARCHAR(100) NOT NULL,
                        [LastName] NVARCHAR(100) NOT NULL,
                        [PasswordHash] NVARCHAR(MAX) NOT NULL,
                        [IsEmailVerified] BIT NOT NULL DEFAULT 0,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedAt] DATETIME2 NULL,
                        [IsActive] BIT NOT NULL DEFAULT 1
                    );
                    
                    CREATE TABLE [OtpVerifications] (
                        [Id] INT PRIMARY KEY IDENTITY(1,1),
                        [UserId] INT NOT NULL,
                        [OtpCode] NVARCHAR(6) NOT NULL,
                        [ExpiresAt] DATETIME2 NOT NULL,
                        [IsUsed] BIT NOT NULL DEFAULT 0,
                        [Email] NVARCHAR(255) NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [FK_OtpVerifications_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
                    );
                    
                    CREATE INDEX [IX_OtpVerifications_UserId] ON [OtpVerifications]([UserId]);
                    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users]([Email]);
                    
                    IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251113000000_InitialCreate')
                    BEGIN
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES ('20251113000000_InitialCreate', '9.0.0');
                    END
                ";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation("Tables created successfully!");
            }
            else
            {
                logger.LogInformation("Users table already exists.");
            }
        }
        
        await connection.CloseAsync();
    }
    catch (Exception ex)
    {
        var setupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        setupLogger.LogError($"An error occurred while setting up the database: {ex.Message}");
        setupLogger.LogError($"Stack trace: {ex.StackTrace}");
    }
}

app.Run();
