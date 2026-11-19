using Core.Results;
using MediatR;

namespace Service.Features.Orders.Commands.CheckOut
{
    public sealed record checkOutCommand(
        string street, string City, string? state, string? PostalCode, string Country) 
        :IRequest<Result<Guid>>;
    
}
