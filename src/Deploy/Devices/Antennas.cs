using System;
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
            Lib.Debug("Desactiving buttons for '{0}' antenna", antenna.part.partInfo.title);
            // Don't allow extending/retracting when has no ec

            string ev = "";
            foreach (var e in animator.Events)
            {
              ev += ("'" + e.name + "',");
            }
            ev = ev.Substring(0, ev.Length - 1);
            Lib.Debug("Available events:" + ev);
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
            Lib.Debug("Desactiving buttons for '{0}' antenna", transmitter.part.partInfo.title);
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
        if (animator != null)
        {
          if (animator.isDeployed || (Settings.ExtendedAntenna == false))
          {
            antenna.extended = hasEnergy;
          }
          else antenna.extended = false;
        }
        else
        {
          // this means that antenna is fixed
          antenna.extended = hasEnergy;
        }
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

  public sealed class AntennasEC : AdvancedEC
  {
    [KSPField(isPersistant = true)] public double antennaPower;   // CommNet doesn't ignore ModuleDataTransmitter disabled, this way I have to set power to 0 to disable it.
    public ModuleAnimateGeneric customAnim;                       // Support to custom animation for ModuleDataTransmitter module

    public override void OnStart(StartState state)
    {
      base.OnStart(state);

      // don't break tutorial scenarios & do something only in Flight scenario
      if (Lib.DisableScenario(this) || !Lib.IsFlight()) return;
   
      if(Features.Signal)
      {
        Antenna a = part.FindModuleImplementing<Antenna>();
        if (antennaPower == 0 && a != null) antennaPower = a.dist;
      }
      else
      {
        ModuleDataTransmitter transmitter = part.FindModuleImplementing<ModuleDataTransmitter>();
        if (transmitter != null) antennaPower = new AntennaEC(part.FindModuleImplementing<ModuleDataTransmitter>(), extra_Cost, extra_Deploy, antennaPower).Init(antennaPower);
      }

      // verify if is using custom animation for CommNet
      customAnim = part.FindModuleImplementing<ModuleAnimateGeneric>();
    }

    public override void OnUpdate()
    {
      if (!Lib.IsFlight()) return;

      // get energy from cache
      resources = ResourceCache.Info(vessel, "ElectricCharge");
      hasEnergy = resources.amount > double.Epsilon;

      if (!hasEnergy || broken)
      {
        actualCost = 0;
        isConsuming = false;
      }
      else
      {
        isConsuming = GetIsConsuming();
      }

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

        hasEnergyChanged = hasEnergy;
        lastBrokenState = false;
        // Update UI
        Update_UI(hasEnergy);
      }
      // Constantly Update UI for special modules
      if (customAnim != null)
      {
        if (broken) Constant_OnGUI(!broken);
        else Constant_OnGUI(hasEnergy);
      }
    }

    public override bool GetIsConsuming()
    {
      try
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
      }
      catch(Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
      actualCost = extra_Deploy;
      return true;
    }

    public override void Update_UI(bool isEnabled)
    {
      try
      {
        switch (type)
        {
          case "ModuleDataTransmitter":
            new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GUI_Update(isEnabled);
            break;
          case "Antenna":
            new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).GUI_Update(isEnabled);
            break;
        }
      }
      catch (Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
    }

    public override void FixModule(bool isEnabled)
    {
      try
      {
        switch (type)
        {
          case "ModuleDataTransmitter":
            new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).FixModule(isEnabled);
            break;
          case "Antenna":
            new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).FixModule(isEnabled);
            break;
        }
      }
      catch(Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
    }

    // Some modules need to constantly update UI
    public override void Constant_OnGUI(bool isEnabled)
    {
      try
      {
        switch (type)
        {
          case "ModuleDataTransmitter":
            new AntennaEC(module as ModuleDataTransmitter, extra_Cost, extra_Deploy, antennaPower).GUI_Update(isEnabled);
            break;
          case "Antenna":
            new AntennaEC(module as Antenna, extra_Cost, extra_Deploy).GUI_Update(isEnabled);
            break;
        }
      }
      catch (Exception e)
      {
        Lib.Error("'{0}': {1}", part.partInfo.title, e.Message);
      }
    }
  }
}