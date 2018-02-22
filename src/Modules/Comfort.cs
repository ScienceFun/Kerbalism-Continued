using System.Collections.Generic;

namespace KERBALISM 
{
  public sealed class Comfort : PartModule, ISpecifics
  {
    [KSPField(isPersistant = true)] 
    public string bonus = string.Empty;                   // the comfort bonus provided

    [KSPField] 
    public string desc = string.Empty;                    // short description shown in part tooltip

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;
    }

    public override string GetInfo()
    {
      return Specs().Info(desc);
    }

    // specifics support
    public Specifics Specs()
    {
      Specifics specs = new Specifics();
      specs.Add("bonus", bonus);
      return specs;
    }
  } 
  
  public sealed class Comforts
  {
    public Comforts(Vessel v, bool env_firm_ground, bool env_not_alone, bool env_call_home)
    {
      // environment factors
      firm_ground = env_firm_ground;
      not_alone = env_not_alone;
      call_home = env_call_home;

      if (v.loaded)
      {
        // scan parts for comfort
        foreach(Comfort c in Lib.FindModules<Comfort>(v))
        {
          switch(c.bonus)
          {
            case "firm-ground": firm_ground = true; break;
            case "not-alone":   not_alone = true;   break;
            case "call-home":   call_home = true;   break;
            case "exercise":    exercise = true;    break;
            case "panorama":    panorama = true;    break;
          }
        }

        // scan parts for gravity ring
        if (ResourceCache.Info(v, "ElectricCharge").amount >= 0.01)
        {
          firm_ground |= Lib.HasModule<GravityRing>(v, k => k.deployed);
        }
      }
      else
      {
        // scan parts for comfort
        foreach(ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Comfort"))
        {
          switch(Lib.Proto.GetString(m, "bonus"))
          {
            case "firm-ground": firm_ground = true; break;
            case "not-alone":   not_alone = true;   break;
            case "call-home":   call_home = true;   break;
            case "exercise":    exercise = true;    break;
            case "panorama":    panorama = true;    break;
          }
        }

        // scan parts for gravity ring
        if (ResourceCache.Info(v, "ElectricCharge").amount >= 0.01)
        {
          firm_ground |= Lib.HasModule(v.protoVessel, "GravityRing", k => Lib.Proto.GetBool(k, "deployed"));
        }
      }

      // calculate factor
      factor = 0.1;
      if (firm_ground) factor += Settings.ComfortFirmGround;
      if (not_alone) factor += Settings.ComfortNotAlone;
      if (call_home) factor += Settings.ComfortCallHome;
      if (exercise) factor += Settings.ComfortExercise;
      if (panorama) factor += Settings.ComfortPanorama;
      factor = Lib.Clamp(factor, 0.1, 1.0);
    }

    public Comforts(List<Part> parts, bool env_firm_ground, bool env_not_alone, bool env_call_home)
    {
      // environment factors
      firm_ground = env_firm_ground;
      not_alone = env_not_alone;
      call_home = env_call_home;

      // for each parts
      foreach(Part p in parts)
      {
        // for each modules in part
        foreach(PartModule m in p.Modules)
        {
          // skip disabled modules
          if (!m.isEnabled) continue;

          // comfort
          if (m.moduleName == "Comfort")
          {
            Comfort c = m as Comfort;
            switch(c.bonus)
            {
              case "firm-ground": firm_ground = true; break;
              case "not-alone":   not_alone = true;   break;
              case "call-home":   call_home = true;   break;
              case "exercise":    exercise = true;    break;
              case "panorama":    panorama = true;    break;
            }
          }
          // gravity ring
          // - ignoring if ec is present or not here
          else if (m.moduleName == "GravityRing")
          {
            GravityRing ring = m as GravityRing;
            firm_ground |= ring.deployed;
          }
        }
      }

      // calculate factor
      factor = 0.1;
      if (firm_ground) factor += Settings.ComfortFirmGround;
      if (not_alone) factor += Settings.ComfortNotAlone;
      if (call_home) factor += Settings.ComfortCallHome;
      if (exercise) factor += Settings.ComfortExercise;
      if (panorama) factor += Settings.ComfortPanorama;
      factor = Lib.Clamp(factor, 0.1, 1.0);
    }

    public string Tooltip()
    {
      const string yes = "<b><color=#00ff00>yes</color></b>";
      const string no = "<b><color=#ff0000>no</color></b>";

      List<string> factors = new List<string>();
      factors.Add("<align=left />");
      if (Settings.ComfortFirmGround > double.Epsilon) factors.Add(Lib.BuildString("firm ground\t", firm_ground ? yes : no));
      if (Settings.ComfortExercise > double.Epsilon) factors.Add(Lib.BuildString("exercise\t\t", exercise ? yes : no));
      if (Settings.ComfortNotAlone > double.Epsilon) factors.Add(Lib.BuildString("not alone\t", not_alone ? yes : no));
      if (Settings.ComfortCallHome > double.Epsilon) factors.Add(Lib.BuildString("call home\t", call_home ? yes : no));
      if (Settings.ComfortPanorama > double.Epsilon) factors.Add(Lib.BuildString("panorama\t", panorama ? yes : no));

      string tooltip_text = string.Empty;
      for (int i = 0; i < factors.Count; i++)
      {
        if (i > 1) tooltip_text += "\n";
        tooltip_text += factors[i];
      }
      return tooltip_text;
    }

    public string Summary()
    {
      if (factor >= 0.99) return "ideal";
      else if (factor >= 0.66) return "good";
      else if (factor >= 0.33) return "modest";
      else if (factor > 0.1) return "poor";
      else return "none";
    }

    public bool firm_ground;
    public bool exercise;
    public bool not_alone;
    public bool call_home;
    public bool panorama;
    public double factor;
  }
}