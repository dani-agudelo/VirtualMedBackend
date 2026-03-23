namespace VirtualMed.Domain.Entities;

public class ClinicalEncounter
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;

    public DateTime StartAt { get; set; }
    public DateTime? EndAt { get; set; }

    public string ChiefComplaint { get; set; } = null!;
    public string? CurrentCondition { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
    public string? Notes { get; set; }
    public bool IsLocked { get; set; }
    public string? RecordingUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Diagnosis> Diagnoses { get; set; } = new List<Diagnosis>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

