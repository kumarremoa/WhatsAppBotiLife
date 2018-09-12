namespace Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Knowledge")]
    public partial class Knowledge
    {
        public long id { get; set; }

        [StringLength(1000)]
        public string TagName { get; set; }

        public string Answer { get; set; }

        [StringLength(555)]
        public string Description { get; set; }
    }
}
