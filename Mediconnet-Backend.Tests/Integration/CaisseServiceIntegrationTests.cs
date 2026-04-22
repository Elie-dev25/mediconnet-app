using Microsoft.Extensions.Logging;
using Moq;
using Mediconnet_Backend.Core.Entities;
using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Data;
using Mediconnet_Backend.Hubs;
using Mediconnet_Backend.Services;

namespace Mediconnet_Backend.Tests.Integration;

public class CaisseServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CaisseService _caisseService;

    public CaisseServiceIntegrationTests()
    {
        _context = TestDbContextFactory.Create();
        
        var rendezVousServiceMock = new Mock<IRendezVousService>();
        var slotLockServiceMock = new Mock<ISlotLockService>();
        var notificationServiceMock = new Mock<IAppointmentNotificationService>();
        var logger = new Mock<ILogger<CaisseService>>();

        _caisseService = new CaisseService(
            _context,
            logger.Object,
            rendezVousServiceMock.Object,
            slotLockServiceMock.Object,
            notificationServiceMock.Object
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetKpisAsync_WithNoData_ReturnsEmptyKpis()
    {
        // Arrange
        var caissierUserId = 1;

        // Act
        var result = await _caisseService.GetKpisAsync(caissierUserId);

        // Assert
        result.Should().NotBeNull();
        result.NombreTransactionsJour.Should().Be(0);
        result.FacturesEnAttente.Should().Be(0);
        result.CaisseOuverte.Should().BeFalse();
    }

    [Fact]
    public async Task GetKpisAsync_WithActiveSession_ReturnsCaisseOuverte()
    {
        // Arrange
        var caissierUserId = 1;
        var session = new SessionCaisse
        {
            IdCaissier = caissierUserId,
            Statut = "ouverte",
            MontantOuverture = 50000m,
            DateOuverture = DateTime.UtcNow
        };
        _context.SessionsCaisse.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _caisseService.GetKpisAsync(caissierUserId);

        // Assert
        result.Should().NotBeNull();
        result.CaisseOuverte.Should().BeTrue();
        result.SoldeCaisse.Should().Be(50000m);
    }

    [Fact]
    public async Task GetKpisAsync_WithPendingInvoices_ReturnsCorrectCount()
    {
        // Arrange
        var caissierUserId = 1;

        // Factures en attente - using minimal required properties
        _context.Factures.Add(new Facture
        {
            NumeroFacture = "FAC-001",
            TypeFacture = "consultation",
            Statut = "en_attente",
            MontantTotal = 5000m,
            MontantPatient = 5000m,
            DateEcheance = DateTime.UtcNow.AddDays(7)
        });
        _context.Factures.Add(new Facture
        {
            NumeroFacture = "FAC-002",
            TypeFacture = "consultation",
            Statut = "partiel",
            MontantTotal = 10000m,
            MontantPatient = 5000m,
            DateEcheance = DateTime.UtcNow.AddDays(7)
        });
        _context.Factures.Add(new Facture
        {
            NumeroFacture = "FAC-003",
            TypeFacture = "consultation",
            Statut = "payee", // Not pending
            MontantTotal = 5000m,
            MontantPatient = 5000m,
            DateEcheance = DateTime.UtcNow.AddDays(7)
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _caisseService.GetKpisAsync(caissierUserId);

        // Assert
        result.Should().NotBeNull();
        result.FacturesEnAttente.Should().Be(2);
    }
}
