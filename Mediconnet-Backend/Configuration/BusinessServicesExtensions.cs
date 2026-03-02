using Mediconnet_Backend.Core.Interfaces.Services;
using Mediconnet_Backend.Core.Services;
using Mediconnet_Backend.Services;
using Microsoft.Extensions.Configuration;
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
    /// Ajoute les services DMP (Dossier Médical Partagé)
    /// </summary>
    public static IServiceCollection AddDMP(this IServiceCollection services)
    {
        services.AddScoped<IDMPService, DMPService>();
        return services;
    }

    /// <summary>
    /// Ajoute le service de chiffrement des documents
    /// </summary>
    public static IServiceCollection AddDocumentEncryption(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EncryptionSettings>(configuration.GetSection(EncryptionSettings.SectionName));
        services.AddSingleton<IDocumentEncryptionService, DocumentEncryptionService>();
        return services;
    }

    /// <summary>
    /// Ajoute le service d'audit des consultations
    /// </summary>
    public static IServiceCollection AddConsultationAudit(this IServiceCollection services)
    {
        services.AddScoped<IConsultationAuditService, ConsultationAuditService>();
        return services;
    }

    /// <summary>
    /// Ajoute tous les services métier avancés
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddAdvancedBilling();
        services.AddMedicalAlerts();
        services.AddBedManagement();
        services.AddDMP();
        services.AddConsultationAudit();
        
        if (configuration != null)
        {
            services.AddDocumentEncryption(configuration);
        }
        
        return services;
    }
}
