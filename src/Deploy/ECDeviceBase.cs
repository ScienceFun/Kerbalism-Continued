using System.Collections.Generic;

namespace KERBALISM
{
  public abstract class ECDeviceBase
  {
    public KeyValuePair<bool, double> GetConsume()
    {
      return new KeyValuePair<bool, double>(IsConsuming, actualCost);
    }

    protected abstract bool IsConsuming { get; }

    public abstract void UI_Update(bool hasEnergy);

    public abstract void FixModule(bool hasEnergy);

    public void ToggleActions(PartModule partModule, bool value)
    {
      Lib.Debug("'{0}' module, setting actions to {1}", partModule.moduleName, value ? "ON" : "OFF");
      foreach (BaseAction ac in partModule.Actions)
      {
        ac.active = value;
      }
    }

    // Return
    public double actualCost;
    public double extra_Cost;
    public double extra_Deploy;
  }
}
