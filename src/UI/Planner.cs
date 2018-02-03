using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using ModuleWheels;

namespace KERBALISM 
{
  public sealed class Planner
  {
    public Planner()
    {
      // left menu style
      leftmenu_style = new GUIStyle(HighLogic.Skin.label);
      leftmenu_style.richText = true;
      leftmenu_style.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
      leftmenu_style.fixedWidth = 80.0f;
      leftmenu_style.stretchHeight = true;
      leftmenu_style.fontSize = 10;
      leftmenu_style.alignment = TextAnchor.MiddleLeft;

      // right menu style
      rightmenu_style = new GUIStyle(leftmenu_style);
      rightmenu_style.alignment = TextAnchor.MiddleRight;

      // quote style
      quote_style = new GUIStyle(HighLogic.Skin.label);
      quote_style.richText = true;
      quote_style.normal.textColor = Color.black;
      quote_style.stretchWidth = true;
      quote_style.stretchHeight = true;
      quote_style.fontSize = 11;
      quote_style.alignment = TextAnchor.LowerCenter;

      // center icon style
      icon_style = new GUIStyle();
      icon_style.alignment = TextAnchor.MiddleCenter;

      // set default body index & situation
      body_index = FlightGlobals.GetHomeBodyIndex();
      situation_index = 2;
      sunlight = true;

      // analyzers
      sim = new Resource_Simulator();
      env = new Environment_Analyzer();
      va = new Vessel_Analyzer();

      // resource panels
      panel_resource = new List<string>();
      Profile.supplies.FindAll(k => k.resource != "ElectricCharge").ForEach(k => panel_resource.Add(k.resource));

      // special panels
      // - stress & radiation panels require that a rule using the living_space/radiation modifier exist (current limitation)
      panel_special = new List<string>();
      if (Features.LivingSpace && Profile.rules.Find(k => k.modifiers.Contains("living_space")) != null) panel_special.Add("qol");
      if (Features.Radiation && Profile.rules.Find(k => k.modifiers.Contains("radiation")) != null) panel_special.Add("radiation");
      if (Features.Reliability) panel_special.Add("reliability");
      if (Features.Signal) panel_special.Add("signal");

      // environment panels
      panel_environment = new List<string>();
      if (Features.Pressure || Features.Poisoning) panel_environment.Add("habitat");
      panel_environment.Add("environment");

      // panel ui
      panel = new Panel();
    }

    public void Update()
    {
      // clear the panel
      panel.Clear();

      // if there is something in the editor
      if (EditorLogic.RootPart != null)
      {
        // get body, situation and altitude multiplier
        CelestialBody body = FlightGlobals.Bodies[body_index];
        string situation = situations[situation_index];
        double altitude_mult = altitude_mults[situation_index];

        // get parts recursively
        List<Part> parts = Lib.GetPartsRecursively(EditorLogic.RootPart);

        // analyze
        env.Analyze(body, altitude_mult, sunlight);
        va.Analyze(parts, sim, env);
        sim.Analyze(parts, env, va);

        // ec panel
        Render_EC(panel);

        // resource panel
        if (panel_resource.Count > 0)
        {
          Render_Resource(panel, panel_resource[resource_index]);
        }

        // special panel
        if (panel_special.Count > 0)
        {
          switch(panel_special[special_index])
          {
            case "qol":         Render_Stress(panel);      break;
            case "radiation":   Render_Radiation(panel);   break;
            case "reliability": Render_Reliability(panel); break;
            case "signal":      Render_Signal(panel);      break;
          }
        }

        // environment panel
        switch(panel_environment[environment_index])
        {
          case "habitat":       Render_Habitat(panel);     break;
          case "environment":   Render_Environment(panel); break;
        }
      }
    }

    public void Render()
    {
      // if there is something in the editor
      if (EditorLogic.RootPart != null)
      {
        // get body, situation and altitude multiplier
        CelestialBody body = FlightGlobals.Bodies[body_index];
        string situation = situations[situation_index];
        double altitude_mult = altitude_mults[situation_index];

        // start header
        GUILayout.BeginHorizontal(Styles.title_container);

        // body selector
        GUILayout.Label(new GUIContent(body.name, "Target body"), leftmenu_style);
        if (Lib.IsClicked()) { body_index = (body_index + 1) % FlightGlobals.Bodies.Count; if (body_index == 0) ++body_index; }
        else if (Lib.IsClicked(1)) { body_index = (body_index - 1) % FlightGlobals.Bodies.Count; if (body_index == 0) body_index = FlightGlobals.Bodies.Count - 1; }

        // sunlight selector
        GUILayout.Label(new GUIContent(sunlight ? Icons.sun_white : Icons.sun_black, "In sunlight/shadow"), icon_style);
        if (Lib.IsClicked()) sunlight = !sunlight;

        // situation selector
        GUILayout.Label(new GUIContent(situation, "Target situation"), rightmenu_style);
        if (Lib.IsClicked()) { situation_index = (situation_index + 1) % situations.Length; }
        else if (Lib.IsClicked(1)) { situation_index = (situation_index == 0 ? situations.Length : situation_index) - 1; }

        // end header
        GUILayout.EndHorizontal();

        // render panel
        panel.Render();
      }
      // if there is nothing in the editor
      else
      {
        // render quote
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.Label("<i>In preparing for space, I have always found that\nplans are useless but planning is indispensable.\nWernher von Kerman</i>", quote_style);
        GUILayout.EndHorizontal();
        GUILayout.Space(10.0f);
      }
    }

    public float Width()
    {
      return 260.0f;
    }

    public float Height()
    {
      if (EditorLogic.RootPart != null)
      {
        return 30.0f + panel.Height(); // header + ui content
      }
      else
      {
        return 66.0f; // quote-only
      }
    }

    void Render_Environment(Panel p)
    {
      string flux_tooltip = Lib.BuildString
      (
        "<align=left /><b>source\t\tflux\t\ttemp</b>\n",
        "solar\t\t", env.solar_flux > 0.0 ? Lib.HumanReadableFlux(env.solar_flux) : "none\t", "\t", Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env.solar_flux)), "\n",
        "albedo\t\t", env.albedo_flux > 0.0 ? Lib.HumanReadableFlux(env.albedo_flux) : "none\t", "\t", Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env.albedo_flux)), "\n",
        "body\t\t", env.body_flux > 0.0 ? Lib.HumanReadableFlux(env.body_flux) : "none\t", "\t", Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env.body_flux)), "\n",
        "background\t", Lib.HumanReadableFlux(Sim.BackgroundFlux()), "\t", Lib.HumanReadableTemp(Sim.BlackBodyTemperature(Sim.BackgroundFlux())), "\n",
        "total\t\t", Lib.HumanReadableFlux(env.total_flux), "\t", Lib.HumanReadableTemp(Sim.BlackBodyTemperature(env.total_flux))
      );
      string atmosphere_tooltip = Lib.BuildString
      (
        "<align=left />",
        "breathable\t\t<b>", Sim.Breathable(env.body) ? "yes" : "no", "</b>\n",
        "pressure\t\t<b>", Lib.HumanReadablePressure(env.body.atmospherePressureSeaLevel), "</b>\n",
        "light absorption\t\t<b>", Lib.HumanReadablePerc(1.0 - env.atmo_factor), "</b>\n",
        "gamma absorption\t<b>", Lib.HumanReadablePerc(1.0 - Sim.GammaTransparency(env.body, 0.0)), "</b>"
      );
      string shadowtime_str = Lib.HumanReadableDuration(env.shadow_period) + " (" + (env.shadow_time * 100.0).ToString("F0") + "%)";

      p.SetSection("ENVIRONMENT", string.Empty, () => p.Prev(ref environment_index, panel_environment.Count), () => p.Next(ref environment_index, panel_environment.Count));
      p.SetContent("temperature", Lib.HumanReadableTemp(env.temperature), env.body.atmosphere && env.landed ? "atmospheric" : flux_tooltip);
      p.SetContent("difference", Lib.HumanReadableTemp(env.temp_diff), "difference between external and survival temperature");
      p.SetContent("atmosphere", env.body.atmosphere ? "yes" : "no", atmosphere_tooltip);
      p.SetContent("shadow time", shadowtime_str, "the time in shadow\nduring the orbit");
    }

    void Render_EC(Panel p)
    {
      // get simulated resource
      Simulated_Resource res = sim.Resource("ElectricCharge");

      // create tooltip
      string tooltip = res.Tooltip();

      // render the panel section
      p.SetSection("ELECTRIC CHARGE");
      p.SetContent("storage", Lib.HumanReadableAmount(res.storage), tooltip);
      p.SetContent("consumed", Lib.HumanReadableRate(res.consumed), tooltip);
      p.SetContent("produced", Lib.HumanReadableRate(res.produced), tooltip);
      p.SetContent("duration", Lib.HumanReadableDuration(res.LifeTime()));
    }

    void Render_Resource(Panel p, string res_name)
    {
      // get simulated resource
      Simulated_Resource res = sim.Resource(res_name);

      // create tooltip
      string tooltip = res.Tooltip();

      // render the panel section
      p.SetSection(Lib.SpacesOnCaps(res_name).ToUpper(), string.Empty, () => p.Prev(ref resource_index, panel_resource.Count), () => p.Next(ref resource_index, panel_resource.Count));
      p.SetContent("storage", Lib.HumanReadableAmount(res.storage), tooltip);
      p.SetContent("consumed", Lib.HumanReadableRate(res.consumed), tooltip);
      p.SetContent("produced", Lib.HumanReadableRate(res.produced), tooltip);
      p.SetContent("duration", Lib.HumanReadableDuration(res.LifeTime()));
    }

    void Render_Stress(Panel p)
    {
      // get first living space rule
      // - guaranteed to exist, as this panel is not rendered if it doesn't
      // - even without crew, it is safe to evaluate the modifiers that use it
      Rule rule = Profile.rules.Find(k => k.modifiers.Contains("living_space"));

      // render title
      p.SetSection("STRESS", string.Empty, () => p.Prev(ref special_index, panel_special.Count), () => p.Next(ref special_index, panel_special.Count));

      // render living space data
      // generate details tooltips
      string living_space_tooltip = Lib.BuildString
      (
        "volume per-capita: <b>", Lib.HumanReadableVolume(va.volume / (double)Math.Max(va.crew_count, 1)), "</b>\n",
        "ideal living space: <b>", Lib.HumanReadableVolume(Settings.IdealLivingSpace), "</b>"
      );
      p.SetContent("living space", Habitat.Living_Space_to_String(va.living_space), living_space_tooltip);

      // render comfort data
      if (rule.modifiers.Contains("comfort"))
      {
        p.SetContent("comfort", va.comforts.Summary(), va.comforts.Tooltip());
      }
      else
      {
        p.SetContent("comfort", "n/a");
      }

      // render pressure data
      if (rule.modifiers.Contains("pressure"))
      {
        string pressure_tooltip = va.pressurized
          ? "Free roaming in a pressurized environment is\nvastly superior to living in a suit."
          : "Being forced inside a suit all the time greatly\nreduce the crew quality of life.\nThe worst part is the diaper.";
        p.SetContent("pressurized", va.pressurized ? "yes" : "no", pressure_tooltip);
      }
      else
      {
        p.SetContent("pressurized", "n/a");
      }

      // render life estimate
      double mod = Modifiers.Evaluate(env, va, sim, rule.modifiers);
      p.SetContent("duration", Lib.HumanReadableDuration(rule.fatal_threshold / (rule.degeneration * mod)));
    }

    void Render_Radiation(Panel p)
    {
      // get first radiation rule
      // - guaranteed to exist, as this panel is not rendered if it doesn't
      // - even without crew, it is safe to evaluate the modifiers that use it
      Rule rule = Profile.rules.Find(k => k.modifiers.Contains("radiation"));

      // detect if it use shielding
      bool use_shielding = rule.modifiers.Contains("shielding");

      // calculate various radiation levels
      var levels = new []
      {
        Math.Max(Radiation.Nominal, (env.surface_rad + va.emitted)),        // surface
        Math.Max(Radiation.Nominal, (env.magnetopause_rad + va.emitted)),   // inside magnetopause
        Math.Max(Radiation.Nominal, (env.inner_rad + va.emitted)),          // inside inner belt
        Math.Max(Radiation.Nominal, (env.outer_rad + va.emitted)),          // inside outer belt
        Math.Max(Radiation.Nominal, (env.heliopause_rad + va.emitted)),     // interplanetary
        Math.Max(Radiation.Nominal, (env.extern_rad + va.emitted)),         // interstellar
        Math.Max(Radiation.Nominal, (env.storm_rad + va.emitted))           // storm
      };

      // evaluate modifiers (except radiation)
      List<string> modifiers_except_radiation = new List<string>();
      foreach(string s in rule.modifiers) { if (s != "radiation") modifiers_except_radiation.Add(s); }
      double mod = Modifiers.Evaluate(env, va, sim, modifiers_except_radiation);

      // calculate life expectancy at various radiation levels
      var estimates = new double[7];
      for(int i=0; i<7; ++i)
      {
        estimates[i] = rule.fatal_threshold / (rule.degeneration * mod * levels[i]);
      }

      // generate tooltip
      var mf = Radiation.Info(env.body).model;
      string tooltip = Lib.BuildString
      (
        "<align=left />",
        "surface\t\t<b>", Lib.HumanReadableDuration(estimates[0]), "</b>\n",
        mf.has_pause ? Lib.BuildString("magnetopause\t<b>", Lib.HumanReadableDuration(estimates[1]), "</b>\n") : "",
        mf.has_inner ? Lib.BuildString("inner belt\t<b>", Lib.HumanReadableDuration(estimates[2]), "</b>\n") : "",
        mf.has_outer ? Lib.BuildString("outer belt\t<b>", Lib.HumanReadableDuration(estimates[3]), "</b>\n") : "",
        "interplanetary\t<b>", Lib.HumanReadableDuration(estimates[4]), "</b>\n",
        "interstellar\t<b>", Lib.HumanReadableDuration(estimates[5]), "</b>\n",
        "storm\t\t<b>", Lib.HumanReadableDuration(estimates[6]), "</b>"
      );

      // render the panel
      p.SetSection("RADIATION", string.Empty, () => p.Prev(ref special_index, panel_special.Count), () => p.Next(ref special_index, panel_special.Count));
      p.SetContent("surface", Lib.HumanReadableRadiation(env.surface_rad + va.emitted), tooltip);
      p.SetContent("orbit", Lib.HumanReadableRadiation(env.magnetopause_rad), tooltip);
      if (va.emitted >= 0.0) p.SetContent("emission", Lib.HumanReadableRadiation(va.emitted), tooltip);
      else p.SetContent("active shielding", Lib.HumanReadableRadiation(-va.emitted), tooltip);
      p.SetContent("shielding", rule.modifiers.Contains("shielding") ? Lib.HumanReadableShielding(va.shielding) : "n/a", tooltip);
    }

    void Render_Reliability(Panel p)
    {
      // evaluate redundancy metric
      // - 0: no redundancy
      // - 0.5: all groups have 2 elements
      // - 1.0: all groups have 3 or more elements
      double redundancy_metric = 0.0;
      foreach(var pair in va.redundancy)
      {
        switch(pair.Value)
        {
          case 1:  break;
          case 2:  redundancy_metric += 0.5 / (double)va.redundancy.Count; break;
          default: redundancy_metric += 1.0 / (double)va.redundancy.Count; break;
        }
      }

      // traduce the redundancy metric to string
      string redundancy_str = string.Empty;
      if (redundancy_metric <= 0.1) redundancy_str = "none";
      else if (redundancy_metric <= 0.33) redundancy_str = "poor";
      else if (redundancy_metric <= 0.66) redundancy_str = "okay";
      else redundancy_str = "great";

      // generate redundancy tooltip
      string redundancy_tooltip = string.Empty;
      if (va.redundancy.Count > 0)
      {
        StringBuilder sb = new StringBuilder();
        foreach(var pair in va.redundancy)
        {
          if (sb.Length > 0) sb.Append("\n");
          sb.Append("<b>");
          switch(pair.Value)
          {
            case 1: sb.Append("<color=red>"); break;
            case 2: sb.Append("<color=yellow>"); break;
            default: sb.Append("<color=green>"); break;
          }
          sb.Append(pair.Value.ToString());
          sb.Append("</color></b>\t");
          sb.Append(pair.Key);
        }
        redundancy_tooltip = Lib.BuildString("<align=left />", sb.ToString());
      }

      // generate repair string and tooltip
      string repair_str = "none";
      string repair_tooltip = string.Empty;
      if (va.crew_engineer)
      {
        repair_str = "engineer";
        repair_tooltip = "The engineer on board should\nbe able to handle all repairs";
      }
      else if (va.crew_capacity == 0)
      {
        repair_str = "safemode";
        repair_tooltip = "We have a chance of repairing\nsome of the malfunctions remotely";
      }

      // render panel
      p.SetSection("RELIABILITY", string.Empty, () => p.Prev(ref special_index, panel_special.Count), () => p.Next(ref special_index, panel_special.Count));
      p.SetContent("malfunctions", Lib.HumanReadableAmount(va.failure_year, "/y"), "average case estimate\nfor the whole vessel");
      p.SetContent("high quality", Lib.HumanReadablePerc(va.high_quality), "percentage of high quality components");
      p.SetContent("redundancy", redundancy_str, redundancy_tooltip);
      p.SetContent("repair", repair_str, repair_tooltip);
    }

    // TODO :Add suport to CommNet
    void Render_Signal(Panel p)
    {
      // range tooltip
      string range_tooltip = "";
      if (va.direct_dist > double.Epsilon)
      {
        if (va.direct_dist < va.home_dist_min) range_tooltip = "<color=#ff0000>out of range</color>";
        else if (va.direct_dist < va.home_dist_max) range_tooltip = "<color=#ffff00>partially out of range</color>";
        else range_tooltip = "<color=#00ff00>in range</color>";
        if (va.home_dist_max > double.Epsilon) //< if not landed at home
        {
          if (Math.Abs(va.home_dist_min - va.home_dist_max) <= double.Epsilon)
          {
            range_tooltip += Lib.BuildString("\ntarget distance: <b>", Lib.HumanReadableRange(va.home_dist_min), "</b>");
          }
          else
          {
            range_tooltip += Lib.BuildString
            (
              "\ntarget distance (min): <b>", Lib.HumanReadableRange(va.home_dist_min), "</b>",
              "\ntarget distance (max): <b>", Lib.HumanReadableRange(va.home_dist_max), "</b>"
            );
          }
        }
      }
      else if (va.crew_capacity == 0)
      {
        range_tooltip = "<color=#ff0000>no antenna on unmanned vessel</color>";
      }

      // data rate tooltip
      string rate_tooltip = va.direct_rate > double.Epsilon
        ? Lib.BuildString
        (
          "<align=left />",
          "<i>data transmission rate at target distance</i>\n\n",
          "<b>data size</b>\t<b>transmission time</b>",
          "\n250Mb\t\t", Lib.HumanReadableDuration(250.0 / va.direct_rate),
          "\n500Mb\t\t", Lib.HumanReadableDuration(500.0 / va.direct_rate),
          "\n1Gb\t\t", Lib.HumanReadableDuration(1000.0 / va.direct_rate),
          "\n2Gb\t\t", Lib.HumanReadableDuration(2000.0 / va.direct_rate),
          "\n4Gb\t\t", Lib.HumanReadableDuration(4000.0 / va.direct_rate),
          "\n8Gb\t\t", Lib.HumanReadableDuration(8000.0 / va.direct_rate)
        ) : string.Empty;

      // transmission cost tooltip
      string cost_tooltip = va.direct_cost > double.Epsilon
        ? "the <b>ElectricCharge</b> per-second consumed\nfor data transmission directly to <b>DSN</b>"
        : string.Empty;

      // indirect tooltip
      string indirect_tooltip = va.indirect_dist > double.Epsilon
        ? Lib.BuildString
        (
          "<align=left />",
          "<i>inter-vessel communication capabilities</i>\n\n",
          "range (max)\t<b>", Lib.HumanReadableRange(va.indirect_dist), "</b>\n",
          "rate (best)\t<b>", Lib.HumanReadableDataRate(va.indirect_rate), "</b>\n",
          "cost\t\t<b>", va.indirect_cost.ToString("F2"), " EC/s", "</b>"
        ) : string.Empty;

      // render the panel
      p.SetSection("SIGNAL", string.Empty, () => p.Prev(ref special_index, panel_special.Count), () => p.Next(ref special_index, panel_special.Count));
      p.SetContent("range", Lib.HumanReadableRange(va.direct_dist), range_tooltip);
      p.SetContent("rate", Lib.HumanReadableDataRate(va.direct_rate), rate_tooltip);
      p.SetContent("cost", va.direct_cost > double.Epsilon ? Lib.BuildString(va.direct_cost.ToString("F2"), " EC/s") : "none", cost_tooltip);
      p.SetContent("inter-vessel", va.indirect_dist > double.Epsilon ? "yes" : "no", indirect_tooltip);
    }

    void Render_Habitat(Panel p)
    {
      Simulated_Resource atmo_res = sim.Resource("Atmosphere");
      Simulated_Resource waste_res = sim.Resource("WasteAtmosphere");

      // generate tooltips
      string atmo_tooltip = atmo_res.Tooltip();
      string waste_tooltip = waste_res.Tooltip(true);

      // generate status string for scrubbing
      string waste_status = !Features.Poisoning                   //< feature disabled
        ? "n/a"
        : waste_res.produced <= double.Epsilon                    //< unnecessary
        ? "not required"
        : waste_res.consumed <= double.Epsilon                    //< no scrubbing
        ? "<color=#ffff00>none</color>"
        : waste_res.produced > waste_res.consumed * 1.001         //< insufficient scrubbing
        ? "<color=#ffff00>inadequate</color>"
        : "good";                                                 //< sufficient scrubbing

      // generate status string for pressurization
      string atmo_status = !Features.Pressure                     //< feature disabled
        ? "n/a"
        : atmo_res.consumed <= double.Epsilon                     //< unnecessary
        ? "not required"
        : atmo_res.produced <= double.Epsilon                     //< no pressure control
        ? "none"
        : atmo_res.consumed > atmo_res.produced * 1.001           //< insufficient pressure control
        ? "<color=#ffff00>inadequate</color>"
        : "good";                                                 //< sufficient pressure control

      p.SetSection("HABITAT", string.Empty, () => p.Prev(ref environment_index, panel_environment.Count), () => p.Next(ref environment_index, panel_environment.Count));
      p.SetContent("volume", Lib.HumanReadableVolume(va.volume), "volume of enabled habitats");
      p.SetContent("surface", Lib.HumanReadableSurface(va.surface), "surface of enabled habitats");
      p.SetContent("scrubbing", waste_status, waste_tooltip);
      p.SetContent("pressurization", atmo_status, atmo_tooltip);
    }

    // store situations and altitude multipliers
    string[] situations = { "Landed", "Low Orbit", "Orbit", "High Orbit" };
    double[] altitude_mults = { 0.0, 0.33, 1.0, 3.0 };

    // styles
    GUIStyle leftmenu_style;
    GUIStyle rightmenu_style;
    GUIStyle quote_style;
    GUIStyle icon_style;

    // analyzers
    Resource_Simulator sim = new Resource_Simulator();
    Environment_Analyzer env = new Environment_Analyzer();
    Vessel_Analyzer va = new Vessel_Analyzer();

    // panel arrays
    List<string> panel_resource;
    List<string> panel_special;
    List<string> panel_environment;

    // body/situation/sunlight indexes
    int body_index;
    int situation_index;
    bool sunlight;

    // panel indexes
    int resource_index;
    int special_index;
    int environment_index;

    // panel ui
    Panel panel;
  }

  // analyze the environment
  public sealed class Environment_Analyzer
  {
    public void Analyze(CelestialBody body, double altitude_mult, bool sunlight)
    {
      // shortcuts
      CelestialBody sun = FlightGlobals.Bodies[0];

      this.body = body;
      altitude = body.Radius * altitude_mult;
      landed = altitude <= double.Epsilon;
      breathable = Sim.Breathable(body) && landed;
      atmo_factor = Sim.AtmosphereFactor(body, 0.7071);
      sun_dist = Sim.Apoapsis(Lib.PlanetarySystem(body)) - sun.Radius - body.Radius;
      Vector3d sun_dir = (sun.position - body.position).normalized;
      solar_flux = sunlight ? Sim.SolarFlux(sun_dist) * (landed ? atmo_factor : 1.0) : 0.0;
      albedo_flux = sunlight ? Sim.AlbedoFlux(body, body.position + sun_dir * (body.Radius + altitude)) : 0.0;
      body_flux = Sim.BodyFlux(body, altitude);
      total_flux = solar_flux + albedo_flux + body_flux + Sim.BackgroundFlux();
      temperature = !landed || !body.atmosphere ? Sim.BlackBodyTemperature(total_flux) : body.GetTemperature(0.0);
      temp_diff = Sim.TempDiff(temperature, body, landed);
      orbital_period = Sim.OrbitalPeriod(body, altitude);
      shadow_period = Sim.ShadowPeriod(body, altitude);
      shadow_time = shadow_period / orbital_period;

      var rb = Radiation.Info(body);
      var sun_rb = Radiation.Info(sun);
      gamma_transparency = Sim.GammaTransparency(body, 0.0);
      extern_rad = Settings.ExternRadiation ;
      heliopause_rad = extern_rad + sun_rb.radiation_pause;
      magnetopause_rad = heliopause_rad + rb.radiation_pause;
      inner_rad = magnetopause_rad + rb.radiation_inner;
      outer_rad = magnetopause_rad + rb.radiation_outer;
      surface_rad = magnetopause_rad * gamma_transparency;
      storm_rad = heliopause_rad + Settings.StormRadiation * (solar_flux > double.Epsilon ? 1.0 : 0.0);
    }

    public CelestialBody body;                            // target body
    public double altitude;                               // target altitude
    public bool landed;                                   // true if landed
    public bool breathable;                               // true if inside breathable atmosphere
    public double atmo_factor;                            // proportion of sun flux not absorbed by the atmosphere
    public double sun_dist;                               // distance from the sun
    public double solar_flux;                             // flux received from the sun (consider atmospheric absorption)
    public double albedo_flux;                            // solar flux reflected from the body
    public double body_flux;                              // infrared radiative flux from the body
    public double total_flux;                             // total flux at vessel position
    public double temperature;                            // vessel temperature
    public double temp_diff;                              // average difference from survival temperature
    public double orbital_period;                         // length of orbit
    public double shadow_period;                          // length of orbit in shadow
    public double shadow_time;                            // proportion of orbit that is in shadow

    public double gamma_transparency;                     // proportion of radiation not blocked by atmosphere
    public double extern_rad;                             // environment radiation outside the heliopause
    public double heliopause_rad;                         // environment radiation inside the heliopause
    public double magnetopause_rad;                       // environment radiation inside the magnetopause
    public double inner_rad;                              // environment radiation inside the inner belt
    public double outer_rad;                              // environment radiation inside the outer belt
    public double surface_rad;                            // environment radiation on the surface of the body
    public double storm_rad;                              // environment radiation during a solar storm, inside the heliopause
  }

  // analyze the vessel (excluding resource-related stuff)
  public sealed class Vessel_Analyzer
  {
    public void Analyze(List<Part> parts, Resource_Simulator sim, Environment_Analyzer env)
    {
      // note: vessel analysis require resource analysis, but at the same time resource analysis
      // require vessel analysis, so we are using resource analysis from previous frame (that's okay)
      // in the past, it was the other way around - however that triggered a corner case when va.comforts
      // was null (because the vessel analysis was still never done) and some specific rule/process
      // in resource analysis triggered an exception, leading to the vessel analysis never happening
      // inverting their order avoided this corner-case

      Analyze_Crew(parts);
      Analyze_Habitat(sim, env);
      Analyze_Radiation(parts, sim);
      Analyze_Reliability(parts);
      Analyze_Signal(parts, env);
      Analyze_QOL(parts, sim, env);
    }

    void Analyze_Crew(List<Part> parts)
    {
      // get number of kerbals assigned to the vessel in the editor
      // note: crew manifest is not reset after root part is deleted
      var manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
      List<ProtoCrewMember> crew = manifest.GetAllCrew(false).FindAll(k => k != null);
      crew_count = (uint)crew.Count;
      crew_engineer = crew.Find(k => k.trait == "Engineer") != null;
      crew_scientist = crew.Find(k => k.trait == "Scientist") != null;
      crew_pilot = crew.Find(k => k.trait == "Pilot") != null;

      crew_engineer_maxlevel = 0;
      crew_scientist_maxlevel = 0;
      crew_pilot_maxlevel = 0;
      foreach (ProtoCrewMember c in crew)
      {
        switch(c.trait)
        {
          case "Engineer":  crew_engineer_maxlevel = Math.Max(crew_engineer_maxlevel, (uint)c.experienceLevel); break;
          case "Scientist": crew_scientist_maxlevel = Math.Max(crew_scientist_maxlevel, (uint)c.experienceLevel); break;
          case "Pilot":     crew_pilot_maxlevel = Math.Max(crew_pilot_maxlevel, (uint)c.experienceLevel); break;
        }
      }

      // scan the parts
      crew_capacity = 0;
      foreach(Part p in parts)
      {
        // accumulate crew capacity
        crew_capacity += (uint)p.CrewCapacity;
      }

      // if the user press ALT, the planner consider the vessel crewed at full capacity
      if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) crew_count = crew_capacity;
    }

    void Analyze_Habitat(Resource_Simulator sim, Environment_Analyzer env)
    {
      // calculate total volume
      volume = sim.Resource("Atmosphere").capacity;

      // calculate total surface
      surface = sim.Resource("Shielding").capacity;

      // determine if the vessel has pressure control capabilities
      pressurized = sim.Resource("Atmosphere").produced > 0.0 || env.breathable;

      // determine if the vessel has scrubbing capabilities
      scrubbed = sim.Resource("WasteAtmosphere").consumed > 0.0 || env.breathable;
    }

    void Analyze_Radiation(List<Part> parts, Resource_Simulator sim)
    {
      // scan the parts
      emitted = 0.0;
      foreach(Part p in parts)
      {
        // for each module
        foreach(PartModule m in p.Modules)
        {
          // skip disabled modules
          if (!m.isEnabled) continue;

          // accumulate emitter radiation
          if (m.moduleName == "Emitter")
          {
            Emitter emitter = m as Emitter;

            emitted += emitter.running ? emitter.radiation : 0.0;
          }
        }
      }

      // calculate shielding factor
      double amount = sim.Resource("Shielding").amount;
      double capacity = sim.Resource("Shielding").capacity;
      shielding = (capacity > double.Epsilon ? amount / capacity : 1.0) * Settings.ShieldingEfficiency;
    }

    void Analyze_Reliability(List<Part> parts)
    {
      // reset data
      high_quality = 0.0;
      components = 0;
      failure_year = 0.0;
      redundancy = new Dictionary<string, int>();

      // scan the parts
      double year_time = 60.0 * 60.0 * Lib.HoursInDay() * Lib.DaysInYear();
      foreach(Part p in parts)
      {
        // for each module
        foreach(PartModule m in p.Modules)
        {
          // skip disabled modules
          if (!m.isEnabled) continue;

          // malfunctions
          if (m.moduleName == "Reliability")
          {
            Reliability reliability = m as Reliability;

            // calculate mtbf
            double mtbf = reliability.mtbf * (reliability.quality ? Settings.QualityScale : 1.0);

            // accumulate failures/y
            failure_year += year_time / mtbf;

            // accumulate high quality percentage
            high_quality += reliability.quality ? 1.0 : 0.0;

            // accumulate number of components
            ++components;

            // compile redundancy data
            if (reliability.redundancy.Length > 0)
            {
              int count = 0;
              if (redundancy.TryGetValue(reliability.redundancy, out count))
              {
                redundancy[reliability.redundancy] = count + 1;
              }
              else
              {
                redundancy.Add(reliability.redundancy, 1);
              }
            }

          }
        }
      }

      // calculate high quality percentage
      high_quality /= (double)Math.Max(components, 1u);
    }

    // TODO: Add Support to CommNet
    void Analyze_Signal(List<Part> parts, Environment_Analyzer env)
    {
      // approximate min/max distance between home and target body
      CelestialBody home = FlightGlobals.GetHomeBody();
      home_dist_min = 0.0;
      home_dist_max = 0.0;
      if (env.body == home)
      {
        home_dist_min = env.altitude;
        home_dist_max = env.altitude;
      }
      else if (env.body.referenceBody == home)
      {
        home_dist_min = Sim.Periapsis(env.body);
        home_dist_max = Sim.Apoapsis(env.body);
      }
      else
      {
        double home_p = Sim.Periapsis(Lib.PlanetarySystem(home));
        double home_a = Sim.Apoapsis(Lib.PlanetarySystem(home));
        double body_p = Sim.Periapsis(Lib.PlanetarySystem(env.body));
        double body_a = Sim.Apoapsis(Lib.PlanetarySystem(env.body));
        home_dist_min = Math.Min(Math.Abs(home_a - body_p), Math.Abs(home_p - body_a));
        home_dist_max = home_a + body_a;
      }

      // scan the parts
      direct_dist = 0.0;
      direct_rate = 0.0;
      direct_cost = 0.0;
      indirect_dist = 0.0;
      indirect_rate = 0.0;
      indirect_cost = 0.0;
      foreach(Part p in parts)
      {
        // for each module
        foreach(PartModule m in p.Modules)
        {
          // skip disabled modules
          if (!m.isEnabled) continue;

          // antenna
          // - we consider them even if not extended, for ease of use
          //   and because of module animator behaviour in editor
          if (m.moduleName == "Antenna")
          {
            Antenna antenna = m as Antenna;

            // calculate direct range/rate/cost
            direct_dist = Math.Max(direct_dist, antenna.dist);
            direct_rate += Antenna.Calculate_Rate(home_dist_min, antenna.dist, antenna.rate);
            direct_cost += antenna.cost;

            // calculate indirect range/rate/cost
            if (antenna.type == KAntennaType.low_gain)
            {
              indirect_dist = Math.Max(indirect_dist, antenna.dist);
              indirect_rate += antenna.rate; //< best case
              indirect_cost += antenna.cost;
            }
          }
        }
      }
    }

    // TODO: Add support to others confort mods
    void Analyze_QOL(List<Part> parts, Resource_Simulator sim, Environment_Analyzer env)
    {
      // calculate living space factor
      living_space = Lib.Clamp((volume / (double)Math.Max(crew_count, 1u)) / Settings.IdealLivingSpace, 0.1, 1.0);

      // calculate comfort factor
      comforts = new Comforts(parts, env.landed, crew_count > 1, direct_rate > 0.0 || !Features.Signal);
    }

    // general
    public uint   crew_count;                             // crew member on board
    public uint   crew_capacity;                          // crew member capacity
    public bool   crew_engineer;                          // true if an engineer is among the crew
    public bool   crew_scientist;                         // true if a scientist is among the crew
    public bool   crew_pilot;                             // true if a pilot is among the crew
    public uint   crew_engineer_maxlevel;                 // experience level of top enginner on board
    public uint   crew_scientist_maxlevel;                // experience level of top scientist on board
    public uint   crew_pilot_maxlevel;                    // experience level of top pilot on board

    // habitat
    public double volume;                                 // total volume in m^3
    public double surface;                                // total surface in m^2
    public bool   pressurized;                            // true if the vessel has pressure control capabilities
    public bool   scrubbed;                               // true if the vessel has co2 scrubbing capabilities

    // radiation related
    public double emitted;                                // amount of radiation emitted by components
    public double shielding;                              // shielding factor

    // quality-of-life related
    public double living_space;                           // living space factor
    public Comforts comforts;                             // comfort info

    // reliability-related
    public uint   components;                             // number of components that can fail
    public double high_quality;                           // percentual of high quality components
    public double failure_year;                           // estimated failures per-year, averaged per-component
    public Dictionary<string, int> redundancy;            // number of components per redundancy group

    // signal-related
    public double direct_dist;                            // max comm range to DSN
    public double direct_rate;                            // data transmission rate to DSN from target destination
    public double direct_cost;                            // ec required for transmission to DSN
    public double indirect_dist;                          // max comm range to other vessels
    public double indirect_rate;                          // best-case data transmission rate to other vessels
    public double indirect_cost;                          // ec required for transmission to other vessels
    public double home_dist_min;                          // best-case distance from target to home body
    public double home_dist_max;                          // worst-case distance from target to home body
  }

  // simulate resource consumption & production
  public class Resource_Simulator
  {
    public void Analyze(List<Part> parts, Environment_Analyzer env, Vessel_Analyzer va)
    {
      // clear previous resource state
      resources.Clear();

      // get amount and capacity from parts
      foreach(Part p in parts)
      {
        for(int i=0; i < p.Resources.Count; ++i)
        {
          Process_Part(p, p.Resources[i].resourceName);
        }
      }

      // process all rules
      foreach(Rule r in Profile.rules)
      {
        if (r.input.Length > 0 && r.rate > 0.0)
        {
          Process_Rule(r, env, va);
        }
      }

      // process all processes
      foreach(Process p in Profile.processes)
      {
        Process_Process(p, env, va);
      }

      // process all modules
      foreach(Part p in parts)
      {
        // get planner controller in the part
        PlannerController ctrl = p.FindModuleImplementing<PlannerController>();

        // ignore all modules in the part if specified in controller
        if (ctrl != null && !ctrl.considered) continue;

        // for each module
        foreach(PartModule m in p.Modules)
        {
          // skip disabled modules
          // rationale: the Selector disable non-selected modules in this way
          if (!m.isEnabled) continue;

          switch(m.moduleName)
          {
            case "Greenhouse": Process_GreenHouse(m as Greenhouse, env, va);                              break;
            case "GravityRing":                  Process_Ring(m as GravityRing);                          break;
            case "Emitter":                      Process_Emitter(m as Emitter);                           break;
            case "Harvester":                    Process_Harvester(m as Harvester);                       break;
            case "Laboratory":                   Process_Laboratory(m as Laboratory);                     break;
            case "Antenna":                      Process_Antenna(m as Antenna);                           break;
            case "Experiment":                   Process_Experiment(m as Experiment);                     break;
            case "ModuleCommand":                Process_Command(m as ModuleCommand);                     break;
            case "ModuleDeployableSolarPanel":   Process_Panel(m as ModuleDeployableSolarPanel, env);     break;
            case "ModuleGenerator":              Process_Generator(m as ModuleGenerator, p);              break;
            case "ModuleResourceConverter":      Process_Converter(m as ModuleResourceConverter, va);     break;
            case "ModuleKPBSConverter":          Process_Converter(m as ModuleResourceConverter, va);     break;
            case "ModuleResourceHarvester":      Process_Harvester(m as ModuleResourceHarvester, va);     break;
            case "ModuleScienceConverter":       Process_Stocklab(m as ModuleScienceConverter);           break;
            case "ModuleActiveRadiator":         Process_Radiator(m as ModuleActiveRadiator);             break;
            case "ModuleWheelMotor":             Process_Wheel_Motor(m as ModuleWheelMotor);              break;
            case "ModuleWheelMotorSteering":     Process_Wheel_Steering(m as ModuleWheelMotorSteering);   break;
            case "ModuleLight":                  Process_Light(m as ModuleLight);                         break;
            case "ModuleColoredLensLight":       Process_Light(m as ModuleLight);                         break;
            case "ModuleMultiPointSurfaceLight": Process_Light(m as ModuleLight);                         break;
            case "SCANsat":                      Process_Scanner(m);                                      break;
            case "ModuleSCANresourceScanner":    Process_Scanner(m);                                      break;
            case "ModuleCurvedSolarPanel":       Process_Curved_Panel(p, m, env);                         break;
            case "FissionGenerator":             Process_Fission_Generator(p, m);                         break;
            case "ModuleRadioisotopeGenerator":  Process_Radioisotope_Generator(p, m);                    break;
            case "ModuleCryoTank":               Process_CryoTank(p, m);                                  break;
          }
        }
      }

      // execute all possible recipes
      bool executing = true;
      while(executing)
      {
        executing = false;
        for(int i=0; i<recipes.Count; ++i)
        {
          Simulated_Recipe recipe = recipes[i];
          if (recipe.left > double.Epsilon)
          {
            executing |= recipe.Execute(this);
          }
        }
      }
      recipes.Clear();

      // clamp all resources
      foreach(var pair in resources) pair.Value.Clamp();
    }

    public Simulated_Resource Resource(string name)
    {
      if (!resources.TryGetValue(name, out Simulated_Resource res))
      {
        res = new Simulated_Resource();
        resources.Add(name, res);
      }
      return res;
    }

    void Process_Part(Part p, string res_name)
    {
      Simulated_Resource res = Resource(res_name);
      res.storage += Lib.Amount(p, res_name);
      res.amount += Lib.Amount(p, res_name);
      res.capacity += Lib.Capacity(p, res_name);
    }

    void Process_Rule(Rule r, Environment_Analyzer env, Vessel_Analyzer va)
    {
      // deduce rate per-second
      double rate = (double)va.crew_count * (r.interval > 0.0 ? r.rate / r.interval : r.rate);

      // evaluate modifiers
      double k = Modifiers.Evaluate(env, va, this, r.modifiers);

      // prepare recipe
      if (r.output.Length == 0)
      {
        Resource(r.input).Consume(rate * k, r.name);
      }
      else if (rate > double.Epsilon)
      {
        // - rules always dump excess overboard (because it is waste)
        Simulated_Recipe recipe = new Simulated_Recipe(r.name);
        recipe.Input(r.input, rate * k);
        recipe.Output(r.output, rate * k * r.ratio, true);
        recipes.Add(recipe);
      }
    }

    void Process_Process(Process p, Environment_Analyzer env, Vessel_Analyzer va)
    {
      // evaluate modifiers
      double k = Modifiers.Evaluate(env, va, this, p.modifiers);

      // prepare recipe
      Simulated_Recipe recipe = new Simulated_Recipe(p.name);
      foreach(var input in p.inputs)
      {
        recipe.Input(input.Key, input.Value * k);
      }
      foreach(var output in p.outputs)
      {
        recipe.Output(output.Key, output.Value * k, p.dump.Check(output.Key));
      }
      recipes.Add(recipe);
    }

    void Process_GreenHouse(Greenhouse g, Environment_Analyzer env, Vessel_Analyzer va)
    {
      // skip disabled greenhouses
      if (!g.active) return;

      // shortcut to resources
      Simulated_Resource ec = Resource("ElectricCharge");
      Simulated_Resource res = Resource(g.crop_resource);

      // calculate natural and artificial lighting
      double natural = env.solar_flux;
      double artificial = Math.Max(g.light_tolerance - natural, 0.0);

      // if lamps are on and artificial lighting is required
      if (artificial > 0.0)
      {
        // consume ec for the lamps
        ec.Consume(g.ec_rate * (artificial / g.light_tolerance), "greenhouse");
      }

      // execute recipe
      Simulated_Recipe recipe = new Simulated_Recipe("greenhouse");
      foreach (ModuleResource input in g.resHandler.inputResources) recipe.Input(input.name, input.rate);
      foreach (ModuleResource output in g.resHandler.outputResources) recipe.Output(output.name, output.rate, true);
      recipes.Add(recipe);

      // determine environment conditions
      bool lighting = natural + artificial >= g.light_tolerance;
      bool pressure = va.pressurized || g.pressure_tolerance <= double.Epsilon;
      bool radiation = (env.landed ? env.surface_rad : env.magnetopause_rad) * (1.0 - va.shielding) < g.radiation_tolerance;

      // if all conditions apply
      // note: we are assuming the inputs are satisfied, we can't really do otherwise here
      if (lighting && pressure && radiation)
      {
        // produce food
        res.Produce(g.crop_size * g.crop_rate, "greenhouse");

        // add harvest info
        res.harvests.Add(Lib.BuildString(g.crop_size.ToString("F0"), " in ", Lib.HumanReadableDuration(1.0 / g.crop_rate)));
      }
    }

    void Process_Ring(GravityRing ring)
    {
      if (ring.deployed) Resource("ElectricCharge").Consume(ring.ec_rate, "gravity ring");
    }

    void Process_Emitter(Emitter emitter)
    {
      if (emitter.running) Resource("ElectricCharge").Consume(emitter.ec_rate, "emitter");
    }

    void Process_Harvester(Harvester harvester)
    {
      if (harvester.running)
      {
        Simulated_Recipe recipe = new Simulated_Recipe("harvester");
        if (harvester.ec_rate > double.Epsilon) recipe.Input("ElectricCharge", harvester.ec_rate);
        recipe.Output(harvester.resource, harvester.rate, true);
        recipes.Add(recipe);
      }
    }

    void Process_Laboratory(Laboratory lab)
    {
      // note: we are not checking if there is a scientist in the part
      if (lab.running)
      {
        Resource("ElectricCharge").Consume(lab.ec_rate, "laboratory");
      }
    }

    // TODO: Add support to CommNet
    void Process_Antenna(Antenna antenna)
    {
      Resource("ElectricCharge").Consume(antenna.cost, "transmission");
    }

    void Process_Experiment(Experiment exp)
    {
      if (exp.recording)
      {
        Resource("ElectricCharge").Consume(exp.ec_rate, exp.transmissible ? "sensor" : "experiment");
      }
    }

    void Process_Command(ModuleCommand command)
    {
      foreach(ModuleResource res in command.resHandler.inputResources)
      {
        Resource(res.name).Consume(res.rate, "command");
      }
    }

    void Process_Panel(ModuleDeployableSolarPanel panel, Environment_Analyzer env)
    {
      double generated = panel.resHandler.outputResources[0].rate * env.solar_flux / Sim.SolarFluxAtHome();
      Resource("ElectricCharge").Produce(generated, "solar panel");
    }

    void Process_Generator(ModuleGenerator generator, Part p)
    {
       // skip launch clamps, that include a generator
       if (Lib.PartName(p) == "launchClamp1") return;

       Simulated_Recipe recipe = new Simulated_Recipe("generator");
       foreach(ModuleResource res in generator.resHandler.inputResources)
       {
         recipe.Input(res.name, res.rate);
       }
       foreach(ModuleResource res in generator.resHandler.outputResources)
       {
         recipe.Output(res.name, res.rate, true);
       }
       recipes.Add(recipe);
    }

    void Process_Converter(ModuleResourceConverter converter, Vessel_Analyzer va)
    {
      // calculate experience bonus
      float exp_bonus = converter.UseSpecialistBonus
        ? converter.EfficiencyBonus * (converter.SpecialistBonusBase + (converter.SpecialistEfficiencyFactor * (va.crew_engineer_maxlevel + 1)))
        : 1.0f;

      // use part name as recipe name
      // - include crew bonus in the recipe name
      string recipe_name = Lib.BuildString(converter.part.partInfo.title, " (efficiency: ", Lib.HumanReadablePerc(exp_bonus), ")");

      // generate recipe
      Simulated_Recipe recipe = new Simulated_Recipe(recipe_name);
      foreach(ResourceRatio res in converter.inputList)
      {
        recipe.Input(res.ResourceName, res.Ratio * exp_bonus);
      }
      foreach(ResourceRatio res in converter.outputList)
      {
        recipe.Output(res.ResourceName, res.Ratio * exp_bonus, res.DumpExcess);
      }
      recipes.Add(recipe);
    }

    void Process_Harvester(ModuleResourceHarvester harvester, Vessel_Analyzer va)
    {
      // calculate experience bonus
      float exp_bonus = harvester.UseSpecialistBonus
        ? harvester.EfficiencyBonus * (harvester.SpecialistBonusBase + (harvester.SpecialistEfficiencyFactor * (va.crew_engineer_maxlevel + 1)))
        : 1.0f;

      // use part name as recipe name
      // - include crew bonus in the recipe name
      string recipe_name = Lib.BuildString(harvester.part.partInfo.title, " (efficiency: ", Lib.HumanReadablePerc(exp_bonus), ")");

      // generate recipe
      Simulated_Recipe recipe = new Simulated_Recipe(recipe_name);
      foreach(ResourceRatio res in harvester.inputList)
      {
        recipe.Input(res.ResourceName, res.Ratio);
      }
      recipe.Output(harvester.ResourceName, harvester.Efficiency * exp_bonus, true);
      recipes.Add(recipe);
    }

    void Process_Stocklab(ModuleScienceConverter lab)
    {
      Resource("ElectricCharge").Consume(lab.powerRequirement, "lab");
    }

    void Process_Radiator(ModuleActiveRadiator radiator)
    {
      // note: IsCooling is not valid in the editor, for deployable radiators,
      // we will have to check if the related deploy module is deployed
      // we use PlannerController instead
      foreach(var res in radiator.resHandler.inputResources)
      {
        Resource(res.name).Consume(res.rate, "radiator");
      }
    }

    void Process_Wheel_Motor(ModuleWheelMotor motor)
    {
      foreach(var res in motor.resHandler.inputResources)
      {
        Resource(res.name).Consume(res.rate, "wheel");
      }
    }

    void Process_Wheel_Steering(ModuleWheelMotorSteering steering)
    {
      foreach(var res in steering.resHandler.inputResources)
      {
        Resource(res.name).Consume(res.rate, "wheel");
      }
    }

    void Process_Light(ModuleLight light)
    {
      if (light.useResources && light.isOn)
      {
        Resource("ElectricCharge").Consume(light.resourceAmount, "light");
      }
    }

    void Process_Scanner(PartModule m)
    {
      Resource("ElectricCharge").Consume(SCANsat.EcConsumption(m), "SCANsat");
    }

    void Process_Curved_Panel(Part p, PartModule m, Environment_Analyzer env)
    {
      // note: assume half the components are in sunlight, and average inclination is half

      // get total rate
      double tot_rate = Lib.ReflectionValue<float>(m, "TotalEnergyRate");

      // get number of components
      int components = p.FindModelTransforms(Lib.ReflectionValue<string>(m, "PanelTransformName")).Length;

      // approximate output
      // 0.7071: average clamped cosine
      Resource("ElectricCharge").Produce(tot_rate * 0.7071 * env.solar_flux / Sim.SolarFluxAtHome(), "curved panel");
    }

    void Process_Fission_Generator(Part p, PartModule m)
    {
      double max_rate = Lib.ReflectionValue<float>(m, "PowerGeneration");

      // get fission reactor tweakable, will default to 1.0 for other modules
      var reactor = p.FindModuleImplementing<ModuleResourceConverter>();
      double tweakable = reactor == null ? 1.0 : Lib.ReflectionValue<float>(reactor, "CurrentPowerPercent") * 0.01f;

      Resource("ElectricCharge").Produce(max_rate * tweakable, "fission generator");
    }

    void Process_Radioisotope_Generator(Part p, PartModule m)
    {
      double max_rate = Lib.ReflectionValue<float>(m, "BasePower");

      Resource("ElectricCharge").Produce(max_rate, "radioisotope generator");
    }

    void Process_CryoTank(Part p, PartModule m)
    {
       // note: assume cooling is active
       double cooling_cost = Lib.ReflectionValue<float>(m, "CoolingCost");
       string fuel_name = Lib.ReflectionValue<string>(m, "FuelName");

       Resource("ElectricCharge").Consume(cooling_cost * Lib.Capacity(p, fuel_name) * 0.001, "cryotank");
    }

    Dictionary<string, Simulated_Resource> resources = new Dictionary<string, Simulated_Resource>();
    List<Simulated_Recipe> recipes = new List<Simulated_Recipe>();
  }

  public sealed class Simulated_Resource
  {
    public Simulated_Resource()
    {
      consumers = new Dictionary<string, wrapper>();
      producers = new Dictionary<string, wrapper>();
      harvests = new List<string>();
    }

    public void Consume(double quantity, string name)
    {
      if (quantity >= double.Epsilon)
      {
        amount -= quantity;
        consumed += quantity;

        if (!consumers.ContainsKey(name)) consumers.Add(name, new wrapper());
        consumers[name].value += quantity;
      }
    }

    public void Produce(double quantity, string name)
    {
      if (quantity >= double.Epsilon)
      {
        amount += quantity;
        produced += quantity;

        if (!producers.ContainsKey(name)) producers.Add(name, new wrapper());
        producers[name].value += quantity;
      }
    }

    public void Clamp()
    {
      amount = Lib.Clamp(amount, 0.0, capacity);
    }

    public double LifeTime()
    {
      double rate = produced - consumed;
      return amount <= double.Epsilon ? 0.0 : rate > -1e-10 ? double.NaN : amount / -rate;
    }

    public string Tooltip(bool invert=false)
    {
      var green = !invert ? producers : consumers;
      var red = !invert ? consumers : producers;

      var sb = new StringBuilder();
      foreach(var pair in green)
      {
        if (sb.Length > 0) sb.Append("\n");
        sb.Append("<b><color=#00ff00>");
        sb.Append(Lib.HumanReadableRate(pair.Value.value));
        sb.Append("</color></b>\t");
        sb.Append(pair.Key);
      }
      foreach(var pair in red)
      {
        if (sb.Length > 0) sb.Append("\n");
        sb.Append("<b><color=#ff0000>");
        sb.Append(Lib.HumanReadableRate(pair.Value.value));
        sb.Append("</color></b>\t");
        sb.Append(pair.Key);
      }
      if (harvests.Count > 0)
      {
        sb.Append("\n\n<b>Harvests</b>");
        foreach(string s in harvests)
        {
          sb.Append("\n");
          sb.Append(s);
        }
      }
      return Lib.BuildString("<align=left />", sb.ToString());
    }

    public double storage;                        // amount stored (at the start of simulation)
    public double capacity;                       // storage capacity
    public double amount;                         // amount stored (during simulation)
    public double consumed;                       // total consumption rate
    public double produced;                       // total production rate
    public List<string> harvests;                 // some extra data about harvests

    public class wrapper { public double value; }
    public Dictionary<string, wrapper> consumers; // consumers metadata
    public Dictionary<string, wrapper> producers; // producers metadata
  }

  public sealed class Simulated_Recipe
  {
    public Simulated_Recipe(string name)
    {
      this.name = name;
      this.inputs = new List<Resource_Recipe.Entry>();
      this.outputs = new List<Resource_Recipe.Entry>();
      this.left = 1.0;
    }

    // add an input to the recipe
    public void Input(string resource_name, double quantity)
    {
      if (quantity > double.Epsilon) //< avoid division by zero
      {
        inputs.Add(new Resource_Recipe.Entry(resource_name, quantity));
      }
    }

    // add an output to the recipe
    public void Output(string resource_name, double quantity, bool dump)
    {
      if (quantity > double.Epsilon) //< avoid division by zero
      {
        outputs.Add(new Resource_Recipe.Entry(resource_name, quantity, dump));
      }
    }

    // execute the recipe
    public bool Execute(Resource_Simulator sim)
    {
      // determine worst input ratio
      double worst_input = left;
      if (outputs.Count > 0)
      {
        for(int i=0; i<inputs.Count; ++i)
        {
          var e = inputs[i];
          Simulated_Resource res = sim.Resource(e.name);
          worst_input = Lib.Clamp(res.amount * e.inv_quantity, 0.0, worst_input);
        }
      }

      // determine worst output ratio
      double worst_output = left;
      if (inputs.Count > 0)
      {
        for(int i=0; i<outputs.Count; ++i)
        {
          var e = outputs[i];
          if (!e.dump) // ignore outputs that can dump overboard
          {
            Simulated_Resource res = sim.Resource(e.name);
            worst_output = Lib.Clamp((res.capacity - res.amount) * e.inv_quantity, 0.0, worst_output);
          }
        }
      }

      // determine worst-io
      double worst_io = Math.Min(worst_input, worst_output);

      // consume inputs
      for(int i=0; i<inputs.Count; ++i)
      {
        var e = inputs[i];
        Simulated_Resource res = sim.Resource(e.name);
        res.Consume(e.quantity * worst_io, name);
      }

      // produce outputs
      for(int i=0; i<outputs.Count; ++i)
      {
        var e = outputs[i];
        Simulated_Resource res = sim.Resource(e.name);
        res.Produce(e.quantity * worst_io, name);
      }

      // update amount left to execute
      left -= worst_io;

      // the recipe was executed, at least partially
      return worst_io > double.Epsilon;
    }

    // store inputs and outputs
    public string name;                         // name used for consumer/producer tooltip
    public List<Resource_Recipe.Entry> inputs;  // set of input resources
    public List<Resource_Recipe.Entry> outputs; // set of output resources
    public double left;                         // what proportion of the recipe is left to execute
  }
}