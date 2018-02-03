using System.Collections.Generic;
using UnityEngine;

namespace KERBALISM
{
  public class GroundStationData
  {
    public GroundStationData()
    {
      color = Color.red;
      frequencies = new List<short> { 0, 1, 2, 3, 4 };
    }

    public GroundStationData(ConfigNode node)
    {
      frequencies = new List<short>();
      
      color = Color.blue;

      foreach (string s in node.GetValues("frequencies"))
      {
        frequencies.Add(Lib.Parse.ToShort(s));
      }
    }

    public void Save(ConfigNode node)
    {
      node.AddValue("color", color);

      var freq = node.AddNode("frequencies");

      foreach (uint id in frequencies)
      {
        freq.AddValue("frequency",id.ToString());
      }
    }

    public Color color = Color.red;
    public List<short> frequencies;
  }
}
