namespace VirtualMed.Domain.Patients;

public class Patient
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string Document { get; set; } = default!;
}

