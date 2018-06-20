using System;
using System.Linq;
using System.Collections.Generic;
using STARS.Applications.VETS.Interfaces.Constants;
using STARS.Applications.VETS.Interfaces.ResourceData;

namespace ToolsForReuse
{
    public static class OnlineResources
    {
        private static IDeployPath DeployPath = new DeployPath();
        public static string RootPath { get { return DeployPath.GetRootPath(); } }
        public static Dictionary<string, string> Repository = new Dictionary<string, string>();

        public static void AddEntry(string key, string path)
        {
            if(Repository.ContainsKey(key))
            {
                Repository[key] = path;
            }
            else Repository.Add(key, path);
        }

        public static string GetValueAsString(string key)
        {
            object obj = MEF.ProvideValues.GetValue(RootPath + Repository[key]);
            if (obj == null) return String.Empty;
            return obj.ToString();
        }

        public static double GetValueAsDouble(string key)
        {
            object obj = MEF.ProvideValues.GetValue(RootPath + Repository[key]);
            if (obj == null) return 0.0;
            return TypeCast.ToDouble(obj.ToString());
        }

        public static int GetValueAsInt(string key)
        {
            object obj = MEF.ProvideValues.GetValue(RootPath + Repository[key]);
            if (obj == null) return 0;
            return TypeCast.ToInt(obj.ToString());
        }

        public static bool GetValueAsBool(string key)
        {
            object obj = MEF.ProvideValues.GetValue(RootPath + Repository[key]);
            if (obj == null) return false;
            return TypeCast.ToBool(obj.ToString());
        }

        public static object GetValue(string key)
        {
            return MEF.ProvideValues.GetValue(RootPath + Repository[key]);
        }

        public static void SetValue(string key, object val)
        {
            MEF.ProvideValues.SetValue(RootPath + Repository[key], val);
        }
    }

    public interface IDeployPath
    {
        /// <summary>
        /// Get the deployment path for a variable or method
        /// </summary>
        /// <param name="resourceReference">A reference to the variable or method</param>
        /// <returns>The deployment path</returns>
        string GetDeployPath(ResourceReference resourceReference);

        /// <summary>
        /// Get the deployment path for a variable or method within the Execution Environment
        /// </summary>
        /// <param name="resourceReference">A reference to the variable or method</param>
        /// <returns>The deployment path</returns>
        string GetEEPath(ResourceReference resourceReference);

        string GetRootPath();
    }

    internal class DeployPath : IDeployPath
    {
        private const string WorkstationResourcePath = @"\\Root_UserData_SR_WorkstationStatus";
        private const string RtNodeString = "RT";
        private string _rootDeployPath;

        #region Implementation of IDeployPath

        /// <summary>
        /// Get the deployment path for a variable
        /// </summary>
        /// <param name="resourceReference">A reference to the variable</param>
        /// <returns>The deployment path</returns>
        public string GetDeployPath(ResourceReference resourceReference)
        {
            var starsSeperator = new string(new[] { ResourceReference.PathSeparator });
            switch (resourceReference.RootType)
            {
                case ReferenceRoot.SR:
                    return RootDeployPath + ResourceReference.SrNodeString + starsSeperator +
                           string.Join(starsSeperator, resourceReference.NodeNames.ToArray());

                case ReferenceRoot.SN:
                    return RootDeployPath + ResourceReference.SnNodeString + starsSeperator +
                           string.Join(starsSeperator, resourceReference.NodeNames.ToArray());

                case ReferenceRoot.RT:
                    return RootDeployPath + RtNodeString + starsSeperator +
                           string.Join(starsSeperator, resourceReference.NodeNames.ToArray());

                //case ReferenceRoot.ActiveTestStand:
                //    string testStandDeployPath = GetTestStandDeployPath();
                //    return testStandDeployPath == null
                //               ? null
                //               : testStandDeployPath + starsSeperator + string.Join(starsSeperator, resourceReference.NodeNames.ToArray());

                case ReferenceRoot.VETS:
                    return ValueProviders.BagData + starsSeperator + string.Join(starsSeperator, resourceReference.NodeNames.ToArray());

                case ReferenceRoot.ExecutionRoot:
                    return RootDeployPath + string.Join(starsSeperator, resourceReference.NodeNames.ToArray());

                default:
                    return string.Join(starsSeperator, resourceReference.NodeNames.ToArray());
            }
        }

        /// <summary>
        /// Get the deployment path for a variable or method within the Execution Environment
        /// </summary>
        /// <param name="resourceReference">A reference to the variable or method</param>
        /// <returns>The deployment path</returns>
        public string GetEEPath(ResourceReference resourceReference)
        {
            var deployPath = GetDeployPath(resourceReference);
            if (!String.IsNullOrEmpty(RootDeployPath) && deployPath.StartsWith(RootDeployPath))
                return ResourceReference.PathRoot + deployPath.Substring(RootDeployPath.Length);

            return resourceReference.ToString();
        }

        #endregion

        #region Implemementation

        //Root EE path
        public string RootDeployPath
        {
            get
            {
                if (_rootDeployPath == null)
                {
                    // Bit of a hack this but allows us to form SR deploy paths before items are loaded so was don't have to wait for the test load.
                    string[] deployPaths = MEF.LiveResources.GetDeployPath(WorkstationResourcePath, new[] { Environment.MachineName });
                    if (deployPaths == null)
                    {
                        throw new ApplicationException("Could not find EE root path.");
                    }

                    _rootDeployPath = deployPaths[0].Replace("SR_WorkstationStatus", null);

                    if(_rootDeployPath.EndsWith("-"))
                    {
                        _rootDeployPath = _rootDeployPath.Substring(0, _rootDeployPath.Length - 1);
                    }

                }
                return _rootDeployPath;
            }
        }

        public string GetRootPath()
        {
            return RootDeployPath;
        }
        #endregion
    }
}
