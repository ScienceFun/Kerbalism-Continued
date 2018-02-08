namespace KERBALISM
{
  public class ScienceEC : ECDeviceBase
  {
    // List of target
    // Part name: 
    //    OrbitalScanner      has ModuleAnimationGroup module   * TESTING
    //    SurveyScanner       has ModuleAnimationGroup module   * TESTING
    //    InfraredTelescope   no animation module
    //    GooExperiment       has ModuleAnimateGeneric module   * TESTING
    //    science_module      has ModuleAnimateGeneric module   * TESTING
    //    Large_Crewed_Lab    has already a energy consumption!
    protected override bool IsConsuming
    {
      get
      {
        return false;
      }
    }

    public override void GUI_Update(bool isEnabled)
    {
      throw new System.NotImplementedException();
    }

    public override void FixModule(bool hasEnergy)
    {
    }
  }
}