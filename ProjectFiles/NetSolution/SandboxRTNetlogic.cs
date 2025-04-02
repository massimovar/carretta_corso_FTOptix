#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.S7TiaProfinet;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.UI;
using FTOptix.Core;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Modbus;
#endregion

public class SandboxRTNetlogic : BaseNetLogic
{
    public override void Start()
    {
        Log.Info(Owner.BrowseName + " started successfully");
    }

    public override void Stop()
    {
        Log.Info(Owner.BrowseName + " stopped successfully");
    }

    [ExportMethod]
    public void UpdateBackground()
    {
        var backgroundToUpdate = LogicObject.GetVariable("BackgroundToUpdate");
        backgroundToUpdate.Value = Colors.Red;
    }
      
}
