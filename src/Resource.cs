﻿using System;
using System.Collections.Generic;

namespace KERBALISM 
{
  // store info about a resource in a vessel
  public sealed class Resource_Info
  {
    public Resource_Info(Vessel v, string res_name)
    {
      // remember resource name
      resource_name = res_name;

      // get amount & capacity
      if (v.loaded)
      {
        foreach(Part p in v.Parts)
        {
          foreach(PartResource r in p.Resources)
          {
            if (r.flowState && r.resourceName == resource_name)
            {
              amount += r.amount;
              capacity += r.maxAmount;
            }
          }
        }
      }
      else
      {
        foreach(ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
        {
          foreach(ProtoPartResourceSnapshot r in p.resources)
          {
            if (r.flowState && r.resourceName == resource_name)
            {
              amount += r.amount;
              capacity += r.maxAmount;
            }
          }
        }
      }

      // calculate level
      level = capacity > double.Epsilon ? amount / capacity : 0.0;
    }

    // record a deferred production
    public void Produce(double quantity)
    {
      deferred += quantity;
    }

    // record a deferred consumption
    public void Consume(double quantity)
    {
      deferred -= quantity;
    }

    // synchronize amount from cache to vessel
    public void Sync(Vessel v, double elapsed_s)
    {
      // # OVERVIEW
      // - deferred consumption/production is accumulated, then this function called
      // - detect amount/capacity in vessel
      // - clamp deferred to amount/capacity
      // - apply deferred
      // - update cached amount [disabled, see comments]
      // - calculate change rate per-second
      // - calculate resource level
      // - reset deferred

      // # NOTE
      // It is impossible to guarantee coherency in resource simulation of loaded vessels,
      // if consumers/producers external to the resource cache exist in the vessel (#96).
      // Such is the case for example on loaded vessels with stock solar panels.
      // The effect is that the whole resource simulation become dependent on timestep again.
      // From the user point-of-view, there are two cases:
      // - (A) the timestep-dependent error is smaller than capacity
      // - (B) the timestep-dependent error is bigger than capacity
      // In case [A], there are no consequences except a slightly wrong computed level and rate.
      // In case [B], the simulation became incoherent and from that point anything can happen,
      // like for example insta-death by co2 poisoning or climatization.
      // To avoid the consequences of [B]:
      // - we hacked the stock solar panel to use the resource cache
      // - we detect incoherency on loaded vessels, and forbid the two highest warp speeds


      // remember amount currently known, to calculate rate later on
      double old_amount = amount;

      // remember capacity currently known, to detect flow state changes
      double old_capacity = capacity;

      // iterate over all enabled resource containers and detect amount/capacity again
      // - this detect production/consumption from stock and third-party mods
      //   that by-pass the resource cache, and flow state changes in general
      amount = 0.0;
      capacity = 0.0;
      if (v.loaded)
      {
        foreach(Part p in v.Parts)
        {
          foreach(PartResource r in p.Resources)
          {
            if (r.flowState && r.resourceName == resource_name)
            {
              amount += r.amount;
              capacity += r.maxAmount;
            }
          }
        }
      }
      else
      {
        foreach(ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
        {
          foreach(ProtoPartResourceSnapshot r in p.resources)
          {
            if (r.flowState && r.resourceName == resource_name)
            {
              amount += r.amount;
              capacity += r.maxAmount;
            }
          }
        }
      }

      // if incoherent producers are detected, do not allow high timewarp speed
      // - ignore incoherent consumers (no negative consequences for player)
      // - ignore flow state changes (avoid issue with process controllers)
      // - unloaded vessels can't be incoherent, we are in full control there
      // - can be disabled in settings
      // - avoid false detection due to precision errors in stock amounts
      if (Settings.EnforceCoherency && v.loaded && TimeWarp.CurrentRateIndex >= 6 && amount - old_amount > 1e-05 && capacity - old_capacity < 1e-05)
      {
        Message.Post
        (
          Severity.warning,
          Lib.BuildString
          (
            !v.isActiveVessel ? Lib.BuildString("On <b>", v.vesselName, "</b>\na ") : "A ",
            "producer of <b>", resource_name, "</b> has\n",
            "incoherent behaviour at high warp speed.\n",
            "<i>Unload the vessel before warping</i>"
          )
        );
        Lib.StopWarp(5);
      }

      // clamp consumption/production to vessel amount/capacity
      // - if deferred is negative, then amount is guaranteed to be greater than zero
      // - if deferred is positive, then capacity - amount is guaranteed to be greater than zero
      deferred = Lib.Clamp(deferred, -amount, capacity - amount);

      // apply deferred consumption/production, simulating ALL_VESSEL_BALANCED
      // - iterating again is faster than using a temporary list of valid PartResources
      // - avoid very small values in deferred consumption/production
      if(Math.Abs(deferred) > 1e-10)
      {
        if (v.loaded)
        {
          foreach(Part p in v.parts)
          {
            foreach(PartResource r in p.Resources)
            {
              if (r.flowState && r.resourceName == resource_name)
              {
                // calculate consumption/production coefficient for the part
                double k = deferred < 0.0
                  ? r.amount / amount
                  : (r.maxAmount - r.amount) / (capacity - amount);

                // apply deferred consumption/production
                r.amount += deferred * k;
              }
            }
          }
        }
        else
        {
          foreach(ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
          {
            foreach(ProtoPartResourceSnapshot r in p.resources)
            {
              if (r.flowState && r.resourceName == resource_name)
              {
                // calculate consumption/production coefficient for the part
                double k = deferred < 0.0
                  ? r.amount / amount
                  : (r.maxAmount - r.amount) / (capacity - amount);

                // apply deferred consumption/production
                r.amount += deferred * k;
              }
            }
          }
        }
      }

      // update amount, to get correct rate and levels at all times
      amount += deferred;

      // calculate rate of change per-second
      // - don't update rate during and immediately after warp blending (stock modules have instabilities during warp blending)
      // - don't update rate during the simulation steps where meal is consumed, to avoid counting it twice
      if ((!v.loaded || Kerbalism.warp_blending > 50) && !meal_happened) rate = (amount - old_amount) / elapsed_s;

      // recalculate level
      level = capacity > double.Epsilon ? amount / capacity : 0.0;

      // reset deferred production/consumption
      deferred = 0.0;

      // reset meal flag
      meal_happened = false;
    }

    // estimate time until depletion
    public double Depletion(int crew_count)
    {
      // calculate all interval-normalized rates from related rules
      double meal_rate = 0.0;
      if (crew_count > 0)
      {
        foreach(Rule rule in Profile.rules)
        {
          if (rule.interval > 0)
          {
            if (rule.input == resource_name)  meal_rate -= rule.rate / rule.interval;
            if (rule.output == resource_name) meal_rate += rule.rate / rule.interval;
          }
        }
        meal_rate *= (double)crew_count;
      }

      // calculate total rate of change
      double delta = rate + meal_rate;

      // return depletion
      return amount <= double.Epsilon ? 0.0 : delta >= -1e-10 ? double.NaN : amount / -delta;
    }

    public string resource_name;        // associated resource name
    public double deferred;             // accumulate deferred requests
    public double amount;               // amount of resource
    public double capacity;             // storage capacity of resource
    public double level;                // amount vs capacity, or 0 if there is no capacity
    public double rate;                 // rate of change in amount per-second
    public bool   meal_happened;        // true if a meal-like consumption/production was processed in the last simulation step
  }

  public sealed class Resource_Recipe
  {
    public Resource_Recipe()
    {
      inputs = new List<Entry>();
      outputs = new List<Entry>();
      left = 1.0;
    }

    public struct Entry
    {
      public Entry(string name, double quantity, bool dump=true)
      {
        this.name = name;
        this.quantity = quantity;
        this.inv_quantity = 1.0 / quantity;
        this.dump = dump;
      }
      public string name;
      public double quantity;
      public double inv_quantity;
      public bool   dump;
    }

    // add an input to the recipe
    public void Input(string resource_name, double quantity)
    {
      if (quantity > double.Epsilon) //< avoid division by zero
      {
        inputs.Add(new Entry(resource_name, quantity));
      }
    }

    // add an output to the recipe
    public void Output(string resource_name, double quantity, bool dump)
    {
      if (quantity > double.Epsilon) //< avoid division by zero
      {
        outputs.Add(new Entry(resource_name, quantity, dump));
      }
    }

    // execute the recipe
    public bool Execute(Vessel v, Vessel_Resources resources)
    {
      // determine worst input ratio
      // - pure input recipes can just underflow
      double worst_input = left;
      if (outputs.Count > 0)
      {
        for(int i=0; i<inputs.Count; ++i)
        {
          Entry e = inputs[i];
          Resource_Info res = resources.Info(v, e.name);
          worst_input = Lib.Clamp((res.amount + res.deferred) * e.inv_quantity, 0.0, worst_input);
        }
      }

      // determine worst output ratio
      // - pure output recipes can just overflow
      double worst_output = left;
      if (inputs.Count > 0)
      {
        for(int i=0; i<outputs.Count; ++i)
        {
          Entry e = outputs[i];
          if (!e.dump) // ignore outputs that can dump overboard
          {
            Resource_Info res = resources.Info(v, e.name);
            worst_output = Lib.Clamp((res.capacity - (res.amount + res.deferred)) * e.inv_quantity, 0.0, worst_output);
          }
        }
      }

      // determine worst-io
      double worst_io = Math.Min(worst_input, worst_output);

      // consume inputs
      for(int i=0; i<inputs.Count; ++i)
      {
        Entry e = inputs[i];
        resources.Consume(v, e.name, e.quantity * worst_io);
      }

      // produce outputs
      for(int i=0; i<outputs.Count; ++i)
      {
        Entry e = outputs[i];
        resources.Produce(v, e.name, e.quantity * worst_io);
      }

      // update amount left to execute
      left -= worst_io;

      // the recipe was executed, at least partially
      return worst_io > double.Epsilon;
    }

    public List<Entry>  inputs;   // set of input resources
    public List<Entry>  outputs;  // set of output resources
    public double       left;     // what proportion of the recipe is left to execute
  }

  // the resource cache of a vessel
  public sealed class Vessel_Resources
  {
    // return a resource handler
    public Resource_Info Info(Vessel v, string resource_name)
    {
      // try to get existing entry if any
      if (resources.TryGetValue(resource_name, out Resource_Info res)) return res;

      // create new entry
      res = new Resource_Info(v, resource_name);

      // remember new entry
      resources.Add(resource_name, res);

      // return new entry
      return res;
    }

    // apply deferred requests for a vessel and synchronize the new amount in the vessel
    public void Sync(Vessel v, double elapsed_s)
    {
      // execute all possible recipes
      bool executing = true;
      while(executing)
      {
        executing = false;
        for(int i=0; i<recipes.Count; ++i)
        {
          Resource_Recipe recipe = recipes[i];
          if (recipe.left > double.Epsilon)
          {
            executing |= recipe.Execute(v, this);
          }
        }
      }

      // forget the recipes
      recipes.Clear();

      // apply all deferred requests and synchronize to vessel
      foreach(var pair in resources) pair.Value.Sync(v, elapsed_s);
    }

    // record deferred production of a resource (shortcut)
    public void Produce(Vessel v, string resource_name, double quantity)
    {
      Info(v, resource_name).Produce(quantity);
    }

    // record deferred consumption of a resource (shortcut)
    public void Consume(Vessel v, string resource_name, double quantity)
    {
      Info(v, resource_name).Consume(quantity);
    }

    // record deferred execution of a recipe
    public void Transform(Resource_Recipe recipe)
    {
      recipes.Add(recipe);
    }

    public Dictionary<string, Resource_Info> resources = new Dictionary<string, Resource_Info>(32);
    public List<Resource_Recipe> recipes = new List<Resource_Recipe>(4);
  }

  // manage per-vessel resource caches
  public static class ResourceCache
  {
    public static void Init()
    {
      entries = new Dictionary<Guid, Vessel_Resources>();
    }

    public static void Clear()
    {
      entries.Clear();
    }

    public static void Purge(Vessel v)
    {
      entries.Remove(v.id);
    }

    public static void Purge(ProtoVessel pv)
    {
      entries.Remove(pv.vesselID);
    }

    // return resource cache for a vessel
    public static Vessel_Resources Get(Vessel v)
    {
      // try to get existing entry if any
      if (entries.TryGetValue(v.id, out Vessel_Resources entry)) return entry;

      // create new entry
      entry = new Vessel_Resources();

      // remember new entry
      entries.Add(v.id, entry);

      // return new entry
      return entry;
    }

    // return a resource handler (shortcut)
    public static Resource_Info Info(Vessel v, string resource_name)
    {
      return Get(v).Info(v, resource_name);
    }

    // register deferred production of a resource (shortcut)
    public static void Produce(Vessel v, string resource_name, double quantity)
    {
      Info(v, resource_name).Produce(quantity);
    }

    // register deferred consumption of a resource (shortcut)
    public static void Consume(Vessel v, string resource_name, double quantity)
    {
      Info(v, resource_name).Consume(quantity);
    }

    // register deferred execution of a recipe (shortcut)
    public static void Transform(Vessel v, Resource_Recipe recipe)
    {
      Get(v).Transform(recipe);
    }

    // resource cache
    static Dictionary<Guid, Vessel_Resources> entries;
  }
}