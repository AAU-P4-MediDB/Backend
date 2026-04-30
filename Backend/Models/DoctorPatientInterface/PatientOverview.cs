namespace Backend.Models;

public class PatientOverview
{
    public string name { get ; set; } = null!;
    public string cpr { get; set; } = null!;
    public string pronouns { get ; set; }
    public string birthdate { get ; set; }
    public string pfp { get ; set; }
}