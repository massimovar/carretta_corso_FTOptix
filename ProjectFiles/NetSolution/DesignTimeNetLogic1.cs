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

public class DesignTimeNetLogic1 : BaseNetLogic
{
    [ExportMethod]
    public void LedGenerator()
    {
        var motors = Project.Current.Get("Model/Motors").Children.Count;

        var vLayout = InformationModel.Make<ColumnLayout>("MyVerticalLayout");
        vLayout.HorizontalAlignment = HorizontalAlignment.Right;
        vLayout.VerticalAlignment = VerticalAlignment.Bottom;

        for (int i = 0; i < motors; i++)
        {
            var myLed = InformationModel.Make<Led>("myLed" + i);
            vLayout.Add(myLed);
        }

        Owner.Add(vLayout);

    }
}
