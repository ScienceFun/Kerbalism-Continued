﻿using System;

namespace KERBALISM 
{
  public sealed class Laboratory : PartModule, ISpecifics, IContractObjectiveModule
  {
    [KSPField] public double ec_rate;                     // ec consumed per-second
    [KSPField] public double analysis_rate;               // analysis speed in Mb/s
    [KSPField] public string researcher = string.Empty;   // required crew for analysis

    [KSPField(isPersistant = true)] public bool running;  // true if the lab is active

    CrewSpecs researcher_cs;                              // crew specs for the researcher
    string status = string.Empty;                         // string to show next to the ui button

    [KSPField(isPersistant = true, guiName = "analysis_rateAVG", guiUnits = "", guiActive = true, guiFormat = "")] public double analysis_rateAVG = 0;

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // do nothing in the editors and when compiling parts
      if (!Lib.IsFlight()) return;

      // parse crew specs
      researcher_cs = new CrewSpecs(researcher);
    }

    public void Update()
    {
      if (Lib.IsFlight())
      {
        Events["Toggle"].guiName = Lib.StatusToggle("Lab", status);
      }
      else
      {
        Events["Toggle"].guiName = Lib.StatusToggle("Lab", running ? "enabled" : "disabled");
      }
    }

    public void FixedUpdate()
    {
      // do nothing in the editor
      if (Lib.IsEditor()) return;

      // if enabled
      if (running)
      {
        int qtty = 0, crewlvl = 0;
        // if a researcher is not required, or the researcher is present
        if (!researcher_cs || researcher_cs.Check(part.protoModuleCrew, out qtty, out crewlvl))
        {
          // get next sample to analyze
          string sample_filename = Next_sample(vessel);

          // if there is a sample to analyze
          if (sample_filename.Length > 0)
          {
            // consume EC
            Resource_Info ec = ResourceCache.Info(vessel, "ElectricCharge");
            ec.Consume(ec_rate * Kerbalism.elapsed_s);

            // if there was ec
            // - comparing against amount in previous simulation step
            if (ec.amount > double.Epsilon)
            {
              analysis_rateAVG = analysis_rate;
              if (researcher_cs) analysis_rateAVG *= qtty * crewlvl;
              // analyze the sample
              Analyze(vessel, sample_filename, analysis_rateAVG * Kerbalism.elapsed_s);

              //Analyze(vessel, sample_filename, analysis_rate * Kerbalism.elapsed_s);

              // update status
              status = Science.Experiment(sample_filename).name;
            }
            // if there was no ec
            else
            {
              // update status
              status = "<color=yellow>no electric charge</color>";
            }
          }
          // if there is no sample to analyze
          else
          {
            // update status
            status = "no samples to analyze";
          }
        }
        // if a researcher is required, but missing
        else
        {
          // update status
          status = Lib.BuildString("<color=yellow>", researcher_cs.Warning(), "</color>");
        }
      }
      // if disabled
      else
      {
        // update status
        status = "disabled";
      }
    }

    public static void BackgroundUpdate(Vessel v, ProtoPartSnapshot p, ProtoPartModuleSnapshot m, Laboratory lab, Resource_Info ec, double elapsed_s)
    {
      // if enabled
      if (Lib.Proto.GetBool(m, "running"))
      {
        // if a researcher is not required, or the researcher is present
        CrewSpecs researcher_cs = new CrewSpecs(lab.researcher);
        int qtty = 0, crewlvl = 0;
        if (!researcher_cs || researcher_cs.Check(p.protoModuleCrew, out qtty, out crewlvl))
        {
          // get sample to analyze
          string sample_filename = Next_sample(v);

          // if there is a sample to analyze
          if (sample_filename.Length > 0)
          {
            // consume EC
            ec.Consume(lab.ec_rate * elapsed_s);

            // if there was ec
            // - comparing against amount in previous simulation step
            if (ec.amount > double.Epsilon)
            {
              double analysis_rateAVG = lab.analysis_rate;
              if (researcher_cs) analysis_rateAVG *= qtty * crewlvl;
              // analyze the sample
              Analyze(v, sample_filename, analysis_rateAVG * elapsed_s);
            }
          }
        }
      }
    }

    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "_", active = true)]
    public void Toggle()
    {
      running = !running;
    }

    // action groups
    [KSPAction("Enable/Disable Lab")] public void Action(KSPActionParam param) { Toggle(); }

    public override string GetInfo()
    {
      return Specs().Info("Analyze samples to produce transmissible data");
    }

    // specifics support
    public Specifics Specs()
    {
      Specifics specs = new Specifics();
      specs.Add("Researcher", new CrewSpecs(researcher).Info());
      specs.Add("EC rate", Lib.HumanReadableRate(ec_rate));
      specs.Add("Analysis rate", Lib.HumanReadableDataRate(analysis_rate));
      return specs;
    }

    // contract objective support
    public bool CheckContractObjectiveValidity()  { return true; }
    public string GetContractObjectiveType()      { return "Laboratory"; }

    // get sample to analyze, return null if there isn't a sample
    static string Next_sample(Vessel v)
    {
      // get vessel drive
      Drive drive = DB.Vessel(v).drive;

      // for each sample
      foreach(var pair in drive.samples)
      {
        // shortcuts
        string filename = pair.Key;
        Sample sample = pair.Value;

        // if flagged for analysis
        if (sample.analyze)
        {
          // we found it
          return filename;
        }
      }

      // there was no sample to analyze
      return string.Empty;
    }

    // analyze a sample
    static void Analyze(Vessel v, string filename, double amount)
    {
      // get vessel drive
      Drive drive = DB.Vessel(v).drive;

      // get sample
      Sample sample = drive.samples[filename];

      // analyze, and produce data
      amount = Math.Min(amount, sample.size);
      bool completed = amount >= sample.size - double.Epsilon;
      drive.Delete_Sample(filename, amount);
      drive.Record_File(filename, amount);

      // if the analysis is completed
      if (completed)
      {
        // inform the user
        Message.Post
        (
          Lib.BuildString("<color=cyan><b>ANALYSIS COMPLETED</b></color>\nOur laboratory on <b>", v.vesselName, "</b> analyzed <b>", Science.Experiment(filename).name, "</b>"),
          "The results can be transmitted now"
        );

        // record landmark event
        if (!Lib.Landed(v)) DB.landmarks.space_analysis = true;
      }
    }
  }
}