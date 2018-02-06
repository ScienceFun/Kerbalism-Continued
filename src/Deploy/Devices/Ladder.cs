﻿namespace KERBALISM
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

    [KSPField(guiName = "Status", guiActive = false)]
    string moving = "Moving";

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this) || !Lib.IsFlight()) return;

      ladder = part.FindModuleImplementing<RetractableLadder>();
      // Replace the OnGUI
      if (ladder != null)
      {
        ladder.Events["Retract"].guiActive = ladder.Events["Retract"].guiActiveUnfocused = false;
        ladder.Events["Extend"].guiActive = ladder.Events["Extend"].guiActiveUnfocused = false;
      }
    }

    public override bool GetIsConsuming()
    {
      if (targetState == "") targetState = ladder.StateName;
      if (targetState == ladder.StateName)
      {
        isPlaying = false;
        return false;
      }

      actualCost = extra_Deploy;
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
