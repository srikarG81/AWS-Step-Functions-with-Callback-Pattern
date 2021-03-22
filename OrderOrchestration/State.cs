using System;
using System.Collections.Generic;
using System.Text;

namespace OrderOrchestration
{
    public class Order
    {
        public string OrderID { get; set; }
        public List<string> OrderItems { get; set; }
        public int Cost { get; set; } 
        public bool? IsApprovalRequired { get; set; }
        public bool? IsOrderValid { get; set; }
        public bool? IsApproved { get; set; }
        public string Token { get; set; }
    }
}
