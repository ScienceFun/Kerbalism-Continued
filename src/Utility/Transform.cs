using UnityEngine;

namespace KERBALISM
{
  public sealed class Transformator
  {
    Part part;
    Transform transf;
    string name;

    Quaternion baseAngles;

    float rotationRateGoal = 0f;
    float CurrentSpinRate = 0f;

    float SpinRate = 0f;
    float spinAccel = 0f;

    public Transformator(Part p, string transf_name, float SpinRate, float spinAccel)
    {
      transf = null;
      name = string.Empty;
      part = p;

      if (transf_name.Length > 0)
      {
        Lib.Debug("Looking for : {0}", transf_name);
        Transform[] transfArray = p.FindModelTransforms(transf_name);
        if (transfArray.Length > 0)
        {
          Lib.Debug("Transform has been found");
          name = transf_name;

          transf = transfArray[0];
          this.SpinRate = SpinRate;
          this.spinAccel = spinAccel;
          baseAngles = transf.localRotation;
        }
      }
    }

    public void Play()
    {
      if (transf != null) rotationRateGoal = 1.0f;
    }

    public void Stop()
    {
      if (transf != null) rotationRateGoal = 0;
    }

    public void DoSpin()
    {
      if(rotationRateGoal == 0.0f)
      {
        if (Quaternion.Angle(baseAngles, transf.localRotation) <= 2.0f)
        {
          CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, 0.0f, TimeWarp.fixedDeltaTime * spinAccel * 20f);
        }
      }
      else
      {
        CurrentSpinRate = Mathf.MoveTowards(CurrentSpinRate, rotationRateGoal * SpinRate, TimeWarp.fixedDeltaTime * spinAccel);
      }

      float spin = Mathf.Clamp(TimeWarp.fixedDeltaTime * CurrentSpinRate, -10.0f, 10.0f);
      transf.Rotate(Vector3.forward * spin);
    }
  }
}
