namespace Backend.Models;

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
}
  