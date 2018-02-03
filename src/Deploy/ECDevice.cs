using System.Collections.Generic;

namespace KERBALISM
{
  public abstract class ECDevice
  {
    public abstract KeyValuePair<bool, double> GetConsume();

    public abstract bool IsConsuming { get; }

    public abstract void UI_Update(bool hasEnergy);

    public void ToggleActions(PartModule partModule, bool value)
    {
      Lib.Debug("'{0}' module, setting actions to {1}", partModule.moduleName, value ? "ON" : "OFF");
      foreach (BaseAction ac in partModule.Actions)
      {
        ac.active = value;
      }
    }
  }
}
