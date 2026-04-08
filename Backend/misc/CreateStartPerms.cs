namespace Backend.Models;

static class perms
{
  public static Dictionary<Guid, int> CreateStartPerms(Guid[] clinicDrGuids)
  {
    Dictionary<Guid, int> temp = new Dictionary<Guid, int>();

    foreach (Guid clinicDrGuid in clinicDrGuids)
    {
      temp.Add(clinicDrGuid, 0);
    }

    return temp;
  }
}
  