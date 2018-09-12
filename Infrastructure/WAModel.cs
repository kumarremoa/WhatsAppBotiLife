namespace Infrastructure
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class WAModel : DbContext
    {
        public WAModel()
            : base("name=WAModel")
        {
        }

        public virtual DbSet<IncomingMessage> IncomingMessages { get; set; }
        public virtual DbSet<OutgoingMessage> OutgoingMessages { get; set; }
        public virtual DbSet<Knowledge> Knowledges { get; set; }

        public virtual DbSet<User> Users { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IncomingMessage>()
                .Property(e => e.messagetext)
                .IsUnicode(false);

            modelBuilder.Entity<IncomingMessage>()
                .Property(e => e.sender)
                .IsUnicode(false);

            modelBuilder.Entity<OutgoingMessage>()
                .Property(e => e.messagetext)
                .IsUnicode(false);

            modelBuilder.Entity<OutgoingMessage>()
                .Property(e => e.receiver)
                .IsUnicode(false);

            modelBuilder.Entity<OutgoingMessage>()
              .Property(e => e.userid)
              .IsUnicode(false);
           
            modelBuilder.Entity<OutgoingMessage>()
                .Property(e => e.divison)
                .IsUnicode(false);

            modelBuilder.Entity<Knowledge>()
             .Property(e => e.TagName)
             .IsUnicode(false);

            modelBuilder.Entity<Knowledge>()
                .Property(e => e.Answer)
                .IsUnicode(false);

            modelBuilder.Entity<Knowledge>()
                .Property(e => e.Description)
                .IsUnicode(false);

            modelBuilder.Entity<User>()
               .Property(e => e.userid)
               .IsUnicode(false);

            modelBuilder.Entity<User>()
                .Property(e => e.password)
                .IsUnicode(false);
        }
    }
}
