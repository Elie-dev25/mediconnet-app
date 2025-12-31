namespace Mediconnet_Backend.Controllers.Base;

/// <summary>
/// Contrôleur de base pour les fonctionnalités administratives
/// Hérite de BaseApiController et ajoute des vérifications spécifiques admin
/// </summary>
public abstract class BaseAdminController : BaseApiController
{
    // Les méthodes GetCurrentUserId(), IsAdmin(), CheckAdminAccess() 
    // sont déjà héritées de BaseApiController
}
