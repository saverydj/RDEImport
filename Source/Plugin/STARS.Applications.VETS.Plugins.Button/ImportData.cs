using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using STARS.Applications.Interfaces.Dialogs;
using System.ComponentModel.Composition;
using STARS.Applications.VETS.Plugins.RDEImportTool.Properties;
using STARS.Applications.Interfaces.EntityManager;
using STARS.Applications.VETS.Interfaces;
using STARS.Applications.VETS.UI.Controls;
using STARS.Applications.VETS.Interfaces.Entities;
using System.IO;
using STARS.Applications.VETS.Interfaces.EntityProperties.TestProcedure;
using System.Runtime.InteropServices;
using System.Threading;


namespace STARS.Applications.VETS.Plugins.RDEImportTool
{
    [Export(typeof(ImportData))]
    class ImportData
    {

        #region DLL Import

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_SHOWWINDOW = 0x0040;

        #endregion

        const string IllegalChars = "~!@#$%^&*()-+=|\\{}[]\"'<>?/:,._";
        private readonly IDialogService _dialogService;
        private readonly IEntityCreate _entityCreate;
        private List<string> _filePaths = new List<string>();

        [ImportingConstructor]
        public ImportData(IDialogService dialogService, IEntityCreate entityCreate)
        {
            _dialogService = dialogService;
            _entityCreate = entityCreate;
        }

        public void Import()
        {
            _filePaths.Clear();

            if (Config.AskToRunExe)
            {
                var result = _dialogService.PromptUser(Resources.Title, Resources.Message, DialogIcon.Question, DialogButton.Yes, DialogButton.Yes, DialogButton.No, DialogButton.Cancel);
                if (result == DialogButton.Yes)
                {
                    InvokeRDETool();
                    if(Config.ShowCase)ParseRDELog();
                    else ChooseFilesForImport();
                }
                else if (result == DialogButton.No)
                {
                    ChooseFilesForImport();
                }
                else
                {
                    return;
                }
            }
            else
            {
                InvokeRDETool();
            }

            if (_filePaths != null && _filePaths.Count > 0)
            {
                Trace newTrace;
                foreach (string filePath in _filePaths)
                {
                    newTrace = new Trace();
                    SetTraceProperties(newTrace, filePath);
                    _entityCreate.Create("EntityUris.VETSTRACE.Trace", newTrace.Properties, new TimeSpan());
                }
                _dialogService.PromptUser(Resources.Title, "RDE trace data was sucessfully imported.", DialogIcon.Information, DialogButton.OK, DialogButton.OK);
            }

        }

        private void InvokeRDETool()
        {
            try
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.Start(Config.RDEToolPath);
                bool isRunning = false;
                while (!isRunning)
                {
                    System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
                    List<string> listProc = new List<string>();
                    foreach (System.Diagnostics.Process p in processes)
                    {
                        if (p.MainWindowHandle.ToInt32() > 0)
                        {
                            if (String.Compare(p.ProcessName, "OgpCore") == 0) isRunning = true;
                        }
                    }
                }
                Thread.Sleep(1000);
                IntPtr windowHandle = process.MainWindowHandle;
                SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                process.WaitForExit();
            }
            catch
            {
                throw new System.Exception("Error: RDE Importer tool " + Config.RDEToolPath + " was not found or could not be started.");
            }
        }

        private void ParseRDELog()
        {
            List<string> pathsInLog = new List<string>();
            if (File.Exists(Config.LogFilePath))
            {
                try
                {
                    string[] logLines = File.ReadAllLines(Config.LogFilePath);
                    foreach (string logLine in logLines)
                    {
                        if (logLine.Contains("Successfully exported data to "))
                        {
                            pathsInLog.Add(logLine);
                        }
                    }
                    pathsInLog.Sort();
                    pathsInLog.Reverse();
                }
                catch (Exception ex)
                {
                    throw new System.Exception("Error: RDE log file could not be parsed. Original error: " + ex.Message);
                }
            }
            else
            {
                throw new System.Exception("Error: No RDE log file found in " + Config.LogFilePath);
            }

            if (pathsInLog.Count == 0 || Convert.ToDateTime(pathsInLog[0].Split(',')[0].Split('[')[1].Replace('-', '/')).AddMinutes(1) < DateTime.Now)
            {
                ChooseFilesForImport();
                return;
            }
            string filepath = pathsInLog[0].Split(new string[] { "Successfully exported data to " }, StringSplitOptions.None)[1];
            filepath = filepath.Substring(0, filepath.Length - 1);
            _filePaths.Add(filepath);
        }

        private void ChooseFilesForImport()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Text Files (*.txt, *.csv)|*.txt;*.csv";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            if (Directory.Exists(Config.LastOpenedFilePath)) openFileDialog1.InitialDirectory = Config.LastOpenedFilePath;
            else openFileDialog1.InitialDirectory = @"C:\";

            if (openFileDialog1.ShowDialog(new Form { TopMost = true }) == DialogResult.OK)
            {
                try
                {
                    while (openFileDialog1.FileNames.Length <= 0) ;
                    foreach (string filePath in openFileDialog1.FileNames)
                    {
                        _filePaths.Add(filePath);
                        if(filePath == openFileDialog1.FileNames.Last())
                        {
                            Config.LastOpenedFilePath = Path.GetDirectoryName(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new System.Exception("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void SetTraceProperties(Trace newTrace, string filePath)
        {
            SetTraceName(newTrace, filePath);
            SetTraceVectorData(newTrace, filePath);
        }

        private void SetTraceName(Trace newTrace, string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            foreach (char c in IllegalChars)
            {
                if (fileName.Contains(c.ToString()))
                {
                    fileName = fileName.Replace(c.ToString(), "");
                }
            }
            newTrace.Name = fileName;
        }

        private void SetTraceVectorData(Trace newTrace, string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            if (String.Compare(fileExtension, ".txt") != 0 && String.Compare(fileExtension, ".csv") != 0)
            {
                throw new System.Exception("RDE resource file " + filePath + " must be either a txt or csv file.");
            }

            string[] fileContents;
            try
            {
                fileContents = File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                throw new System.Exception("Error: Could not read RDE resource file. Original error: " + ex.Message);
            }

            if (fileContents == null || fileContents.Length == 0)
            {
                throw new System.Exception("Error: RDE resource file is empty.");
            }

            if (fileExtension == ".txt")
            {               
                SetTraceVectorDataFromTxt(newTrace, filePath);
            }
            else if (fileExtension == ".csv")
            {
                SetTraceVectorDataFromCSV(newTrace, filePath);
            }
        }

        private void SetTraceVectorDataFromTxt(Trace newTrace, string filePath)
        {
            string[] fileContents = File.ReadAllLines(filePath);
            int startOfData = fileContents.Length;
            int timeIndex = -1;
            int speedIndex = -1;
            double speedMod;

            for (int i = 0; i < fileContents.Length; i++)
            {
                if (fileContents[i].Contains("time") && fileContents[i].Contains("speed"))
                {
                    for (int j = 0; j < fileContents[i].Split(',').Length; j++)
                    {
                        if (fileContents[i].Split(',')[j].Contains("time")) timeIndex = j;
                        else if (fileContents[i].Split(',')[j].Contains("speed")) speedIndex = j;
                    }
                    startOfData = i + 2;
                    break;
                }
            }

            if (timeIndex < 0 || speedIndex < 0)
            {
                throw new System.Exception("Error: RDE text file " + filePath + " does not contain time and speed information");
            }

            if (fileContents[startOfData - 1].Split(',')[speedIndex].Contains("mi/h")) speedMod = 2.23694;
            else if (fileContents[startOfData - 1].Split(',')[speedIndex].Contains("m/s")) speedMod = 1;
            else speedMod = 3.6;

            double[] time = new double[fileContents.Length - startOfData];
            double[] speed = new double[fileContents.Length - startOfData];

            for (int i = 0; i < fileContents.Length - startOfData; i++)
            {
                time[i] = Convert.ToDouble(fileContents[i + startOfData].Split(',')[timeIndex]);
                speed[i] = Convert.ToDouble(fileContents[i + startOfData].Split(',')[speedIndex]) / speedMod;
            }

            newTrace.Vectors[0].Data = time.Cast<object>().ToArray(); 
            newTrace.Vectors[1].Data = speed.Cast<object>().ToArray();

            string slopeFilePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "_Slope";
            if (!File.Exists(slopeFilePath)) return;

            string[] slopeFileContents;
            try
            {
                slopeFileContents = File.ReadAllLines(slopeFilePath);
            }
            catch (Exception ex)
            {
                throw new System.Exception("Error: Could not read RDE slope file. Original error: " + ex.Message);
            }

            if (slopeFileContents != null && slopeFileContents.Length - 1 == fileContents.Length - startOfData)
            {
                double[] gradient = new double[slopeFileContents.Length - 1];
                for (int i = 0; i < slopeFileContents.Length - 1; i++)
                {
                    gradient[i] = Convert.ToDouble(slopeFileContents[i + 1].Split('\t')[1]) / 100;
                }

                var traceVectors = newTrace.Vectors.ToList();
                var gradientVector = new TypedVectorData<double>();
                gradientVector.Data = gradient.Cast<object>().ToArray();
                gradientVector.Name = "Gradient";
                gradientVector.Unit = "%";
                traceVectors.Add(gradientVector);
                newTrace.Vectors = traceVectors.ToArray();
            }

        }

        private void SetTraceVectorDataFromCSV(Trace newTrace, string filePath)
        {
            string[] fileContents = File.ReadAllLines(filePath);
            int startOfData = fileContents.Length;
            int timeIndex = -1;
            int speedIndex = -1;
            double speedMod;

            for (int i = 0; i < fileContents.Length; i++)
            {
                if (fileContents[i].Contains("Relative_time") && fileContents[i].Contains("OBD_VehicleSpeed"))
                {
                    for (int j = 0; j < fileContents[i].Split(',').Length; j++)
                    {
                        if (String.Compare(fileContents[i].Split(',')[j], "Relative_time") == 0) timeIndex = j;
                        else if (String.Compare(fileContents[i].Split(',')[j], "OBD_VehicleSpeed") == 0) speedIndex = j;
                    }
                    startOfData = i + 1;
                    break;
                }
            }

            if (timeIndex < 0 || speedIndex < 0)
            {
                throw new System.Exception("Error: RDE csv file " + filePath + " does not contain time and speed information");
            }

            if (fileContents[startOfData - 2].Split(',')[speedIndex].Contains("mi/h")) speedMod = 2.23694;
            else if (fileContents[startOfData - 2].Split(',')[speedIndex].Contains("m/s")) speedMod = 1;
            else speedMod = 3.6;

            double[] time = new double[fileContents.Length - startOfData];
            double[] speed = new double[fileContents.Length - startOfData];

            for (int i = 0; i < fileContents.Length - startOfData; i++)
            {
                time[i] = Convert.ToDouble(fileContents[i + startOfData].Split(',')[timeIndex]);
                speed[i] = Convert.ToDouble(fileContents[i + startOfData].Split(',')[speedIndex]) / speedMod;
            }

            newTrace.Vectors[0].Data = time.Cast<object>().ToArray();
            newTrace.Vectors[1].Data = speed.Cast<object>().ToArray();
        }

    }
}
