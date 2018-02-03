using System.Collections.Generic;

namespace KERBALISM
{
  public static class Features
  {
    public static void Detect()
    {
      // set user-specified features
      Reliability   = Settings.Reliability;
      Signal        = Settings.Signal;
      KCommNet      = Settings.KCommNet && !Settings.Signal;
      AdvancedEC    = Settings.AdvancedEC;
      Science       = Settings.Science;
      SpaceWeather  = Settings.SpaceWeather;
      Automation    = Settings.Automation;

      // force-disable some features based on mods detected
      Reliability &= !Lib.HasAssembly("TestFlight");
      Signal      &= !Lib.HasAssembly("RemoteTech");
      KCommNet    &= !Signal;

      // detect all modifiers in use by current profile
      HashSet<string> modifiers = new HashSet<string>();
      foreach (Rule rule in Profile.rules)
      {
        foreach (string s in rule.modifiers) modifiers.Add(s);
      }
      foreach (Process process in Profile.processes)
      {
        foreach (string s in process.modifiers) modifiers.Add(s);
      }

      // detect features from modifiers
      Radiation = modifiers.Contains("radiation");
      Shielding = modifiers.Contains("shielding");
      LivingSpace = modifiers.Contains("living_space");
      Comfort = modifiers.Contains("comfort");
      Poisoning = modifiers.Contains("poisoning");
      Pressure = modifiers.Contains("pressure");

      // habitat is enabled if any of the values it provides are in use
      Habitat =
           Shielding
        || LivingSpace
        || Poisoning
        || Pressure
        || modifiers.Contains("volume")
        || modifiers.Contains("surface");

      // supplies is enabled if any non-EC supply exist
      Supplies = Profile.supplies.Find(k => k.resource != "ElectricCharge") != null;

      // log features
      Lib.Verbose("features:");
      Lib.Verbose("- Reliability: " + Reliability);
      Lib.Verbose("- Signal: " + Signal);
      Lib.Verbose("- KCommNet: " + KCommNet);
      Lib.Verbose("- AdvancedEC: " + AdvancedEC);
      Lib.Verbose("- Science: " + Science);
      Lib.Verbose("- SpaceWeather: " + SpaceWeather);
      Lib.Verbose("- Automation: " + Automation);
      Lib.Verbose("- Radiation: " + Radiation);
      Lib.Verbose("- Shielding: " + Shielding);
      Lib.Verbose("- LivingSpace: " + LivingSpace);
      Lib.Verbose("- Comfort: " + Comfort);
      Lib.Verbose("- Poisoning: " + Poisoning);
      Lib.Verbose("- Pressure: " + Pressure);
      Lib.Verbose("- Habitat: " + Habitat);
      Lib.Verbose("- Supplies: " + Supplies);
    }

    // user-specified features
    public static bool Reliability;
    public static bool Signal;
    public static bool KCommNet;
    public static bool AdvancedEC;
    public static bool Science;
    public static bool SpaceWeather;
    public static bool Automation;

    // features detected automatically from modifiers
    public static bool Radiation;
    public static bool Shielding;
    public static bool LivingSpace;
    public static bool Comfort;
    public static bool Poisoning;
    public static bool Pressure;

    // features detected in other ways
    public static bool Habitat;
    public static bool Supplies;
  }
}