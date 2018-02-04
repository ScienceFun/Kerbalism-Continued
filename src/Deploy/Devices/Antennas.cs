using System.Collections.Generic;

namespace KERBALISM
{
  public class AntennaEC : ECDeviceBase
  {
    public AntennaEC(Antenna antenna, double extra_Cost, double extra_Deploy)
    {
      this.antenna = antenna;
      this.extra_Cost = extra_Cost;
      this.extra_Deploy = extra_Deploy;
      animator = antenna.part.FindModuleImplementing<ModuleAnimationGroup>();
    }

    public AntennaEC(ModuleDataTransmitter antenna, double extra_Cost, double extra_Deploy, double antennaPower)
    {
      transmitter = antenna;
      this.extra_Cost = extra_Cost;
      this.extra_Deploy = extra_Deploy;
      this.antennaPower = antennaPower;
      stockAnim = antenna.part.FindModuleImplementing<ModuleDeployableAntenna>();
      customAnim = antenna.part.FindModuleImplementing<ModuleAnimateGeneric>();
    }

    public double Init(double currentPower)
    {
      return (currentPower != transmitter.antennaPower && transmitter.antennaPower > 0 ? transmitter.antennaPower : currentPower);
    }

    protected override bool IsConsuming
    {
      get
      {
        if (Features.Signal)
        {
          if (animator != null)
          {
            if (animator.DeployAnimation.isPlaying)
            {
              actualCost = extra_Deploy;
              return true;
            }
            else if (animator.isDeployed || (Settings.ExtendedAntenna == false))
            {
              actualCost = extra_Cost;
              return true;
            }
          }
          else
          {
            // this means that antenna is fixed
            actualCost = extra_Cost;
            return true;
          }
        }
        else if (Features.KCommNet)
        {
          if (stockAnim != null)
          {
            if (stockAnim.deployState == ModuleDeployablePart.DeployState.RETRACTING || stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDING)
            {
              actualCost = extra_Deploy;
              return true;
            }
            else if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED || (Settings.ExtendedAntenna == false))
            {
              actualCost = extra_Cost;
              return true;
            }
          }
          else if (customAnim != null)
          {
            if (customAnim.aniState == ModuleAnimateGeneric.animationStates.MOVING)
            {
              actualCost = extra_Deploy;
              return true;
            }
            else if (customAnim.aniState > 0 || (Settings.ExtendedAntenna == false))
            {
              actualCost = extra_Cost;
              return true;
            }
            else
            {
              return false;
            }
          }
          else
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
      if (Features.Signal)
      {
        if (hasEnergy)
        {
          if (animator != null)
          {
            Lib.Debug("Activing buttons for '{0}' antenna", antenna.part.partInfo.title);
            if (animator.DeployAnimation.isPlaying)
            {
              animator.Events["RetractModule"].active = false;
              animator.Events["DeployModule"].active = false;
            }
            if (animator.isDeployed)
            {
              animator.Events["RetractModule"].active = true;
              animator.Events["DeployModule"].active = false;
            }
            else
            {
              animator.Events["RetractModule"].active = false;
              animator.Events["DeployModule"].active = true;
            }
          }
        }
        else
        {
          if (animator != null)
          {
            Lib.Debug("Desactiving buttons");
            // Don't allow extending/retracting when has no ec
            animator.Events["RetractModule"].active = false;
            animator.Events["DeployModule"].active = false;
          }
        }
      }
      else if (Features.KCommNet)
      {
        if (hasEnergy)
        {
          if (stockAnim != null)
          {
            Lib.Debug("Activing buttons for '{0}' antenna", transmitter.part.partInfo.title);
            if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED)
            {
              stockAnim.Events["Retract"].active = true;
              stockAnim.Events["Extend"].active = false;
            }
            else if (stockAnim.deployState == ModuleDeployablePart.DeployState.RETRACTED)
            {
              stockAnim.Events["Retract"].active = false;
              stockAnim.Events["Extend"].active = true;
            }
            else
            {
              stockAnim.Events["Retract"].active = false;
              stockAnim.Events["Extend"].active = false;
            }
          }
          else if (customAnim != null)
          {
            // Do not add log here, this interface has constantly update
            if (customAnim.aniState == ModuleAnimateGeneric.animationStates.MOVING)
            {
              customAnim.Events["RetractModule"].active = false;
              customAnim.Events["DeployModule"].active = false;
            }
            else if (customAnim.animSpeed > 0 || (Settings.ExtendedAntenna == false))
            {
              customAnim.Events["RetractModule"].active = true;
              customAnim.Events["DeployModule"].active = false;
            }
            else
            {
              customAnim.Events["RetractModule"].active = false;
              customAnim.Events["DeployModule"].active = true;
            }
          }
        }
        else
        {
          if (stockAnim != null)
          {
            Lib.Debug("Desactiving buttons");
            // Don't allow extending/retracting when has no ec
            stockAnim.Events["Retract"].active = false;
            stockAnim.Events["Extend"].active = false;
          }
          else if (customAnim != null)
          {
            // Do not add log here, this interface has constantly update
            // Don't allow extending/retracting when has no ec
            customAnim.Events["Toggle"].active = false;
          }
        }
      }
    }

    public override void FixModule(bool hasEnergy)
    {
      double right;
      if (Features.Signal)
      {
        // Makes antenna valid to AntennaInfo
        // TODO: review if has a better way to do it
        antenna.extended = hasEnergy;

        if (animator != null) ToggleActions(animator, hasEnergy);
      }
      else if (Features.KCommNet)
      {
        // Save antennaPower
        antennaPower = (antennaPower != transmitter.antennaPower && transmitter.antennaPower > 0 ? transmitter.antennaPower : antennaPower);
        right = hasEnergy ? antennaPower : 0;
        if (stockAnim != null)
        {
          ToggleActions(stockAnim, hasEnergy);
          if (stockAnim.deployState == ModuleDeployablePart.DeployState.EXTENDED)
          {
            // Recover antennaPower only if antenna is Extended
            Lib.Debug("Setting antenna '{0}' power: {1}",transmitter.part.partInfo.title, right);
            transmitter.antennaPower = right;
          }
          else
          {
            if (Settings.ExtendedAntenna)
            {
              Lib.Debug("Setting antenna '{0}' power: {1}", transmitter.part.partInfo.title, right);
              transmitter.antennaPower = right;
            }
            else
            {
              Lib.Debug("Setting antenna '{0}' power: {1}", transmitter.part.partInfo.title, 0);
              transmitter.antennaPower = 0;
            }
          }
        }
        else
        {
          // Recover antennaPower for fixed antenna
          Lib.Debug("Setting antenna '{0}' power: {1}", transmitter.part.partInfo.title, right);
          transmitter.antennaPower = right;
        }
      }
    }

    // Kerbalism Antenna
    Antenna antenna;
    ModuleAnimationGroup animator;

    // CommNet Antenna
    ModuleDataTransmitter transmitter;
    ModuleDeployableAntenna stockAnim;

    // Support to custom ModuleDataTransmitter
    public ModuleAnimateGeneric customAnim;

    double antennaPower;
  }

  public sealed class AntennasEC : PartModule
  {
    [KSPField(isPersistant = true)] public string type;                  // component name
    [KSPField] public double extra_Cost;                             // extra energy cost to keep the part active
    [KSPField] public double extra_Deploy;                           // extra eergy cost to do a deploy(animation)
    [KSPField(isPersistant = true)] public double antennaPower;   // CommNet doesn't ignore ModuleDataTransmitter disabled, this way I have to set power to 0 to disable it.

    PartModule module;                                            // component cache, the Reliability.cs is one to many, instead the AdvancedEC will be one to one

    [KSPField(guiName = "EC Usage", guiUnits = "/sec", guiActive = false, guiFormat = "F3")]
    double actualCost = 0;                                        // Energy Consume

    KeyValuePair<bool, double> modReturn;                         // Return from ECDevice

    bool hasEnergy;                                               // Check if vessel has energy, otherwise will disable animations and functions
    bool isConsuming;                                             // Module is consuming energy
    bool isInitialized;

    bool hasUpdateEnergyChanged;                                  // Energy state has changed since last update?
    bool hasFixedEnergyChanged;                                   // Energy state has changed since last update?

    public Resource_Info resources;
    public ModuleAnimateGeneric customAnim;                       // Support to custom animation for ModuleDataTransmitter module

    public override void OnStart(StartState state)
    {
      // do nothing in the editors and when compiling part or when advanced EC is not enabled
      if (!Lib.IsFlight() || !Features.AdvancedEC) return;

      // cache list of modules
      module = part.FindModulesImplementing<PartModule>().FindLast(k => k.moduleName == type);

      // setup UI
      Fields["actualCost"].guiActive = true;
    }

    public void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // get energy from cache
        resources = ResourceCache.Info(vessel, "ElectricCharge");
        hasEnergy = resources.amount > double.Epsilon;

        // Update UI only if hasEnergy has changed or if is the first time
        if (hasUpdateEnergyChanged != hasEnergy || !isInitialized)
        {
          Lib.Debug("Energy state has changed: {0}", hasEnergy);

          if(!isInitialized)
          {
            antennaPower = new AntennaEC(part.FindModuleImplementing<ModuleDataTransmitter>(), extra_Cost, extra_Deploy, antennaPower).Init(antennaPower);
            // verify if is using custom animation for CommNet
            customAnim = part.FindModuleImplementing<ModuleAnimateGeneric>();

            Lib.Debug("'{0}' has antennaPower: {1}", part.partInfo.title, antennaPower);
            Lib.Debug("'{0}' has extra_Cost: {1}", part.partInfo.title, extra_Cost);
            Lib.Debug("'{0}' has extra_Deploy: {1}", part.partInfo.title, extra_Deploy);
            Lib.Debug("customAnim isNull: {0}", customAnim== null);
            isInitialized = true;
          }

          // Update UI
          GUI_Update(hasEnergy);
          hasUpdateEnergyChanged = hasEnergy;
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
        if(customAnim != null) Constant_OnGUI(hasEnergy);
      }
    }

    public void FixedUpdate()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        if (hasFixedEnergyChanged != hasEnergy || !isInitialized)
        {
          if(!isInitialized) Lib.Debug("Energy state has changed: {0}", hasEnergy);
          FixModule(hasEnergy);

          hasFixedEnergyChanged = hasEnergy;
        }

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
        case "ModuleDataTransmitter":
          modReturn = new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;

        case "Antenna":
          modReturn = new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).GetConsume();
          actualCost = modReturn.Value;
          return modReturn.Key;
      }
      actualCost = extra_Deploy;
      return true;
    }

    public void GUI_Update(bool b)
    {
      Lib.Debug("Type is '{0}'", type);
      Lib.Debug("Module isNull: {0}", module == null);
      switch (type)
      {
        case "ModuleDataTransmitter":
          new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GUI_Update(b);
          break;

        case "Antenna":
          new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).GUI_Update(b);
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
      }
    }

    // Some modules need to constantly update UI
    public void Constant_OnGUI(bool b)
    {
      switch (type)
      {
        case "ModuleDataTransmitter":
          new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GUI_Update(b);
          break;
      }
    }
  }

}
