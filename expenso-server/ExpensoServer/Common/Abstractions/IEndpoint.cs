namespace ExpensoServer.Common.Abstractions;

public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}