namespace Mediconnet_Backend.Core.CQRS.Commands;

/// <summary>
/// Interface de base pour les commandes CQRS (écriture)
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Interface pour les commandes avec résultat
/// </summary>
/// <typeparam name="TResult">Type du résultat</typeparam>
public interface ICommand<TResult> : ICommand
{
}

/// <summary>
/// Interface pour les handlers de commandes sans résultat
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface pour les handlers de commandes avec résultat
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
