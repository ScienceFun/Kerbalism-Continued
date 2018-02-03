using System;

namespace KERBALISM
{
  // Add a specific environment reading to a part ui, and to the telemetry panel.
  public sealed class Sensor : PartModule, ISpecifics
  {
    [KSPField(isPersistant=true)] public string type;   // type of telemetry provided
    [KSPField] public string pin = string.Empty;        // pin animation

    [KSPField(guiActive = true, guiName = "_")] public string Status;

    Animator pin_anim;

    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // create animator
      pin_anim = new Animator(part, pin);

      // setup ui
      Fields["Status"].guiName = Lib.UppercaseFirst(type);
    }

    public void Update()
    {
      // in flight
      if (Lib.IsFlight())
      {
        // get info from cache
        Vessel_Info vi = Cache.VesselInfo(vessel);

        // do nothing if vessel is invalid
        if (!vi.is_valid) return;

        // update status
        Status = Telemetry_Content(vessel, vi, type);

        // if there is a pin animation
        if (pin.Length > 0)
        {
          // still-play pin animation
          pin_anim.Still(Telemetry_Pin(vessel, vi, type));
        }
      }
    }

    // part tooltip
    public override string GetInfo()
    {
      return Specs().Info("Add telemetry readings to the part ui, and to the telemetry panel");
    }

    // specifics support
    public Specifics Specs()
    {
      var specs = new Specifics();
      specs.Add("Type", type);
      return specs;
    }

    // get readings value in [0,1] range, for pin animation
    public static double Telemetry_Pin(Vessel v, Vessel_Info vi, string type)
    {
      switch(type)
      {
        case "temperature":   return Math.Min(vi.temperature / 11000.0, 1.0);
        case "radiation":     return Math.Min(vi.radiation * 3600.0 / 11.0, 1.0);
        case "pressure":      return Math.Min(v.mainBody.GetPressure(v.altitude) / Sim.PressureAtSeaLevel() / 11.0, 1.0);
        case "gravioli":      return Math.Min(vi.gravioli, 1.0);
      }
      return 0.0;
    }

    // get readings value
    public static double Telemetry_Value(Vessel v, Vessel_Info vi, string type)
    {
      switch(type)
      {
        case "temperature":   return vi.temperature;
        case "radiation":     return vi.radiation;
        case "pressure":      return v.mainBody.GetPressure(v.altitude);;
        case "gravioli":      return vi.gravioli;
      }
      return 0.0;
    }

    // get readings short text info
    public static string Telemetry_Content(Vessel v, Vessel_Info vi, string type)
    {
      switch(type)
      {
        case "temperature":   return Lib.HumanReadableTemp(vi.temperature);
        case "radiation":     return Lib.HumanReadableRadiation(vi.radiation);
        case "pressure":      return Lib.HumanReadablePressure(v.mainBody.GetPressure(v.altitude));
        case "gravioli":      return vi.gravioli < 0.33 ? "nothing here" : vi.gravioli < 0.66 ? "almost one" : "WOW!";
      }
      return string.Empty;
    }

    // get readings tooltip
    public static string Telemetry_Tooltip(Vessel v, Vessel_Info vi, string type)
    {
      switch(type)
      {
        case "temperature":
          return Lib.BuildString
          (
            "<align=left />",
            "solar flux\t<b>", Lib.HumanReadableFlux(vi.solar_flux), "</b>\n",
            "albedo flux\t<b>", Lib.HumanReadableFlux(vi.albedo_flux), "</b>\n",
            "body flux\t<b>", Lib.HumanReadableFlux(vi.body_flux), "</b>"
          );

        case "radiation":
          return string.Empty;

        case "pressure":
          return vi.underwater
            ? "inside <b>ocean</b>"
            : vi.atmo_factor < 1.0
            ? Lib.BuildString("inside <b>atmosphere</b> (", vi.breathable ? "breathable" : "not breathable", ")")
            : Sim.InsideThermosphere(v)
            ? "inside <b>thermosphere</b>"
            : Sim.InsideExosphere(v)
            ? "inside <b>exosphere</b>"
            : string.Empty;

        case "gravioli":
          return Lib.BuildString
          (
            "Gravioli detection events per-year: <b>", vi.gravioli.ToString("F2"), "</b>\n\n",
            "<i>The elusive negative gravioli particle\nseem to be much harder to detect\n",
            "than expected. On the other\nside there seems to be plenty\nof useless positive graviolis around.</i>"
          );
      }
      return string.Empty;
    }
  }
}