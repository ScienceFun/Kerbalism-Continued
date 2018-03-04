﻿using System;

namespace KERBALISM 
{
  public sealed class Habitat : PartModule, ISpecifics, IConfigurable
  {
    [KSPField] public double  volume = 0.0;            // habitable volume in m^3, deduced from bounding box if not specified
    [KSPField] public double  surface = 0.0;           // external surface in m^2, deduced from bounding box if not specified
    [KSPField] public string  inflate = string.Empty;  // inflate animation, if any
    [KSPField] public bool    toggle = true;           // show the enable/disable toggle
    [KSPField] public bool    animBackwards;
    [KSPField] public int     CrewCapacity;

    [KSPField(isPersistant = true)] public State    state = State.enabled;
    [KSPField(isPersistant = true)] private double  perctDeployed = 0;

    [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Volume")]  public string Volume;
    [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Surface")] public string Surface;

    Animator inflate_anim;

    // pseudo-ctor
    public override void OnStart(StartState state)
    {
      // don't break tutorial scenarios
      if (Lib.DisableScenario(this)) return;

      // calculate habitat internal volume
      if (volume <= double.Epsilon) volume = Lib.PartVolume(part);

      // calculate habitat external surface
      if (surface <= double.Epsilon) surface = Lib.PartSurface(part);

      if (CrewCapacity == 0) CrewCapacity = part.CrewCapacity;

      if (inflate.Length != 0)
      {
        SetCrewCapacity(Lib.Level(part, "Atmosphere") >= 1);
        RefreshPartData();
      }

      // set RMB UI status strings
      Volume = Lib.HumanReadableVolume(volume);
      Surface = Lib.HumanReadableSurface(surface);

      // hide toggle if specified
      Events["Toggle"].active = toggle;
      Actions["Action"].active = toggle;

#if DEBUG
      Fields["Volume"].guiActive = true;
      Fields["Surface"].guiActive = true;
      Fields["CrewCapacity"].guiActive = true;
      Fields["state"].guiActive = true;
#endif

      // create animators
      inflate_anim = new Animator(part, inflate);

      // configure on start
      Configure(true);
    }

    public void Configure(bool enable)
    {
      if (enable)
      {
        // if never set, this is the case if:
        // - part is added in the editor
        // - module is configured first time either in editor or in flight
        // - module is added to an existing savegame
        if (!part.Resources.Contains("Atmosphere"))
        {
          // add internal atmosphere resources
          // - disabled habitats start with zero atmosphere
          Lib.AddResource(part, "Atmosphere", (state == State.enabled && Features.Pressure) ? volume : 0.0, volume);
          Lib.AddResource(part, "WasteAtmosphere", 0.0, volume);

          // add external surface shielding
          Lib.AddResource(part, "Shielding", 0.0, surface);

          // inflatable habitats can't be shielded (but still need the capacity)
          part.Resources["Shielding"].isTweakable = inflate.Length == 0;

          // if shielding feature is disabled, just hide it
          part.Resources["Shielding"].isVisible = Features.Shielding;
        }
      }
      else
      {
        Lib.RemoveResource(part, "Atmosphere", 0.0, volume);
        Lib.RemoveResource(part, "WasteAtmosphere", 0.0, volume);
        Lib.RemoveResource(part, "Shielding", 0.0, surface);
      }
    }

    void Set_Flow(bool b)
    {
      Lib.SetResourceFlow(part, "Atmosphere", b);
      Lib.SetResourceFlow(part, "WasteAtmosphere", b);
      Lib.SetResourceFlow(part, "Shielding", b);
    }

    State Equalize()
    {
      // in flight
      if (Lib.IsFlight())
      {
        double atmosphereAmount = 0;
        double atmosphereMaxAmount = 0;
        double partsHabVolume = 0;
        // Get all habs no inflate in the vessel
        foreach (Habitat partHabitat in vessel.FindPartModulesImplementing<Habitat>())
        {
          if (partHabitat.inflate.Length == 0)
          {
            PartResource t = partHabitat.part.Resources["Atmosphere"];
            atmosphereAmount += t.amount;
            atmosphereMaxAmount += t.maxAmount;
            partsHabVolume += partHabitat.volume;
          }
        }
        PartResource hab_atmo = part.Resources["Atmosphere"];

        // get level of atmosphere in part
        double hab_level = Lib.Level(part, "Atmosphere", true);

        // equalization succeeded if the levels are the same
        if (perctDeployed == 1)
        {
          RefreshPartData();
          return State.enabled;
        }

        // case if it has only one hab part and it is inflate
        // sample: proto + inflate habitat
        if (atmosphereMaxAmount == 0) return State.equalizing;

        // determine equalization speed
        // we deal with the case where a big hab is sucking all atmosphere from the rest of the vessel
        double amount = Math.Min(partsHabVolume, volume) * equalize_speed * Kerbalism.elapsed_s;

        // the others hab pressure are higher
        if ((atmosphereAmount / atmosphereMaxAmount) > hab_level)
        {
          // clamp amount to what's available in the hab and what can fit in the part
          amount = Math.Min(amount, atmosphereAmount);
          amount = Math.Min(amount, hab_atmo.maxAmount - hab_atmo.amount);

          // consume from all enabled habs in the vessel that are not Inflate
          foreach (Habitat partHabitat in vessel.FindPartModulesImplementing<Habitat>())
          {
            if (partHabitat.inflate.Length == 0)
            {
              PartResource t = partHabitat.part.Resources["Atmosphere"];
              t.amount -= (amount * (t.amount / atmosphereAmount));
            }
          }

          // produce in the part
          hab_atmo.amount += amount;
        }

        // equalization still in progress
        return State.equalizing;
      }
      // in the editors
      else
      {
        // set amount to max capacity
        PartResource hab_atmo = part.Resources["Atmosphere"];
        hab_atmo.amount = hab_atmo.maxAmount;

        // return new state
        return State.enabled;
      }
    }

    State Venting()
    {
      // in flight
      if (Lib.IsFlight())
      {
        // shortcuts
        PartResource atmo = part.Resources["Atmosphere"];
        PartResource waste = part.Resources["WasteAtmosphere"];

        // get level of atmosphere in part
        double hab_level = Lib.Level(part, "Atmosphere", true);

        // venting succeeded if the amount reached zero
        if (atmo.amount <= double.Epsilon && waste.amount <= double.Epsilon)
        {
          RefreshPartData();
          return State.disabled;
        }

        // how much to vent
        double rate = volume * equalize_speed * Kerbalism.elapsed_s;
        double atmo_k = atmo.amount / (atmo.amount + waste.amount);
        double waste_k = waste.amount / (atmo.amount + waste.amount);

        // consume from the part, clamp amount to what's available
        atmo.amount = Math.Max(atmo.amount - rate * atmo_k, 0.0);
        waste.amount = Math.Max(waste.amount - rate * waste_k, 0.0);

        // venting still in progress
        return State.venting;
      }
      // in the editors
      else
      {
        // set amount to zero
        part.Resources["Atmosphere"].amount = 0.0;
        part.Resources["WasteAtmosphere"].amount = 0.0;

        // return new state
        RefreshPartData();
        return State.disabled;
      }
    }

    public void Update()
    {
      perctDeployed = Lib.Level(part, "Atmosphere", true);

      string status_str = string.Empty;
      switch (state)
      {
        case State.enabled:     status_str = "enabled"; break;
        case State.disabled:    status_str = "disabled"; break;
        case State.equalizing:  status_str = inflate.Length == 0 ? "equalizing...(" : "inflating...("; break;
        case State.venting:     status_str = inflate.Length == 0 ? "venting...(" : "deflating...("; break;
      }
      // update ui
      if (state == State.equalizing || state == State.venting) status_str += (Math.Round(perctDeployed, 2) * 100).ToString() + "%)";
      Events["Toggle"].guiName = Lib.StatusToggle("Habitat", status_str);

      // if there is an inflate animation, set still animation from pressure
      if (animBackwards) inflate_anim.Still(Math.Abs(perctDeployed - 1));
      else inflate_anim.Still((perctDeployed));
    }

    public void FixedUpdate()
    {
      // if part is manned (even in the editor), force enabled
      if (Lib.IsManned(part) && state != State.enabled) state = State.equalizing;

      // state machine
      switch (state)
      {
        case State.enabled:
            Set_Flow(true);
          break;

        case State.disabled:
            Set_Flow(false);
          break;

        case State.equalizing:
            Set_Flow(true);
            state = Equalize();
          break;

        case State.venting:
            Set_Flow(false);
            state = Venting();
          break;
      }

      if(inflate.Length != 0)
      {
        SetCrewCapacity(state == State.enabled);
      }

      // instant pressurization and scrubbing inside breathable atmosphere
      if (!Lib.IsEditor() && Cache.VesselInfo(vessel).breathable && inflate.Length == 0)
      {
        var atmo = part.Resources["Atmosphere"];
        var waste = part.Resources["WasteAtmosphere"];
        if (Features.Pressure) atmo.amount = atmo.maxAmount;
        if (Features.Poisoning) waste.amount = 0.0;
      }
    }

    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "_", active = true)]
    public void Toggle()
    {
      // if manned, we can't depressurize
      if (Lib.IsManned(part) && (state == State.enabled || state == State.equalizing))
      {
        Message.Post(Lib.BuildString("Can't disable <b>", Lib.PartName(part), " habitat</b> while crew is inside"));
        return;
      }

      // state switching
      switch (state)
      {
        case State.enabled:     state = State.venting;    break;
        case State.disabled:    state = State.equalizing; break;
        case State.equalizing:  state = State.venting;    break;
        case State.venting:     state = State.equalizing; break;
      }
    }

    // action groups
    [KSPAction("Enable/Disable Habitat")] public void Action(KSPActionParam param) { Toggle(); }

    // part tooltip
    public override string GetInfo()
    {
      return Specs().Info();
    }

    // specifics support
    public Specifics Specs()
    {
      Specifics specs = new Specifics();
      specs.Add("volume", Lib.HumanReadableVolume(volume > double.Epsilon ? volume : Lib.PartVolume(part)));
      specs.Add("surface", Lib.HumanReadableSurface(surface > double.Epsilon ? surface : Lib.PartSurface(part)));
      if (inflate.Length > 0) specs.Add("Inflatable", "yes");
      return specs;
    }

    // return habitat volume in a vessel in m^3
    public static double Total_Volume(Vessel v)
    {
      // we use capacity: this mean that partially pressurized parts will still count,
      return ResourceCache.Info(v, "Atmosphere").capacity;
    }

    // return habitat surface in a vessel in m^2
    public static double Total_Surface(Vessel v)
    {
      // we use capacity: this mean that partially pressurized parts will still count,
      return ResourceCache.Info(v, "Shielding").capacity;
    }

    // return normalized pressure in a vessel
    public static double Pressure(Vessel v)
    {
      // the pressure is simply the atmosphere level
      return ResourceCache.Info(v, "Atmosphere").level;
    }

    // return waste level in a vessel atmosphere
    public static double Poisoning(Vessel v)
    {
      // the proportion of co2 in the atmosphere is simply the level of WasteAtmo
      return ResourceCache.Info(v, "WasteAtmosphere").level;
    }

    // return shielding factor in a vessel
    public static double Shielding(Vessel v)
    {
      // the shielding factor is simply the level of shielding, scaled by the 'shielding efficiency' setting
      return ResourceCache.Info(v, "Shielding").level * Settings.ShieldingEfficiency;
    }

    // return living space factor in a vessel
    public static double Living_Space(Vessel v)
    {
      // living space is the volume per-capita normalized against an 'ideal living space' and clamped in an acceptable range
      return Lib.Clamp((Total_Volume(v) / Lib.CrewCount(v)) / Settings.IdealLivingSpace, 0.1, 1.0);
    }

    // traduce living space value to string
    public static string Living_Space_to_String(double v)
    {
      if (v >= 0.99) return "ideal";
      else if (v >= 0.75) return "good";
      else if (v >= 0.5) return "modest";
      else if (v >= 0.25) return "poor";
      else return "cramped";
    }

    // Set the crew capacity of the part
    void SetCrewCapacity(bool canUseCrew)
    {
      if (canUseCrew)
      {
        part.crewTransferAvailable = true;
        part.CrewCapacity = CrewCapacity;
      }
      else
      {
        part.crewTransferAvailable = false;
        part.CrewCapacity = 0;
      }
      //part.CheckTransferDialog();
      //MonoUtilities.RefreshContextWindows(part);
    }

    void RefreshPartData()
    {
      if (HighLogic.LoadedSceneIsEditor)
      {
        GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartTweaked, part);
        GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
      }
      else if (HighLogic.LoadedSceneIsFlight)
      {
        GameEvents.onVesselWasModified.Fire(this.vessel);
      }
      part.CheckTransferDialog();
      MonoUtilities.RefreshContextWindows(part);
    }

    // habitat state
    public enum State
    {
      disabled,   // hab is disabled
      enabled,    // hab is enabled
      equalizing, // hab is equalizing (between disabled and enabled)
      venting     // hab is venting (between enabled and disabled)
    }

    // constants
    const double equalize_speed = 0.1; // equalization/venting speed per-second, in proportion to volume
  }
}