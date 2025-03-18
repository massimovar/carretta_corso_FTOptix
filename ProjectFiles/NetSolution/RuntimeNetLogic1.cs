#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.CoreBase;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.NetLogic;
using FTOptix.SerialPort;
using FTOptix.UI;
using FTOptix.Core;
using FTOptix.WebUI;
using FTOptix.S7TiaProfinet;
using FTOptix.CommunicationDriver;
using FTOptix.S7TCP;
using FTOptix.Alarm;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.EventLogger;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    public void GenerateCSV(){
        
    }
}
