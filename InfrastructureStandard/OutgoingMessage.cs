namespace Cores
{
    using System;


    public partial class OutgoingMessage
    {
        
        public long messageid { get; set; }

       
        public string messagetext { get; set; }

       
        public string receiver { get; set; }

        public DateTime? created_date { get; set; }

        public bool? sent { get; set; }

       
        public string userid { get; set; }    

     
        public string divison { get; set; }
    }
}
