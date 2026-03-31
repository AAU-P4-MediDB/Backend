namespace Backend.Models;

public class PatientAssignmentRequest
{
    public Guid uuid_pt { get; set; }
    public Guid uuid_dr { get; set; }
}