namespace ExpensoServer.Common.Api;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}