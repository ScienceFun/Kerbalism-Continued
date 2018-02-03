namespace KERBALISM 
{
  public sealed class AntennaDevice : Device
  {
    public AntennaDevice(Antenna antenna)
    {
      this.antenna = antenna;
      animator = antenna.part.FindModuleImplementing<ModuleAnimationGroup>();
      if (!Features.AdvancedEC) has_ec = true;
      else has_ec = ResourceCache.Info(antenna.part.vessel, "ElectricCharge").amount > double.Epsilon;
    }

    public AntennaDevice(ModuleDataTransmitter transmitter)
    {
      this.transmitter = transmitter;
      stockAnim = this.transmitter.part.FindModuleImplementing<ModuleDeployableAntenna>();
      if (!Features.AdvancedEC) has_ec = true;
      has_ec = ResourceCache.Info(transmitter.part.vessel, "ElectricCharge").amount > double.Epsilon;
    }

    public override string Name()
    {
      return "antenna";
    }

    public override uint Part()
    {
      if (Features.KCommNet) return transmitter.part.flightID;
      else return antenna.part.flightID;
    }

    public override string Info()
    {
      if (Features.AdvancedEC)
      {
        return !has_ec
          ? "<color=orange>inactive</color>"
          : (Features.KCommNet ? stockAnim == null : animator == null)
          ? "fixed"
          : (Features.KCommNet ? stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED : animator.isDeployed)
          ? "<color=cyan>deployed</color>"
          : Features.KCommNet
          ? (stockAnim.deployState == ModuleDeployablePart.DeployState.BROKEN ? "<color=red>broken</color>" : "<color=red>retracted</color>")
          : "<color=red>retracted</color>";
      }
      else
      {
        return (Features.KCommNet ? stockAnim == null : animator == null)
          ? "fixed"
          : (Features.KCommNet ? stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED : animator.isDeployed)
          ? "<color=cyan>deployed</color>"
          : "<color=red>retracted</color>";
      }
    }

    public override void Ctrl(bool value)
    {
      if (!has_ec) return;

      if (Features.KCommNet)
      {
        if (stockAnim.deployState != ModuleDeployablePart.DeployState.EXTENDED && value) stockAnim.Extend();
        else if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED && !value) stockAnim.Retract();
      }
      else
      {
        if (!antenna.extended && value) animator.DeployModule();
        else if (antenna.extended && !value) animator.RetractModule();
      }
    }

    public override void Toggle()
    {
      if (Features.KCommNet && stockAnim!= null)
      {
        if (stockAnim.deployState != ModuleDeployablePart.DeployState.EXTENDED) Ctrl(true);
        else if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED) Ctrl(false);
      }
      else if (animator != null)
      {
        Ctrl(!antenna.extended);
      }
    }

    Antenna antenna;
    ModuleAnimationGroup animator;
    ModuleDataTransmitter transmitter;
    ModuleDeployableAntenna stockAnim;
    bool has_ec;
  }

  public sealed class ProtoAntennaDevice : Device
  {
    public ProtoAntennaDevice(ProtoPartModuleSnapshot antenna, uint part_id, Vessel v)
    {
      if (!Features.AdvancedEC) has_ec = true;
      else has_ec = ResourceCache.Info(v, "ElectricCharge").amount > double.Epsilon;

      if (Features.KCommNet)
      {
        this.antenna = FlightGlobals.FindProtoPartByID(part_id).FindModule("ModuleDataTransmitter");
        this.animator = FlightGlobals.FindProtoPartByID(part_id).FindModule("ModuleDeployableAntenna");
      }
      else
      {
        this.antenna = antenna;
        this.animator = FlightGlobals.FindProtoPartByID(part_id).FindModule("ModuleAnimationGroup");
      }
      this.part_id = part_id;
      vessel = v;
    }

    public override string Name()
    {
      return "antenna";
    }

    public override uint Part()
    {
      return part_id;
    }

    public override string Info()
    {
      if (Features.AdvancedEC)
      {
        return !has_ec
           ? "<color=orange>inactive</color>"
           : animator == null
           ? "fixed"
           : (Features.KCommNet ? Lib.Proto.GetString(animator, "deployState") == "EXTENDED" : Lib.Proto.GetBool(animator, "isDeployed"))
           ? "<color=cyan>deployed</color>"
           : "<color=red>retracted</color>";
      }
      else
      {
        return animator == null
          ? "fixed"
           : (Features.KCommNet ? Lib.Proto.GetString(animator, "deployState") == "EXTENDED" : Lib.Proto.GetBool(animator, "isDeployed"))
          ? "<color=cyan>deployed</color>"
          : "<color=red>retracted</color>";
      }
    }

    public override void Ctrl(bool value)
    {
      if (has_ec && animator != null)
      {
        if (Features.KCommNet)
        {
          string status = value ? "EXTENDED" : "RETRACTED";
          Lib.Proto.Set(antenna, "canComm", value);
          Lib.Proto.Set(animator, "deployState", status);
        }
        else
        {
          Lib.Proto.Set(antenna, "extended", value);
          Lib.Proto.Set(animator, "isDeployed", value);
        }
      }
    }

    public override void Toggle()
    {
      if (animator != null)
      {
        if (Features.KCommNet) Ctrl(Lib.Proto.GetString(animator, "deployState") == "RETRACTED");
        else Ctrl(!Lib.Proto.GetBool(antenna, "extended"));
      }
    }

    ProtoPartModuleSnapshot antenna;
    ProtoPartModuleSnapshot animator;
    bool has_ec;
    Vessel vessel;
    uint part_id;
  }
}