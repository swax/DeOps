using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace DeOps
{
    [Serializable]
    public class AppSettings
    {
        public bool NeedUpgrade;
        public string LastOpened;
        public string LastSimPath;
        public bool MonoHelp;

        [NonSerialized]
        public string SettingsPath;


        public static AppSettings Load(string path)
        {
            AppSettings result = null;
            try
            {
                var serializer = new XmlSerializer(typeof(AppSettings));

                using (var reader = new StreamReader(path))
                    result = (AppSettings)serializer.Deserialize(reader);
            }
            catch
            {
                result = new AppSettings();
            }

            result.SettingsPath = path;
            return result;
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(AppSettings));

            using (var writer = new StreamWriter(SettingsPath))
                serializer.Serialize(writer, this);
        }
    }
}
