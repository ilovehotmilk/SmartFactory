using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace iFlyDotNet
{
    public class IFlyTTS
    {
        [DllImport(IFlyCommon.DllName)]
        private static extern IntPtr QTTSSessionBegin(string param, ref int errorCode);

        [DllImport(IFlyCommon.DllName)]
        private static extern int QTTSTextPut(string sessionID, string textString, uint textLen, string param);

        [DllImport(IFlyCommon.DllName)]
        private static extern IntPtr QTTSAudioGet(string sessionID, ref uint audioLen, ref int synthStatus, ref int errorCode);

        [DllImport(IFlyCommon.DllName)]
        private static extern int QTTSSessionEnd(string sessionID, string hints);

        [DllImport(IFlyCommon.DllName)]
        private static extern int QTTSGetParam(string sessionID, string paramName, string paramValue, ref uint valueLen);

        private IFlyTTS()
        {

        }

        private string sessionId;

        private int errorCode = -1;

        private static IFlyTTS _iFlyTTS;

        public static IFlyTTS GetInstance()
        {
            return _iFlyTTS ?? (_iFlyTTS = new IFlyTTS());
        }

        public byte[] GetPcmDataByTTS(string text)
        {
            string param = "engine_type = local, voice_name = xiaoyan, text_encoding = GB2312, tts_res_path = fo|tts\\xiaoyan.jet;fo|tts\\common.jet, sample_rate = 16000, speed = 50, volume = 50, pitch = 50, rdn = 2";
            sessionId = Marshal.PtrToStringAnsi(QTTSSessionBegin(param, ref errorCode));
            if (sessionId == null)
                throw new Exception("QISRSessionBegin errorCode: " + errorCode);
            errorCode = QTTSTextPut(sessionId, text, (uint)Encoding.Default.GetByteCount(text), null);
            if (errorCode!=0)
                throw new Exception("QTTSTextPut errorCode: " + errorCode);
            List<byte> listData = new List<byte>();
            while (true)
            {
                uint audioLen = 0;
                int synthStatus=0;
                var dataPtr = QTTSAudioGet(sessionId, ref audioLen, ref synthStatus, ref errorCode);
                if (errorCode!=0)
                    break;
                if (dataPtr!=IntPtr.Zero)
                {
                    byte[] tmpData=new byte[audioLen];
                    Marshal.Copy(dataPtr, tmpData, 0, (int)audioLen);
                    listData.AddRange(tmpData);
                }
                if (synthStatus==2)
                    break;
            }
            if (errorCode != 0)
            {
                throw new Exception("QTTSAudioGet errorCode: " + errorCode);
            }
            errorCode = QTTSSessionEnd(sessionId, "Normal");
            if (errorCode!=0)
            {
                throw new Exception("QTTSSessionEnd errorCode: " + errorCode);
            }
            return listData.ToArray();
        }
    }
}
