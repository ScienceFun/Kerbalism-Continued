namespace KERBALISM
{
  public class ModuleAnimateGenericEC : ECDeviceBase
  {
    public ModuleAnimateGenericEC(ModuleAnimateGeneric light, double extra_Cost)
    {
      animateGeneric = light;
      this.extra_Cost = extra_Cost;
    }

    public ModuleAnimateGenericEC(ModuleColorChanger light, double extra_Cost)
    {
      colorChanger = light;
      this.extra_Cost = extra_Cost;
    }

    protected override bool IsConsuming
    {
      get
      {
        if (animateGeneric != null)
        {
          if (animateGeneric.animSpeed > 0)
          {
            actualCost = extra_Cost;
            return true;
          }
        }
        else if (colorChanger != null)
        {
          if (colorChanger.animState)
          {
            actualCost = extra_Cost;
            return true;
          }
        }
        return false;
      }
    }

    public override void GUI_Update(bool hasEnergy)
    {
      if (animateGeneric != null)
      {
        // Do not add log here, this interface has constantly update
        animateGeneric.Events["Toggle"].active = hasEnergy;
      }
      else if (colorChanger != null)
      {
        Lib.Debug("Buttons is '{0}' for '{1}' light2", (hasEnergy == true ? "ON" : "OFF"), colorChanger.part.partInfo.title);
        colorChanger.Events["ToggleEvent"].active = hasEnergy;
      }
    }

    public override void FixModule(bool hasEnergy)
    {
      if (animateGeneric != null)
      {
        if (animateGeneric.animSpeed > 0) animateGeneric.Toggle();
        ToggleActions(animateGeneric, hasEnergy);
      }
      else if (colorChanger != null)
      {
        if (colorChanger.animState) colorChanger.ToggleEvent();
        ToggleActions(colorChanger, hasEnergy);
      }
    }

    // Light types
    ModuleAnimateGeneric animateGeneric;
    ModuleColorChanger colorChanger;
  }
}