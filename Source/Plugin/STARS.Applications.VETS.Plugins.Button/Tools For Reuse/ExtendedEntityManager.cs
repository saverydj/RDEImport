using STARS.Applications.Interfaces.Entities;
using STARS.Applications.Interfaces.EntityProperties.CustomFields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;

namespace ToolsForReuse
{
    public static class ExtendedEntityManager
    {
 
        /// <summary>Creates an entity with exisitng custom field values</summary>
        public static void DeepCreate<T>(T entity) where T : Entity, new()
        {
            /*
             * EntityCreate.Create method sets all custom field values of the entity to String.Empty
             * EntityQuery does not update until the main calling thread returns
             * To get around these issues we: 
             * 1. Get a deep clone of the entity
             * 2. Count how many entities of this type already exist
             * 3. Create the entity
             * 4. Spawn new thread, wait until EnityQuery updates (number of entities changes)
             * 5. Set the custom field values of the newly created entity using the deep clone as reference
            */
           
            var entityClone = ObjectExtensions.Copy(entity); 
            CustomFieldValue[] customFields = entityClone.GetType().GetProperty("CustomFieldValues").GetValue(entityClone, null) as CustomFieldValue[];
            int entityCount = MEF.EntityQuery.Where<T>().ToList().Count();
            MEF.EntityCreate.Create(GetEntityURI<T>(), entity.Properties, new TimeSpan());
            Thread deepSetCFThread = new Thread(x => DeepSetCustomFields<T>(customFields, entityCount, 10)) { IsBackground = true };
            deepSetCFThread.Start();
        }

        /// <summary>Wait for EntityQuery to update then set the custom field values of the most recently created entity</summary>
        /// <param name="entityCount">Number of existing entities prior to entity creation</param>  
        /// <param name="timeOut">Time out in seconds</param>  
        public static void DeepSetCustomFields<T>(CustomFieldValue[] customFields, int entityCount, int timeOut) where T : Entity, new()
        {
            for (int i = 0; i < timeOut * 10; i++)
            {
                if (MEF.EntityQuery.Where<T>().ToList().Count() != entityCount)
                {
                    SetCustomFields<T>(customFields);
                    return;
                }
                Thread.Sleep(100);
            }
        }

        /// <summary>Set the custom field values of most recently created entity</summary>
        public static void SetCustomFields<T>(CustomFieldValue[] customFieldsValues) where T : Entity, new()
        {
            T newestEntity = MEF.EntityQuery.Where<T>().ToList().OrderByDescending(x => x.Created).FirstOrDefault();
            newestEntity.GetType().GetProperty("CustomFieldValues").SetValue(newestEntity, customFieldsValues, null);
            MEF.EntityManagerView.Lookup(GetEntityTypeID<T>()).Value.Update(newestEntity.ID, newestEntity.Properties);
        }

        ///<summary> Gets the entity type name. Looks like: 'Test'</summary>
        public static string GetEntityTypeName<T>()
        {
            string entityTypeName = typeof(T).ToString().Split('.').Last();
            return entityTypeName;
        }

        ///<summary> Gets the entity type id. Looks like: 'VETSTEST'</summary>
        public static string GetEntityTypeID<T>()
        {
            string entityTypeName = typeof(T).ToString().Split('.').Last();
            string entityTypeID = typeof(EntityTypeIDs).GetField(entityTypeName).GetValue(typeof(EntityTypeIDs)).ToString();
            return entityTypeID;
        }

        ///<summary> Gets the entity type uri. Looks like: 'EntityUris.VETSTEST.Test'</summary>
        public static string GetEntityURI<T>()
        {
            string entityTypeName = typeof(T).ToString().Split('.').Last();
            string entityTypeID = typeof(EntityTypeIDs).GetField(entityTypeName).GetValue(typeof(EntityTypeIDs)).ToString();
            string entityTypeURI = "EntityUris." + entityTypeID + "." + entityTypeName;
            return entityTypeURI;
        }

        /// <summary>
        /// Predicts the name which VETS will auto assign to the entity (to avoid duplicate names) upon creation
        /// </summary>
        /// <typeparam name="T">Type of entity, i.e. 'Test'</typeparam>
        /// <param name="baseName">Base name of entity before creation</param>
        /// <returns></returns>
        public static string PredictEntityName<T>(string baseName, List<string> additionalNames = null) where T : Entity, new()
        {
            List<string> existingNames = MEF.EntityQuery.Where<T>().Select(x => x.Name).ToList();
            if (additionalNames != null) existingNames.Concat(additionalNames);

            string predictiveName = baseName;
            int extensionNumber = 0;
            bool uniqueName = false;
            while (!uniqueName)
            {
                uniqueName = true;
                foreach (string name in existingNames)
                {
                    if (name == predictiveName)
                    {
                        uniqueName = false;
                        extensionNumber++;
                        predictiveName = baseName + " Copy";
                        if (extensionNumber > 1) predictiveName += " " + extensionNumber;
                    }
                }
            }

            return predictiveName;
        }

        ///<summary>Serialze the specified entity, save as file on path</summary>
        public static void SerialzeEntity<T>(T toSerialize, string path)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(fs);
            DataContractSerializer ser = new DataContractSerializer(typeof(T));
            ser.WriteObject(writer, toSerialize);
            writer.Close();
            fs.Close();
        }

        ///<summary>Deserialze the entity on the specified path</summary>
        public static T DeserializeEntity<T>(string path)
        {
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
            DataContractSerializer ser = new DataContractSerializer(typeof(T));
            T deserializedObject = (T)ser.ReadObject(reader);
            reader.Close();
            fs.Close();
            return deserializedObject;
        }

        ///<summary>Deserialze the entity having specified byte array</summary>
        public static T DeserializeEntity<T>(byte[] resourceData)
        {
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(resourceData, new XmlDictionaryReaderQuotas());
            DataContractSerializer ser = new DataContractSerializer(typeof(T));
            T deserializedObject = (T)ser.ReadObject(reader);
            reader.Close();
            return deserializedObject;
        }

    }
}
