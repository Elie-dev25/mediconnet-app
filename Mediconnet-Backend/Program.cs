using System.Security.Cryptography;
using System.Text;
using AspNetCoreRateLimit;
using FluentValidation;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Configuration;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Admin;
using Mediconnet_Backend.Services;
using Mediconnet_Backend.Hubs;
using Mediconnet_Backend.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var isAdminSeedMode = args.Contains("--seed-admin");

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

// Add HttpContextAccessor for services that need HTTP context
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddScoped<IUserDetailsService, UserDetailsService>();
builder.Services.AddScoped<IInfirmierManagementService, InfirmierManagementService>();
builder.Services.AddScoped<IAffectationServiceService, AffectationServiceService>();
builder.Services.AddScoped<ICoordinationInterventionService, CoordinationInterventionService>();

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

// Add Standard Chambre Service
builder.Services.AddScoped<IStandardChambreService, StandardChambreService>();

// Add Pharmacie Stock Service
builder.Services.AddScoped<IPharmacieStockService, PharmacieStockService>();

// Add Facturation Avancée Service (PDF, échéanciers, remboursements assurance)
builder.Services.AddScoped<IFactureService, FactureAvanceeService>();

// Add Assurance Couverture Service (calcul couverture par type de prestation)
builder.Services.AddScoped<IAssuranceCouvertureService, AssuranceCouvertureService>();

// Add Facture Assurance Services (PDF generation, Email, Management)
builder.Services.AddScoped<IFacturePdfService, FacturePdfService>();
builder.Services.AddScoped<IFactureEmailService, FactureEmailService>();
builder.Services.AddScoped<IFactureAssuranceService, FactureAssuranceService>();

// Add Medecin Helper Service
builder.Services.AddScoped<IMedecinHelperService, MedecinHelperService>();

// Add Prescription Service (centralized prescription management)
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

// Add Programmation Intervention Service (notifications, blocage créneaux)
builder.Services.AddScoped<ProgrammationInterventionService>();

// Add Bloc Operatoire Service (gestion des blocs opératoires)
builder.Services.AddScoped<IBlocOperatoireService, BlocOperatoireService>();

// Add Data Seeder
builder.Services.AddScoped<DataSeeder>();

// Add Document Storage Service
builder.Services.Configure<DocumentStorageSettings>(
    builder.Configuration.GetSection(DocumentStorageSettings.SectionName));
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();

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
// Limites augmentées pour environnement de développement/test réseau local
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 500 },
        new RateLimitRule { Endpoint = "*:/api/auth/login", Period = "1m", Limit = 50 },
        new RateLimitRule { Endpoint = "*:/api/auth/register", Period = "1m", Limit = 20 },
        new RateLimitRule { Endpoint = "*:/api/patient/*", Period = "1m", Limit = 100 },
        new RateLimitRule { Endpoint = "*:/api/consultations/*", Period = "1m", Limit = 100 }
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
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT secret is not configured. Please set Jwt__Secret in the environment variables.");
}

var key = Encoding.ASCII.GetBytes(jwtSecret);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MediConnect";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MediConnectUsers";

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
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };

        // Support JWT pour SignalR (WebSocket): token transmis via query string ?access_token=
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Autoriser le token via query string pour tous les hubs SignalR
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/hubs/appointments") || path.StartsWithSegments("/hubs/notifications")))
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

if (isAdminSeedMode)
{
    var exitCode = await RunSeedAdminModeAsync(app, args);
    Environment.Exit(exitCode);
}

// ==================== MIDDLEWARE ====================
// Configure HTTP request pipeline
// Swagger activé en dev ET production pour faciliter le débogage
app.UseSwagger();
app.UseSwaggerUI();

// Rate Limiting Middleware - Must be early in the pipeline
app.UseIpRateLimiting();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hubs for real-time updates
app.MapHub<AppointmentHub>("/hubs/appointments");
app.MapHub<NotificationHub>("/hubs/notifications");

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

static async Task<int> RunSeedAdminModeAsync(WebApplication app, string[] args)
{
    var cliArgs = ParseArguments(args);

    if (!cliArgs.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
    {
        Console.Error.WriteLine("❌ Veuillez fournir l'email de l'administrateur avec --email");
        return 1;
    }

    var nom = cliArgs.TryGetValue("nom", out var nomValue) && !string.IsNullOrWhiteSpace(nomValue)
        ? nomValue
        : "Admin";
    var prenom = cliArgs.TryGetValue("prenom", out var prenomValue) && !string.IsNullOrWhiteSpace(prenomValue)
        ? prenomValue
        : "Systeme";
    var telephone = cliArgs.TryGetValue("telephone", out var phoneValue) && !string.IsNullOrWhiteSpace(phoneValue)
        ? phoneValue
        : "000000000";
    var passwordProvided = cliArgs.TryGetValue("password", out var passwordValue) && !string.IsNullOrWhiteSpace(passwordValue);
    var password = passwordProvided ? passwordValue! : GenerateSecurePassword();

    using var scope = app.Services.CreateScope();
    var userService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

    var request = new CreateUserRequest
    {
        Nom = nom,
        Prenom = prenom,
        Email = email,
        Telephone = telephone,
        Password = password,
        Role = "administrateur"
    };

    var (success, message, _) = await userService.CreateUserAsync(request);
    if (!success)
    {
        Console.Error.WriteLine($"❌ {message}");
        return 1;
    }

    Console.WriteLine("✅ Administrateur créé avec succès");
    Console.WriteLine($"    Email     : {email}");
    Console.WriteLine($"    Nom       : {nom} {prenom}");
    Console.WriteLine($"    Téléphone : {telephone}");
    if (!passwordProvided)
    {
        Console.WriteLine($"    Mot de passe généré : {password}");
    }

    return 0;
}

static Dictionary<string, string> ParseArguments(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var current = args[i];
        if (!current.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = current[2..];
        string value = "true";

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = args[i + 1];
            i++;
        }

        result[key] = value;
    }

    return result;
}

static string GenerateSecurePassword(int length = 16)
{
    const string allowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@$!#%";
    var bytes = RandomNumberGenerator.GetBytes(length);
    var chars = new char[length];

    for (var i = 0; i < length; i++)
    {
        chars[i] = allowedChars[bytes[i] % allowedChars.Length];
    }

    return new string(chars);
}
