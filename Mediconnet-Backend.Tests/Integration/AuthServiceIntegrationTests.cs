using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mediconnet_Backend.Core.Configuration;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.DTOs.Auth;
using Mediconnet_Backend.Services;

namespace Mediconnet_Backend.Tests.Integration;

public class AuthServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IEmailConfirmationService> _emailConfirmationServiceMock;

    public AuthServiceIntegrationTests()
    {
        _context = TestDbContextFactory.Create();
        
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _jwtTokenServiceMock
            .Setup(x => x.GenerateTokenAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("test-jwt-token");

        _auditServiceMock = new Mock<IAuditService>();
        _emailConfirmationServiceMock = new Mock<IEmailConfirmationService>();

        var emailSettings = Options.Create(new EmailSettings
        {
            EnableEmailConfirmation = false
        });

        var logger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _context,
            _jwtTokenServiceMock.Object,
            _auditServiceMock.Object,
            _emailConfirmationServiceMock.Object,
            emailSettings,
            logger.Object
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new Utilisateur
        {
            Email = "test@example.com",
            Telephone = "0612345678",
            PasswordHash = passwordHash,
            Nom = "Test",
            Prenom = "User",
            Role = "patient",
            EmailConfirmed = true
        };
        _context.Utilisateurs.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Identifier = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("test-jwt-token");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new Utilisateur
        {
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Nom = "Test",
            Prenom = "User",
            Role = "patient",
            EmailConfirmed = true
        };
        _context.Utilisateurs.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Identifier = "test@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var request = new LoginRequest
        {
            Identifier = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithPhoneNumber_ReturnsToken()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new Utilisateur
        {
            Email = "test@example.com",
            Telephone = "0612345678",
            PasswordHash = passwordHash,
            Nom = "Test",
            Prenom = "User",
            Role = "patient",
            EmailConfirmed = true
        };
        _context.Utilisateurs.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Identifier = "0612345678",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("test-jwt-token");
    }

    [Fact]
    public async Task LoginAsync_WithUnconfirmedEmail_WhenConfirmationRequired_ReturnsRequiresConfirmation()
    {
        // Arrange - Create service with email confirmation enabled
        var emailSettings = Options.Create(new EmailSettings
        {
            EnableEmailConfirmation = true
        });

        var authServiceWithConfirmation = new AuthService(
            _context,
            _jwtTokenServiceMock.Object,
            _auditServiceMock.Object,
            _emailConfirmationServiceMock.Object,
            emailSettings,
            new Mock<ILogger<AuthService>>().Object
        );

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new Utilisateur
        {
            Email = "unconfirmed@example.com",
            PasswordHash = passwordHash,
            Nom = "Test",
            Prenom = "User",
            Role = "patient",
            EmailConfirmed = false // Not confirmed
        };
        _context.Utilisateurs.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Identifier = "unconfirmed@example.com",
            Password = "password123"
        };

        // Act
        var result = await authServiceWithConfirmation.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().BeNull();
        result.RequiresEmailConfirmation.Should().BeTrue();
        result.Message.Should().Be("EMAIL_NOT_CONFIRMED");
    }

    [Fact]
    public async Task RegisterAsync_WithNewUser_CreatesUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            FirstName = "Jean",
            LastName = "Dupont",
            Email = "jean.dupont@example.com",
            Telephone = "0698765432",
            Password = "SecurePass123!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("jean.dupont@example.com");
        result.Nom.Should().Be("Dupont");
        result.Prenom.Should().Be("Jean");
        result.Role.Should().Be("patient");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsNull()
    {
        // Arrange
        var existingUser = new Utilisateur
        {
            Email = "existing@example.com",
            Nom = "Existing",
            Prenom = "User",
            Role = "patient"
        };
        _context.Utilisateurs.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = "existing@example.com",
            Telephone = "0612345678",
            Password = "SecurePass123!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingPhone_ReturnsNull()
    {
        // Arrange
        var existingUser = new Utilisateur
        {
            Email = "other@example.com",
            Telephone = "0612345678",
            Nom = "Existing",
            Prenom = "User",
            Role = "patient"
        };
        _context.Utilisateurs.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@example.com",
            Telephone = "0612345678",
            Password = "SecurePass123!"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ForMedecin_ReturnsTitreAffiche()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new Utilisateur
        {
            Email = "medecin@example.com",
            PasswordHash = passwordHash,
            Nom = "Docteur",
            Prenom = "Martin",
            Role = "medecin",
            EmailConfirmed = true
        };
        _context.Utilisateurs.Add(user);
        await _context.SaveChangesAsync();

        // Add specialite
        var specialite = new Specialite
        {
            NomSpecialite = "Cardiologie"
        };
        _context.Specialites.Add(specialite);
        await _context.SaveChangesAsync();

        // Add medecin
        var medecin = new Medecin
        {
            IdUser = user.IdUser,
            IdSpecialite = specialite.IdSpecialite,
            NumeroOrdre = "MED001"
        };
        _context.Medecins.Add(medecin);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Identifier = "medecin@example.com",
            Password = "password123"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.TitreAffiche.Should().Contain("Cardiologie");
        result.IdSpecialite.Should().Be(specialite.IdSpecialite);
    }
}
