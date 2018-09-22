namespace Cores
{
    using System;

   
    public partial class IncomingMessage
    {
       
        public long messageid { get; set; }

      
        public string messagetext { get; set; }

     
        public string sender { get; set; }

        public DateTime? created_date { get; set; }
    }
}
