namespace VirtualMed.Application.Patients;

public class PatientDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Document { get; set; } = default!;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = default!;
    public string BloodType { get; set; } = default!;
    public string? Allergies { get; set; }
}

