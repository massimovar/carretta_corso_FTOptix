#region Using directives
using System.IO;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.SerialPort;
using FTOptix.WebUI;
using FTOptix.S7TiaProfinet;
using FTOptix.CommunicationDriver;
using FTOptix.S7TCP;
using FTOptix.Alarm;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Recipe;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Modbus;
#endregion

public class FTOptixStudioVersionLogic : BaseNetLogic
{
    public override void Start()
    {
        string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string strWorkPath = Path.GetDirectoryName(strExeFilePath);
        string versionFile = strWorkPath + Path.DirectorySeparatorChar
            + ".." + Path.DirectorySeparatorChar
            + ".." + Path.DirectorySeparatorChar
            + ".." + Path.DirectorySeparatorChar
            + "IDEVersion.txt";

        var fileStream = new FileStream(versionFile, FileMode.Open, FileAccess.Read);
        // Read the source file into a byte array.
        byte[] bytes = new byte[fileStream.Length];
        int numBytesToRead = (int)fileStream.Length;
        int numBytesRead = 0;
        while (numBytesToRead > 0)
        {
            // Read may return anything from 0 to numBytesToRead.
            var n = fileStream.Read(bytes, numBytesRead, numBytesToRead);

            // Break when the end of the file is reached.
            if (n == 0)
                break;

            numBytesRead += n;
            numBytesToRead -= n;
        }

        var versionString = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);

        Label label = Owner as Label;
        label.Text = versionString;
    }

    public override void Stop()
    {
    }
}
