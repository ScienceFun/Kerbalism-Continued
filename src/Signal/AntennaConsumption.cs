namespace KERBALISM
{
  public static class AntennaConsumption
  {
    public static void ConsumptionOmin()
    {
      // J = jales
      // r = Range
      // E = energy
      // N = Network connections
      // kW = J/(1000*sec)        = Convert to Kerbalism

      // Formulas
      // J = π * r^2              = Consumption
      // Max dist = Sqrt(E/π)     = Antenna Max distance

      // Energy Consumption in a Network
      // for(i=1;i<N;i++) SUM += (π * i.r^2)


    }

    public static void ConsumptionDirect()
    {
      // J = jales
      // α = angular spread
      // r = range
      // E = energy
      // N = Network connections
      // kW = J/(1000*sec)        = Convert to Kerbalism

      // J = α/2 * r^2            = Consumption
      // dist = Sqrt(2*E/α)       = Antenna Max distance

      // Energy Consumption in a Network
      // for(i=1;i<N;i++) SUM += (i.α/2 * i.r^2)

    }
  }
}