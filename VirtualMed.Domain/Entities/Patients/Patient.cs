namespace VirtualMed.Domain.Entities.Patients;

public class Patient
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Document { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string BloodType { get; set; }
    public string Gender { get; set; }
    public string Allergies { get; set; }
}

