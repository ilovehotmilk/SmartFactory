using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PyFaceRecognition
{
    public class FaceRecoginition
    {
        private dynamic face_recognition = null;
        private dynamic np = null;
        private dynamic io = null;
        private dynamic pil = null;
        private dynamic builtins = null;
        private FaceEncodings recFaceEncodings;
        private CancellationTokenSource faceRecTokenSource;
        private Task faceRecTask;
        private static FaceRecoginition instance;
        private Func<Bitmap> getBitmap;
        private volatile float threshold = 0.3f;
        private volatile int editEncodings=0;
        private volatile string faceName="";
        private static object locker = new object();
        private FaceRecoginition(Func<Bitmap> func)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string envPythonHome = exeDir + @"Python35";
            Environment.SetEnvironmentVariable("PYTHONHOME", envPythonHome, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONPATH", envPythonHome + "\\Lib;"
                + envPythonHome + "\\Lib\\site-packages;"
                + envPythonHome + "\\DLLs", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PATH", envPythonHome, EnvironmentVariableTarget.Process);
            getBitmap = func;
           
        }

        public float Threshold { get=>threshold; set=> threshold = value; }

        public List<string> FaceList
        {
            get
            {
                if (recFaceEncodings == null)
                    return null;
                return recFaceEncodings.Items.Select((face) => face.Name).Distinct().ToList();
            }
        }

        public Action<string> ResultEvent;

        public Action<string> ExceptionEvent;

        public bool IsRunning
        {
            get
            {
                return faceRecTask == null ? false: faceRecTask.Status == TaskStatus.Running;
            }
        }

        public static FaceRecoginition GetInstance(Func<Bitmap> func)
        {
            return instance ?? (instance = new FaceRecoginition(func));
        }

        public void Start()
        {
            if (!IsRunning)
            {
                faceRecTask = new Task(() => RecThread(threshold), (faceRecTokenSource = new CancellationTokenSource()).Token);
                faceRecTask.Start();
            }
        }

        private void RecThread(float threshold)
        {
            try
            {

                if (!PythonEngine.IsInitialized)
                    PythonEngine.Initialize();
                var ts = PythonEngine.BeginAllowThreads();
                using (Py.GIL())
                {
                    builtins = PythonEngine.ImportModule("builtins");
                    np = PythonEngine.ImportModule("numpy");
                    face_recognition = PythonEngine.ImportModule("face_recognition");
                    io = PythonEngine.ImportModule("io");
                    pil = PythonEngine.ImportModule("PIL");
                    string result = "";
                    while (!faceRecTokenSource.IsCancellationRequested)
                    {
                        var unknown_face_encodings = GetUnknownFaceEncodings();
                        if (unknown_face_encodings == null) //未检测到人脸
                        {
                            result = "未检测到人脸";
                            editEncodings = 0;
                            goto OnEvent;
                        }
                        var unknown_face_encoding = unknown_face_encodings[0];
                        if (editEncodings > 0) //录入或删除
                        {
                            var unknownFaceEncoding = (unknown_face_encoding as PyObject).As<float[]>();
                            using (FileStream fs = new FileStream("ValidFaceEncodings.xml", FileMode.OpenOrCreate))
                            {
                                XmlSerializer xmlSerializer = new XmlSerializer(typeof(FaceEncodings));
                                try
                                {
                                    recFaceEncodings = (FaceEncodings)xmlSerializer.Deserialize(fs);
                                    if (recFaceEncodings == null)
                                        throw new Exception();
                                    switch (editEncodings)
                                    {
                                        case 1: //录入
                                            if (recFaceEncodings.Count(faceName) >= 5)
                                                recFaceEncodings[faceName] = unknownFaceEncoding;
                                            else
                                                recFaceEncodings.Items.Add(new FaceEncoding(faceName, unknownFaceEncoding));
                                            break;
                                        case 2: //删除
                                            if (recFaceEncodings[faceName] != null)
                                                recFaceEncodings.Items.RemoveAll((face) => face.Name == faceName);
                                            break;
                                        default: break;
                                    }
                                }
                                catch
                                {
                                    if (editEncodings == 2) //删除
                                        goto exit;
                                    recFaceEncodings = new FaceEncodings();
                                    recFaceEncodings.Items.Add(new FaceEncoding(faceName, unknownFaceEncoding));
                                }
                                fs.SetLength(0); //clear
                                xmlSerializer.Serialize(fs, recFaceEncodings);
                            }
                            exit:
                            editEncodings = 0;
                            faceName = "";
                        }
                        else //识别
                        {
                            using (FileStream fs = new FileStream("ValidFaceEncodings.xml", FileMode.OpenOrCreate))
                            {
                                XmlSerializer xmlSerializer = new XmlSerializer(typeof(FaceEncodings));
                                try
                                {
                                    recFaceEncodings = (FaceEncodings)xmlSerializer.Deserialize(fs);
                                    if (recFaceEncodings == null)
                                        throw new Exception();
                                }
                                catch
                                {
                                    result = "请先录入人脸";
                                    goto OnEvent;
                                }
                            }
                        }
                        var known_face_encodings = GetKnownFaceEncodings();
                        var face_distances = face_recognition.face_distance(known_face_encodings, unknown_face_encoding);
                        var faceDistances = ((PyObject)face_distances).As<float[]>().ToList();
                        var validDistances = faceDistances.Where((dis) => dis <= threshold);
                        if (!validDistances.Any())
                            result = "未知";
                        else
                            result = recFaceEncodings.Items[faceDistances.IndexOf(validDistances.Min())].Name;
                        ((PyObject)face_distances).Dispose();
                        ((PyObject)unknown_face_encodings).Dispose();
                        ((PyObject)known_face_encodings).Dispose();

                        OnEvent:
                        if (!faceRecTokenSource.IsCancellationRequested)
                            ResultEvent?.Invoke(result);
                    }

                    dynamic GetKnownFaceEncodings()
                    {
                        var _known_face_encodings = new PyList();
                        var knownFaces = new List<float[]>();
                        recFaceEncodings.Items.ForEach((face) =>
                        {
                            knownFaces.Add(face.Encoding);
                        });

                        knownFaces.ForEach((face) =>
                        {
                            var known_face = new PyList();
                            var listface = new List<float>(face);
                            listface.ForEach((faceitem) =>
                            {
                                known_face.Append(new PyFloat(faceitem));
                            });
                            _known_face_encodings.Append(np.array(known_face));
                            known_face.Dispose();
                        });
                        return _known_face_encodings;
                    }

                    dynamic GetUnknownFaceEncodings()
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        try
                        {
                            getBitmap().Save(memoryStream, ImageFormat.Jpeg);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("获取图片失败，请检查图片源后重试。\n" + ex);
                        }
                        var imageBytes = memoryStream.ToArray();
                        var bytes = np.array(imageBytes.ToList(), Py.kw("dtype", np.uint8));
                        var image_bytes = io.BytesIO(bytes);
                        var image = pil.Image.open(image_bytes);
                        var unknown_image = np.array(image);
                        var unknown_face_encodings = face_recognition.face_encodings(unknown_image);
                        var _len = builtins.len(unknown_face_encodings);
                        var len = ((PyObject)_len).As<int>();
                        ((PyObject)bytes).Dispose();
                        ((PyObject)image_bytes).Dispose();
                        ((PyObject)image).Dispose();
                        ((PyObject)unknown_image).Dispose();
                        ((PyObject)_len).Dispose();
                        if (len > 0)
                            return unknown_face_encodings;
                        else
                            return null;
                    }

                }
                PythonEngine.EndAllowThreads(ts);
            }
            catch (Exception ex)
            {
                ExceptionEvent?.Invoke("RecThread Exception :" + ex);
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                faceRecTokenSource.Cancel();
                while (IsRunning)
                    Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            ResultEvent = null;
            ExceptionEvent = null;
            getBitmap = null;
            Stop();
        }

        public void EncodingEnter(string name)
        {
            faceName = name;
            editEncodings = 1; //录入
        }

        public void EncodingDelete(string name)
        {
            faceName = name;
            editEncodings = 2; //删除
        }
    }
}
