using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Features.Orders.Commands.Admin.UpdateOrderStatus
{
    public enum OrderAction
    {
        Confirm = 1,
        Ship = 2,
        Deliver = 3
    }
}
