using Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Service.Features.Cart.Commands.DecreaseCartItem
{
    public sealed class DecreaseCartItemCommand:IRequest<Result>
  {
    [JsonIgnore]
    public Guid ProductId { get; set; }  

    public int Amount { get; set; } = 1;
  }

}
