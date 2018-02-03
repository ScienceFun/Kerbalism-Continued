namespace KERBALISM 
{
  public static class VesselConfig
  {
    public static void Config(this Panel p, Vessel v)
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
      p.Title(Lib.BuildString(Lib.Ellipsis(v.vesselName, 20), " <color=#cccccc>VESSEL CONFIG</color>"));

      // time-out simulation
      if (p.Timeout(vi)) return;

      // get data from db
      VesselData vd = DB.Vessel(v);

      // toggle rendering
      string tooltip;
      if (Features.Signal || Features.Reliability)
      {
        p.SetSection("RENDERING");
      }
      if (Features.Signal)
      {
        tooltip = "Render the connection line\nin mapview and tracking station";
        p.SetContent("show links", string.Empty, tooltip);
        p.SetIcon(vd.cfg_showlink ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_showlink));
      }
      if (Features.Reliability)
      {
        tooltip = "Highlight failed components";
        p.SetContent("highlight malfunctions", string.Empty, tooltip);
        p.SetIcon(vd.cfg_highlights ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_highlights));
      }

      // toggle messages
      p.SetSection("MESSAGES");
      tooltip = "Receive a message when\nElectricCharge level is low";
      p.SetContent("battery", string.Empty, tooltip);
      p.SetIcon(vd.cfg_ec ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_ec));
      if (Features.Supplies)
      {
        tooltip = "Receive a message when\nsupply resources level is low";
        p.SetContent("supply", string.Empty, tooltip);
        p.SetIcon(vd.cfg_supply ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_supply));
      }
      if (Features.Signal || Features.KCommNet)
      {
        tooltip = "Receive a message when signal is lost or obtained";
        p.SetContent("signal", string.Empty, tooltip);
        p.SetIcon(vd.cfg_signal ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_signal));
      }
      if (Features.Reliability)
      {
        tooltip = "Receive a message\nwhen a component fail";
        p.SetContent("reliability", string.Empty, tooltip);
        p.SetIcon(vd.cfg_malfunction ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_malfunction));
      }
      if (Features.SpaceWeather)
      {
        tooltip = "Receive a message\nduring CME events";
        p.SetContent("storm", string.Empty, tooltip);
        p.SetIcon(vd.cfg_storm ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_storm));
      }
      if (Features.Automation)
      {
        tooltip = "Receive a message when\nscripts are executed";
        p.SetContent("script", string.Empty, tooltip);
        p.SetIcon(vd.cfg_script ? Icons.toggle_green : Icons.toggle_red, tooltip, () => p.Toggle(ref vd.cfg_script));
      }
    }
  }
}