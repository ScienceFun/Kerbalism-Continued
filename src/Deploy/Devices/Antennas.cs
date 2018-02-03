using System.Collections.Generic;

namespace KERBALISM
{
  public class AntennaEC : ECDevice
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
    }

    public double Init(double currentPower)
    {
      return (currentPower != transmitter.antennaPower && transmitter.antennaPower > 0 ? transmitter.antennaPower : currentPower);
    }

    public override KeyValuePair<bool, double> GetConsume()
    {
      return new KeyValuePair<bool, double>(IsConsuming, actualCost);
    }

    public override bool IsConsuming
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
          else
          {
            actualCost = extra_Cost;
            return true;
          }
        }
        actualCost = 0;
        return false;
      }
    }

    public override void UI_Update(bool hasEnergy)
    {
      if (Features.Signal)
      {
        if (hasEnergy)
        {
          if (animator != null)
          {
            if(animator.DeployAnimation.isPlaying)
            {
              animator.Events["RetractModule"].active = false;
              animator.Events["DeployModule"].active = false;
            }
            else if (animator.isDeployed)
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
        }
      }
    }

    public void FixCommNetAntenna(bool hasEnergy)
    {
      double right;
      if (Features.KCommNet)
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

    // Logical
    double extra_Cost;
    double extra_Deploy;
    double antennaPower;

    // Return
    double actualCost;
  }
}
