using System.Collections.Generic;

namespace KERBALISM
{
  public sealed class DataStream
  {
    public DataStream()
    {
      queue = new List<ScienceData>();
      transmitted = new List<double>();
    }

    public void Append(ScienceData data)
    {
      queue.Add(data);
      transmitted.Add(0.0);
    }

    public void Update(double size, Vessel v)
    {
      if (queue.Count > 0)
      {
        // get first data in queue
        ScienceData data = queue[0];

        // transmit some
        transmitted[0] += size;

        // if transmission is completed
        if (transmitted[0] >= data.dataAmount)
        {
          // if triggered, fire the triggered data callback
          if (data.triggered)
          {
            // we can't just integrate triggered data with Science data transmission,
            // because virtually all the listener callbacks assume the vessel is loaded
            GameEvents.OnTriggeredDataTransmission.Fire(data, v, false);
          }
          // if not triggered, we just submit the data
          else
          {
            ScienceSubject subject = ResearchAndDevelopment.GetSubjectByID(data.subjectID);
            if (subject != null)
            {
              ResearchAndDevelopment.Instance.SubmitScienceData(data.dataAmount, subject, 1.0f, v.protoVessel);
            }
          }

          // remove data from queue
          queue.RemoveAt(0);
          transmitted.RemoveAt(0);
        }
      }
    }

    public void Abort(Vessel v)
    {
      foreach (ScienceData data in queue)
      {
        foreach (ModuleScienceContainer container in Lib.FindModules<ModuleScienceContainer>(v))
        {
          // add the data to the container
          container.ReturnData(data);

          // if, for some reasons, it wasn't possible to add the data, try the next container
          // note: this also deal with multiple versions of same data in the entire vessel
          if (!container.HasData(data)) continue;

          // data was added, process the next data
          break;
        }
      }

      queue.Clear();
      transmitted.Clear();
    }

    public string Current_file()
    {
      return queue.Count > 0 ? queue[0].title : string.Empty;
    }

    public double Current_progress()
    {
      return queue.Count > 0 ? transmitted[0] / queue[0].dataAmount : 0.0;
    }

    public bool Transmitting()
    {
      return queue.Count > 0;
    }

    List<ScienceData> queue;
    List<double> transmitted;
  }
}
