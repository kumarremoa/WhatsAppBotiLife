using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using InfrastructureCores;
using Cores;

namespace InfrastructureCores
{
    public partial class WAI_DBContext : DbContext
    {
        public WAI_DBContext()
        {
        }

        public WAI_DBContext(DbContextOptions<WAI_DBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<IncomingMessage> IncomingMessages { get; set; }
        public virtual DbSet<Knowledge> Knowledges { get; set; }
        public virtual DbSet<OutgoingMessage> OutgoingMessages { get; set; }
        //public virtual DbSet<Subscriber> Subscribers { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Data Source=103.18.133.144;Initial Catalog=WAI_DB;User ID=sa;Password=brucelee2010$");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IncomingMessage>(entity =>
            {
                entity.HasKey(e => e.messageid);

                entity.ToTable("IncomingMessage");

                entity.HasIndex(e => e.messagetext)
                    .HasName("NonClusteredIndex-20180823-100535");

                entity.HasIndex(e => e.sender)
                    .HasName("NonClusteredIndex-20180823-100558");

                entity.Property(e => e.messageid).HasColumnName("messageid");

                entity.Property(e => e.created_date)
                    .HasColumnName("created_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.messagetext)
                    .HasColumnName("messagetext")
                    .HasMaxLength(550)
                    .IsUnicode(false);

                entity.Property(e => e.sender)
                    .HasColumnName("sender")
                    .HasMaxLength(250)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Knowledge>(entity =>
            {
                entity.ToTable("Knowledge");

                entity.Property(e => e.id).HasColumnName("id");

                entity.Property(e => e.Answer).IsUnicode(false);

                entity.Property(e => e.Description)
                    .HasMaxLength(555)
                    .IsUnicode(false);

                entity.Property(e => e.TagName)
                    .HasMaxLength(1000)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<OutgoingMessage>(entity =>
            {
                entity.HasKey(e => e.messageid);

                entity.ToTable("OutgoingMessage");

                entity.Property(e => e.messageid).HasColumnName("messageid");

                entity.Property(e => e.created_date)
                    .HasColumnName("created_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.divison)
                    .HasColumnName("divison")
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.messagetext)
                    .HasColumnName("messagetext")
                    .HasMaxLength(550)
                    .IsUnicode(false);

                entity.Property(e => e.receiver)
                    .HasColumnName("receiver")
                    .HasMaxLength(250)
                    .IsUnicode(false);

                entity.Property(e => e.sent).HasColumnName("sent");

                entity.Property(e => e.userid)
                    .HasColumnName("userid")
                    .HasMaxLength(150)
                    .IsUnicode(false);
            });

            //modelBuilder.Entity<Subscriber>(entity =>
            //{
            //    entity.HasKey(e => e.NoHp);

            //    entity.ToTable("Subscriber");

            //    entity.Property(e => e.NoHp)
            //        .HasColumnName("NoHP")
            //        .HasMaxLength(20)
            //        .IsUnicode(false)
            //        .ValueGeneratedNever();

            //    entity.Property(e => e.Nama)
            //        .HasMaxLength(150)
            //        .IsUnicode(false);
            //});

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.userid)
                    .HasColumnName("userid")
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.password)
                    .HasColumnName("password")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
