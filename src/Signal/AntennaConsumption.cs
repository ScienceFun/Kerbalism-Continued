using System;

namespace KERBALISM
{
  public static class AntennaConsumption
  {
    public static void ConsumptionOmin()
    {
      // J = joule
      // ec = kW/s (Kerbalism)
      // r = Range
      // E = energy
      // N = Network connections
      // kW = J/(1000*sec)        = Convert to Kerbalism

      // Formulas
      // J = π * r^2              = Consumption

      double dist = 1000000;
      double j = Math.PI * Math.Pow(dist, 2);
      // j ≅ 3140000000000  (3.140.000.000.000)
      float sec = 20;
      // watts = 3140000000000/20 (157.000.000.000W)

      double ec = j / (1000 * sec);

      // Energy Consumption in a Network
      // for(i=1;i<N;i++) SUM += (π * i.r^2)


    }

    public static void ConsumptionDirect()
    {
      // J = jales
      // α = beam width
      // r = range
      // E = energy
      // N = Network connections
      // kW = J/(1000*sec)        = Convert to Kerbalism

      // J = α/2 * r^2            = Consumption
      double dist = 1000;
      double ec = 10/2 * Math.Pow(dist, 2);
      // dist = Sqrt(2*E/α)       = Antenna Max distance

      //// Energy Consumption in a Network
      //List<int> N = new List<int>() { 0, 1, 2, 3 };         // Connections
      //double J = 0;
      //for (int i = 1; i < N.Count; i++)
      //{
      //  J += (N[i].α / 2 * N[i].r ^ 2);
      //}

    }
  }
}