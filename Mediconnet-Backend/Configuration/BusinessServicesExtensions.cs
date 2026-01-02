using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mediconnet_Backend.Configuration;

/// <summary>
/// Extensions pour configurer les services métier avancés
/// </summary>
public static class BusinessServicesExtensions
{
    /// <summary>
    /// Ajoute les services de facturation avancée
    /// </summary>
    public static IServiceCollection AddAdvancedBilling(this IServiceCollection services)
    {
        services.AddScoped<IFactureService, FactureAvanceeService>();
        return services;
    }

    /// <summary>
    /// Ajoute les services d'alertes médicales
    /// </summary>
    public static IServiceCollection AddMedicalAlerts(this IServiceCollection services)
    {
        services.AddScoped<IMedicalAlertService, MedicalAlertService>();
        return services;
    }

    /// <summary>
    /// Ajoute les services de gestion des lits
    /// </summary>
    public static IServiceCollection AddBedManagement(this IServiceCollection services)
    {
        services.AddScoped<ILitManagementService, LitManagementService>();
        return services;
    }

    /// <summary>
    /// Ajoute les services de prescriptions électroniques
    /// </summary>
    public static IServiceCollection AddElectronicPrescriptions(this IServiceCollection services)
    {
        services.AddScoped<IPrescriptionElectroniqueService, PrescriptionElectroniqueService>();
        return services;
    }

    /// <summary>
    /// Ajoute les services DMP (Dossier Médical Partagé)
    /// </summary>
    public static IServiceCollection AddDMP(this IServiceCollection services)
    {
        services.AddScoped<IDMPService, DMPService>();
        return services;
    }

    /// <summary>
    /// Ajoute tous les services métier avancés
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddAdvancedBilling();
        services.AddMedicalAlerts();
        services.AddBedManagement();
        services.AddElectronicPrescriptions();
        services.AddDMP();
        
        return services;
    }
}
