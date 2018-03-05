using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PyFaceRecognition
{
    [Serializable]
    public class FaceEncodings
    {
        [XmlArray]
        public List<FaceEncoding> Items { get; } = new List<FaceEncoding>();

        public float[] this[string name]
        {
            get
            {
                if (Items.Exists((face) => face.Name == name))
                    return Items.Find((face) => face.Name == name).Encoding;
                else
                    return null;
            }
            set
            {
                if (Items.Exists((face) => face.Name == name))
                    Items.Find((face) => face.Name == name).Encoding = value;
            }
        }

        public int Count(string name)
        {
            return Items.FindAll((face) => face.Name == name).Count;
        }
    }

    [Serializable]
    public class FaceEncoding
    {
        public FaceEncoding() { }
        public FaceEncoding(string name,float[] encoding)
        {
            Name = name;
            Encoding = encoding;
        }
        
        [XmlElement]
        public string Name { get; set; }
        [XmlArray]
        public float[] Encoding { get; set; }
    }
}
