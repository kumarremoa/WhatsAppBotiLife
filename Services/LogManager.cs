using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
  public static  class LogManager
    {
      private static Object _object = new object();
      public static void WriteLog(string message)
      {
         var logpath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Errorlog.txt";
          lock (_object)
          {
              using (StreamWriter w = File.AppendText(logpath))
              {
                  w.WriteLine(message + "|time:" +
                              DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"));
                  w.Flush();
                  w.Close();
              }

          }

      }
    }
}
