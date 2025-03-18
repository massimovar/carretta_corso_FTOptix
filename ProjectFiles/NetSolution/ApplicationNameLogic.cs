#region Using directives
using FTOptix.HMIProject;
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
#endregion

public class ApplicationNameLogic : BaseNetLogic
{
    public override void Start()
    {
        Label label = Owner as Label;
        label.Text = Project.Current.BrowseName;
    }

    public override void Stop()
    {
    }
}
