using System.Collections;

namespace Backend.Models;

public static class Parser
{
  public static bool[] ParsePermIntToBool(int value)
  {
    bool[] bits = new BitArray(new[] { value })
      .Cast<bool>()
      .Take(14)
      .ToArray();
    
    return bits;
  }

  public static int ParsePermBoolToInt(bool[] bits)
  {
    if (bits.Length != 14)
      throw new ArgumentException("Expected 16 bits");
    
    int value = 0;

    for (int i = 0; i < bits.Length; i++)
    {
      if (bits[i])
        value |= (1 << i);
    }

    return value;
  }

  public static DateOnly Parsebirthdate(int bday)
  {
    int day = bday / 10000;             
    int month = (bday / 100) % 100;     
    int yearPart = bday % 100;

    int currentYear = DateTime.Today.Year % 100;
    int century = (yearPart <= currentYear) ? (DateTime.Today.Year / 100) * 100 : (DateTime.Today.Year / 100 - 1) * 100;
    int year = century + yearPart;


    return  new DateOnly(year, month, day);
  }
}