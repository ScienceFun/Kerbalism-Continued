using System.Collections.Generic;

namespace KERBALISM
{
  // link state
  public enum LinkStatus
  {
    direct_link,
    indirect_link,
    no_link,
    no_antenna,
    blackout
  };

  public sealed class ConnectionInfo
  {
    public ConnectionInfo(LinkStatus status, double rate = 0.0, double cost = 0.0, double relaycost = 0.0)
    {
      this.linked = status == LinkStatus.direct_link || status == LinkStatus.indirect_link;
      this.status = status;
      this.rate = rate;
      this.cost = cost;
      this.relaycost = relaycost;
      this.path = new List<Vessel>();
    }

    public ConnectionInfo(ConnectionInfo other)
    {
      linked = other.linked;
      status = other.status;
      rate = other.rate;
      cost = other.cost;
      relaycost = other.relaycost;
      path = new List<Vessel>();
      foreach (Vessel v in other.path) path.Add(v);
    }

    public bool linked;             // true if there is a connection back to DSN
    public LinkStatus status;       // the link status
    public double rate;             // data rate in Mb/s
    public double cost;             // EC/s consumed for transmission
    public List<Vessel> path;       // set of vessels relaying the data

    // CommNet has different calc for relay
    // The first Node is using 100 of antennas, then your cost should be 100%
    // The next node(used as relay) will use only the relay antennas to forward, even if is connecting to home
    // Kerbalism signal use 100% when is connect to home, even is using as Relay
    public double relaycost;
  }
}