using System.Collections.Generic;

namespace KERBALISM
{
  public abstract class DeployBase : PartModule, IResourceConsumer
  {
    public List<PartResourceDefinition> GetConsumedResources() => new List<PartResourceDefinition>() { PartResourceLibrary.Instance.GetDefinition("ElectricCharge") };

    [KSPField(isPersistant = true)] public double ecCost = 0;                 // ecCost to keep the part active
    [KSPField(isPersistant = true)] public double ecDeploy = 0;               // ecCost to do a deploy(animation)

    [KSPField(guiName = "EC Usage", guiUnits = "/sec", guiActive = false, guiFormat = "F2")]
    public double actualECCost = 0;                                           // Show EcConsume on part display

    [KSPField(isPersistant = true)] 
    public bool isActionGroupchanged;                                         // actionGroup was changed by DeploySystem?

    public PartModule pModule;
    //public string Action;

    public bool hasEC;                                                        // Check if vessel has EC to consume, otherwise will disable animations and functions of the part.
    public bool isConsuming;
    public Resource_Info resourceInfo;

    public override void OnStart(StartState state)
    {
      //Fix the actionGroup when Deploy is disable
      if (!Features.AdvancedEC && isActionGroupchanged)
      {
        foreach (BaseAction ac in pModule.Actions)
        {
          ac.active = true;
        }
        isActionGroupchanged = false;
      }
    }

    public virtual void Update()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        // get ec resource handler
        resourceInfo = ResourceCache.Info(vessel, "ElectricCharge");
        hasEC = resourceInfo.amount > double.Epsilon;

        isConsuming = GetIsConsuming;
        if (!isConsuming) actualECCost = 0;
      }
    }

    public virtual void FixedUpdate()
    {
      if (Lib.IsFlight() && Features.AdvancedEC)
      {
        if (isConsuming)
        {
          if (resourceInfo != null) resourceInfo.Consume(actualECCost * Kerbalism.elapsed_s);
        }
      }
    }

    // Define when it is consuming EC
    public abstract bool GetIsConsuming { get; }

    public void ToggleActions(PartModule partModule, bool value)
    {
      foreach (BaseAction ac in partModule.Actions)
      {
        ac.active = value;
      }
    }
  }
}
