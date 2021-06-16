﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace JMeterTestsScript
{
    public class JmeterScript
    {
        private string jmeterScriptFileName;
        const string REDIS_SCRIPT_FILE_NAME = "csv2redis.redis";
        XElement jmeterScriptXml;
        
        public JmeterScript()
        {

        }

        public JmeterScript(string jmeterScriptFileName)
        {
            this.jmeterScriptFileName = jmeterScriptFileName;
            jmeterScriptXml=XElement.Load(jmeterScriptFileName);
        }

        public void WriteNewFile(string jmeterDestinationFileName)
        {
            jmeterScriptXml.Save(jmeterDestinationFileName);
        }

        public bool HasEnabledCsvControl()
        {
            var foo = jmeterScriptXml.Descendants("CSVDataSet")
                .Where(item => (bool)item.Attribute("enabled"));
            return foo.Count() >0;
        }

        public void AddRedisControl()
        {
            var enabledCsvConfigElements = jmeterScriptXml.Descendants("CSVDataSet")
                .Where(item => (bool)item.Attribute("enabled"));
            foreach (var csvElement in enabledCsvConfigElements)
            {
                string stringPropTag = "stringProp";
                string csvElementName = csvElement.Attribute("testname").Value;
                string columnNamesFromJmeter = csvElement.Elements(stringPropTag).FirstOrDefault(a => a.Attribute("name").Value == "variableNames").Value;
                var fileNameElement = csvElement.Elements(stringPropTag).Where(a => a.Attribute("name").Value == "filename").FirstOrDefault<XElement>();
                if (String.IsNullOrEmpty(fileNameElement.Value))
                    throw new FileNotFoundException($"There is no csv file name in the {csvElementName} csv configuration.");
                string csvFileName = fileNameElement.Value;
                Csv2Redis.ConvertToRedis(csvFileName, REDIS_SCRIPT_FILE_NAME);
                string redisKey= Path.GetFileNameWithoutExtension(csvFileName);
                string columnNamesFromCsvFile = Csv2Redis.GetCsvColumnNames(csvFileName);
                XElement redisConfigElement = XElement.Parse(JMeterTestsScript.Properties.Resources.redisConfigString);
                csvElement.Attribute("enabled").SetValue(false);
                redisConfigElement.Attribute("testname").SetValue(csvElementName);
                redisConfigElement.Elements(stringPropTag).FirstOrDefault(a => a.Attribute("name").Value == "variableNames").Value = String.IsNullOrEmpty(columnNamesFromJmeter) ? columnNamesFromCsvFile : columnNamesFromJmeter;
                redisConfigElement.Elements(stringPropTag).FirstOrDefault(a => a.Attribute("name").Value == "host").Value = "jmeter-redis-master";
                redisConfigElement.Elements(stringPropTag).FirstOrDefault(a => a.Attribute("name").Value == "redisKey").Value = redisKey;
                csvElement.AddAfterSelf(redisConfigElement);
                csvElement.AddAfterSelf(new XElement("hashTree"));
            }
            //jmeterScriptXml.Element("hashTree").Element("hashTree").Add(xElement);
        }

        public void CustomizeColumnNames()
        {
            throw new NotImplementedException();
        }
    }
}
