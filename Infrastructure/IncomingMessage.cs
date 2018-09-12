namespace Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("IncomingMessage")]
    public partial class IncomingMessage
    {
        [Key]
        public long messageid { get; set; }

        [StringLength(550)]
        public string messagetext { get; set; }

        [StringLength(250)]
        public string sender { get; set; }

        public DateTime? created_date { get; set; }
    }
}
