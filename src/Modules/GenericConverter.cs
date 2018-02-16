// This class is based in Tac mod
namespace KERBALISM
{
  public class GenericConverter
  {
    [KSPField] public string converterName = "Generic Converter";
    [KSPField(isPersistant = true)] public bool converterEnabled = false;
    [KSPField] public bool alwaysActive = false;
    [KSPField] public bool requiresOxygenAtmo = false;
    [KSPField] public float conversionRate = 1f;

  }
}
