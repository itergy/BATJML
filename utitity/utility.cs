using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace utitity
{
    public class xmlcommon
    {

        XmlDocument xmlDoc = new XmlDocument();

        public void LoadXMLDoc(string filePath)
        {
            xmlDoc.Load(filePath);
        }

        public string GetSingleXMLValue(string xmlPath)
        {
            XmlNode node = xmlDoc.SelectSingleNode(xmlPath);
            string value = node.InnerText;
            return value;

        }



    }
}
