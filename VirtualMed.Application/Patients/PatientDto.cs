namespace VirtualMed.Application.Patients;

public class PatientDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string Document { get; set; } = default!;
}

