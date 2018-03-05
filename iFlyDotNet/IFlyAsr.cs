using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace iFlyDotNet
{

    public class IFlyAsr
    {
        [DllImport(IFlyCommon.DllName)]
        private static extern int QISRAudioWrite(string sessionID, byte[] waveData, uint waveLen, int audioStatus, ref int epStatus, ref int recogStatus);

        [DllImport(IFlyCommon.DllName)]
        private static extern IntPtr QISRGetResult(string sessionID, ref int rsltStatus, int waitTime, ref int errorCode);

        [DllImport(IFlyCommon.DllName)]
        private static extern int QISRBuildGrammar(string grammarType, byte[] grammarContent, uint grammarLength, string param, BuildGrammarCallBack callback, IntPtr userData);

        [DllImport(IFlyCommon.DllName)]
        private static extern IntPtr QISRSessionBegin(string grammarList, string param, ref int errorCode);

        [DllImport(IFlyCommon.DllName)]
        private static extern int QISRSessionEnd(string sessionID, string hints);

        private delegate int BuildGrammarCallBack(int ecode, string info, IntPtr c);

        private struct UserData
        {
            public int build_fini;  //标识语法构建是否完成
            public int update_fini; //标识更新词典是否完成
            public int errcode; //记录语法构建或更新词典回调错误码
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string grammar_id; //保存语法构建返回的语法ID
        }

        private UserData globalUserData
        {
            get
            {
                return Marshal.PtrToStructure<UserData>(userDataPtr);
            }
            set
            {
                if (userDataPtr != null)
                    Marshal.StructureToPtr(value, userDataPtr, true);
            }
        }
        private readonly string asrResPath = "fo|asr/common.jet";
        private readonly string grmBuildPath = "asr/GrmBuild";
        private IntPtr userDataPtr;
        private string sessionId;
        int errorCode = -1;
        int epStatus = -1;
        int recStatus = -1;
        private IFlyAsr()
        {

        }

        private static IFlyAsr _iFlyAsr;

        public Action<string> GetResult;
        public static IFlyAsr GetInstance()
        {
            return _iFlyAsr ?? (_iFlyAsr = new IFlyAsr());
        }

        public void Init(string bnfPath)
        {
            userDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<UserData>());
         
            byte[] bnfStr;
            using (FileStream fs = new FileStream(bnfPath, FileMode.Open))
            {
                bnfStr = new byte[fs.Length];
                fs.Read(bnfStr, 0, (int)fs.Length);
            }
            string param = string.Format("engine_type = local,asr_res_path = {0}, sample_rate = 16000, grm_build_path = {1}, ", asrResPath, grmBuildPath);
            int BuildGrammarCallBack(int ecode, string info, IntPtr ptr)
            {
                var data = Marshal.PtrToStructure<UserData>(ptr);
                data.build_fini = 1;
                data.errcode = ecode;
                if (ecode == 0 && info != null)
                    data.grammar_id = info;
                globalUserData = data;
                return 0;
            }
            var ret = QISRBuildGrammar("bnf", bnfStr, (uint)bnfStr.Length, param, BuildGrammarCallBack, userDataPtr);
            if (ret != 0)
                throw new Exception("构建语法调用失败");
            while (globalUserData.build_fini != 1)
                Thread.Sleep(100);
            if (globalUserData.errcode != 0)
                throw new Exception("构建语法失败,错误码：" + globalUserData.errcode);
        }

        public string RunAsr(byte[] pcmData, int confidence)
        {
            string xml = null;
            string param = string.Format("engine_type = local, " +
                "asr_res_path = {0}, asr_threshold = {1}, " +
                "sample_rate = 16000, grm_build_path = {2}, " +
                "local_grammar = {3}, result_type = xml, " +
                "result_encoding = GB2312, ", asrResPath, confidence, grmBuildPath, globalUserData.grammar_id);
            int errorCode = -1;
            var sessionId = Marshal.PtrToStringAnsi(QISRSessionBegin(null, param, ref errorCode));
            if (sessionId == null)
                throw new Exception("QISRSessionBegin errorCode: " + errorCode);
            try
            {
                int epStatus = -1, recStatus = -1;
                errorCode = QISRAudioWrite(sessionId, pcmData, (uint)pcmData.Length, 1, ref epStatus, ref recStatus);
                if (errorCode != 0)
                    throw new Exception("QISRAudioWrite errorCode: " + errorCode);
                errorCode = QISRAudioWrite(sessionId, null, 0, 4, ref epStatus, ref recStatus);
                if (errorCode != 0)
                    throw new Exception("QISRAudioWrite errorCode: " + errorCode);
                int rssStatus = -1;
                while (5 != rssStatus && 0 == errorCode)
                {
                    xml = Marshal.PtrToStringAnsi(QISRGetResult(sessionId, ref rssStatus, 0, ref errorCode));
                }
                if (errorCode != 0)
                    throw new Exception("QISRGetResult errorCode: " + errorCode);
                QISRSessionEnd(sessionId, null);
            }
            catch (Exception ex)
            {
                QISRSessionEnd(sessionId, null);
                throw ex;
            }
            if (xml == null)
                return null;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var node = xmlDoc.SelectSingleNode("//command");
            return node.InnerText;
        }

        public void RunAsrRealTime(byte[] pcmData, int confidence)
        {
            string xml = null;
            if (sessionId == null)
            {
                string param = string.Format("engine_type = local, " +
                "asr_res_path = {0}, asr_threshold = {1}, " +
                "sample_rate = 16000, grm_build_path = {2}, " +
                "local_grammar = {3}, result_type = xml, " +
                "result_encoding = GB2312, vad_eos = 500", asrResPath, confidence, grmBuildPath, globalUserData.grammar_id);
                sessionId = Marshal.PtrToStringAnsi(QISRSessionBegin(null, param, ref errorCode));
                if (sessionId == null)
                    throw new Exception("QISRSessionBegin errorCode: " + errorCode);
                errorCode = QISRAudioWrite(sessionId, pcmData, (uint)pcmData.Length, 1, ref epStatus, ref recStatus); //first
                if (errorCode != 0)
                    throw new Exception("QISRAudioWrite errorCode: " + errorCode);
            }
            try
            {
                if (epStatus != 3)
                {
                    errorCode = QISRAudioWrite(sessionId, pcmData, (uint)pcmData.Length, 2, ref epStatus, ref recStatus); //continue
                    if (errorCode != 0)
                        throw new Exception("QISRAudioWrite errorCode: " + errorCode);
                    return;
                }
                errorCode = QISRAudioWrite(sessionId, null, 0, 4, ref epStatus, ref recStatus); //last
                if (errorCode != 0)
                    throw new Exception("QISRAudioWrite errorCode: " + errorCode);
                int rssStatus = -1;
                while (5 != rssStatus && 0 == errorCode)
                {
                    xml = Marshal.PtrToStringAnsi(QISRGetResult(sessionId, ref rssStatus, 0, ref errorCode));
                }
                if (errorCode != 0)
                    throw new Exception("QISRGetResult errorCode: " + errorCode);
                QISRSessionEnd(sessionId, null);
                sessionId = null;
            }
            catch (Exception ex)
            {
                QISRSessionEnd(sessionId, null);
                sessionId = null;
                GetResult?.Invoke(ex.Message);
            }
            if (xml == null)
                return;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var result = xmlDoc.SelectSingleNode("//command").InnerText;
            GetResult?.Invoke(result);
        }

        public void StopAsrRealTime()
        {
            if (sessionId != null)
            {
                QISRSessionEnd(sessionId, null);
                sessionId = null;
            }
        }
    }
}
