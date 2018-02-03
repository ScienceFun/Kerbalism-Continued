using System;
using System.Collections.Generic;

namespace KERBALISM 
{
  public static class Telemetry
  {
    // Monitoring all indicator to support life
    public static void Telemetry_Life(this Panel p, Vessel v)
    {
      // avoid corner-case when this is called in a lambda after scene changes
      v = FlightGlobals.FindVessel(v.id);

      // if vessel doesn't exist anymore, leave the panel empty
      if (v == null) return;

      // get info from the cache
      Vessel_Info vi = Cache.VesselInfo(v);

      // if not a valid vessel, leave the panel empty
      if (!vi.is_valid) return;

      // set metadata
      p.Title(Lib.BuildString(Lib.Ellipsis(v.vesselName, 20), " <color=#cccccc>TELEMETRY</color>"));

      // time-out simulation
      if (p.Timeout(vi)) return;

      // get vessel data
      VesselData vd = DB.Vessel(v);

      // get resources
      Vessel_Resources resources = ResourceCache.Get(v);

      // get crew
      var crew = Lib.CrewList(v);

      // draw the content
      Render_Crew(p, crew);
      Render_Greenhouse(p, vi);
      Render_Supplies(p, v, vi, resources);
      Render_Habitat(p, v, vi);
      Render_Environment(p, v, vi);

      // collapse eva kerbal sections into one
      if (v.isEVA) p.Collapse("EVA SUIT");
    }

    static void Render_Environment(Panel p, Vessel v, Vessel_Info vi)
    {
      // don't show env panel in eva kerbals
      if (v.isEVA) return;

      // get all sensor readings
      HashSet<string> readings = new HashSet<string>();
      if (v.loaded)
      {
        foreach(var s in Lib.FindModules<Sensor>(v))
        {
          readings.Add(s.type);
        }
      }
      else
      {
        foreach(ProtoPartModuleSnapshot m in Lib.FindModules(v.protoVessel, "Sensor"))
        {
          readings.Add(Lib.Proto.GetString(m, "type"));
        }
      }
      readings.Remove(string.Empty);

      p.SetSection("ENVIRONMENT");
      foreach(string type in readings)
      {
        p.SetContent(type, Sensor.Telemetry_Content(v, vi, type), Sensor.Telemetry_Tooltip(v, vi, type));
      }
      if (readings.Count == 0) p.SetContent("<i>no sensors installed</i>");
    }

    static void Render_Habitat(Panel p, Vessel v, Vessel_Info vi)
    {
      // if habitat feature is disabled, do not show the panel
      if (!Features.Habitat) return;

      // if vessel is unmanned, do not show the panel
      if (vi.crew_count == 0) return;

      // render panel, add some content based on enabled features
      p.SetSection("HABITAT");
      if (Features.Poisoning) p.SetContent("co2 level", Lib.Color(Lib.HumanReadablePerc(vi.poisoning, "F2"), vi.poisoning > Settings.PoisoningThreshold, "yellow"));
      if (!v.isEVA)
      {
        if (Features.Pressure) p.SetContent("pressure", Lib.HumanReadablePressure(vi.pressure * Sim.PressureAtSeaLevel()));
        if (Features.Shielding) p.SetContent("shielding", Lib.HumanReadableShielding(vi.shielding));
        if (Features.LivingSpace) p.SetContent("living space", Habitat.Living_Space_to_String(vi.living_space));
        if (Features.Comfort) p.SetContent("comfort", vi.comforts.Summary(), vi.comforts.Tooltip());
      }
    }

    static void Render_Supplies(Panel p, Vessel v, Vessel_Info vi, Vessel_Resources resources)
    {
      // for each supply
      int supplies = 0;
      foreach(Supply supply in Profile.supplies)
      {
        // get resource info
        Resource_Info res = resources.Info(v, supply.resource);

        // only show estimate if the resource is present
        if (res.amount <= double.Epsilon) continue;

        // render panel title, if not done already
        if (supplies == 0) p.SetSection("SUPPLIES");

        // rate tooltip
        string rate_tooltip = Math.Abs(res.rate) >= 1e-10 ? Lib.BuildString
        (
          res.rate > 0.0 ? "<color=#00ff00><b>" : "<color=#ff0000><b>",
          Lib.HumanReadableRate(Math.Abs(res.rate)),
          "</b></color>"
        ) : string.Empty;

        // determine label
        string label = supply.resource == "ElectricCharge"
          ? "battery"
          : Lib.SpacesOnCaps(supply.resource).ToLower();

        // finally, render resource supply
        p.SetContent(label, Lib.HumanReadableDuration(res.Depletion(vi.crew_count)), rate_tooltip);
        ++supplies;
      }
    }

    static void Render_Crew(Panel p, List<ProtoCrewMember> crew)
    {
      // do nothing if there isn't a crew, or if there are no rules
      if (crew.Count == 0 || Profile.rules.Count == 0) return;

      // panel section
      p.SetSection("VITALS");

      // for each crew
      foreach(ProtoCrewMember kerbal in crew)
      {
        // get kerbal data from DB
        KerbalData kd = DB.Kerbal(kerbal.name);

        // analyze issues
        UInt32 health_severity = 0;
        UInt32 stress_severity = 0;

        // generate tooltip
        List<string> tooltips = new List<string>();
        foreach(Rule r in Profile.rules)
        {
          // get rule data
          RuleData rd = kd.Rule(r.name);

          // add to the tooltip
          tooltips.Add(Lib.BuildString("<b>", Lib.HumanReadablePerc(rd.problem / r.fatal_threshold), "</b>\t", Lib.SpacesOnCaps(r.name).ToLower()));

          // analyze issue
          if (rd.problem > r.danger_threshold)
          {
            if (!r.breakdown) health_severity = Math.Max(health_severity, 2);
            else stress_severity = Math.Max(stress_severity, 2);
          }
          else if (rd.problem > r.warning_threshold)
          {
            if (!r.breakdown) health_severity = Math.Max(health_severity, 1);
            else stress_severity = Math.Max(stress_severity, 1);
          }
        }
        string tooltip = Lib.BuildString("<align=left />", String.Join("\n", tooltips.ToArray()));

        // generate kerbal name
        string name = kerbal.name.ToLower().Replace(" kerman", string.Empty);

        // render selectable title
        p.SetContent(Lib.Ellipsis(name, 20), kd.disabled ? "<color=#00ffff>HYBERNATED</color>" : string.Empty);
        p.SetIcon(health_severity == 0 ? Icons.health_white : health_severity == 1 ? Icons.health_yellow : Icons.health_red, tooltip);
        p.SetIcon(stress_severity == 0 ? Icons.brain_white : stress_severity == 1 ? Icons.brain_yellow : Icons.brain_red, tooltip);
      }
    }

    static void Render_Greenhouse(Panel p, Vessel_Info vi)
    {
      // do nothing without greenhouses
      if (vi.greenhouses.Count == 0) return;

      // panel section
      p.SetSection("GREENHOUSE");

      // for each greenhouse
      for(int i = 0; i < vi.greenhouses.Count; ++i)
      {
        var greenhouse = vi.greenhouses[i];

        // state string
        string state = greenhouse.issue.Length > 0
          ? Lib.BuildString("<color=yellow>", greenhouse.issue, "</color>")
          : greenhouse.growth >= 0.99
          ? "<color=green>ready to harvest</color>"
          : "growing";

        // tooltip with summary
        string tooltip = greenhouse.growth < 0.99 ? Lib.BuildString
        (
          "<align=left />",
          "time to harvest\t<b>", Lib.HumanReadableDuration(greenhouse.tta), "</b>\n",
          "growth\t\t<b>", Lib.HumanReadablePerc(greenhouse.growth), "</b>\n",
          "natural lighting\t<b>", Lib.HumanReadableFlux(greenhouse.natural), "</b>\n",
          "artificial lighting\t<b>", Lib.HumanReadableFlux(greenhouse.artificial), "</b>"
        ) : string.Empty;

        // render it
        p.SetContent(Lib.BuildString("crop #", (i + 1).ToString()), state, tooltip);

        // issues too, why not
        p.SetIcon(greenhouse.issue.Length == 0 ? Icons.plant_white : Icons.plant_yellow, tooltip);
      }
    }
  }
}