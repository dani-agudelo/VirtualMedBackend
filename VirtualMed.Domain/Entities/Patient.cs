namespace VirtualMed.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Document { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string BloodType { get; set; }
    public string Gender { get; set; }
    public string Allergies { get; set; }
}

