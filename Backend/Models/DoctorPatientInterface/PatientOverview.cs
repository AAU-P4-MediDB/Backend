namespace Backend.Models;

public class PatientOverview
{
    public Guid uuid { get; set; }
    public string name { get ; set; } = null!;
    public string cpr { get; set; } = null!;
    public string pronouns { get ; set; }
    public DateOnly birthdate { get ; set; }
    public string pfp { get ; set; }
}