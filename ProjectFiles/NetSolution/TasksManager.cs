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

public class TasksManager : BaseNetLogic
{
    private FTOptix.Modbus.Station modbusStation;
    private IUAVariable modbusTestTag;
    private PeriodicTask pTask;
    private LongRunningTask longRunningTask;
    private DelayedTask delayedTask;

    public override void Start()
    {
        modbusStation = Project.Current.Get<FTOptix.Modbus.Station>("CommDrivers/ModbusDriver1/ModbusStation1");
        modbusTestTag = Project.Current.GetVariable("CommDrivers/ModbusDriver1/ModbusStation1/ModbusTag1");

        pTask = new PeriodicTask(CheckStationConnStatus, 5000, LogicObject);
        pTask.Start();

        longRunningTask = new LongRunningTask(SimpleLog, LogicObject);

        delayedTask = new DelayedTask(DelayedLog, 6000, LogicObject);
        delayedTask.Start();

        RemoteVariableSynchronizer remoteVariableSynchronizer = new RemoteVariableSynchronizer(new TimeSpan(0,0,1));
        remoteVariableSynchronizer.Add(modbusTestTag);
    }

    private void DelayedLog()
    {
        Log.Info("I have been delayed");
    }

    [ExportMethod]
    public void StartLongRunningTask()
    {
        longRunningTask.Start();
    }

    private void SimpleLog()
    {
        Log.Info("I'm a Long running task method");
    }

    private void CheckStationConnStatus()
    {
        Log.Info("CheckStationConnStatus", "Connection status: " + modbusStation.OperationCode.ToString());
        Log.Info("CheckStationConnStatus", "ModbusTestTag value: " + modbusTestTag.RemoteRead());
    }

    public override void Stop()
    {
        pTask?.Dispose();
        longRunningTask?.Dispose();
        delayedTask?.Dispose();
    }
}
