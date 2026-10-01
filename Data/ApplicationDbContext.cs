using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Receptionist> Receptionists => Set<Receptionist>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Appointment>()
            .HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor).WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MedicalRecord>()
            .HasOne(m => m.Patient).WithMany(p => p.MedicalRecords)
            .HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MedicalRecord>()
            .HasOne(m => m.Doctor).WithMany(d => d.MedicalRecords)
            .HasForeignKey(m => m.DoctorId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MedicalRecord>()
            .HasOne(m => m.Appointment).WithMany()
            .HasForeignKey(m => m.AppointmentId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Prescription>()
            .HasOne(p => p.MedicalRecord).WithMany(m => m.Prescriptions)
            .HasForeignKey(p => p.MedicalRecordId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Prescription>()
            .HasOne(p => p.Patient).WithMany(p => p.Prescriptions)
            .HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prescription>()
            .HasOne(p => p.Doctor).WithMany(d => d.Prescriptions)
            .HasForeignKey(p => p.DoctorId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PrescriptionItem>()
            .HasOne(item => item.Prescription).WithMany(prescription => prescription.Items)
            .HasForeignKey(item => item.PrescriptionId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bill>()
            .HasOne(b => b.Patient).WithMany(p => p.Bills)
            .HasForeignKey(b => b.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Doctor>()
            .HasOne(d => d.ApplicationUser).WithMany()
            .HasForeignKey(d => d.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Patient>()
            .HasOne(p => p.ApplicationUser).WithMany()
            .HasForeignKey(p => p.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Appointment>().HasIndex(a => new { a.DoctorId, a.AppointmentDate });
        builder.Entity<Appointment>().HasIndex(a => new { a.PatientId, a.AppointmentDate });

        builder.Entity<Bill>()
            .HasOne(b => b.Appointment).WithMany()
            .HasForeignKey(b => b.AppointmentId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<BillItem>()
            .HasOne(item => item.Bill).WithMany(bill => bill.Items)
            .HasForeignKey(item => item.BillId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasOne(payment => payment.Bill).WithMany(bill => bill.Payments)
            .HasForeignKey(payment => payment.BillId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Payment>()
            .HasOne(payment => payment.RecordedBy).WithMany()
            .HasForeignKey(payment => payment.RecordedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
