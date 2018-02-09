namespace KERBALISM
{
  public class LadderEC : AdvancedEC
  {
    RetractableLadder ladder;

    // I have to replaced the Events because, I cannot access the Animation values
    [KSPEvent(guiActive = false, guiName = "#autoLOC_6001411", active = true, guiActiveUnfocused = true, unfocusedRange = 4f)]
    public void ExtendLadder()
    {
      ladder.Extend();
      targetState = "Extended";
      isPlaying = true;
    }

    [KSPEvent(guiActive = false, guiName = "#autoLOC_6001412", active = true, guiActiveUnfocused = true, unfocusedRange = 4f)]
    private void RetractLadder()
    {
      ladder.Retract();
      targetState = "Retracted";
      isPlaying = true;
    }

    // Controllers to know when the animation is playing
    public string targetState = "";
    bool isPlaying;
    bool isPlayed;

    [KSPField(guiName = "Status", guiActive = false)]
    string moving = "Moving";

    public override void OnStart(StartState state)
    {
      base.OnStart(state);
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this) || !Lib.IsFlight()) return;

      ladder = part.FindModuleImplementing<RetractableLadder>();

      isPlayed = !isPlaying;

      // Replace the OnGUI
      ladder.Events["Retract"].guiActive = ladder.Events["Retract"].guiActiveUnfocused = false;
      ladder.Events["Extend"].guiActive = ladder.Events["Extend"].guiActiveUnfocused = false;
    }

    public override void OnUpdate()
    {
      if (!Lib.IsFlight()) return;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      if (!hasEnergy)
      {
        actualCost = 0;
        isConsuming = false;
      }
      else
      {
        isConsuming = GetIsConsuming();
      }

      if(isPlayed != isPlaying)
      {
        isPlayed = isPlaying;
        Update_UI(hasEnergy);
      }
    }

    public override void FixedUpdate()
    {
      if (!Lib.IsFlight()) return;

      if (hasEnergyChanged != hasEnergy)
      {
        // Update module
        FixModule(hasEnergy);
      }

      // If isConsuming
      if (isConsuming && resources != null) resources.Consume(actualCost * Kerbalism.elapsed_s);
    }

    public override bool GetIsConsuming()
    {
      if (targetState == "") targetState = ladder.StateName;
      if (targetState == ladder.StateName)
      {
        isPlaying = false;
        actualCost = 0;
      }
      else
      {
        actualCost = extra_Deploy;
      }
      return isPlaying;
    }

    public override void Update_UI(bool isEnabled)
    {
      Events["RetractLadder"].guiActive = Events["RetractLadder"].guiActiveUnfocused = (targetState != "Retracted" && isEnabled && !isPlaying);
      Events["ExtendLadder"].guiActive = Events["ExtendLadder"].guiActiveUnfocused = (targetState == "Retracted" && isEnabled && !isPlaying);
      Fields["moving"].guiActive = isPlaying;
    }

    public override void FixModule(bool isEnabled)
    {
      ToggleActions(ladder, isEnabled);
    }
  }
}