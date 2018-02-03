using CommNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KERBALISM
{
  public static class Cache
  {
    public static void Init()
    {
      vessels = new Dictionary<uint, Vessel_Info>();
      next_inc = 0;
    }

    public static void Clear()
    {
      vessels.Clear();
      next_inc = 0;
    }

    public static void Purge(Vessel v)
    {
      uint vID = Lib.VesselID(v);
      vessels.Remove(vID);
    }

    public static void Purge(ProtoVessel pv)
    {
      uint vID = Lib.VesselID(pv);
      vessels.Remove(vID);
    }

    public static void Update()
    {
      // purge the oldest entry from the vessel cache
      if (vessels.Count > 0)
      {
        UInt64 oldest_inc = UInt64.MaxValue;
        UInt32 oldest_id = 0;
        foreach (KeyValuePair<UInt32, Vessel_Info> pair in vessels)
        {
          if (pair.Value.inc < oldest_inc)
          {
            oldest_inc = pair.Value.inc;
            oldest_id = pair.Key;
          }
        }
        vessels.Remove(oldest_id);
      }
    }

    public static Vessel_Info VesselInfo(Vessel v)
    {
      // get vessel id
      UInt32 id = Lib.VesselID(v);

      // get the info from the cache, if it exist
      if (vessels.TryGetValue(id, out Vessel_Info info)) return info;

      // compute vessel info
      info = new Vessel_Info(v, id, next_inc++);

      // store vessel info in the cache
      vessels.Add(id, info);

      return info;
    }

    public static bool HasVesselInfo(Vessel v, out Vessel_Info vi) => vessels.TryGetValue(Lib.VesselID(v), out vi);
    static Dictionary<UInt32, Vessel_Info> vessels;           // vessel cache
    static UInt64 next_inc;                                   // used to generate unique id
  }
}
