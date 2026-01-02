using System.Text;
using AspNetCoreRateLimit;
using FluentValidation;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Configuration;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Services;
using Mediconnet_Backend.Hubs;
using Mediconnet_Backend.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONFIGURATION ====================
// Configure Email Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection(AppSettings.SectionName));

// ==================== SERVICES ====================
// Add Database - MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=mediconnect;User=app;Password=app;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>()
);

// Add Authentication Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordValidationService, PasswordValidationService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Add Admin Management Services
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IServiceManagementService, ServiceManagementService>();

// Add Caisse Services
builder.Services.AddScoped<ICaisseService, CaisseService>();

// Add RendezVous Services
builder.Services.AddScoped<ISlotLockService, SlotLockService>();
builder.Services.AddScoped<IRendezVousService, RendezVousService>();

// Add Medecin Planning Services
builder.Services.AddScoped<IMedecinPlanningService, MedecinPlanningService>();

// Add Medecin Service
builder.Services.AddScoped<IMedecinService, MedecinService>();

// Add Email Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();

// Add Parametre Services
builder.Services.AddScoped<IParametreService, ParametreService>();

// Add Reception Patient Service
builder.Services.AddScoped<IReceptionPatientService, ReceptionPatientService>();

// Add Patient Service
builder.Services.AddScoped<IPatientService, PatientService>();

// Add Consultation Service
builder.Services.AddScoped<IConsultationService, ConsultationService>();

// Add Assurance Service
builder.Services.AddScoped<IAssuranceService, AssuranceService>();

// Add Hospitalisation Service
builder.Services.AddScoped<IHospitalisationService, HospitalisationService>();

// Add Chambre Service (Admin Settings)
builder.Services.AddScoped<IChambreService, ChambreService>();

// Add Pharmacie Stock Service
builder.Services.AddScoped<IPharmacieStockService, PharmacieStockService>();

// Add Data Seeder
builder.Services.AddScoped<DataSeeder>();

// ==================== INFRASTRUCTURE SERVICES ====================
// Repository Pattern & Unit of Work
builder.Services.AddRepositories();

// CQRS Query Handlers
builder.Services.AddCQRS();

// Caching (Memory Cache)
builder.Services.AddCaching();

// Background Jobs
builder.Services.AddBackgroundJobs();

// Health Checks
builder.Services.AddCustomHealthChecks();

// ==================== BUSINESS SERVICES ====================
// Advanced Billing, Medical Alerts, Bed Management, E-Prescriptions, DMP
builder.Services.AddBusinessServices();

// ==================== SECURITY SERVICES ====================
// Data Protection Service for encrypting sensitive medical data
builder.Services.AddDataProtection()
    .SetApplicationName("Mediconnet")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
builder.Services.AddScoped<IDataProtectionService, DataProtectionService>();

// Rate Limiting - Protection against brute force attacks
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 100 },
        new RateLimitRule { Endpoint = "*:/api/auth/login", Period = "1m", Limit = 10 },
        new RateLimitRule { Endpoint = "*:/api/auth/register", Period = "1m", Limit = 5 },
        new RateLimitRule { Endpoint = "*:/api/patient/*", Period = "1m", Limit = 30 },
        new RateLimitRule { Endpoint = "*:/api/consultations/*", Period = "1m", Limit = 30 }
    };
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// FluentValidation - Input validation
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// Add SignalR for real-time updates
builder.Services.AddSignalR();
builder.Services.AddScoped<IAppointmentNotificationService, AppointmentNotificationService>();

// Notification Service (centralized)
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<NotificationIntegrationService>();

// ==================== JWT CONFIGURATION ====================
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "default-super-secret-key-minimum-32-characters-long-!!!";

var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MediConnect",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MediConnectUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Support JWT pour SignalR (WebSocket): token transmis via query string ?access_token=
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/appointments"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ==================== CORS CONFIGURATION ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true) // Autoriser toutes les origines en dev
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ==================== API CONFIGURATION ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MediConnect API",
        Version = "v1",
        Description = "API pour la plateforme hospitaliere MediConnect"
    });
});

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== SEED DATA ====================
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

// ==================== MIDDLEWARE ====================
// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Rate Limiting Middleware - Must be early in the pipeline
app.UseIpRateLimiting();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub for real-time updates
app.MapHub<AppointmentHub>("/hubs/appointments");

// Map Health Checks endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// ==================== RUN ====================
app.Run();
