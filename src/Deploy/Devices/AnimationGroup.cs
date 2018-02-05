namespace KERBALISM
{
  public class AnimationGroupEC : ECDeviceBase
  {
    public AnimationGroupEC(ModuleAnimationGroup module, double extra_Deploy)
    {
      this.module = module;
      this.extra_Deploy = extra_Deploy;
    }

    protected override bool IsConsuming
    {
      get
      {
        if (module.ActiveAnimation.isPlaying)
        {
          actualCost = extra_Deploy;
          return true;
        }
        return false;
      }
    }

    public override void GUI_Update(bool hasEnergy)
    {
      Lib.Debug("Buttons is '{0}' for '{1}' moduleAnimationGroup", (hasEnergy == true ? "ON" : "OFF"), module.part.partInfo.title);
      module.Events["RetractModule"].guiActive = module.isDeployed && hasEnergy;
      module.Events["RetractModule"].guiActiveUnfocused = module.isDeployed && hasEnergy;
      module.Events["DeployModule"].guiActive = !module.isDeployed && hasEnergy;
      module.Events["DeployModule"].guiActiveUnfocused = !module.isDeployed && hasEnergy;
    }

    public override void FixModule(bool hasEnergy)
    {
      ToggleActions(module, hasEnergy);
    }

    ModuleAnimationGroup module;
  }
}
