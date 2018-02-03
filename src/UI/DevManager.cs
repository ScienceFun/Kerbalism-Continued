using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM 
{
  public static class DevManager
  {
    public static void DevMan(this Panel p, Vessel v)
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
      p.Title(Lib.BuildString(Lib.Ellipsis(v.vesselName, 24), " <color=#cccccc>DEV MANAGER</color>"));

      // time-out simulation
      if (p.Timeout(vi)) return;

      // get devices
      Dictionary<uint,Device> devices = Computer.Boot(v);

      // direct control
      if (script_index == 0)
      {
        // draw section title and desc
        p.SetSection
        (
          "DEVICES",
          Description(),
          () => p.Prev(ref script_index, (int)ScriptType.last),
          () => p.Next(ref script_index, (int)ScriptType.last)
        );

        // for each device
        foreach(var pair in devices)
        {
          // render device entry
          Device dev = pair.Value;
          p.SetContent(dev.Name(), dev.Info(), string.Empty, dev.Toggle, () => Highlighter.Set(dev.Part(), Color.cyan));
        }
      }
      // script editor
      else
      {
        // get script
        ScriptType script_type = (ScriptType)script_index;
        string script_name = script_type.ToString().Replace('_', ' ').ToUpper();
        Script script = DB.Vessel(v).computer.Get(script_type);

        // draw section title and desc
        p.SetSection
        (
          script_name,
          Description(),
          () => p.Prev(ref script_index, (int)ScriptType.last),
          () => p.Next(ref script_index, (int)ScriptType.last)
        );

        // for each device
        foreach(var pair in devices)
        {
          // determine tribool state
          int state = !script.states.ContainsKey(pair.Key)
            ? -1
            : !script.states[pair.Key]
            ? 0
            : 1;

          // render device entry
          Device dev = pair.Value;
          p.SetContent
          (
            dev.Name(),
            state == -1 ? "<color=#999999>don't care</color>" : state == 0 ? "<color=red>off</color>" : "<color=cyan>on</color>",
            string.Empty,
            () =>
            {
              switch(state)
              {
                case -1: script.Set(dev, true);  break;
                case  0: script.Set(dev, null);  break;
                case  1: script.Set(dev, false); break;
              }
            },
            () => Highlighter.Set(dev.Part(), Color.cyan)
          );
        }
      }

      // no devices case
      if (devices.Count == 0)
      {
        p.SetContent("<i>no devices</i>");
      }
    }

    // return short description of a script, or the time-out message
    static string Description()
    {
      if (script_index == 0)          return "<i>Control vessel components directly</i>";
      switch((ScriptType)script_index)
      {
        case ScriptType.landed:       return "<i>Called on landing</i>";
        case ScriptType.atmo:         return "<i>Called on entering atmosphere</i>";
        case ScriptType.space:        return "<i>Called on reaching space</i>";
        case ScriptType.sunlight:     return "<i>Called when sun became visible</i>";
        case ScriptType.shadow:       return "<i>Called when sun became occluded</i>";
        case ScriptType.power_high:   return "<i>Called when EC level goes above 80%</i>";
        case ScriptType.power_low:    return "<i>Called when EC level goes below 20%</i>";
        case ScriptType.rad_high:     return "<i>Called when radiation exceed 0.05 rad/h</i>";
        case ScriptType.rad_low:      return "<i>Called when radiation goes below 0.02 rad/h</i>";
        case ScriptType.linked:       return "<i>Called when signal is regained</i>";
        case ScriptType.unlinked:     return "<i>Called when signal is lost</i>";
        case ScriptType.eva_out:      return "<i>Called when going out on EVA</i>";
        case ScriptType.eva_in:       return "<i>Called when returning from EVA</i>";
        case ScriptType.action1:      return "<i>Called by pressing <b>1</b> on the keyboard</i>";
        case ScriptType.action2:      return "<i>Called by pressing <b>2</b> on the keyboard</i>";
        case ScriptType.action3:      return "<i>Called by pressing <b>3</b> on the keyboard</i>";
        case ScriptType.action4:      return "<i>Called by pressing <b>4</b> on the keyboard</i>";
        case ScriptType.action5:      return "<i>Called by pressing <b>5</b> on the keyboard</i>";
      }
      return string.Empty;
    }

    // mode/script index
    static int script_index;
  }
}