using UnityEngine;
using KSP.UI.Screens.Flight.Dialogs;

// TODO: Separate classes into files 
namespace KERBALISM 
{
  // Manipulate science dialog callbacks to remove the data from the experiment
  // (rendering it inoperable) and store it in the vessel drive. The same data
  // capture method as in MiniHijacker is used, but the science dialog is not hidden.
  // Any event closing the dialog (like going on eva, or recovering) will act as
  // if the 'keep' button was pressed for each page.
  public sealed class Hijacker : MonoBehaviour
  {
    void Start()
    {
      dialog = gameObject.GetComponentInParent<ExperimentsResultDialog>();
      if (dialog == null) { Destroy(gameObject); return; }
    }

    void Update()
    {
      var page = dialog.currentPage;
      page.OnKeepData = (ScienceData data) => Hijack(data, false);
      page.OnTransmitData = (ScienceData data) => Hijack(data, true);
      page.showTransmitWarning = false; //< mom's spaghetti
    }

    void Hijack(ScienceData data, bool send)
    {
      // shortcut
      ExperimentResultDialogPage page = dialog.currentPage;

      // collect and deduce all data necessary just once
      MetaData meta = new MetaData(data, page.host);

      // hijack the dialog
      if (!meta.is_rerunnable)
      {
        popup = Lib.Popup
        (
          "Warning!",
          "Recording the data will render this module inoperable.\n\nRestoring functionality will require a scientist.",
          new DialogGUIButton("Record data", () => Record(meta, data, send)),
          new DialogGUIButton("Discard data", () => Dismiss(data))
        );
      }
      else
      {
        Record(meta, data, send);
      }
    }

    void Record(MetaData meta, ScienceData data, bool send)
    {
      // if amount is zero, warn the user and do nothing else
      if (data.dataAmount <= double.Epsilon)
      {
        Message.Post("There is no more useful data here");
        return;
      }

      // if this is a sample and we are trying to send it, warn the user and do nothing else
      if (meta.is_sample && send)
      {
        Message.Post("We can't transmit a sample", "Need to be recovered, or analyzed in a lab");
        return;
      }

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

      // flag for sending if specified
      if (!meta.is_sample && send) drive.Send(data.subjectID, true);

      // render experiment inoperable if necessary
      if (!meta.is_rerunnable) meta.experiment.SetInoperable();

      // dismiss the dialog and popups
      Dismiss(data);

      // inform the user
      Message.Post
      (
        Lib.BuildString("<b>", Science.Experiment(data.subjectID).fullname, "</b> recorded"),
        !meta.is_rerunnable ? "The experiment is now inoperable, resetting will require a <b>Scientist</b>" : string.Empty
      );
    }

    void Dismiss(ScienceData data)
    {
      // shortcut
      ExperimentResultDialogPage page = dialog.currentPage;

      // dump the data
      page.OnDiscardData(data);

      // close the confirm popup, if it is open
      if (popup != null)
      {
        popup.Dismiss();
        popup = null;
      }
    }

    ExperimentsResultDialog dialog;
    PopupDialog popup;
  }
}