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

public class MyFirstDTNetLogic : BaseNetLogic
{
    [ExportMethod]
    public void VariablesGenerator(){
        Log.Info("Starting variables generator...");

        var numbersOfVariablesToGenerate = LogicObject.GetVariable("NumbersOfVariablesToGenerate").Value;

        // Retrieve Model folder

        var modelFolder = Project.Current.Get<Folder>("Model");

        // Create "MyFolder" inside Model folder, if exists

        var myFolder = InformationModel.Make<Folder>("MyFolder");

        if (modelFolder.Get<Folder>("MyFolder") != null)
        {
            Log.Warning("MyFolder already exists");
            myFolder = Project.Current.Get<Folder>("Model/MyFolder");
        } else {
            modelFolder.Add(myFolder);
            Log.Info("MyFolder created successfully");
        }

        myFolder.Children.Clear();

        // Create 10 variables inside "MyFolder" with random values
        for (int i = 0; i < numbersOfVariablesToGenerate; i++)
        {
            var myVar = InformationModel.MakeVariable("myVar" + i, OpcUa.DataTypes.Int32);
            myVar.Value = i * 10;
            myFolder.Add(myVar);
        }
    }
}
