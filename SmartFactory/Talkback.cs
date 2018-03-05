using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartFactory
{
    public class Talkback
    {
        public static string GetResult(string asrStr)
        {
            try
            {
                using (StreamReader sr = new StreamReader("tengen.tbk"))
                {
                    string line=null;
                    while((line = sr.ReadLine()) != null)
                    {
                        var ret = line.Split('|');
                        if (ret.Length != 2)
                            continue;
                        if (ret[0] == asrStr)
                            return ret[1];
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
