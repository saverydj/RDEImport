using System;
using System.IO;
using System.Linq;
using ToolsForReuse;
using System.Threading;
using System.Windows.Forms;
using Stars.ApplicationManager;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using STARS.Applications.Interfaces.Dialogs;
using STARS.Applications.VETS.Interfaces.Entities;
using STARS.Applications.VETS.Plugins.RDEImportTool.Properties;
using STARS.Applications.VETS.Interfaces.EntityProperties.TestProcedure;

namespace STARS.Applications.VETS.Plugins.RDEImportTool
{
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
        private List<string> _filePaths = new List<string>();

        public ImportData()
        {
        }

        public void Import()
        {
            _filePaths.Clear();
            SelectUsage();
            CreateEntities();
        }

        /// <summary>
        /// Gets the library resource tree node
        /// </summary>
        /// <returns>The library resource folder</returns>
        private IAppFolder GetLibraryFolder(string path)
        {
            var libraryFolder = MEF.StarsApplication.GetResourceByFullName(path);

            return libraryFolder as IAppFolder;
        }

        #region RDE

        private void SelectUsage()
        {
            if (Config.AskToRunExe)
            {
                var result = MEF.DialogService.PromptUser(Resources.Title, Resources.Message, DialogIcon.Question, DialogButton.Yes, DialogButton.Yes, DialogButton.No, DialogButton.Cancel);
                if (result == DialogButton.Yes)
                {
                    InvokeRDETool();
                    if (Config.ShowCase) ParseRDELog();
                    else ChooseFilesForImport();
                }
                else if (result == DialogButton.No) ChooseFilesForImport();
                else return;
            }
            else InvokeRDETool();
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

            openFileDialog1.Filter = "Text Files (*.txt)|*.txt";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;
            if (Directory.Exists(Settings.Default["LastOpenedFilePath"].ToString())) openFileDialog1.InitialDirectory = Settings.Default["LastOpenedFilePath"].ToString();
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
                            Settings.Default["LastOpenedFilePath"] = Path.GetDirectoryName(filePath);
                            Settings.Default.Save();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new System.Exception("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private FileContents GetFileContents(string filePath)
        {
            string[] fileLines;

            string fileExtension = Path.GetExtension(filePath);
            if (fileExtension != ".txt")
            {
                throw new System.Exception("RDE resource file " + filePath + " must be a txt file.");
            }

            try
            {
                fileLines = File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                throw new System.Exception("Error: Could not read RDE resource file. Original error: " + ex.Message);
            }

            if (fileLines == null || fileLines.Length == 0)
            {
                throw new System.Exception("Error: RDE resource file is empty.");
            }

            bool isValid = false;
            for (int i = 0; i < fileLines.Length; i++)
            {
                if (fileLines[i].Contains("time") && fileLines[i].Contains("speed")) isValid = true;
            }

            if (!isValid)
            {
                throw new System.Exception("Error: RDE text file " + filePath + " does not contain time and speed information");
            }

            return new FileContents(fileLines);
        }

        #endregion

        #region VETS

        private void CreateEntities()
        {
            if (_filePaths != null && _filePaths.Count > 0)
            {
                List<string> traceNames = new List<string>();
                List<string> testProcedureNames = new List<string>();
                List<string> shiftTableNames = new List<string>();
                List<string> shiftListNames = new List<string>();

                foreach (string filePath in _filePaths)
                {
                    Trace newTrace = new Trace();
                    TestProcedure newTestProcedure = new TestProcedure();
                    ShiftTable newShiftTable = new ShiftTable(); ;
                    ShiftList newShiftList = new ShiftList();

                    string baseName = GetNameFromPath(filePath);
                    FileContents fileContents = GetFileContents(filePath);
                    List<TraceColumn> columnData = new List<TraceColumn>();

                    string predictedTraceName = ExtendedEntityManager.PredictEntityName<Trace>(baseName + " Trace", traceNames);
                    string predictedTestProcedureName = ExtendedEntityManager.PredictEntityName<TestProcedure>(baseName + " Test Procedure", testProcedureNames);
                    string predictedShiftTableName = ExtendedEntityManager.PredictEntityName<ShiftTable>(baseName + " Shift Table", shiftTableNames);
                    string predictedShiftListName = ExtendedEntityManager.PredictEntityName<ShiftList>(baseName + " Shift List", shiftListNames);

                    traceNames.Add(predictedTraceName);
                    testProcedureNames.Add(predictedTestProcedureName);
                    shiftTableNames.Add(predictedShiftTableName);
                    shiftListNames.Add(predictedShiftListName);

                    SetTraceData(ref newTrace, ref columnData, fileContents, predictedTraceName);
                    SetTestProcedureData(ref newTestProcedure, predictedTestProcedureName, predictedTraceName);
                    SetShiftTableData(ref newShiftTable, columnData, fileContents, predictedShiftTableName, predictedTraceName);
                    SetShiftListData(ref newShiftList, predictedShiftListName, predictedTestProcedureName, predictedShiftTableName);

                    MEF.EntityManagerView.Traces.Value.Create(ExtendedEntityManager.GetEntityURI<Trace>(), newTrace.Properties);
                    MEF.EntityManagerView.TestProcedures.Value.Create(ExtendedEntityManager.GetEntityURI<TestProcedure>(), newTestProcedure.Properties);
                    MEF.EntityManagerView.ShiftTables.Value.Create(ExtendedEntityManager.GetEntityURI<ShiftTable>(), newShiftTable.Properties);
                    MEF.EntityManagerView.ShiftLists.Value.Create(ExtendedEntityManager.GetEntityURI<ShiftList>(), newShiftList.Properties);

                    ShowDialog(predictedTraceName, predictedTestProcedureName, predictedShiftTableName, predictedShiftListName);

                    //if (filePath == _filePaths.Last())
                    //{
                    //    Thread newThread = new Thread(x => ShowImportedTrace(predictedTraceName)) { IsBackground = true };
                    //    newThread.Start();
                    //}
                }
            }
        }

        private void SetTraceData(ref Trace newTrace, ref List<TraceColumn> columnData, FileContents fileContents, string name)
        {
            for (int i = 0; i < fileContents.NamesSplit.Length; i++)
            {
                if (fileContents.NamesSplit[i].Contains("time")) columnData.Add(new TraceColumn("Time", "s", i, fileContents.DataLength, fileContents.UnitsSplit[i]));
                else if (fileContents.NamesSplit[i].Contains("speed")) columnData.Add(new TraceColumn("Speed", "km/h", i, fileContents.DataLength, fileContents.UnitsSplit[i]));
                else if (fileContents.NamesSplit[i].Contains("slope")) columnData.Add(new TraceColumn("Gradient", "%", i, fileContents.DataLength, fileContents.UnitsSplit[i]));
                else if (fileContents.NamesSplit[i].Contains("ambient-temperature")) columnData.Add(new TraceColumn("Temperature", "°C", i, fileContents.DataLength, fileContents.UnitsSplit[i]));
                else if (fileContents.NamesSplit[i].Contains("ambient-humidity")) columnData.Add(new TraceColumn("RelativeHumidity", "%", i, fileContents.DataLength, fileContents.UnitsSplit[i]));
            }

            for (int i = 0; i < columnData.Count; i++)
            {
                for (int j = 0; j < fileContents.DataLength; j++)
                {
                    columnData[i].Data[j] = columnData[i].Units.Convert(Convert.ToDouble(fileContents.Lines[j + fileContents.DataIndex].Split(fileContents.Delimiter)[columnData[i].Index]));
                }
            }

            List<VectorData> vectorData = new List<VectorData>();
            for (int i = 0; i < columnData.Count; i++)
            {
                vectorData.Add(new TypedVectorData<double>());
                vectorData[i].Data = columnData[i].Data.Cast<object>().ToArray();
                vectorData[i].Name = columnData[i].Name;
                vectorData[i].Unit = columnData[i].DisplayUnits;
            }
            newTrace.Vectors = vectorData.ToArray();
            newTrace.Name = name;

        }

        private void SetTestProcedureData(ref TestProcedure newTestProcedure, string name, string traceName)
        {
            TestProcedure templateTestProcedure = ExtendedEntityManager.DeserializeEntity<TestProcedure>(Properties.Resources.TestProcedure);
            newTestProcedure.Blocks = templateTestProcedure.Blocks;
            (newTestProcedure.Blocks[0] as DriveUnitBlock).TraceNames = new string[] { traceName };
            newTestProcedure.Name = name;
        }

        private void SetShiftTableData(ref ShiftTable newShiftTable, List<TraceColumn> columnData, FileContents fileContents, string name, string traceName)
        {
            ShiftTable templateShiftTableAutomatic = ExtendedEntityManager.DeserializeEntity<ShiftTable>(Properties.Resources.ShiftTableA);
            ShiftTable templateShiftTableManual = ExtendedEntityManager.DeserializeEntity<ShiftTable>(Properties.Resources.ShiftTableM);

            int gearIndex = -1;
            bool isAutomatic = true;

            for (int i = 0; i < fileContents.NamesSplit.Length; i++)
            {
                if (fileContents.NamesSplit[i].Contains("gear")) gearIndex = i;
            }

            if (gearIndex >= 0)
            {
                for (int i = fileContents.DataIndex; i < fileContents.DataLength + fileContents.DataIndex; i++)
                {
                    string thisGear = fileContents.Lines[i].Split(fileContents.Delimiter)[gearIndex];
                    if(CaseInsensitiveContains(thisGear, "MANUAL"))
                    {
                        isAutomatic = false;
                        break;
                    }
                    if (CaseInsensitiveContains(thisGear, "AUTOMATIC"))
                    {
                        isAutomatic = true;
                        break;
                    }
                }
            }

            double[] timeData = columnData.FirstOrDefault(x => x.Name == "Time").Data;
            if (isAutomatic)
            {
                double[] speedData = columnData.FirstOrDefault(x => x.Name == "Speed").Data;

                int start = -1;
                int end = speedData.Length - 1;
                for(int i = 0; i < speedData.Length; i++)
                {
                    if (speedData[i] != 0 && start < 0) start = i - 1;
                    if (speedData[i] != 0) end = i + 1;
                }
                if (start >= 5) start = start - 5;
                else if (start < 0) start = 0;
                if (end > speedData.Length - 1) end = speedData.Length - 1;

                newShiftTable.MaximumGear = templateShiftTableAutomatic.MaximumGear;
                newShiftTable.TransmissionType = templateShiftTableAutomatic.TransmissionType;
                newShiftTable.Vectors = templateShiftTableAutomatic.Vectors;
                newShiftTable.Vectors[0].Data = (new double[] { start, end }).Cast<object>().ToArray();
            }
            else
            {
                List<double> time = new List<double>();
                List<string> gear = new List<string>();
                List<bool> declutch = new List<bool>();
                List<string> message = new List<string>();
                for (int i = fileContents.DataIndex; i < fileContents.DataLength + fileContents.DataIndex; i++)
                {
                    string thisGear = fileContents.Lines[i].Split(fileContents.Delimiter)[gearIndex];
                    if (thisGear != String.Empty)
                    {
                        thisGear = thisGear.Split('-').Last();
                        if(TypeCast.IsInt(thisGear))
                        {
                            time.Add(timeData[i- fileContents.DataIndex]);
                            gear.Add(thisGear);
                            declutch.Add(false);
                            message.Add(String.Empty);                           
                        }
                    }
                }
                newShiftTable.MaximumGear = TypeCast.ToInt(gear.Max());
                newShiftTable.TransmissionType = templateShiftTableManual.TransmissionType;
                newShiftTable.Vectors = templateShiftTableManual.Vectors;
                newShiftTable.Vectors[0].Data = time.Cast<object>().ToArray();
                newShiftTable.Vectors[1].Data = gear.Cast<object>().ToArray();
                newShiftTable.Vectors[2].Data = declutch.Cast<object>().ToArray();
                newShiftTable.Vectors[3].Data = message.Cast<object>().ToArray();
            }

            newShiftTable.TraceName = traceName;
            newShiftTable.Name = name;
        }

        private void SetShiftListData(ref ShiftList newShiftList, string name, string testProcedureName, string shiftTableName)
        {
            newShiftList.TestProcedureName = testProcedureName;
            newShiftList.ShiftTableNames = new string[] { shiftTableName };
            newShiftList.Name = name;
        }

        private void ShowDialog(string traceName, string testProcedureName, string shiftTableName, string shiftListName)
        {
            SystemLogServices.DisplayMessageInVETSLog(String.Format("Importing Trace resource '{0}'.", traceName));
            SystemLogServices.DisplayMessageInVETSLog(String.Format("Importing Test Procedure resource '{0}'.", testProcedureName));
            SystemLogServices.DisplayMessageInVETSLog(String.Format("Importing Shift Table resource '{0}'.", shiftTableName));
            SystemLogServices.DisplayMessageInVETSLog(String.Format("Importing Shift List resource '{0}'.", shiftListName));
        }

        private void ShowImportedTrace(string predictedTraceName)
        {
            while (!MEF.EntityQuery.Where<Trace>().Any(x => x.Name == predictedTraceName));
            Thread.Sleep(100);
            SwapView.Show<Trace>(predictedTraceName);
        }

        #endregion

        #region Misc

        private string GetNameFromPath(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            foreach (char c in IllegalChars)
            {
                if (name.Contains(c.ToString()))
                {
                    name = name.Replace(c.ToString(), "");
                }
            }
            return name;
        }

        private bool CaseInsensitiveContains(string source, string toCheck)
        {
            return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #endregion

    }
}
