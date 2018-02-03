using System.Collections.Generic;
using UnityEngine;

// This class is a Reliability modified.
namespace KERBALISM
{
  public sealed class AdvancedEC : PartModule
  {
    [KSPField] public string type;                          // component name
    [KSPField] public double extra_Cost = 0;                // extra energy cost to keep the part active
    [KSPField] public double extra_Deploy = 0;              // extra eergy cost to do a deploy(animation)

    [KSPField(guiName = "EC Usage", guiUnits = "/sec", guiActive = false, guiFormat = "F3")]
    public double actualCost = 0;                           // Show Energy Consume
    List<PartModule> modules;                               // components cache

    bool hasEnergy;                                         // Check if vessel has energy, otherwise will disable animations and functions
    bool isConsuming;                                       // Device is consuming energy
    
    bool isInitialized;                                     // 
    bool hasEnergyChanged;                                  //

    KeyValuePair<bool, double> modReturn;                   // Return from ECDevice
    Resource_Info resources;                                // Vessel resources

    // Exclusive properties to special cases
    // CommNet Antennas
    [KSPField(isPersistant = true)] public double antennaPower;   // CommNet don't ignore ModuleDataTransmitter disabled, this way I have to set power to 0 to disable it.


    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // do nothing in the editors and when compiling part or when advanced EC is not enabled
      if (!Lib.IsFlight() || !Features.AdvancedEC) return;

      // cache list of modules
      modules = part.FindModulesImplementing<PartModule>().FindAll(k => k.moduleName == type);

      // setup UI
      Fields["actualCost"].guiActive = true;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      // sync monobehaviour state with module state
      // - required as the monobehaviour state is not serialized
      if (!hasEnergy)
      {
        foreach (PartModule m in modules)
        {
          m.enabled = false;
        }
      }
    }

    public void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // get energy from cache
        resources = ResourceCache.Info(vessel, "ElectricCharge");
        hasEnergy = resources.amount > double.Epsilon;

        // UI update only if hasEnergy has changed or if is the first time
        if(hasEnergyChanged != hasEnergy || !isInitialized)
        {
          // UI
          UI_Update(hasEnergy);

          // enforce state
          foreach (PartModule m in modules)
          {
            m.enabled = hasEnergy;
            m.isEnabled = hasEnergy;
          }

          if (!isInitialized)
          {
            Lib.Debug("Initializing with hasEnergy = {0}", hasEnergy);
            antennaPower = new AntennaEC(part.FindModuleImplementing<ModuleDataTransmitter>(), extra_Cost, extra_Deploy, antennaPower).Init(antennaPower);
          }
          Lib.Debug("Energy state has changed: {0}", hasEnergy);
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

    public void FixedUpdate()
    {
      // do nothing in the editor
      if (Lib.IsEditor()) return;

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

    public bool GetIsConsuming()
    {
      switch (type)
      {
        case "ModuleDataTransmitter":
          ModuleDataTransmitter x = part.FindModuleImplementing<ModuleDataTransmitter>();
          modReturn = new AntennaEC(x, extra_Cost, extra_Deploy, antennaPower).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;
      }
      actualCost = extra_Deploy;
      return true;
    }

    // apply type-specific hacks to enable/disable the module
    void UI_Update(bool b)
    {
      switch (type)
      {
        case "ModuleDataTransmitter":
          ModuleDataTransmitter x = part.FindModuleImplementing<ModuleDataTransmitter>();
          new AntennaEC(x, extra_Cost, extra_Deploy, antennaPower).UI_Update(b);
          break;
      }
    }

    void FixModule(bool b)
    {
      switch (type)
      {
        case "ModuleDataTransmitter":
          ModuleDataTransmitter x = part.FindModuleImplementing<ModuleDataTransmitter>();
          new AntennaEC(x, extra_Cost, extra_Deploy, antennaPower).FixCommNetAntenna(b);
          break;
      }
    }
  }
}
