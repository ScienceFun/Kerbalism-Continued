using System.Collections.Generic;

namespace KERBALISM
{
  public abstract class AdvancedECBase : PartModule
  {
    [KSPField] public double extra_Cost = 0;            // extra energy cost to keep the part active
    [KSPField] public double extra_Deploy = 0;          // extra eergy cost to do a deploy(animation)

    [KSPField(guiName = "EC Usage", guiUnits = "/sec", guiActive = false, guiFormat = "F3")]
    public double actualCost = 0;                       // Energy Consume

    public bool hasEnergy;                              // Check if vessel has energy, otherwise will disable animations and functions
    public bool isConsuming;                            // Module is consuming energy

    public bool isInitialized;
    public bool hasEnergyChanged;                       // Energy state has changed since last update?

    public Resource_Info resources;

    public virtual void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // get ec resource handler
        resources = ResourceCache.Info(vessel, "ElectricCharge");
        hasEnergy = resources.amount > double.Epsilon;

        // Update UI only if hasEnergy has changed or if is the first time
        if (hasEnergyChanged != hasEnergy || !isInitialized)
        {
          // Update UI
          OnGUI(hasEnergy);
        }

        if (!hasEnergy)
        {
          actualCost = 0;
          isConsuming = false;
        }
        else
        {
          isConsuming = GetIsConsuming();
        }
      }
    }

    public virtual void FixedUpdate()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        if (hasEnergyChanged != hasEnergy || !isInitialized)
        {
          Lib.Debug("Energy state has changed: {0}", hasEnergy);
          FixModule(hasEnergy);

          hasEnergyChanged = hasEnergy;
          isInitialized = true;
        }

        // If has energym and isConsuming
        if (isConsuming)
        {
          if (resources != null) resources.Consume(actualCost * Kerbalism.elapsed_s);
        }
      }
    }

    public abstract bool GetIsConsuming();

    public abstract void OnGUI(bool hasEnergy);

    public abstract void FixModule(bool hasEnergy);

    public void ToggleActions(PartModule partModule, bool value)
    {
      foreach (BaseAction ac in partModule.Actions)
      {
        ac.active = value;
      }
    }
  }
}
