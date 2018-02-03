using UnityEngine;
using KSP.UI.Screens.Flight.Dialogs;

namespace KERBALISM
{
  // Remove the data from experiments (and set them inoperable) as soon as the
  // science dialog is opened, and store the data in the vessel drive.
  // This method support any module that set an appropriate OnDiscardData() callback
  // when opening the science dialog, this include stock science experiments and others.
  // Hiding the science dialog can be used by who doesn't want it.
  public sealed class MiniHijacker : MonoBehaviour
  {
    void Start()
    {
      // get dialog
      dialog = gameObject.GetComponentInParent<ExperimentsResultDialog>();
      if (dialog == null) { Destroy(gameObject); return; }

      // prevent rendering
      dialog.gameObject.SetActive(false);

      // for each page
      // - some mod may collect multiple experiments at once
      while (dialog.pages.Count > 0)
      {
        // get page
        var page = dialog.pages[0];

        // get science data
        ScienceData data = page.pageData;

        // collect and deduce all info necessary
        MetaData meta = new MetaData(data, page.host);

        // record data in the drive
        Drive drive = DB.Vessel(meta.vessel).drive;
        if (!meta.is_sample)
        {
          drive.Record_File(data.subjectID, data.dataAmount);
        }
        else
        {
          drive.Record_Sample(data.subjectID, data.dataAmount);
        }

        // render experiment inoperable if necessary
        if (!meta.is_rerunnable)
        {
          meta.experiment.SetInoperable();
        }

        // dump the data
        page.OnDiscardData(data);

        // inform the user
        Message.Post
        (
          Lib.BuildString("<b>", Science.Experiment(data.subjectID).fullname, "</b> recorded"),
          !meta.is_rerunnable ? "The experiment is now inoperable, resetting will require a <b>Scientist</b>" : string.Empty
        );
      }

      // dismiss the dialog
      dialog.Dismiss();
    }

    ExperimentsResultDialog dialog;
  }
}
