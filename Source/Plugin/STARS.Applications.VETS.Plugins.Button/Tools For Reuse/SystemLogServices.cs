using System;
using STARS.Applications.Interfaces.Dialogs;
using STARS.Applications.VETS.Interfaces.Constants;

namespace ToolsForReuse
{
    public static class SystemLogServices
    {
        public static void DisplayErrorInVETSLog(Exception e)
        {
            MEF.Logger.AddLogEntry(System.Diagnostics.TraceEventType.Error, SystemLogSources.DataAccess, e.Message + "\r\n" + e.StackTrace);
            throw e;
        }

        public static void DisplayErrorInVETSLog(string message, string title = null)
        {
            if (title != null) DisplayErrorInPopup(title, message);
            throw new Exception(message);
        }

        public static void DisplayErrorInPopup(string title, string message)
        {
            MEF.DialogService.PromptUser(title, message, DialogIcon.Alert, DialogButton.OK, DialogButton.OK);
        }

        public static void DisplayMessageInVETSLog(string message, string title = null)
        {
            if (title != null) DisplayMessageInPopup(title, message);
            MEF.Logger.AddLogEntry(System.Diagnostics.TraceEventType.Information, SystemLogSources.DataAccess, message);
        }

        public static void DisplayMessageInPopup(string title, string message)
        {
            MEF.DialogService.PromptUser(title, message, DialogIcon.Information, DialogButton.OK, DialogButton.OK);
        }

        public static void DisplayErrorInVETSLogNoReturn(string message, string title = null)
        {
            if (title != null) DisplayErrorInPopup(title, message);
            MEF.Logger.AddLogEntry(System.Diagnostics.TraceEventType.Error, SystemLogSources.DataAccess, message);
        }
    }

}
