using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Domain.Entities.Dimencion
{
    public class DimCustomers
    {
        public int CustomerKey { get; set; }

        public int CustomerId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }
    }
}
