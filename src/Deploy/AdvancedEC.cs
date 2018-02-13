using ModuleWheels;
using System;
using System.Collections.Generic;

namespace KERBALISM
{
  public class AdvancedEC : PartModule
  {
    [KSPField] public string type;                      // component name
    [KSPField] public double extra_Cost = 0;            // extra energy cost to keep the part active
    [KSPField] public double extra_Deploy = 0;          // extra eergy cost to do a deploy(animation)

    [KSPField(isPersistant = true, guiName = "IsBroken", guiUnits = "", guiActive = false, guiFormat = "")] public bool broken;// true if broken
    public bool lastBrokenState;                        // broken state has changed since last update?
    public bool lastFixedBrokenState;                   // broken state has changed since last fixed update?

    [KSPField(guiName = "EC Usage", guiUnits = "/s", guiActive = false, guiFormat = "F3")]
    public double actualCost = 0;                       // Energy Consume

    public bool hasEnergy;                              // Check if vessel has energy, otherwise will disable animations and functions
    public bool isConsuming;                            // Module is consuming energy
    public bool hasEnergyChanged;                       // Energy state has changed since last update?
    public bool hasFixedEnergyChanged;                  // Energy state has changed since last fixed update?

    public Resource_Info resources;

    public PartModule module;                           // component cache, the Reliability.cs is one to many, instead the AdvancedEC will be one to one
    public KeyValuePair<bool, double> modReturn;        // Return from ECDevice

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios & do something only in Flight scenario
      if (Lib.DisableScenario(this) || !Lib.IsFlight()) return;

      Lib.Debug("Executing OnStart");
      // cache list of modules
      module = part.FindModulesImplementing<PartModule>().FindLast(k => k.moduleName == type);

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      // Force the update to run at least once
      lastBrokenState = !broken;
      hasEnergyChanged = !hasEnergy;
      hasFixedEnergyChanged = !hasEnergy;

#if DEBUG
      // setup UI
      Fields["actualCost"].guiActive = true;
      Fields["broken"].guiActive = true;
#endif
    }

    public override void OnUpdate()
    {
      if (!Lib.IsFlight() || module == null) return;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      // Update UI only if hasEnergy has changed or if is broken state has changed
      if (broken)
      {
        if (broken != lastBrokenState)
        {
          lastBrokenState = broken;
          Update_UI(!broken);
        }
      }
      else if (hasEnergyChanged != hasEnergy)
      {
        Lib.Debug("Energy state has changed: {0}", hasEnergy);
        
        // Wait 1 second before enabled UI.
        if (hasEnergy) Lib.Delay(1f);

        hasEnergyChanged = hasEnergy;
        lastBrokenState = false;
        // Update UI
        Update_UI(hasEnergy);
      }
      // Constantly Update UI for special modules
      if (broken) Constant_OnGUI(!broken);
      else Constant_OnGUI(hasEnergy);

      if (!hasEnergy || broken)
      {
        actualCost = 0;
        isConsuming = false;
      }
      else
      {
        isConsuming = GetIsConsuming();
      }
    }

    public virtual void FixedUpdate()
    {
      if (!Lib.IsFlight() || module == null) return;

      if (broken)
      {
        if (broken != lastFixedBrokenState)
        {
          lastFixedBrokenState = broken;
          FixModule(!broken);
        }
      }
      else if (hasFixedEnergyChanged != hasEnergy)
      {
        // Wait 1 second before start consum EC.
        if (hasEnergy) Lib.Delay(1f);

        hasFixedEnergyChanged = hasEnergy;
        lastFixedBrokenState = false;
        // Update module
        FixModule(hasEnergy);
      }

      // If isConsuming
      if (isConsuming && resources != null) resources.Consume(actualCost * Kerbalism.elapsed_s);
    }

    public virtual bool GetIsConsuming()
    {
      try
      {
        switch (type)
        {
          case "ModuleWheelDeployment":
            modReturn = new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).GetConsume();
            actualCost = modReturn.Value;
            return modReturn.Key;

          case "ModuleColorChanger":
            modReturn = new ModuleAnimateGenericEC(module as ModuleColorChanger, extra_Cost).GetConsume();
            actualCost = modReturn.Value;
            return modReturn.Key;

          case "ModuleAnimateGeneric":
            modReturn = new ModuleAnimateGenericEC(module as ModuleAnimateGeneric, extra_Cost).GetConsume();
            actualCost = modReturn.Value;
            return modReturn.Key;

          case "ModuleAnimationGroup":
            modReturn = new AnimationGroupEC(module as ModuleAnimationGroup, extra_Cost, extra_Deploy).GetConsume();
            actualCost = modReturn.Value;
            return modReturn.Key;
        }
      }
      catch (Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
      actualCost = extra_Deploy;
      return true;
    }

    public virtual void Update_UI(bool isEnabled)
    {
      try
      {
        switch (type)
        {
          case "ModuleWheelDeployment":
            new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).GUI_Update(isEnabled);
            break;

          case "ModuleColorChanger":
            new ModuleAnimateGenericEC(module as ModuleColorChanger, extra_Cost).GUI_Update(isEnabled);
            break;
        }
      }
      catch (Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
    }

    public virtual void FixModule(bool isEnabled)
    {
      try
      {
        switch (type)
        {
          case "ModuleWheelDeployment":
            new LandingGearEC(module as ModuleWheelDeployment, extra_Deploy).FixModule(isEnabled);
            break;

          case "ModuleAnimateGeneric":
            new ModuleAnimateGenericEC(module as ModuleAnimateGeneric, extra_Cost).FixModule(isEnabled);
            break;

          case "ModuleColorChanger":
            new ModuleAnimateGenericEC(module as ModuleColorChanger, extra_Cost).FixModule(isEnabled);
            break;

          case "ModuleAnimationGroup":
            new AnimationGroupEC(module as ModuleAnimationGroup, extra_Cost, extra_Deploy).FixModule(isEnabled);
            break;
        }
      }
      catch (Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
    }

    // Some modules need to constantly update UI
    public virtual void Constant_OnGUI(bool isEnabled)
    {
      try
      {
        switch (type)
        {
          case "ModuleAnimateGeneric":
            new ModuleAnimateGenericEC(module as ModuleAnimateGeneric, extra_Cost).GUI_Update(isEnabled);
            break;

          case "ModuleAnimationGroup":
            new AnimationGroupEC(module as ModuleAnimationGroup, extra_Cost, extra_Deploy).GUI_Update(isEnabled);
            break;
        }
      }
      catch (Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
    }

    public void ToggleActions(PartModule partModule, bool value)
    {
      Lib.Debug("Part '{0}'.'{1}', setting actions to {2}", partModule.part.partInfo.title, partModule.moduleName, value ? "ON" : "OFF");
      foreach (BaseAction ac in partModule.Actions)
      {
        ac.active = value;
      }
    }
  }
}