using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Domain.Entities.Facts
{
    public class FactSales
    {
        public int OrderKey { get; set; }

        public int DateKey { get; set; }

        public int CustomerKey { get; set; }

        public int ProductKey { get; set; }

        public int StatusKey { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
