﻿using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
  public static class Lib
  {
    #region UTILS ----------------------------------------------------------------
    public static readonly string NAME_LOG_PREFIX = "[Kerbalism-Continued]";

    // write a message to the log 
    public static void Verbose(string message, params object[] param)
    {
      UnityEngine.Debug.Log(string.Format("{0} -> verbose: {1}", NAME_LOG_PREFIX, string.Format(message, param)));
    }

    public static void Debug(string message, params object[] param)
    {
#if DEBUG
      StackTrace stackTrace = new StackTrace();
      UnityEngine.Debug.Log(string.Format("{0} -> debug: {1}.{2} - {3}", NAME_LOG_PREFIX, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name, stackTrace.GetFrame(1).GetMethod().Name,
                                          string.Format(message, param)));
#endif
    }

    public static void Error(string message, params object[] param)
    {
      StackTrace stackTrace = new StackTrace(true);

      UnityEngine.Debug.Log(string.Format("{0} -> error: {1}.{2} - {3}", NAME_LOG_PREFIX, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name, stackTrace.GetFrame(1).GetMethod().Name
                                          , string.Format(message, param)));
    }

    // return version as a string
    static string _version;
    public static string Version()
    {
      if (_version == null) _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      return _version;
    }

    // return true if an assembly with specified name is loaded
    public static bool HasAssembly(string name)
    {
      foreach (var a in AssemblyLoader.loadedAssemblies)
      {
        if (a.name == name) return true;
      }
      return false;
    }

    // swap two variables
    public static void Swap<T>(ref T a, ref T b)
    {
      T tmp = b;
      b = a;
      a = tmp;
    }
    #endregion

    #region MATH -----------------------------------------------------------------
    // clamp a value
    public static int Clamp(int value, int min, int max)
    {
      return Math.Max(min, Math.Min(value, max));
    }

    // clamp a value
    public static float Clamp(float value, float min, float max)
    {
      return Math.Max(min, Math.Min(value, max));
    }

    // clamp a value
    public static double Clamp(double value, double min, double max)
    {
      return Math.Max(min, Math.Min(value, max));
    }

    // blend between two values
    public static float Mix(float a, float b, float k)
    {
      return a * (1.0f - k) + b * k;
    }

    // blend between two values
    public static double Mix(double a, double b, double k)
    {
      return a * (1.0 - k) + b * k;
    }
    #endregion

    #region RANDOM ---------------------------------------------------------------
    // store the random number generator
    static System.Random rng = new System.Random();

    // return random integer
    public static int RandomInt(int max_value)
    {
      return rng.Next(max_value);
    }

    // return random float [0..1]
    public static float RandomFloat()
    {
      return (float)rng.NextDouble();
    }

    // return random double [0..1]
    public static double RandomDouble()
    {
      return rng.NextDouble();
    }

    // return random float in [-1,+1] range
    // - it is less random than the c# RNG, but is way faster
    // - the seed is meant to overflow! (turn off arithmetic overflow/underflow exceptions)
    static int fast_float_seed = 1;
    public static float FastRandomFloat()
    {
      fast_float_seed *= 16807;
      return (float)fast_float_seed * 4.6566129e-010f;
    }
    #endregion

    #region HASH -----------------------------------------------------------------
    // combine two guid, irregardless of their order (eg: Combine(a,b) == Combine(b,a))
    public static Guid CombineGuid(Guid a, Guid b)
    {
      byte[] a_buf = a.ToByteArray();
      byte[] b_buf = b.ToByteArray();
      byte[] c_buf = new byte[16];
      for (int i = 0; i < 16; ++i) c_buf[i] = (byte)(a_buf[i] ^ b_buf[i]);
      return new Guid(c_buf);
    }

    // combine two guid, in a non-commutative way
    public static Guid OrderedCombineGuid(Guid a, Guid b)
    {
      byte[] a_buf = a.ToByteArray();
      byte[] b_buf = b.ToByteArray();
      byte[] c_buf = new byte[16];
      for (int i = 0; i < 16; ++i) c_buf[i] = (byte)(a_buf[i] & ~b_buf[i]);
      return new Guid(c_buf);
    }

    // get 32bit FNV-1a hash of a string
    public static UInt32 Hash32(string s)
    {
      // offset basis
      UInt32 h = 2166136261u;

      // for each byte of the buffer
      for (int i = 0; i < s.Length; ++i)
      {
        // xor the bottom with the current octet
        h ^= (uint)s[i];

        // equivalent to h *= 16777619 (FNV magic prime mod 2^32)
        h += (h << 1) + (h << 4) + (h << 7) + (h << 8) + (h << 24);
      }

      //return the hash
      return h;
    }
    #endregion

    #region TIME -----------------------------------------------------------------
    // return hours in a day
    public static double HoursInDay()
    {
      return GameSettings.KERBIN_TIME ? 6.0 : 24.0;
    }

    // return year length
    public static double DaysInYear()
    {
      if (!FlightGlobals.ready) return 426.0;
      return Math.Floor(FlightGlobals.GetHomeBody().orbit.period / (HoursInDay() * 60.0 * 60.0));
    }

    // stop time warping
    public static void StopWarp(int rate = 0)
    {
      TimeWarp.fetch.CancelAutoWarp();
      TimeWarp.SetRate(rate, true, false);
    }

    // disable time warping above a specified level
    public static void DisableWarp(uint max_level)
    {
      for (uint i = max_level + 1u; i < 8; ++i)
      {
        TimeWarp.fetch.warpRates[i] = TimeWarp.fetch.warpRates[max_level];
      }
    }

    // get current time
    public static UInt64 Clocks()
    {
      return (UInt64)Stopwatch.GetTimestamp();
    }

    // convert from clocks to microseconds
    public static double Microseconds(UInt64 clocks)
    {
      return (double)clocks * 1000000.0 / (double)Stopwatch.Frequency;
    }

    public static double Milliseconds(UInt64 clocks)
    {
      return (double)clocks * 1000.0 / (double)Stopwatch.Frequency;
    }

    public static double Seconds(UInt64 clocks)
    {
      return (double)clocks / (double)Stopwatch.Frequency;
    }

    // return human-readable timestamp of planetarium time
    public static string PlanetariumTimestamp()
    {
      double t = Planetarium.GetUniversalTime();
      const double len_min = 60.0;
      const double len_hour = len_min * 60.0;
      double len_day = len_hour * Lib.HoursInDay();
      double len_year = len_day * Lib.DaysInYear();

      double year = Math.Floor(t / len_year);
      t -= year * len_year;
      double day = Math.Floor(t / len_day);
      t -= day * len_day;
      double hour = Math.Floor(t / len_hour);
      t -= hour * len_hour;
      double min = Math.Floor(t / len_min);

      return BuildString
      (
        "[",
        ((uint)year + 1).ToString("D4"),
        "/",
        ((uint)day + 1).ToString("D2"),
        " ",
        ((uint)hour).ToString("D2"),
        ":",
        ((uint)min).ToString("D2"),
        "]"
      );
    }

    // return true half the time
    public static int Alternate(int seconds, int elements)
    {
      return ((int)Time.realtimeSinceStartup / seconds) % elements;
    }

    // Delay
    public static IEnumerator Delay(float s)
    {
#if DEBUG
      StackTrace stackTrace = new StackTrace();
      UnityEngine.Debug.Log(string.Format("{0} -> debug: {1}.{2} - '{3}'sec delay has been created.", NAME_LOG_PREFIX, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name, stackTrace.GetFrame(1).GetMethod().Name, s));
#endif
      yield return new WaitForSeconds(s);
    }
    #endregion

    #region REFLECTION -----------------------------------------------------------
    // return a value from a module using reflection
    // note: useful when the module is from another assembly, unknown at build time
    // note: useful when the value isn't persistent
    // note: this function break hard when external API change, by design
    public static T ReflectionValue<T>(PartModule m, string value_name)
    {
      BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
      return (T)m.GetType().GetField(value_name, flags).GetValue(m);
    }

    // set a value from a module using reflection
    // note: useful when the module is from another assembly, unknown at build time
    // note: useful when the value isn't persistent
    // note: this function break hard when external API change, by design
    public static void ReflectionValue<T>(PartModule m, string value_name, T value)
    {
      BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
      m.GetType().GetField(value_name, flags).SetValue(m, value);
    }

    // get access to a private field
    public static T PrivateField<T>(Type type, object instance, string field_name) where T : class
    {
      BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
      FieldInfo field = type.GetField(field_name, flags);
      return field.GetValue(instance) as T;
    }
    #endregion

    #region STRING ---------------------------------------------------------------
    // return string limited to len, with ... at the end
    public static string Ellipsis(string s, uint len)
    {
      len = Math.Max(len, 3u);
      return s.Length <= len ? s : Lib.BuildString(s.Substring(0, (int)len - 3), "...");
    }

    // tokenize a string
    public static List<string> Tokenize(string txt, char separator)
    {
      List<string> ret = new List<string>();
      string[] strings = txt.Split(separator);
      foreach (string s in strings)
      {
        string trimmed = s.Trim();
        if (trimmed.Length > 0) ret.Add(trimmed);
      }
      return ret;
    }

    // return message with the macro expanded
    // - variant: tokenize the string by '|' and select one
    public static string ExpandMsg(string txt, Vessel v = null, ProtoCrewMember c = null, uint variant = 0)
    {
      // get variant
      var variants = txt.Split('|');
      if (variants.Length > variant) txt = variants[variant];

      // macro expansion
      string v_name = v != null ? (v.isEVA ? "EVA" : v.vesselName) : "";
      string c_name = c != null ? c.name : "";
      return txt
        .Replace("@", "\n")
        .Replace("$VESSEL", BuildString("<b>", v_name, "</b>"))
        .Replace("$KERBAL", "<b>" + c_name + "</b>")
        .Replace("$ON_VESSEL", v != null && v.isActiveVessel ? "" : BuildString("On <b>", v_name, "</b>, "))
        .Replace("$HIS_HER", c != null && c.gender == ProtoCrewMember.Gender.Male ? "his" : "her");
    }

    // make the first letter uppercase
    public static string UppercaseFirst(string s)
    {
      return s.Length > 0 ? char.ToUpper(s[0]) + s.Substring(1) : string.Empty;
    }

    // return string with specified color if condition evaluate to true
    public static string Color(string s, bool cond, string clr)
    {
      return !cond ? s : BuildString("<color=", clr, ">", s, "</color>");
    }

    // add spaces on caps
    public static string SpacesOnCaps(string s)
    {
      return System.Text.RegularExpressions.Regex.Replace(s, "[A-Z]", " $0").TrimStart();
    }

    // convert to smart_case
    public static string SmartCase(string s)
    {
      return SpacesOnCaps(s).ToLower().Replace(' ', '_');
    }

    // select a string at random
    public static string TextVariant(params string[] list)
    {
      return list.Length == 0 ? string.Empty : list[RandomInt(list.Length)];
    }
    #endregion

    #region BUILD STRING ---------------------------------------------------------
    // compose a set of strings together, without creating temporary objects
    // note: the objective here is to minimize number of temporary variables for GC
    // note: okay to call recursively, as long as all individual concatenation is atomic
    static StringBuilder sb = new StringBuilder(256);
    public static string BuildString(params string[] args)
    {
      sb.Length = 0;
      foreach (string s in args) sb.Append(s);
      return sb.ToString();
    }
    #endregion

    #region HUMAN READABLE -------------------------------------------------------
    // pretty-print a resource rate
    // - rate: rate per second, must be positive
    public static string HumanReadableRate(double rate, string precision = "F2")
    {
      if (rate <= double.Epsilon) return "none";
      if (rate >= 0.01) return BuildString(rate.ToString(precision), "/s");
      rate *= 60.0; // per-minute
      if (rate >= 0.01) return BuildString(rate.ToString(precision), "/m");
      rate *= 60.0; // per-hour
      if (rate >= 0.01) return BuildString(rate.ToString(precision), "/h");
      rate *= HoursInDay();  // per-day
      if (rate >= 0.01) return BuildString(rate.ToString(precision), "/d");
      return BuildString((rate * DaysInYear()).ToString(precision), "/y");
    }

    // pretty-print a duration
    // - duration: duration in seconds, must be positive
    public static string HumanReadableDuration(double duration)
    {
      if (duration <= double.Epsilon) return "none";
      if (double.IsInfinity(duration) || double.IsNaN(duration)) return "perpetual";

      double hours_in_day = HoursInDay();
      double days_in_year = DaysInYear();

      // seconds
      if (duration < 60.0) return BuildString(duration.ToString("F0"), "s");

      // minutes + seconds
      double duration_min = Math.Floor(duration / 60.0);
      duration -= duration_min * 60.0;
      if (duration_min < 60.0) return BuildString(duration_min.ToString("F0"), "m", (duration < 1.0 ? "" : BuildString(" ", duration.ToString("F0"), "s")));

      // hours + minutes
      double duration_h = Math.Floor(duration_min / 60.0);
      duration_min -= duration_h * 60.0;
      if (duration_h < hours_in_day) return BuildString(duration_h.ToString("F0"), "h", (duration_min < 1.0 ? "" : BuildString(" ", duration_min.ToString("F0"), "m")));

      // days + hours
      double duration_d = Math.Floor(duration_h / hours_in_day);
      duration_h -= duration_d * hours_in_day;
      if (duration_d < days_in_year) return BuildString(duration_d.ToString("F0"), "d", (duration_h < 1.0 ? "" : BuildString(" ", duration_h.ToString("F0"), "h")));

      // years + days
      double duration_y = Math.Floor(duration_d / days_in_year);
      duration_d -= duration_y * days_in_year;
      return BuildString(duration_y.ToString("F0"), "y", (duration_d < 1.0 ? "" : BuildString(" ", duration_d.ToString("F0"), "d")));
    }

    // pretty-print a range
    // - range: range in meters, must be positive
    public static string HumanReadableRange(double range)
    {
      if (range <= double.Epsilon) return "none";
      if (range < 1000.0) return BuildString(range.ToString("F1"), " m");
      range /= 1000.0;
      if (range < 1000.0) return BuildString(range.ToString("F1"), " Km");
      range /= 1000.0;
      if (range < 1000.0) return BuildString(range.ToString("F1"), " Mm");
      range /= 1000.0;
      if (range < 1000.0) return BuildString(range.ToString("F1"), " Gm");
      range /= 1000.0;
      if (range < 1000.0) return BuildString(range.ToString("F1"), " Tm");
      range /= 1000.0;
      if (range < 1000.0) return BuildString(range.ToString("F1"), " Pm");
      range /= 1000.0;
      return BuildString(range.ToString("F1"), " Em");
    }

    // pretty-print temperature
    public static string HumanReadableTemp(double temp)
    {
      return BuildString(temp.ToString("F1"), " K");
    }

    // pretty-print flux
    public static string HumanReadableFlux(double flux)
    {
      return BuildString(flux >= 0.0001 ? flux.ToString("F1") : flux.ToString(), " W/m²");
    }

    // pretty-print magnetic strength
    public static string HumanReadableField(double strength)
    {
      return BuildString(strength.ToString("F1"), " uT"); //< micro-tesla
    }

    // pretty-print radiation rate
    public static string HumanReadableRadiation(double rad)
    {
      if (rad <= double.Epsilon) return "none";
      else if (rad <= 0.0000002777) return "nominal";
      return BuildString((rad * 3600.0).ToString("F3"), " rad/h");
    }

    // pretty-print percentual
    public static string HumanReadablePerc(double v, string format = "F0")
    {
      return BuildString((v * 100.0).ToString(format), "%");
    }

    // pretty-print pressure (value is in kPa)
    public static string HumanReadablePressure(double v)
    {
      return BuildString(v.ToString("F1"), " kPa");
    }

    // pretty-print volume (value is in m^3)
    public static string HumanReadableVolume(double v)
    {
      return BuildString(v.ToString("F2"), " m³");
    }

    // pretty-print surface (value is in m^2)
    public static string HumanReadableSurface(double v)
    {
      return BuildString(v.ToString("F2"), " m²");
    }

    // pretty-print mass
    public static string HumanReadableMass(double v)
    {
      return BuildString(v.ToString("F3"), " t");
    }

    // pretty-print cost
    public static string HumanReadableCost(double v)
    {
      return BuildString(v.ToString("F0"), " $");
    }

    // format a value, or return 'none'
    public static string HumanReadableAmount(double value, string append = "")
    {
      return (Math.Abs(value) <= double.Epsilon ? "none" : BuildString(value.ToString("F2"), append));
    }

    // format data size
    // - size: data size in Mb
    public static string HumanReadableDataSize(double size)
    {
      size *= 1000000.0; //< to bit
      if (size <= 1.0) return "none";
      if (size < 1000.0) return BuildString(size.ToString("F0"), " b");
      size /= 1000.0;
      if (size < 1000.0) return BuildString(size.ToString("F2"), " Kb");
      size /= 1000.0;
      if (size < 1000.0) return BuildString(size.ToString("F2"), " Mb");
      size /= 1000.0;
      if (size < 1000.0) return BuildString(size.ToString("F2"), " Gb");
      size /= 1000.0;
      return BuildString(size.ToString("F2"), " Tb");
    }

    // format data rate
    // - rate: data rate in Mb/s
    public static string HumanReadableDataRate(double rate)
    {
      return rate < 0.000001 ? "none" : BuildString(HumanReadableDataSize(rate), "/s");
    }

    // format science credits
    public static string HumanReadableScience(double value)
    {
      return BuildString("<color=cyan>", value.ToString("F1"), " CREDITS</color>");
    }

    // format shielding capability
    public static string HumanReadableShielding(double v)
    {
      return v <= double.Epsilon ? "none" : BuildString((20.0 * v / Settings.ShieldingEfficiency).ToString("F2"), " mm Pb");
    }

    #endregion

    #region GAME LOGIC -----------------------------------------------------------
    // return true if the current scene is flight
    public static bool IsFlight()
    {
      return HighLogic.LoadedSceneIsFlight;
    }

    // return true if the current scene is editor
    public static bool IsEditor()
    {
      return HighLogic.LoadedSceneIsEditor;
    }

    // return true if the current scene is not the main menu
    public static bool IsGame()
    {
      return HighLogic.LoadedSceneIsGame;
    }

    // return true if game is paused
    public static bool IsPaused()
    {
      return FlightDriver.Pause || Planetarium.Pause;
    }

    // return true if a tutorial scenario is active
    public static bool IsScenario()
    {
      return HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO
          || HighLogic.CurrentGame.Mode == Game.Modes.SCENARIO_NON_RESUMABLE;
    }

    // disable the module and return true if a tutorial scenario is active
    public static bool DisableScenario(PartModule m)
    {
      if (IsScenario())
      {
        m.enabled = false;
        m.isEnabled = false;
        return true;
      }
      return false;
    }
    #endregion

    #region BODY -----------------------------------------------------------------
    // return reference body of the planetary system that contain the specified body
    public static CelestialBody PlanetarySystem(CelestialBody body)
    {
      if (body.flightGlobalsIndex == 0) return body;
      while (body.referenceBody.flightGlobalsIndex != 0) body = body.referenceBody;
      return body;
    }

    // return selected body in tracking-view/map-view
    // if a vessel is selected, return its main body
    public static CelestialBody SelectedBody()
    {
      var target = PlanetariumCamera.fetch.target;
      return
          target == null
        ? null
        : target.celestialBody != null
        ? target.celestialBody
        : target.vessel != null
        ? target.vessel.mainBody
        : null;
    }

    // return terrain height at point specified
    // - body terrain must be loaded for this to work: use it only for loaded vessels
    public static double TerrainHeight(CelestialBody body, Vector3d pos)
    {
      PQS pqs = body.pqsController;
      if (pqs == null) return 0.0;
      Vector2d latlong = body.GetLatitudeAndLongitude(pos);
      Vector3d radial = QuaternionD.AngleAxis(latlong.y, Vector3d.down) * QuaternionD.AngleAxis(latlong.x, Vector3d.forward) * Vector3d.right;
      return (pos - body.position).magnitude - pqs.GetSurfaceHeight(radial);
    }
    #endregion

    #region VESSEL ---------------------------------------------------------------
    // return true if landed somewhere
    public static bool Landed(Vessel v)
    {
      if (v.loaded) return v.Landed || v.Splashed;
      else return v.protoVessel.landed || v.protoVessel.splashed;
    }

    // return vessel position
    public static Vector3d VesselPosition(Vessel v)
    {
      // the issue
      //   - GetWorldPos3D() return mainBody position for a few ticks after scene changes
      //   - we can detect that, and fall back to evaluating position from the orbit
      //   - orbit is not valid if the vessel is landed, and for a tick on prelauch/staging/decoupling
      //   - evaluating position from latitude/longitude work in all cases, but is probably the slowest method

      // get vessel position
      Vector3d pos = v.GetWorldPos3D();

      // during scene changes, it will return mainBody position
      if (Vector3d.SqrMagnitude(pos - v.mainBody.position) < 1.0)
      {
        // try to get it from orbit
        pos = v.orbit.getPositionAtUT(Planetarium.GetUniversalTime());

        // if the orbit is invalid (landed, or 1 tick after prelauch/staging/decoupling)
        if (double.IsNaN(pos.x))
        {
          // get it from lat/long (work even if it isn't landed)
          pos = v.mainBody.GetWorldSurfacePosition(v.latitude, v.longitude, v.altitude);
        }
      }

      // victory
      return pos;
    }

    // return set of crew on a vessel
    public static List<ProtoCrewMember> CrewList(Vessel v)
    {
      return v.loaded ? v.GetVesselCrew() : v.protoVessel.GetVesselCrew();
    }

    // return crew count of a vessel
    public static int CrewCount(Vessel v)
    {
      return v.isEVA ? 1 : CrewList(v).Count;
    }

    // return crew capacity of a vessel
    public static int CrewCapacity(Vessel v)
    {
      if (v.isEVA) return 1;
      if (v.loaded)
      {
        return v.GetCrewCapacity();
      }
      else
      {
        int capacity = 0;
        foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
        {
          capacity += p.partInfo.partPrefab.CrewCapacity;
        }
        return capacity;
      }
    }

    // return true if this is a 'vessel'
    public static bool IsVessel(Vessel v)
    {
      // something weird is going on
      if (v == null) return false;

      // if the vessel is in DEAD status, we consider it invalid
      if (v.state == Vessel.State.DEAD) return false;

      // if the vessel is a debris, a flag or an asteroid, ignore it
      // - the user can change vessel type, in that case he is actually disabling this mod for the vessel
      //   the alternative is to scan the vessel for ModuleCommand, but that is slower, and rescue vessels have no module command
      // - flags have type set to 'station' for a single update, can still be detected as they have vesselID == 0
      if (v.vesselType == VesselType.Debris || v.vesselType == VesselType.Flag || v.vesselType == VesselType.SpaceObject || v.vesselType == VesselType.Unknown) return false;

      // the vessel is valid
      return true;
    }

    // return a 32bit id for a vessel
    public static UInt32 VesselID(Vessel v)
    {
      return BitConverter.ToUInt32(v.id.ToByteArray(), 0);
    }

    // return a 32bit id for a vessel
    public static UInt32 VesselID(ProtoVessel pv)
    {
      return BitConverter.ToUInt32(pv.vesselID.ToByteArray(), 0);
    }

    // return the flight id of the root part of a vessel
    public static UInt32 RootID(Vessel v)
    {
      return v.loaded
        ? v.rootPart.flightID
        : v.protoVessel.protoPartSnapshots[v.protoVessel.rootIndex].flightID;
    }
    #endregion

    #region PART -----------------------------------------------------------------
    // get list of parts recursively, useful from the editors
    public static List<Part> GetPartsRecursively(Part root)
    {
      List<Part> ret = new List<Part>
      {
        root
      };
      foreach (Part p in root.children)
      {
        ret.AddRange(GetPartsRecursively(p));
      }
      return ret;
    }

    // return the name of a part
    public static string PartName(Part p)
    {
      return p.partInfo.name;
    }

    // return the volume of a part, in m^3
    // note: this can only be called when part has not been rotated
    //       we could use the partPrefab bb, but then it isn't available in GetInfo()
    public static double PartVolume(Part p)
    {
      Bounds bb = p.GetPartRendererBound();
      return bb.size.x * bb.size.y * bb.size.z * 0.785398;
    }

    // return the surface of a part, in m^2
    // note: this can only be called when part has not been rotated
    //       we could use the partPrefab bb, but then it isn't available in GetInfo()
    public static double PartSurface(Part p)
    {
      Bounds bb = p.GetPartRendererBound();
      double a = bb.extents.x;
      double b = bb.extents.y;
      double c = bb.extents.z;
      return 2.0 * (a * b + a * c + b * c) * 0.95493;
    }

    // return true if a part is manned, even in the editor
    public static bool IsManned(Part p)
    {
      // outside of the editors, it is easy
      if (!IsEditor()) return p.protoModuleCrew.Count > 0;

      // in the editor we need something more involved
      Int64 part_id = 4294967296L + (Int64)p.GetInstanceID();
      var manifest = KSP.UI.CrewAssignmentDialog.Instance.GetManifest();
      var part_manifest = manifest.GetCrewableParts().Find(k => (Int64)k.PartID == part_id);
      return part_manifest != null && part_manifest.AnySeatTaken();
    }
    #endregion

    #region MODULE ---------------------------------------------------------------
    // return all modules implementing a specific type in a vessel
    // note: disabled modules are not returned
    public static List<T> FindModules<T>(Vessel v) where T : class
    {
      List<T> ret = new List<T>();
      for (int i = 0; i < v.parts.Count; ++i)
      {
        Part p = v.parts[i];
        for (int j = 0; j < p.Modules.Count; ++j)
        {
          PartModule m = p.Modules[j];
          if (m.isEnabled)
          {
            T t = m as T;
            if (t != null)
            {
              ret.Add(t);
            }
          }
        }
      }
      return ret;
    }

    // return all protomodules with a specified name in a vessel
    // note: disabled modules are not returned
    public static List<ProtoPartModuleSnapshot> FindModules(ProtoVessel v, string module_name)
    {
      List<ProtoPartModuleSnapshot> ret = new List<ProtoPartModuleSnapshot>(8);
      for (int i = 0; i < v.protoPartSnapshots.Count; ++i)
      {
        ProtoPartSnapshot p = v.protoPartSnapshots[i];
        for (int j = 0; j < p.modules.Count; ++j)
        {
          ProtoPartModuleSnapshot m = p.modules[j];
          if (m.moduleName == module_name && Proto.GetBool(m, "isEnabled"))
          {
            ret.Add(m);
          }
        }
      }
      return ret;
    }

    // return true if a module implementing a specific type and satisfing the predicate specified exist in a vessel
    // note: disabled modules are ignored
    public static bool HasModule<T>(Vessel v, Predicate<T> filter) where T : class
    {
      for (int i = 0; i < v.parts.Count; ++i)
      {
        Part p = v.parts[i];
        for (int j = 0; j < p.Modules.Count; ++j)
        {
          PartModule m = p.Modules[j];
          if (m.isEnabled)
          {
            T t = m as T;
            if (t != null && filter(t))
            {
              return true;
            }
          }
        }
      }
      return false;
    }

    // return true if a proto module with the specified name and satisfing the predicate specified exist in a vessel
    // note: disabled modules are not returned
    public static bool HasModule(ProtoVessel v, string module_name, Predicate<ProtoPartModuleSnapshot> filter)
    {
      for (int i = 0; i < v.protoPartSnapshots.Count; ++i)
      {
        ProtoPartSnapshot p = v.protoPartSnapshots[i];
        for (int j = 0; j < p.modules.Count; ++j)
        {
          ProtoPartModuleSnapshot m = p.modules[j];
          if (m.moduleName == module_name && Proto.GetBool(m, "isEnabled") && filter(m))
          {
            return true;
          }
        }
      }
      return false;
    }

    // return a module from a part by name, or null if it doesn't exist
    public static PartModule FindModule(Part p, string module_name)
    {
      foreach (PartModule m in p.Modules)
      {
        if (m.moduleName == module_name) return m;
      }
      return null;
    }

    // return a module from a part by name, or null if it doesn't exist
    public static T FindModuleAs<T>(Part p, string module_name) where T : class
    {
      PartModule m = FindModule(p, module_name);
      return m ? m as T : null;
    }

    // add a module to an EVA kerbal
    public static void AddModuleToEVA(string module_name, ConfigNode module_node = null)
    {
      var eva_parts = PartLoader.LoadedPartsList.FindAll(k => k.name == "kerbalEVA" || k.name == "kerbalEVAfemale");
      foreach (AvailablePart p in eva_parts)
      {
        Type type = AssemblyLoader.GetClassByName(typeof(PartModule), module_name);
        PartModule m = p.partPrefab.gameObject.AddComponent(type) as PartModule;
        if (module_node != null) m.Load(module_node);
      }
    }

    // used by ModulePrefab function, to support multiple modules of the same type in a part
    public sealed class module_prefab_data
    {
      public int index;                         // index of current module of this type
      public List<PartModule> prefabs;          // set of module prefabs of this type
    }

    // get module prefab
    //  This function is used to solve the problem of obtaining a specific module prefab,
    //  and support the case where there are multiple modules of the same type in the part.
    public static PartModule ModulePrefab(List<PartModule> module_prefabs, string module_name, Dictionary<string, module_prefab_data> PD)
    {
      // get data related to this module type, or create it
      module_prefab_data data;
      if (!PD.TryGetValue(module_name, out data))
      {
        data = new module_prefab_data();
        data.prefabs = module_prefabs.FindAll(k => k.moduleName == module_name);
        PD.Add(module_name, data);
      }

      // return the module prefab, and increment module-specific index
      // note: if something messed up the prefab, or module were added dynamically,
      // then we have no chances of finding the module prefab so we return null
      return data.index < data.prefabs.Count ? data.prefabs[data.index++] : null;
    }
    #endregion

    #region RESOURCE -------------------------------------------------------------
    // return amount of a resource in a part
    public static double Amount(Part part, string resource_name, bool ignore_flow = false)
    {
      foreach (PartResource res in part.Resources)
      {
        if ((res.flowState || ignore_flow) && res.resourceName == resource_name) return res.amount;
      }
      return 0.0;
    }

    // return capacity of a resource in a part
    public static double Capacity(Part part, string resource_name, bool ignore_flow = false)
    {
      foreach (PartResource res in part.Resources)
      {
        if ((res.flowState || ignore_flow) && res.resourceName == resource_name) return res.maxAmount;
      }
      return 0.0;
    }

    // return level of a resource in a part
    public static double Level(Part part, string resource_name, bool ignore_flow = false)
    {
      foreach (PartResource res in part.Resources)
      {
        if ((res.flowState || ignore_flow) && res.resourceName == resource_name)
        {
          return res.maxAmount > double.Epsilon ? res.amount / res.maxAmount : 0.0;
        }
      }
      return 0.0;
    }

    // add resource amount and capacity to a part
    // create the resource if it doesn't exist already
    public static void AddResource(Part p, string res_name, double amount, double capacity)
    {
      // if the resource is already in the part
      if (p.Resources.Contains(res_name))
      {
        // add amount and capacity
        var res = p.Resources[res_name];
        res.amount += amount;
        res.maxAmount += capacity;
      }
      // if the resource is not already in the part
      else
      {
        // shortcut to resource library
        var reslib = PartResourceLibrary.Instance.resourceDefinitions;

        // if the resource is not known, log a warning and do nothing
        if (!reslib.Contains(res_name))
        {
          Lib.Verbose(Lib.BuildString("error while adding ", res_name, ": the resource doesn't exist"));
          return;
        }

        // get resource definition
        var def = reslib[res_name];

        // create the resource
        ConfigNode res = new ConfigNode("RESOURCE");
        res.AddValue("name", res_name);
        res.AddValue("amount", amount);
        res.AddValue("maxAmount", capacity);

        // add it to the part
        p.Resources.Add(res);
      }
    }

    // remove amount and capacity of a resource from a part
    // remove the resource completely if capacity goes to zero
    public static void RemoveResource(Part p, string res_name, double amount, double capacity)
    {
      // if the resource is not already in the part, do nothing
      if (p.Resources.Contains(res_name))
      {
        // get the resource
        var res = p.Resources[res_name];

        // reduce amount and capacity
        res.amount -= amount;
        res.maxAmount -= capacity;

        // clamp amount to capacity just in case
        res.amount = Math.Min(res.amount, res.maxAmount);

        // if the resource is empty
        if (res.maxAmount <= 0.005) //< deal with precision issues
        {
          // remove it
          p.Resources.Remove(res);
        }
      }
    }

    // note: the resource must exist
    public static void SetResourceCapacity(Part p, string res_name, double capacity)
    {
      // if the resource is not in the part, log a warning and do nothing
      if (!p.Resources.Contains(res_name))
      {
        Lib.Verbose(Lib.BuildString("error while setting capacity for ", res_name, ": the resource is not in the part"));
        return;
      }

      // set capacity and clamp amount
      var res = p.Resources[res_name];
      res.maxAmount = capacity;
      res.amount = Math.Min(res.amount, capacity);
    }

    // set flow of a resource in the specified part
    // do nothing if the resource don't exist in the part
    public static void SetResourceFlow(Part p, string res_name, bool enable)
    {
      // if the resource is not in the part, do nothing
      if (p.Resources.Contains(res_name))
      {
        // set flow state
        var res = p.Resources[res_name];
        res.flowState = enable;
      }
    }

    // return the definition of a resource, or null if it doesn't exist
    public static PartResourceDefinition GetDefinition(string name)
    {
      // shortcut to the resource library
      var reslib = PartResourceLibrary.Instance.resourceDefinitions;

      // return the resource definition, or null if it doesn't exist
      return reslib.Contains(name) ? reslib[name] : null;
    }

    // return name of propellant use in eva
    public static string EvaPropellantName()
    {
      // first, get the kerbal eva part prefab
      Part p = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;

      // then get the KerbalEVA module prefab
      KerbalEVA m = p.FindModuleImplementing<KerbalEVA>();

      // finally, return the propellant name
      return m.propellantResourceName;
    }

    // return capacify of propellant in eva
    public static double EvaPropellantCapacity()
    {
      // first, get the kerbal eva part prefab
      Part p = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;

      // then get the first resource and return capacity
      return p.Resources.Count == 0 ? 0.0 : p.Resources[0].maxAmount;
    }
    #endregion

    #region SCIENCE DATA ---------------------------------------------------------
    // return true if there is experiment data on the vessel
    public static bool HasData(Vessel v)
    {
      // stock science system
      if (!Features.Science)
      {
        // if vessel is loaded
        if (v.loaded)
        {
          // iterate over all science containers/experiments and return true if there is data
          return Lib.HasModule<IScienceDataContainer>(v, k => k.GetData().Length > 0);
        }
        // if not loaded
        else
        {
          // iterate over all science containers/experiments proto modules and return true if there is data
          return Lib.HasModule(v.protoVessel, "ModuleScienceContainer", k => k.moduleValues.GetNodes("ScienceData").Length > 0)
              || Lib.HasModule(v.protoVessel, "ModuleScienceExperiment", k => k.moduleValues.GetNodes("ScienceData").Length > 0);
        }
      }
      // our own science system
      else
      {
        return DB.Vessel(v).drive.files.Count > 0;
      }
    }

    // remove one experiment at random from the vessel
    public static void RemoveData(Vessel v)
    {
      // stock science system
      if (!Features.Science)
      {
        // if vessel is loaded
        if (v.loaded)
        {
          // get all science containers/experiments with data
          List<IScienceDataContainer> modules = Lib.FindModules<IScienceDataContainer>(v).FindAll(k => k.GetData().Length > 0);

          // remove a data sample at random
          if (modules.Count > 0)
          {
            IScienceDataContainer container = modules[Lib.RandomInt(modules.Count)];
            ScienceData[] data = container.GetData();
            container.DumpData(data[Lib.RandomInt(data.Length)]);
          }
        }
        // if not loaded
        else
        {
          // get all science containers/experiments with data
          var modules = new List<ProtoPartModuleSnapshot>();
          modules.AddRange(Lib.FindModules(v.protoVessel, "ModuleScienceContainer").FindAll(k => k.moduleValues.GetNodes("ScienceData").Length > 0));
          modules.AddRange(Lib.FindModules(v.protoVessel, "ModuleScienceExperiment").FindAll(k => k.moduleValues.GetNodes("ScienceData").Length > 0));

          // remove a data sample at random
          if (modules.Count > 0)
          {
            ProtoPartModuleSnapshot container = modules[Lib.RandomInt(modules.Count)];
            ConfigNode[] data = container.moduleValues.GetNodes("ScienceData");
            container.moduleValues.RemoveNode(data[Lib.RandomInt(data.Length)]);
          }
        }
      }
      // our own science system
      else
      {
        // select a file at random and remove it
        Drive drive = DB.Vessel(v).drive;
        if (drive.files.Count > 0) //< it should always be the case
        {
          string filename = string.Empty;
          int i = Lib.RandomInt(drive.files.Count);
          foreach (var pair in drive.files)
          {
            if (i-- == 0)
            {
              filename = pair.Key;
              break;
            }
          }
          drive.files.Remove(filename);
        }
      }
    }
    #endregion

    #region TECH -----------------------------------------------------------------
    // return true if the tech has been researched
    public static bool HasTech(string tech_id)
    {
      // if science is disabled, all technologies are considered available
      if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) return true;

      // if RnD is not initialized
      if (ResearchAndDevelopment.Instance == null)
      {
        // this should not happen, throw exception
        throw new Exception("querying tech '" + tech_id + "' while TechTree is not ready");
      }

      // get the tech
      return ResearchAndDevelopment.GetTechnologyState(tech_id) == RDTech.State.Available;
    }

    // return number of techs researched among the list specified
    public static int CountTech(string[] techs)
    {
      int n = 0;
      foreach (string tech_id in techs) n += HasTech(tech_id) ? 1 : 0;
      return n;
    }
    #endregion

    #region ASSETS ---------------------------------------------------------------
    // return path of directory containing the DLL
    public static string Directory()
    {
      string dll_path = Assembly.GetExecutingAssembly().Location;
      return dll_path.Substring(0, dll_path.LastIndexOf(Path.DirectorySeparatorChar));
    }

    // get a texture
    public static Texture2D GetTexture(string name)
    {
      return GameDatabase.Instance.GetTexture("Kerbalism/Textures/" + name, false);
    }

    // get a material with the shader specified
    static Dictionary<string, Material> shaders;
    public static Material GetShader(string name)
    {
      if (shaders == null)
      {
        shaders = new Dictionary<string, Material>();
        string platform = "windows";
        if (Application.platform == RuntimePlatform.LinuxPlayer) platform = "linux";
        else if (Application.platform == RuntimePlatform.OSXPlayer) platform = "osx";
        using (WWW bundle = new WWW("file://" + KSPUtil.ApplicationRootPath + "GameData/Kerbalism/Shaders/_" + platform))
        {
          if (bundle.error != null)
          {
            throw new Exception("shaders bundle not found");
          }
          foreach (Shader shader in bundle.assetBundle.LoadAllAssets<Shader>())
          {
            shaders.Add(shader.name.Replace("Custom/", string.Empty), new Material(shader));
          }
          bundle.assetBundle.Unload(false);
          bundle.Dispose();
        }
      }

      Material mat;
      if (!shaders.TryGetValue(name, out mat))
      {
        throw new Exception("shader " + name + " not found");
      }
      return mat;
    }
    #endregion

    #region CONFIG ---------------------------------------------------------------
    // get a config node from the config system
    public static ConfigNode ParseConfig(string path)
    {
      return GameDatabase.Instance.GetConfigNode(path) ?? new ConfigNode();
    }

    // get a set of config nodes from the config system
    public static ConfigNode[] ParseConfigs(string path)
    {
      return GameDatabase.Instance.GetConfigNodes(path);
    }

    // get a value from config
    public static T ConfigValue<T>(ConfigNode cfg, string key, T def_value)
    {
      try
      {
        return cfg.HasValue(key) ? (T)Convert.ChangeType(cfg.GetValue(key), typeof(T)) : def_value;
      }
      catch (Exception e)
      {
        Error("error while trying to parse '{0}' from {1} ({2})", key,cfg.name,e.Message);
        return def_value;
      }
    }

    // get an enum from config
    public static T ConfigEnum<T>(ConfigNode cfg, string key, T def_value)
    {
      try
      {
        return cfg.HasValue(key) ? (T)Enum.Parse(typeof(T), cfg.GetValue(key)) : def_value;
      }
      catch (Exception e)
      {
        Error("invalid enum in '{0}' from {1} ({2})", key, cfg.name, e.Message);
        return def_value;
      }
    }
    #endregion

    #region UI -------------------------------------------------------------------
    // return true if last GUILayout element was clicked
    public static bool IsClicked(int button = 0)
    {
      return Event.current.type == EventType.MouseDown
          && Event.current.button == button
          && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
    }

    // return true if the mouse is inside the last GUILayout element
    public static bool IsHover()
    {
      return GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
    }

    // render a textfield with placeholder
    // - id: an unique name for the textfield
    // - text: the previous textfield content
    // - placeholder: the text to show if the content is empty
    // - style: GUIStyle to use for the textfield
    public static string TextFieldPlaceholder(string id, string text, string placeholder, GUIStyle style)
    {
      GUI.SetNextControlName(id);
      text = GUILayout.TextField(text, style);

      if (Event.current.type == EventType.Repaint)
      {
        if (GUI.GetNameOfFocusedControl() == id)
        {
          if (text == placeholder) text = "";
        }
        else
        {
          if (text.Length == 0) text = placeholder;
        }
      }
      return text;
    }

    // used to make rmb ui status toggles look all the same
    public static string StatusToggle(string title, string status)
    {
      return BuildString("<b>", title, "</b>: ", status);
    }

    // show a modal popup window where the user can choose among two options
    public static PopupDialog Popup(string title, string msg, DialogGUIBase one, DialogGUIBase two)
    {
      return PopupDialog.SpawnPopupDialog
      (
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new MultiOptionDialog(title, msg, title, HighLogic.UISkin, one, two),
        false,
        HighLogic.UISkin,
        true,
        string.Empty
      );
    }
    #endregion

    #region PROTO ----------------------------------------------------------------
    public static class Proto
    {
      public static bool GetBool(ProtoPartModuleSnapshot m, string name, bool def_value = false)
      {
        string s = m.moduleValues.GetValue(name);
        return s != null && bool.TryParse(s, out bool v) ? v : def_value;
      }

      public static uint GetUInt(ProtoPartModuleSnapshot m, string name, uint def_value = 0)
      {
        string s = m.moduleValues.GetValue(name);
        return s != null && uint.TryParse(s, out uint v) ? v : def_value;
      }

      public static short GetShort(ProtoPartModuleSnapshot m, string name, short def_value = 0)
      {
        string s = m.moduleValues.GetValue(name);
        return s != null && short.TryParse(s, out short v) ? v : def_value;
      }

      public static float GetFloat(ProtoPartModuleSnapshot m, string name, float def_value = 0.0f)
      {
        // note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
        string s = m.moduleValues.GetValue(name);
        return s != null && float.TryParse(s, out float v) && !float.IsNaN(v) && !float.IsInfinity(v) ? v : def_value;
      }

      public static double GetDouble(ProtoPartModuleSnapshot m, string name, double def_value = 0.0)
      {
        // note: we set NaN and infinity values to zero, to cover some weird inter-mod interactions
        string s = m.moduleValues.GetValue(name);
        return s != null && double.TryParse(s, out double v) && !double.IsNaN(v) && !double.IsInfinity(v) ? v : def_value;
      }

      public static string GetString(ProtoPartModuleSnapshot m, string name, string def_value = "")
      {
        string s = m.moduleValues.GetValue(name);
        return s ?? def_value;
      }

      // set a value in a proto module
      public static void Set<T>(ProtoPartModuleSnapshot module, string value_name, T value)
      {
        module.moduleValues.SetValue(value_name, value.ToString(), true);
      }
    }
    #endregion

    public static class Parse
    {
      public static bool ToBool(string s, bool def_value = false)
      {
        return s != null && bool.TryParse(s, out bool v) ? v : def_value;
      }

      public static short ToShort(string s, short def_value = 0)
      {
        return s != null && short.TryParse(s, out short v) ? v : def_value;
      }

      public static uint ToUInt(string s, uint def_value = 0)
      {
        return s != null && uint.TryParse(s, out uint v) ? v : def_value;
      }

      public static float ToFloat(string s, float def_value = 0.0f)
      {
        return s != null && float.TryParse(s, out float v) ? v : def_value;
      }

      public static double ToDouble(string s, double def_value = 0.0)
      {
        return s != null && double.TryParse(s, out double v) ? v : def_value;
      }
    }
  }
}