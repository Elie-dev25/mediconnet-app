using Microsoft.EntityFrameworkCore;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Data;

/// <summary>
/// DbContext pour MediConnect
/// Gère la persistance des données des tables existantes
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Utilisateur> Utilisateurs { get; set; }
    DbSet<Patient> Patients { get; set; }
    DbSet<Medecin> Medecins { get; set; }
    DbSet<Infirmier> Infirmiers { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
