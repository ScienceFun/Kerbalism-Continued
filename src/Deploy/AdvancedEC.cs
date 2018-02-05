using ModuleWheels;
using System.Collections.Generic;

namespace KERBALISM
{
  public sealed class AdvancedEC : AdvancedECBase
  {
    [KSPField] public string type;                          // component name
    PartModule module;                                      // component cache, the Reliability.cs is one to many, instead the AdvancedEC will be one to one

    KeyValuePair<bool, double> modReturn;                   // Return from ECDevice

    // Exclusive properties to special cases
    // CommNet Antennas
    [KSPField(isPersistant = true)] public double antennaPower;   // CommNet doesn't ignore ModuleDataTransmitter disabled, this way I have to set power to 0 to disable it.

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // do nothing in the editors and when compiling part or when advanced EC is not enabled
      if (!Lib.IsFlight() || !Features.AdvancedEC) return;

      // cache list of modules
      module = part.FindModulesImplementing<PartModule>().FindLast(k => k.moduleName == type);

      // setup UI
      Fields["actualCost"].guiActive = true;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;
    }

    public override void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        base.Update();

        // Update UI only if hasEnergy has changed or if is the first time
        if (hasEnergyChanged != hasEnergy || !isInitialized)
        {
          if (!isInitialized)
          {
            Lib.Debug("Initializing with hasEnergy = {0}", hasEnergy);
            if(Features.KCommNet) antennaPower = new AntennaEC(part.FindModuleImplementing<ModuleDataTransmitter>(), extra_Cost, extra_Deploy, antennaPower).Init(antennaPower);
          }
        }
        // Constantly Update UI for special modules
        Constant_OnGUI(hasEnergy);
      }
    }

    public override bool GetIsConsuming()
    {
      switch (type)
      {
        // TODO: code review, maybe is better to separate Antennas from main AdvancedEC class
        case "ModuleDataTransmitter":
          modReturn = new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;

        case "Antenna":
          modReturn = new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;

        case "ModuleWheelDeployment":
          modReturn = new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;

        case "ModuleColorChanger":
          modReturn = new LightsEC(module as ModuleColorChanger, extra_Cost).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;

        case "ModuleAnimationGroup":
          modReturn = new AnimationGroupEC(module as ModuleAnimationGroup, extra_Deploy).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;
      }
      actualCost = extra_Deploy;
      return true;
    }

    public override void OnGUI(bool b)
    {
      switch (type)
      {
        case "ModuleDataTransmitter":
          new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).UI_Update(b);
          break;

        case "Antenna":
          new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).UI_Update(b);
          break;

        case "ModuleWheelDeployment":
          new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).UI_Update(b);
          break;

        case "ModuleColorChanger":
          new LightsEC(module as ModuleColorChanger, extra_Cost).UI_Update(b);
          break;

        case "ModuleAnimationGroup":
          new AnimationGroupEC(module as ModuleAnimationGroup, extra_Deploy).UI_Update(b);
          break;
      }
    }

    public override void FixModule(bool b)
    {
      switch (type)
      {
        case "ModuleDataTransmitter":
          new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).FixModule(b);
          break;

        case "Antenna":
          new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).FixModule(b);
          break;

        case "ModuleWheelDeployment":
          new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).FixModule(b);
          break;

        case "ModuleAnimateGeneric":
          new LightsEC(module as ModuleAnimateGeneric, extra_Cost).FixModule(b);
          break;

        case "ModuleColorChanger":
          new LightsEC(module as ModuleColorChanger, extra_Cost).FixModule(b);
          break;

        case "ModuleAnimationGroup":
          new AnimationGroupEC(module as ModuleAnimationGroup, extra_Deploy).FixModule(b);
          break;
      }
    }

    // Some modules need to constantly update UI
    public void Constant_OnGUI(bool b)
    {
      switch (type)
      {
        case "ModuleAnimateGeneric":
          new LightsEC(module as ModuleAnimateGeneric, extra_Cost).UI_Update(b);
          break;
      }
    }
  }
}
