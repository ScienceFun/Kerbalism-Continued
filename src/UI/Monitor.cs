using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
  public enum MonitorPage
  {
    telemetry,
    data,
    scripts,
    config,
    net
  }

  public sealed class Monitor
  {
    // ctor
    public Monitor()
    {
      // filter style
      filter_style = new GUIStyle(HighLogic.Skin.label);
      filter_style.normal.textColor = new Color(0.66f, 0.66f, 0.66f, 1.0f);
      filter_style.stretchWidth = true;
      filter_style.fontSize = 12;
      filter_style.alignment = TextAnchor.MiddleCenter;
      filter_style.fixedHeight = 16.0f;
      filter_style.border = new RectOffset(0, 0, 0, 0);

      // vessel config style
      config_style = new GUIStyle(HighLogic.Skin.label);
      config_style.normal.textColor = Color.white;
      config_style.padding = new RectOffset(0, 0, 0, 0);
      config_style.alignment = TextAnchor.MiddleLeft;
      config_style.imagePosition = ImagePosition.ImageLeft;
      config_style.fontSize = 9;

      // group texfield style
      group_style = new GUIStyle(config_style);
      group_style.imagePosition = ImagePosition.TextOnly;
      group_style.stretchWidth = true;
      group_style.fixedHeight = 11.0f;
      group_style.normal.textColor = Color.yellow;

      // initialize panel
      panel = new Panel();

      // auto-switch selected vessel on scene changes
      GameEvents.onVesselChange.Add((Vessel v) => { if (selected_id != Guid.Empty) selected_id = v.id; });
    }

    public void Update()
    {
      // reset panel
      panel.Clear();

      // get vessel
      selected_v = selected_id == Guid.Empty ? null : FlightGlobals.FindVessel(selected_id);

      // if nothing is selected, or if the selected vessel doesn't exist
      // anymore, or if it has become invalid for whatever reason
      if (selected_v == null || !Cache.VesselInfo(selected_v).is_valid)
      {
        // forget the selected vessel, if any
        selected_id = Guid.Empty;

        // filter flag is updated on render_vessel
        show_filter = false;

        // used to detect when no vessels are in list
        bool setup = false;

        // draw active vessel if any
        if (FlightGlobals.ActiveVessel != null)
        {
          setup |= Render_Vessel(panel, FlightGlobals.ActiveVessel);
        }

        // for each vessel
        foreach (Vessel v in FlightGlobals.Vessels)
        {
          // skip active vessel
          if (v == FlightGlobals.ActiveVessel) continue;

          // draw the vessel
          setup |= Render_Vessel(panel, v);
        }

        // empty vessel case
        if (!setup)
        {
          panel.SetHeader("<i>no vessels</i>");
        }
      }
      // if a vessel is selected
      else
      {
        // header act as title
        Render_Vessel(panel, selected_v);

        // update page content
        switch (page)
        {
          case MonitorPage.telemetry: panel.Telemetry_Life(selected_v); break;
          case MonitorPage.data: panel.FileMan(selected_v); break;
          case MonitorPage.scripts: panel.DevMan(selected_v); break;
          case MonitorPage.config: panel.Config(selected_v); break;
        }
      }
    }

    public void Render()
    {
      // start scrolling view
      scroll_pos = GUILayout.BeginScrollView(scroll_pos, HighLogic.Skin.horizontalScrollbar, HighLogic.Skin.verticalScrollbar);

      // render panel content
      panel.Render();

      // end scroll view
      GUILayout.EndScrollView();

      // if a vessel is selected, and exist
      if (selected_v != null)
      {
        Render_Menu(selected_v);
      }
      // if at least one vessel is assigned to a group
      else if (show_filter)
      {
        Render_Filter();
      }

      // right click goes back to list view
      if (Event.current.type == EventType.MouseDown
       && Event.current.button == 1)
      {
        selected_id = Guid.Empty;
      }
    }

    public float Width() => Math.Max(320.0f, panel.Width());

    public float Height()
    {
      // top spacing
      float h = 10.0f;

      // panel height
      h += panel.Height();

      // one is selected, or filter is required
      if (selected_id != Guid.Empty || show_filter)
      {
        h += 26.0f;
      }

      // clamp to screen height
      return Math.Min(h, Screen.height * 0.75f);
    }

    bool Render_Vessel(Panel p, Vessel v)
    {
      // get vessel info
      Vessel_Info vi = Cache.VesselInfo(v);

      // skip invalid vessels
      if (!vi.is_valid) return false;

      if (!Lib.IsVessel(v)) return false;

      // get data from db
      VesselData vd = DB.Vessel(v);

      // determine if filter must be shown
      show_filter |= vd.group.Length > 0 && vd.group != "NONE";

      // skip filtered vessels
      if (Filtered() && vd.group != filter) return false;

      // get resource handler
      Vessel_Resources resources = ResourceCache.Get(v);

      // get vessel crew
      List<ProtoCrewMember> crew = Lib.CrewList(v);

      // get vessel name
      string vessel_name = v.isEVA ? crew[0].name : v.vesselName;

      // get body name
      string body_name = v.mainBody.name.ToUpper();

      // render entry
      p.SetHeader
      (
        Lib.BuildString("<b>", Lib.Ellipsis(vessel_name, 20), "</b> <size=9><color=#cccccc>", Lib.Ellipsis(body_name, 8), "</color></size>"),
        string.Empty,
        () => { selected_id = selected_id != v.id ? v.id : Guid.Empty; }
      );

      // problem indicator
      Indicator_Problems(p, v, vi, crew);

      // battery indicator
      Indicator_EC(p, v, vi);

      // supply indicator
      if (Features.Supplies) Indicator_Supplies(p, v, vi);

      // reliability indicator
      if (Features.Reliability) Indicator_Reliability(p, v, vi);

      // signal indicator
      if (Features.Signal || Features.KCommNet) Indicator_Signal(p, v, vi);

      // done
      return true;
    }

    void Render_Menu(Vessel v)
    {
      const string tooltip = "\n<i>(middle-click to popout in a window)</i>";
      VesselData vd = DB.Vessel(v);
      GUILayout.BeginHorizontal(Styles.entry_container);
      GUILayout.Label(new GUIContent(page == MonitorPage.telemetry ? " <color=#00ffff>INFO</color> " : " INFO ", Icons.small_info, "Telemetry readings" + tooltip), config_style);
      if (Lib.IsClicked()) page = MonitorPage.telemetry;
      else if (Lib.IsClicked(2)) UI.Open((p) => p.Telemetry_Life(v));
      if (Features.Science)
      {
        GUILayout.Label(new GUIContent(page == MonitorPage.data ? " <color=#00ffff>DATA</color> " : " DATA ", Icons.small_folder, "Stored files and samples" + tooltip), config_style);
        if (Lib.IsClicked()) page = MonitorPage.data;
        else if (Lib.IsClicked(2)) UI.Open((p) => p.FileMan(v));
      }
      if (Features.Automation)
      {
        GUILayout.Label(new GUIContent(page == MonitorPage.scripts ? " <color=#00ffff>AUTO</color> " : " AUTO ", Icons.small_console, "Control and automate components" + tooltip), config_style);
        if (Lib.IsClicked()) page = MonitorPage.scripts;
        else if (Lib.IsClicked(2)) UI.Open((p) => p.DevMan(v));
      }
      GUILayout.Label(new GUIContent(page == MonitorPage.config ? " <color=#00ffff>CFG</color> " : " CFG ", Icons.small_config, "Configure the vessel" + tooltip), config_style);
      if (Lib.IsClicked()) page = MonitorPage.config;
      else if (Lib.IsClicked(2)) UI.Open((p) => p.Config(v));

      GUILayout.Label(new GUIContent(" GROUP ", Icons.small_search, "Organize in groups"), config_style);
      vd.group = Lib.TextFieldPlaceholder("Kerbalism_group", vd.group, "NONE", group_style).ToUpper();
      string t = "";
      t = Lib.TextFieldPlaceholder("Kerbalism_group", t, "NONE", group_style).ToUpper();
      GUILayout.EndHorizontal();
      GUILayout.Space(10.0f);
    }

    void Render_Filter()
    {
      // show the group filter
      GUILayout.BeginHorizontal(Styles.entry_container);
      filter = Lib.TextFieldPlaceholder("Kerbalism_filter", filter, filter_placeholder, filter_style).ToUpper();
      GUILayout.EndHorizontal();
      GUILayout.Space(10.0f);
    }

    void Problem_Sunlight(Vessel_Info info, ref List<Texture> icons, ref List<string> tooltips)
    {
      if (info.sunlight <= double.Epsilon)
      {
        icons.Add(Icons.sun_black);
        tooltips.Add("In shadow");
      }
    }

    void Problem_Greenhouses(Vessel v, List<Greenhouse.Data> greenhouses, ref List<Texture> icons, ref List<string> tooltips)
    {
      if (greenhouses.Count == 0) return;

      foreach (Greenhouse.Data greenhouse in greenhouses)
      {
        if (greenhouse.issue.Length > 0)
        {
          if (!icons.Contains(Icons.plant_yellow)) icons.Add(Icons.plant_yellow);
          tooltips.Add(Lib.BuildString("Greenhouse: <b>", greenhouse.issue, "</b>"));
        }
      }
    }

    void Problem_Kerbals(List<ProtoCrewMember> crew, ref List<Texture> icons, ref List<string> tooltips)
    {
      UInt32 health_severity = 0;
      UInt32 stress_severity = 0;
      foreach (ProtoCrewMember c in crew)
      {
        // get kerbal data
        KerbalData kd = DB.Kerbal(c.name);

        // skip disabled kerbals
        if (kd.disabled) continue;

        foreach (Rule r in Profile.rules)
        {
          RuleData rd = kd.Rule(r.name);
          if (rd.problem > r.danger_threshold)
          {
            if (!r.breakdown) health_severity = Math.Max(health_severity, 2);
            else stress_severity = Math.Max(stress_severity, 2);
            tooltips.Add(Lib.BuildString(c.name, ": <b>", r.name, "</b>"));
          }
          else if (rd.problem > r.warning_threshold)
          {
            if (!r.breakdown) health_severity = Math.Max(health_severity, 1);
            else stress_severity = Math.Max(stress_severity, 1);
            tooltips.Add(Lib.BuildString(c.name, ": <b>", r.name, "</b>"));
          }
        }
      }
      if (health_severity == 1) icons.Add(Icons.health_yellow);
      else if (health_severity == 2) icons.Add(Icons.health_red);
      if (stress_severity == 1) icons.Add(Icons.brain_yellow);
      else if (stress_severity == 2) icons.Add(Icons.brain_red);
    }

    void Problem_Radiation(Vessel_Info info, ref List<Texture> icons, ref List<string> tooltips)
    {
      string radiation_str = Lib.BuildString(" (<i>", (info.radiation * 60.0 * 60.0).ToString("F3"), " rad/h)</i>");
      if (info.radiation > 1.0 / 3600.0)
      {
        icons.Add(Icons.radiation_red);
        tooltips.Add(Lib.BuildString("Exposed to extreme radiation", radiation_str));
      }
      else if (info.radiation > 0.15 / 3600.0)
      {
        icons.Add(Icons.radiation_yellow);
        tooltips.Add(Lib.BuildString("Exposed to intense radiation", radiation_str));
      }
      else if (info.radiation > 0.0195 / 3600.0)
      {
        icons.Add(Icons.radiation_yellow);
        tooltips.Add(Lib.BuildString("Exposed to moderate radiation", radiation_str));
      }
    }

    void Problem_Poisoning(Vessel_Info info, ref List<Texture> icons, ref List<string> tooltips)
    {
      string poisoning_str = Lib.BuildString("CO2 level in internal atmosphere: <b>", Lib.HumanReadablePerc(info.poisoning), "</b>");
      if (info.poisoning >= 0.05)
      {
        icons.Add(Icons.recycle_red);
        tooltips.Add(poisoning_str);
      }
      else if (info.poisoning > 0.025)
      {
        icons.Add(Icons.recycle_yellow);
        tooltips.Add(poisoning_str);
      }
    }

    void Problem_Storm(Vessel v, ref List<Texture> icons, ref List<string> tooltips)
    {
      if (Storm.Incoming(v))
      {
        icons.Add(Icons.storm_yellow);
        tooltips.Add(Lib.BuildString("Coronal mass ejection incoming <i>(", Lib.HumanReadableDuration(Storm.TimeBeforeCME(v)), ")</i>"));
      }
      if (Storm.InProgress(v))
      {
        icons.Add(Icons.storm_red);
        tooltips.Add(Lib.BuildString("Solar storm in progress <i>(", Lib.HumanReadableDuration(Storm.TimeLeftCME(v)), ")</i>"));
      }
    }

    void Indicator_Problems(Panel p, Vessel v, Vessel_Info vi, List<ProtoCrewMember> crew)
    {
      // store problems icons & tooltips
      List<Texture> problem_icons = new List<Texture>();
      List<string> problem_tooltips = new List<string>();

      // detect problems
      Problem_Sunlight(vi, ref problem_icons, ref problem_tooltips);
      if (Features.SpaceWeather) Problem_Storm(v, ref problem_icons, ref problem_tooltips);
      if (crew.Count > 0 && Profile.rules.Count > 0) Problem_Kerbals(crew, ref problem_icons, ref problem_tooltips);
      if (crew.Count > 0 && Features.Radiation) Problem_Radiation(vi, ref problem_icons, ref problem_tooltips);
      Problem_Greenhouses(v, vi.greenhouses, ref problem_icons, ref problem_tooltips);
      if (Features.Poisoning) Problem_Poisoning(vi, ref problem_icons, ref problem_tooltips);

      // choose problem icon
      const UInt64 problem_icon_time = 3;
      Texture problem_icon = Icons.empty;
      if (problem_icons.Count > 0)
      {
        UInt64 problem_index = ((UInt64)Time.realtimeSinceStartup / problem_icon_time) % (UInt64)(problem_icons.Count);
        problem_icon = problem_icons[(int)problem_index];
      }

      // generate problem icon
      p.SetIcon(problem_icon, String.Join("\n", problem_tooltips.ToArray()));
    }

    void Indicator_EC(Panel p, Vessel v, Vessel_Info vi)
    {
      Resource_Info ec = ResourceCache.Info(v, "ElectricCharge");
      Supply supply = Profile.supplies.Find(k => k.resource == "ElectricCharge");
      double low_threshold = supply != null ? supply.low_threshold : 0.15;
      double depletion = ec.Depletion(vi.crew_count);

      string tooltip = Lib.BuildString
      (
        "<align=left /><b>name\tlevel\tduration</b>\n",
        ec.level <= 0.005 ? "<color=#ff0000>" : ec.level <= low_threshold ? "<color=#ffff00>" : "<color=#cccccc>",
        "EC\t",
        Lib.HumanReadablePerc(ec.level), "\t",
        depletion <= double.Epsilon ? "depleted" : Lib.HumanReadableDuration(depletion),
        "</color>"
      );

      Texture image = ec.level <= 0.005
        ? Icons.battery_red
        : ec.level <= low_threshold
        ? Icons.battery_yellow
        : Icons.battery_white;

      p.SetIcon(image, tooltip);
    }

    void Indicator_Supplies(Panel p, Vessel v, Vessel_Info vi)
    {
      List<string> tooltips = new List<string>();
      uint max_severity = 0;
      if (vi.crew_count > 0)
      {
        foreach (Supply supply in Profile.supplies.FindAll(k => k.resource != "ElectricCharge"))
        {
          Resource_Info res = ResourceCache.Info(v, supply.resource);
          double depletion = res.Depletion(vi.crew_count);

          if (res.capacity > double.Epsilon)
          {
            if (tooltips.Count == 0)
            {
              tooltips.Add("<align=left /><b>name\t\tlevel\tduration</b>");
            }

            tooltips.Add(Lib.BuildString
            (
              res.level <= 0.005 ? "<color=#ff0000>" : res.level <= supply.low_threshold ? "<color=#ffff00>" : "<color=#cccccc>",
              supply.resource,
              supply.resource != "Ammonia" ? "\t\t" : "\t", //< hack: make ammonia fit damn it
              Lib.HumanReadablePerc(res.level), "\t",
              depletion <= double.Epsilon ? "depleted" : Lib.HumanReadableDuration(depletion),
              "</color>"
            ));

            uint severity = res.level <= 0.005 ? 2u : res.level <= supply.low_threshold ? 1u : 0;
            max_severity = Math.Max(max_severity, severity);
          }
        }
      }

      Texture image = max_severity == 2
        ? Icons.box_red
        : max_severity == 1
        ? Icons.box_yellow
        : Icons.box_white;

      p.SetIcon(image, string.Join("\n", tooltips.ToArray()));
    }

    void Indicator_Reliability(Panel p, Vessel v, Vessel_Info vi)
    {
      Texture image;
      string tooltip;
      if (!vi.malfunction)
      {
        image = Icons.wrench_white;
        tooltip = string.Empty;
      }
      else if (!vi.critical)
      {
        image = Icons.wrench_yellow;
        tooltip = "Malfunctions";
      }
      else
      {
        image = Icons.wrench_red;
        tooltip = "Critical failures";
      }

      p.SetIcon(image, tooltip);
    }

    // TODO: Implement support to CommNet frequency
    void Indicator_Signal(Panel p, Vessel v, Vessel_Info vi)
    {
      ConnectionInfo conn = vi.connection;

      // target name
      string target_str = string.Empty;
      switch (vi.connection.status)
      {
        case LinkStatus.direct_link: target_str = "DSN"; break;
        case LinkStatus.indirect_link: target_str = vi.connection.path[vi.connection.path.Count - 1].vesselName; break;
        default: target_str = "none"; break;
      }

      // transmitted label, content and tooltip
      string comms_label = vi.relaying.Length == 0 ? "transmitting" : "relaying";
      string comms_str = vi.connection.linked ? "telemetry" : "nothing";
      string comms_tooltip = string.Empty;
      if (vi.relaying.Length > 0)
      {
        ExperimentInfo exp = Science.Experiment(vi.relaying);
        comms_str = exp.name;
        comms_tooltip = exp.fullname;
      }
      else if (vi.transmitting.Length > 0)
      {
        ExperimentInfo exp = Science.Experiment(vi.transmitting);
        comms_str = exp.name;
        comms_tooltip = exp.fullname;
      }

      string tooltip = Lib.BuildString
      (
        "<align=left />",
        "connected\t<b>", vi.connection.linked ? "yes" : "no", "</b>\n",
        "rate\t\t<b>", Lib.HumanReadableDataRate(vi.connection.rate), "</b>\n",
        "target\t\t<b>", target_str, "</b>\n",
        comms_label, "\t<b>", comms_str, "</b>"
      );

      Texture image = Icons.signal_red;
      switch (conn.status)
      {
        case LinkStatus.direct_link:
          image = vi.connection.rate > 0.005 ? Icons.signal_white : Icons.signal_yellow;
          break;

        case LinkStatus.indirect_link:
          image = vi.connection.rate > 0.005 ? Icons.signal_white : Icons.signal_yellow;
          tooltip += "\n\n<color=yellow>Signal relayed</color>";
          break;

        case LinkStatus.no_link:
          image = Icons.signal_red;
          break;

        case LinkStatus.no_antenna:
          image = Icons.signal_red;
          tooltip += "\n\n<color=red>No antenna</color>";
          break;

        case LinkStatus.blackout:
          image = Icons.signal_red;
          tooltip += "\n\n<color=red>Blackout</color>";
          break;
      }

      p.SetIcon(image, tooltip);
    }

    // return true if the list of vessels is filtered
    bool Filtered()
    {
      return filter.Length > 0 && filter != filter_placeholder;
    }

    Guid selected_id;                                     // id of selected vessel
    Vessel selected_v;                                    // selected vessel

    // filter
    bool show_filter;                                     // determine if filter is shown
    string filter = string.Empty;                         // store group filter, if any
    const string filter_placeholder = "FILTER BY GROUP";  // group filter placeholder

    // used by scroll window mechanics
    Vector2 scroll_pos;

    // styles
    GUIStyle filter_style;                                // vessel filter
    GUIStyle config_style;                                // config entry label
    GUIStyle group_style;                                 // config group textfield

    // monitor page
    MonitorPage page = MonitorPage.telemetry;
    Panel panel;
  }
}