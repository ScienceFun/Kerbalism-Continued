namespace KERBALISM
{
  public sealed class MetaData
  {
    public MetaData(ScienceData data, Part host)
    {
      // find the part containing the data
      part = host;

      // get the vessel
      vessel = part.vessel;

      // get the container module storing the data
      container = Science.Container(part, Science.Experiment(data.subjectID).id);

      // get the stock experiment module storing the data (if that's the case)
      experiment = container != null ? container as ModuleScienceExperiment : null;

      // determine if this is a sample (non-transmissible)
      // - if this is a third-party data container/experiment module, we assume it is transmissible
      // - stock experiment modules are considered sample if xmit scalar is below a threshold instead
      is_sample = experiment != null && experiment.xmitDataScalar < 0.666f;

      // determine if the container/experiment can collect the data multiple times
      // - if this is a third-party data container/experiment, we assume it can collect multiple times
      is_rerunnable = experiment == null || experiment.rerunnable;
    }

    public Part part;                               // part storing the data
    public Vessel vessel;                           // vessel storing the data
    public IScienceDataContainer container;         // module containing the data
    public ModuleScienceExperiment experiment;      // module containing the data, as a stock experiment module
    public bool is_sample;                          // true if the data can't be transmitted
    public bool is_rerunnable;                      // true if the container/experiment can collect data multiple times
  }
}
