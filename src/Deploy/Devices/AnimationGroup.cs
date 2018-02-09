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
        if (module.ActiveAnimation != null)
        {
          if (module.ActiveAnimation.isPlaying)
          {
            actualCost = extra_Deploy;
            return true;
          }
        }
        return false;
      }
    }

    public override void GUI_Update(bool hasEnergy)
    {
      module.Events["RetractModule"].guiActive = module.isDeployed && hasEnergy;
      module.Events["RetractModule"].guiActiveUnfocused = module.isDeployed && hasEnergy;
      module.Events["DeployModule"].guiActive = !module.isDeployed && hasEnergy;
      module.Events["DeployModule"].guiActiveUnfocused = !module.isDeployed && hasEnergy;
    }

    public override void FixModule(bool hasEnergy)
    {
      if (module.ActiveAnimation != null)
      {
        if (module.ActiveAnimation.isPlaying)
        {
          module.Events["RetractModule"].Invoke();
        }
      }
      ToggleActions(module, hasEnergy);
    }

    ModuleAnimationGroup module;
  }
}
