using ModuleWheels;
using System.Collections.Generic;

namespace KERBALISM
{
  public sealed class AdvancedEC : PartModule
  {
    [KSPField] public string type;                      // component name
    [KSPField] public double extra_Cost = 0;            // extra energy cost to keep the part active
    [KSPField] public double extra_Deploy = 0;          // extra eergy cost to do a deploy(animation)
    PartModule module;                           // component cache, the Reliability.cs is one to many, instead the AdvancedEC will be one to one

    [KSPField(guiName = "EC Usage", guiUnits = "/sec", guiActive = false, guiFormat = "F3")]
    public double actualCost = 0;                       // Energy Consume

    public bool hasEnergy;                              // Check if vessel has energy, otherwise will disable animations and functions
    public bool isConsuming;                            // Module is consuming energy

    public bool isInitialized;
    public bool hasEnergyChanged;                       // Energy state has changed since last update?

    public Resource_Info resources;

    KeyValuePair<bool, double> modReturn;               // Return from ECDevice

    // Exclusive properties to special cases
    // CommNet Antennas
    [KSPField(isPersistant = true)] public double antennaPower;   // CommNet doesn't ignore ModuleDataTransmitter disabled, this way I have to set power to 0 to disable it.

    public override void OnStart(StartState state)
    {
      // do nothing in the editors and when compiling part or when advanced EC is not enabled
      if (!Lib.IsFlight() || !Features.AdvancedEC) return;

      // cache list of modules
      module = part.FindModulesImplementing<PartModule>().FindLast(k => k.moduleName == type);

      // setup UI
      Fields["actualCost"].guiActive = true;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      GUI_Update(hasEnergy);
    }

    public void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // Update UI only if hasEnergy has changed or if is the first time
        if (hasEnergyChanged != hasEnergy || !isInitialized)
        {
          Lib.Debug("Energy state has changed: {0}", hasEnergy);

          if (!isInitialized)
          {
            Lib.Debug("Initializing with hasEnergy = {0}", hasEnergy);
            if(Features.KCommNet) antennaPower = new AntennaEC(part.FindModuleImplementing<ModuleDataTransmitter>(), extra_Cost, extra_Deploy, antennaPower).Init(antennaPower);
          }

          // Update UI
          GUI_Update(hasEnergy);
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

        // Constantly Update UI for special modules
        Constant_OnGUI(hasEnergy);
      }
    }

    public void FixedUpdate()
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

    public bool GetIsConsuming()
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

    public void GUI_Update(bool b)
    {
      Lib.Debug("OnGUI for {0}", module.part.partInfo.title);
      switch (type)
      {
        case "ModuleDataTransmitter":
          new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GUI_Update(b);
          break;

        case "Antenna":
          new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).GUI_Update(b);
          break;

        case "ModuleWheelDeployment":
          new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).GUI_Update(b);
          break;

        case "ModuleColorChanger":
          new LightsEC(module as ModuleColorChanger, extra_Cost).GUI_Update(b);
          break;

        case "ModuleAnimationGroup":
          new AnimationGroupEC(module as ModuleAnimationGroup, extra_Deploy).GUI_Update(b);
          break;
      }
    }

    public void FixModule(bool b)
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
          new LightsEC(module as ModuleAnimateGeneric, extra_Cost).GUI_Update(b);
          break;
      }
    }
  }
}
