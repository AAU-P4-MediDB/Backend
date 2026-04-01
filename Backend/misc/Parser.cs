using System.Collections;

namespace Backend.Models;

public static class Parser
{
  public static bool[] ParsePermInt(int value)
  {
    bool[] bits = new BitArray(new[] { value })
      .Cast<bool>()
      .Take(16)
      .ToArray();
    
    return bits;
  }
}