using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace iFlyDotNet
{
    public class IFlyCommon
    {
        public const string DllName = "msc_x64.dll";

        [DllImport(DllName)]
        private static extern int MSPLogin(string user, string pwd, string param);

        [DllImport(DllName)]
        private static extern int MSPLogout();

        public static void Login(string logConfig)
        {
            if (MSPLogin(null, null, logConfig) != 0)
                throw new Exception(logConfig+" 登陆失败");
        }

        public static void Logout()
        {
            if (MSPLogout() != 0)
                throw new Exception("注销失败");
        }
    }
}
