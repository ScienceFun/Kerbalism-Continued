using System;
using System.Collections.Generic;

namespace KERBALISM 
{
  public enum KAntennaType
  {
    low_gain,
    high_gain
  }

  public sealed class Antenna : PartModule, ISpecifics, IAnimatedModule, IScienceDataTransmitter, IContractObjectiveModule
  {
    [KSPField] public KAntennaType type;                  // type of antenna
    [KSPField] public double cost;                        // cost of transmission in EC/s
    [KSPField] public double rate;                        // transmission rate at zero distance in Mb/s
    [KSPField] public double dist;                        // max transmission distance in meters

    [KSPField(isPersistant=true)] public bool extended;   // true if the low-gain antenna can receive data from other vessels
    [KSPField(isPersistant=true)] public bool relay;      // true if the low-gain antenna can receive data from other vessels

    // utility to transmit data over time when science system is disabled
    public DataStream stream;
    ScreenMessage progress_msg;

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // assume extended if there is no animator
      extended |= part.FindModuleImplementing<ModuleAnimationGroup>() == null;

      // create data stream, used if science system is disabled
      stream = new DataStream();

      // in flight
      if (Lib.IsFlight())
      {
        // get animator module, if any
        var anim = part.FindModuleImplementing<ModuleAnimationGroup>();
        if (anim != null)
        {
          // resync extended state from animator
          // - rationale: extending in editor doesn't set extended to true,
          //   leading to spurious signal loss for 1 tick on prelaunch
          extended = anim.isDeployed;

          // allow extending/retracting even when vessel is not controllable
          anim.Events["DeployModule"].guiActiveUncommand = Settings.UnlinkedControl == UnlinkedCtrl.full;   //  "true" has been changed
          anim.Events["RetractModule"].guiActiveUncommand = Settings.UnlinkedControl == UnlinkedCtrl.full;  //  "true" has been changed
        }
      }
    }

    public void Update()
    {
      if (Lib.IsFlight())
      {
        // update ui
        Events["ToggleRelay"].active = type == KAntennaType.low_gain && (extended || !Settings.ExtendedAntenna) && !vessel.isEVA;
        Events["ToggleRelay"].guiName = Lib.StatusToggle("Relay", relay ? "yes" : "no");

        // show transmission messages
        if (stream.Transmitting())
        {
          string text = Lib.BuildString("Transmitting ", stream.Current_file(), ": ", Lib.HumanReadablePerc(stream.Current_progress()));
          if (progress_msg != null) ScreenMessages.RemoveMessage(progress_msg);
          progress_msg = ScreenMessages.PostScreenMessage(text, 1.0f, ScreenMessageStyle.UPPER_LEFT);
        }
      }
    }

    public void FixedUpdate()
    {
      // in flight
      if (Lib.IsFlight())
      {
        // if we are transmitting using the stock system
        if (stream.Transmitting())
        {
          // get ec resource handler
          Resource_Info ec = ResourceCache.Info(vessel, "ElectricCharge");

          // if we are still linked, and there is ec left
          if (CanTransmit() && ec.amount > double.Epsilon)
          {
            // compression factor
            // - used to avoid making the user wait too much for transmissions that
            //   don't happen in background, while keeping transmission rates realistic
            const double compression = 16.0;

            // transmit using the data stream
            stream.Update(DataRate * Kerbalism.elapsed_s * compression, vessel);

            // consume ec
            ec.Consume(DataResourceCost * Kerbalism.elapsed_s);
          }
          else
          {
            // abort transmission, return data to the vessel
            stream.Abort(vessel);

            // inform the user
            ScreenMessages.PostScreenMessage("Transmission aborted", 5.0f, ScreenMessageStyle.UPPER_LEFT);
          }
        }
      }
    }

    void Transmit(List<ScienceData> queue)
    {
      // this function is here to support two things:
      // - data transmission when Science is disabled
      // - 'triggered' science data (irregardless if Science is enabled or not)

      foreach(ScienceData data in queue)
      {
        // if Science is enabled, it should hijack everything except triggered data
        // if that is not the case, we want to know
        if (Features.Science && !data.triggered)
        {
          throw new Exception("Unable to hijack non-triggered data '" + data.title + "'");
        }

        // add data to the stream
        stream.Append(data);
      }
    }

    [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "_", active = true)]
    public void ToggleRelay()
    {
      relay = !relay;
    }

    public override string GetInfo()
    {
      string desc = type == KAntennaType.low_gain
        ? "A low-gain antenna for short range comminications with <b>DSN and other vessels</b>, that can also <b>relay data</b>"
        : "An high-gain antenna for long range communications with <b>DSN</b>";
      return Specs().Info(desc);
    }

    // specifics support
    public Specifics Specs()
    {
      double[] ranges = new double[12];
      for(int i=0; i < 12; ++i)
      {
        ranges[i] = dist / 13.0 * (double)(i + 1);
      }

      Specifics specs = new Specifics();
      specs.Add("Type", type == KAntennaType.low_gain ? "low-gain" : "high-gain");
      specs.Add("Transmission cost", Lib.BuildString(cost.ToString("F2"), " EC/s"));
      specs.Add("Nominal rate", Lib.HumanReadableDataRate(rate));
      specs.Add("Nominal distance", Lib.HumanReadableRange(dist));
      specs.Add(string.Empty);
      specs.Add("<color=#00ffff><b>Transmission rates</b></color>");
      foreach(double range in ranges)
      {
        specs.Add(Lib.BuildString(Lib.HumanReadableRange(range),  "\t<b>", Lib.HumanReadableDataRate(Calculate_Rate(range * 0.99,  dist, rate)), "</b>"));
      }
      return specs;
    }

    // data transmitter support
    public float DataRate { get { Vessel_Info vi = Cache.VesselInfo(vessel); return vi.is_valid ? (float)vi.connection.rate : 0.0f; } }
    public double DataResourceCost { get { return cost; } }
    public bool CanTransmit() { Vessel_Info vi = Cache.VesselInfo(vessel); return vi.is_valid && vi.connection.linked; }
    public bool IsBusy() { return false; }
    public void TransmitData(List<ScienceData> dataQueue) { Transmit(dataQueue); }

    // animation group support
    public void EnableModule()      { extended = true; }
    public void DisableModule()     { extended = false; }
    public bool ModuleIsActive()    { return false; }
    public bool IsSituationValid()  { return true; }

    // contract objective support
    public bool CheckContractObjectiveValidity()  { return true; }
    public string GetContractObjectiveType()      { return "Antenna"; }

    // return data rate in kbps
    public static double Calculate_Rate(double d, double dist, double rate)
    {
      double k = Math.Max(1.0 - d / dist, 0.0);
      return k * k * rate;
    }
  }
}