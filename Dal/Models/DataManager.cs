using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Dal.Models;

public partial class DataManager : DbContext
{
    public DataManager()
    {
    }

    public DataManager(DbContextOptions<DataManager> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<GownModel> GownModels { get; set; }

    public virtual DbSet<ModelDetails> ModelDetails { get; set; }

    public virtual DbSet<Rental> Rentals { get; set; }

    public virtual DbSet<RentalDetails> RentalDetails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=H:\\GownRental\\Dal\\database\\DB.mdf;Integrated Security=True;Connect Timeout=30");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Appointm__3214EC07B34ADE51");

            entity.ToTable("Appointment");

            entity.Property(e => e.CustomerId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("customerId");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Hour)
                .HasPrecision(5)
                .HasColumnName("hour");
            entity.Property(e => e.NumberOfPeople).HasColumnName("numberOfPeople");

            entity.HasOne(d => d.Customer).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Appointme__custo__403A8C7D");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC07574C3384");

            entity.ToTable("Customer");

            entity.Property(e => e.Id)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<GownModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GownMode__3214EC07DDF2257A");

            entity.ToTable("GownModel");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("color");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Image)
                .HasMaxLength(1)
                .HasColumnName("image");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("name");
        });

        modelBuilder.Entity<ModelDetails>(entity =>
        {
            entity.HasKey(e => e.GownId).HasName("PK__ModelDet__F4B3490DA675EF0D");

            entity.Property(e => e.ModelId).HasColumnName("modelId");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Size).HasColumnName("size");

            entity.HasOne(d => d.Model).WithMany(p => p.ModelDetails)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ModelDeta__model__412EB0B6");
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rental__3214EC07ABA1D90F");

            entity.ToTable("Rental");

            entity.Property(e => e.CustomerId)
                .HasMaxLength(9)
                .IsUnicode(false)
                .HasColumnName("customerId");
            entity.Property(e => e.EventDate).HasColumnName("eventDate");
            entity.Property(e => e.RentalDate).HasColumnName("rentalDate");

            entity.HasOne(d => d.Customer).WithMany(p => p.Rentals)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Rental__customer__4222D4EF");
        });

        modelBuilder.Entity<RentalDetails>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RentalDe__3214EC0781373DC8");

            entity.Property(e => e.GownId).HasColumnName("gownId");
            entity.Property(e => e.RentalId).HasColumnName("rentalId");
            entity.Property(e => e.Size).HasColumnName("size");

            entity.HasOne(d => d.Rental).WithMany(p => p.RentalDetails)
                .HasForeignKey(d => d.RentalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalDet__renta__4316F928");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
