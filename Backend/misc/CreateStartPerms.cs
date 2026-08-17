namespace Backend.Models;

// The 7 data categories a doctor's access to a patient can be scoped to.
// The numeric value is the bit index within a patient's DrPerms int for
// that category: bit `Category` grants read, bit `Category + 7` grants
// write. This order must stay in sync with the read/write toggle lists
// on the frontend permissions page.
public enum PermCategory
{
  Prescription = 0,
  Journal = 1,
  Vitals = 2,
  Diagnosis = 3,
  Appointments = 4,
  PersonInfo = 5,
  LabResults = 6,
}

public enum PermAction
{
  Read,
  Write,
}

static class perms
{
  public static Dictionary<string, int> CreateStartPerms(Guid[] clinicDrGuids)
  {
    Dictionary<string, int> temp = new Dictionary<string, int>();

    foreach (Guid clinicDrGuid in clinicDrGuids)
    {
      temp.Add(clinicDrGuid.ToString(), 0);
    }

    return temp;
  }

  public static bool HasCategoryPermission(int permInt, PermCategory category, PermAction action)
  {
    int bitIndex = (int)category + (action == PermAction.Write ? 7 : 0);
    return ((permInt >> bitIndex) & 1) == 1;
  }

  // True if the doctor has been granted anything at all on this patient
  // (any read or write category) — used to decide whether the patient
  // shows up in the doctor's patient list.
  public static bool HasAnyPermission(int permInt) => permInt > 0;
}
  