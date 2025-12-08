using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Domain.Entities.Dimencion
{
    public class DimDate
    {
        public int DateKey { get; set; }

        public DateTime CompleteDate { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string? MonthName { get; set; }
    }
}
