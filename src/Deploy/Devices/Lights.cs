namespace KERBALISM
{
  public class LightsEC : ECDeviceBase
  {
    public LightsEC(ModuleAnimateGeneric light, double extra_Cost)
    {
      light1 = light;
      this.extra_Cost = extra_Cost;
    }

    public LightsEC(ModuleColorChanger light, double extra_Cost)
    {
      light2 = light;
      this.extra_Cost = extra_Cost;
    }

    protected override bool IsConsuming
    {
      get
      {
        if (light1 != null)
        {
          if (light1.animSpeed > 0)
          {
            actualCost = extra_Cost;
            return true;
          }
        }
        else if (light2 != null)
        {
          if (light2.animState)
          {
            actualCost = extra_Cost;
            return true;
          }
        }
        return false;
      }
    }

    public override void UI_Update(bool hasEnergy)
    {
      if (light1 != null)
      {
        // Do not add log here, this interface has constantly update
        light1.Events["Toggle"].active = hasEnergy;
      }
      else if (light2 != null)
      {
        Lib.Debug("Buttons is '{0}' for '{1}' light2", (hasEnergy == true ? "ON" : "OFF"), light2.part.partInfo.title);
        light2.Events["ToggleEvent"].active = hasEnergy;
      }
    }

    public override void FixModule(bool hasEnergy)
    {
      if (light1 != null)
      {
        if (light1.animSpeed > 0) light1.Toggle();
        ToggleActions(light1, hasEnergy);
      }
      else if (light2 != null)
      {
        if (light2.animState) light2.ToggleEvent();
        ToggleActions(light2, hasEnergy);
      }
    }

    // Light types
    ModuleAnimateGeneric light1;
    ModuleColorChanger light2;
  }
}