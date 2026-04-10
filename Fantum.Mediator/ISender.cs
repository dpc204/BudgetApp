namespace Fantum.Mediator;

public interface ISender
{
  Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
