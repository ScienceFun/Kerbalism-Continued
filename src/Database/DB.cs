using System.Collections.Generic;

namespace KERBALISM
{
  public static class DB
  {
    public static void Load(ConfigNode node)
    {
      // get version (or use current one for new savegames)
      version = Lib.ConfigValue(node, "version", Lib.Version());

      // get unique id (or generate one for new savegames)
      uid = Lib.ConfigValue(node, "uid", Lib.RandomInt(int.MaxValue));

      // if this is an unsupported version, print warning
      if (string.CompareOrdinal(version, "1.1.5.0") < 0)
      {
        Lib.Verbose("DB.Load - Loading save from unsupported version {0}", version);
      }

      // load kerbals data
      kerbals = new Dictionary<string, KerbalData>();
      if (node.HasNode("kerbals"))
      {
        foreach (var kerbal_node in node.GetNode("kerbals").GetNodes())
        {
          kerbals.Add(From_Safe_Key(kerbal_node.name), new KerbalData(kerbal_node));
        }
      }

      // load vessels data
      vessels = new Dictionary<uint, VesselData>();
      if (node.HasNode("vessels"))
      {
        foreach (var vessel_node in node.GetNode("vessels").GetNodes())
        {
          vessels.Add(Lib.Parse.ToUInt(vessel_node.name), new VesselData(vessel_node));
        }
      }

      // load bodies data
      bodies = new Dictionary<string, BodyData>();
      if (node.HasNode("bodies"))
      {
        foreach (var body_node in node.GetNode("bodies").GetNodes())
        {
          bodies.Add(From_Safe_Key(body_node.name), new BodyData(body_node));
        }
      }

      // load landmark data
      if (node.HasNode("landmarks"))
      {
        landmarks = new LandmarkData(node.GetNode("landmarks"));
      }
      else
      {
        landmarks = new LandmarkData();
      }

      // load ui data
      if (node.HasNode("ui"))
      {
        ui = new UIData(node.GetNode("ui"));
      }
      else
      {
        ui = new UIData();
      }

      // load groundstation
      groundstation = new Dictionary<string, GroundStationData>();
      if (node.HasNode("groundstation"))
      {
        foreach (var gStation_node in node.GetNode("groundstation").GetNodes())
        {
          groundstation.Add(From_Safe_Key(gStation_node.name),new GroundStationData(gStation_node));
        }
      }

      // if an old savegame was imported, log some debug info
      if (version != Lib.Version()) Lib.Verbose("DB.Load - Savegame converted from version {0}", version);
    }

    public static void Save(ConfigNode node)
    {
      // save version
      node.AddValue("version", Lib.Version());

      // save unique id
      node.AddValue("uid", uid);

      // save kerbals data
      var kerbals_node = node.AddNode("kerbals");
      foreach (var p in kerbals)
      {
        p.Value.Save(kerbals_node.AddNode(To_Safe_Key(p.Key)));
      }

      // save vessels data
      var vessels_node = node.AddNode("vessels");
      foreach (var p in vessels)
      {
        p.Value.Save(vessels_node.AddNode(p.Key.ToString()));
      }

      // save bodies data
      var bodies_node = node.AddNode("bodies");
      foreach (var p in bodies)
      {
        p.Value.Save(bodies_node.AddNode(To_Safe_Key(p.Key)));
      }

      // save landmark data
      landmarks.Save(node.AddNode("landmarks"));

      // save ui data
      ui.Save(node.AddNode("ui"));

      // save groundstation data
      var gStation_node = node.AddNode("groundstation");
      foreach (var p in groundstation)
      {
        p.Value.Save(gStation_node.AddNode(To_Safe_Key(p.Key)));
      }
    }

    public static KerbalData Kerbal(string name)
    {
      if (!kerbals.ContainsKey(name))
      {
        kerbals.Add(name, new KerbalData());
      }
      return kerbals[name];
    }

    public static VesselData Vessel(Vessel v)
    {
      uint id = Lib.RootID(v);
      if (!vessels.ContainsKey(id))
      {
        vessels.Add(id, new VesselData());
      }
      return vessels[id];
    }

    public static BodyData Body(string name)
    {
      if (!bodies.ContainsKey(name))
      {
        bodies.Add(name, new BodyData());
      }
      return bodies[name];
    }

    public static GroundStationData Station(string name)
    {
      if(!groundstation.ContainsKey(name))
      {
        groundstation.Add(name, new GroundStationData());
      }
      return groundstation[name];
    }

    public static string To_Safe_Key(string key) { return key.Replace(" ", "___"); }

    public static string From_Safe_Key(string key) { return key.Replace("___", " "); }

    public static string version;                                       // savegame version
    public static int uid;                                              // savegame unique id
    public static Dictionary<string, KerbalData> kerbals;               // store data per-kerbal
    public static Dictionary<uint, VesselData> vessels;                 // store data per-vessel, indexed by root part id
    public static Dictionary<string, BodyData> bodies;                  // store data per-body
    public static LandmarkData landmarks;                               // store landmark data
    public static UIData ui;                                            // store ui data
    public static Dictionary<string,GroundStationData> groundstation;   // store groundStation data
  }
}
