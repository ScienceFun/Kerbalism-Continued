﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
  public static class Science
  {
    public static void Init()
    {
      // initialize experiment info cache
      experiments = new Dictionary<string, ExperimentInfo>();

      // make the science dialog invisible, just once
      if (Features.Science)
      {
        GameObject prefab = AssetBase.GetPrefab("ScienceResultsDialog");
        if (Settings.ScienceDialog)
        {
          prefab.gameObject.AddOrGetComponent<Hijacker>();
        }
        else
        {
          prefab.gameObject.AddOrGetComponent<MiniHijacker>();
        }
      }
    }

    // consume EC for transmission, and transmit science data
    public static void Update(Vessel v, Vessel_Info vi, VesselData vd, Vessel_Resources resources, double elapsed_s)
    {
      // hard-coded transmission buffer size in Mb
      const double buffer_capacity = 8.0;

      // do nothing if science system is disabled
      if (!Features.Science) return;

      // avoid corner-case when RnD isn't live during scene changes
      // - this avoid losing science if the buffer reach threshold during a scene change
      if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX && ResearchAndDevelopment.Instance == null) return;

      // get connection info
      ConnectionInfo conn = vi.connection;

      // consume ec if data is transmitted or relayed
      if (vi.transmitting.Length > 0 || vi.relaying.Length > 0)
      {
        if (Features.KCommNet)
        {
          // This means that it is only relay, don't send data from yourself
          // CommNet: When vessel is relay, only antennas that are relay will consume ec
          if (vi.relaying.Length > 0 && vi.transmitting.Length == 0)
          {
            resources.Consume(v, "ElectricCharge", conn.relaycost * elapsed_s);
          }
          else
          {
            resources.Consume(v, "ElectricCharge", conn.cost * elapsed_s);
          }
        }
        resources.Consume(v, "ElectricCharge", conn.cost * elapsed_s);
      }

      // get filename of data being downloaded
      string filename = vi.transmitting;

      // if some data is being downloaded
      // - avoid cornercase at scene changes
      if (filename.Length > 0 && vd.drive.files.ContainsKey(filename))
      {
        // get file
        File file = vd.drive.files[filename];

        // determine how much data is transmitted
        double transmitted = Math.Min(file.size, conn.rate * elapsed_s);

        // consume data in the file
        file.size -= transmitted;

        // accumulate in the buffer
        file.buff += transmitted;

        // if buffer is full, or file was transmitted completely
        if (file.size <= double.Epsilon || file.buff > buffer_capacity)
        {
          // collect the science data
          Science.Credit(filename, file.buff, true, v.protoVessel);

          // reset the buffer
          file.buff = 0.0;
        }

        // if file was transmitted completely
        if (file.size <= double.Epsilon)
        {
          // remove the file
          vd.drive.files.Remove(filename);

          // inform the user
          Message.Post
          (
            Lib.BuildString("<color=cyan><b>DATA RECEIVED</b></color>\nTransmission of <b>", Science.Experiment(filename).name, "</b> completed"),
            Lib.TextVariant("Our researchers will jump on it right now", "The checksum is correct, data must be valid")
          );
        }
      }
    }

    // return name of file being transmitted from vessel specified
    public static string Transmitting(Vessel v, bool linked)
    {
      // never transmitting if science system is disabled
      if (!Features.Science) return string.Empty;

      // not transmitting if unlinked
      if (!linked) return string.Empty;

      // not transmitting if there is no ec left
      if (ResourceCache.Info(v, "ElectricCharge").amount <= double.Epsilon) return string.Empty;

      // get vessel drive
      Drive drive = DB.Vessel(v).drive;

      // get first file flagged for transmission
      foreach (var p in drive.files)
      {
        if (p.Value.send) return p.Key;
      }

      // no file flagged for transmission
      return string.Empty;
    }

    // credit science for the experiment subject specified
    public static double Credit(string subject_id, double size, bool transmitted, ProtoVessel pv)
    {
      // get science subject
      // - if null, we are in sandbox mode
      ScienceSubject subject = ResearchAndDevelopment.GetSubjectByID(subject_id);
      if (subject == null) return 0.0;

      // get science value
      // - the stock system 'degrade' science value after each credit, we don't
      float R = ResearchAndDevelopment.GetReferenceDataValue((float)size, subject);
      float S = subject.science;
      float C = subject.scienceCap;
      float credits = Mathf.Max(Mathf.Min(S + Mathf.Min(R, C), C) - S, 0.0f);

      // credit the science
      subject.science += credits;
      subject.scientificValue = ResearchAndDevelopment.GetSubjectValue(subject.science, subject);
      credits *= HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
      ResearchAndDevelopment.Instance.AddScience(credits, transmitted ? TransactionReasons.ScienceTransmission : TransactionReasons.VesselRecovery);

      // fire game event
      // - this could be slow or a no-op, depending on the number of listeners
      //   in any case, we are buffering the transmitting data and calling this
      //   function only once in a while
      GameEvents.OnScienceRecieved.Fire(credits, subject, pv, false);

      // return amount of science credited
      return credits;
    }

    // return value of some data about a subject, in science credits
    public static double Value(string subject_id, double size)
    {
      // get the subject
      // - will be null in sandbox
      ScienceSubject subject = ResearchAndDevelopment.GetSubjectByID(subject_id);

      // return value in science credits
      return subject != null
        ? ResearchAndDevelopment.GetScienceValue((float)size, subject)
        * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier
        : 0.0;
    }

    // return module acting as container of an experiment
    public static IScienceDataContainer Container(Part p, string experiment_id)
    {
      // first try to get a stock experiment module with the right experiment id
      // - this support parts with multiple experiment modules, like eva kerbal
      foreach (ModuleScienceExperiment exp in p.FindModulesImplementing<ModuleScienceExperiment>())
      {
        if (exp.experimentID == experiment_id) return exp;
      }

      // if none was found, default to the first module implementing the science data container interface
      // - this support third-party modules that implement IScienceDataContainer, but don't derive from ModuleScienceExperiment
      return p.FindModuleImplementing<IScienceDataContainer>();
    }

    // return info about an experiment
    public static ExperimentInfo Experiment(string subject_id)
    {
      if (!experiments.TryGetValue(subject_id, out ExperimentInfo info))
      {
        info = new ExperimentInfo(subject_id);
        experiments.Add(subject_id, info);
      }
      return info;
    }

    // create a new subject entry in the RnD
    // - experiment: experiment_id
    // - situation: an arbitrary situation, can insert biome at the end
    // - body: celestial body involved
    // - biome: biome involved, or empty
    // - multiplier: science multiplier for the body/situation
    public static string Generate_Subject(string experiment, CelestialBody body, string sit, string biome, float multiplier)
    {
      // generate subject id
      string subject_id = Lib.BuildString(experiment, "@", body.name, sit + biome);

      // in sandbox, do nothing else
      if (ResearchAndDevelopment.Instance == null) return subject_id;

      // if the subject id was never added to RnD
      if (ResearchAndDevelopment.GetSubjectByID(subject_id) == null)
      {
        // get experiment
        // - assuming it is valid here
        var exp = ResearchAndDevelopment.GetExperiment(experiment);

        // get subjects container using reflection
        // - we tried just changing the subject.id instead, and
        //   it worked but the new id was obviously used only after
        //   putting RnD through a serialization->deserialization cycle
        var subjects = Lib.PrivateField<Dictionary<string, ScienceSubject>>
        (
          typeof(ResearchAndDevelopment),
          ResearchAndDevelopment.Instance,
          "scienceSubjects"
        );

        // create new subject
        ScienceSubject subject = new ScienceSubject
        (
          subject_id,
          Lib.BuildString(exp.experimentTitle, " (", Lib.SpacesOnCaps(sit + biome), ")"),
          exp.dataScale,
          multiplier,
          exp.scienceCap
        );

        // add it to RnD
        subjects.Add(subject_id, subject);
      }
      return subject_id;
    }

    // return vessel situation valid for specified experiment
    public static string Situation(Vessel v, string situations)
    {
      // shortcuts
      CelestialBody body = v.mainBody;
      Vessel_Info vi = Cache.VesselInfo(v);

      List<string> list = Lib.Tokenize(situations, ',');
      foreach (string sit in list)
      {
        bool b = false;
        switch (sit)
        {
          case "Surface":         b = Lib.Landed(v);                                                                        break;
          case "Atmosphere":      b = body.atmosphere && v.altitude < body.atmosphereDepth;                                 break;
          case "Ocean":           b = body.ocean && v.altitude < 0.0;                                                       break;
          case "Space":           b = body.flightGlobalsIndex != 0 && !Lib.Landed(v) && v.altitude > body.atmosphereDepth;  break;
          case "AbsoluteZero":    b = vi.env_temperature < 30.0;                                                            break;
          case "InnerBelt":       b = vi.inner_belt;                                                                        break;
          case "OuterBelt":       b = vi.outer_belt;                                                                        break;
          case "Magnetosphere":   b = vi.magnetosphere;                                                                     break;
          case "Thermosphere":    b = vi.thermosphere;                                                                      break;
          case "Exosphere":       b = vi.exosphere;                                                                         break;
          case "InterPlanetary":  b = body.flightGlobalsIndex == 0 && !vi.interstellar;                                     break;
          case "InterStellar":    b = body.flightGlobalsIndex == 0 && vi.interstellar;                                      break;
        }
        if (b) return sit;
      }
      return string.Empty;
    }

    public static string Biome(Vessel v, string sit)
    {
      switch (sit)
      {
        case "Surface":
        case "Atmosphere":
        case "Ocean":
        return ScienceUtil.GetExperimentBiome(v.mainBody, v.latitude, v.longitude);

        default:
        return string.Empty;
      }
    }

    public static float Multiplier(Vessel v, string sit)
    {
      var values = v.mainBody.scienceValues;
      float space = (values.InSpaceLowDataValue + values.InSpaceHighDataValue) * 0.5f;
      switch (sit)
      {
        case "Surface": return (values.LandedDataValue + values.SplashedDataValue) * 0.5f;
        case "Atmosphere": return (values.FlyingLowDataValue + values.FlyingHighDataValue) * 0.5f;
        case "Ocean": return values.SplashedDataValue * 2.0f;
        case "Space": return space;
        case "AbsoluteZero": return space * 2.5f;
        case "InnerBelt": return space * 1.5f;
        case "OuterBelt": return space * 2.0f;
        case "Magnetosphere": return space;
        case "Thermosphere": return space * 0.75f;
        case "Exosphere": return space * 0.75f;
        case "InterPlanetary": return space;
        case "InterStellar": return space * 5.0f;
      }
      return 0.0f;
    }

    // experiment info cache
    static Dictionary<string, ExperimentInfo> experiments;
  }
}