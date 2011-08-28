using System;
using System.IO;

namespace AlfaServer
{
    public class Log
    {
        private static Log _instance;

        private static TextWriter _textWriter;

        private Log()
        {}

        public static Log Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Log();
                    _textWriter = new StreamWriter(new FileStream("logs/log" + DateTime.Now.ToBinary() + ".txt", FileMode.CreateNew, FileAccess.Write));
                }
                return _instance;
            }
        }

        public void Write(string message)
        {
            _textWriter.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff") + " : " +  message);
            _textWriter.Flush();
        }
    }
}
