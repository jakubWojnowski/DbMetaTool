namespace DbMetaTool.Features.Commands;

public interface IAsyncHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
