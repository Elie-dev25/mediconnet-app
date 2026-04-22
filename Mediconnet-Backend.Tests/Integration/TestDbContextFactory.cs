using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mediconnet_Backend.Data;

namespace Mediconnet_Backend.Tests.Integration;

/// <summary>
/// Factory pour créer des instances de ApplicationDbContext en mémoire pour les tests
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Crée un nouveau DbContext avec une base de données en mémoire unique
    /// </summary>
    /// <param name="databaseName">Nom unique de la base de données (optionnel)</param>
    /// <returns>ApplicationDbContext configuré pour les tests</returns>
    public static ApplicationDbContext Create(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        
        // S'assurer que la base est créée
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Crée un DbContext avec des données de test pré-remplies
    /// </summary>
    public static ApplicationDbContext CreateWithSeedData(string? databaseName = null)
    {
        var context = Create(databaseName);
        SeedTestData(context);
        return context;
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        // Ajouter des données de test de base
        // Ces données peuvent être utilisées par plusieurs tests
        
        // Note: Les entités spécifiques seront ajoutées dans chaque test
        // selon les besoins
        
        context.SaveChanges();
    }
}
