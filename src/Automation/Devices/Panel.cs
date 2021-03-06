﻿namespace KERBALISM 
{
  public sealed class PanelDevice : Device
  {
    public PanelDevice(ModuleDeployableSolarPanel panel)
    {
      this.panel = panel;
    }

    public override string Name()
    {
      return "solar panel";
    }

    public override uint Part()
    {
      return panel.part.flightID;
    }

    public override string Info()
    {
      if (!panel.isTracking) return "fixed";
      switch(panel.deployState)
      {
        case ModuleDeployablePart.DeployState.EXTENDED:   return "<color=cyan>extended</color>";
        case ModuleDeployablePart.DeployState.RETRACTED:  return "<color=red>retracted</color>";
        case ModuleDeployablePart.DeployState.BROKEN:     return "<color=red>broken</color>";
        case ModuleDeployablePart.DeployState.EXTENDING:  return "extending";
        case ModuleDeployablePart.DeployState.RETRACTING: return "retracting";
      }
      return "unknown";
    }

    public override void Ctrl(bool value)
    {
      if (!panel.isTracking) return;
      if (!value && !panel.retractable) return;
      if (panel.deployState == ModuleDeployablePart.DeployState.BROKEN) return;
      if (value) panel.Extend();
      else panel.Retract();
    }

    public override void Toggle()
    {
      Ctrl(panel.deployState != ModuleDeployablePart.DeployState.EXTENDED);
    }

    ModuleDeployableSolarPanel panel;
  }

  public sealed class ProtoPanelDevice : Device
  {
    public ProtoPanelDevice(ProtoPartModuleSnapshot panel, ModuleDeployableSolarPanel prefab, uint part_id)
    {
      this.panel = panel;
      this.prefab = prefab;
      this.part_id = part_id;
    }

    public override string Name()
    {
      return "solar panel";
    }

    public override uint Part()
    {
      return part_id;
    }

    public override string Info()
    {
      if (!prefab.isTracking) return "fixed";
      string state = Lib.Proto.GetString(panel, "deployState");
      switch(state)
      {
        case "EXTENDED":  return "<color=cyan>extended</color>";
        case "RETRACTED": return "<color=red>retracted</color>";
        case "BROKEN":    return "<color=red>broken</color>";
      }
      return "unknown";
    }

    public override void Ctrl(bool value)
    {
      if (!prefab.isTracking) return;
      if (!value && !prefab.retractable) return;
      if (Lib.Proto.GetString(panel, "deployState") == "BROKEN") return;
      Lib.Proto.Set(panel, "deployState", value ? "EXTENDED" : "RETRACTED");
    }

    public override void Toggle()
    {
      Ctrl(Lib.Proto.GetString(panel, "deployState") != "EXTENDED");
    }

    ProtoPartModuleSnapshot panel;
    ModuleDeployableSolarPanel prefab;
    uint part_id;
  }
}