#region Using directives
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.EventLogger;
using FTOptix.DataLogger;
using FTOptix.Recipe;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.Modbus;
#endregion

public class AlarmGridLogic : BaseNetLogic
{
    public override void Start()
    {
        alarmsDataGridModel = Owner.Get<DataGrid>("AlarmsDataGrid").GetVariable("Model");

        var currentSession = LogicObject.Context.Sessions.CurrentSessionInfo;
        actualLanguageVariable = currentSession.SessionObject.Get<IUAVariable>("ActualLanguage");
        actualLanguageVariable.VariableChange += OnSessionActualLanguageChange;
    }

    public override void Stop()
    {
        actualLanguageVariable.VariableChange -= OnSessionActualLanguageChange;
    }

    public void OnSessionActualLanguageChange(object sender, VariableChangeEventArgs e)
    {
        var dynamicLink = alarmsDataGridModel.GetVariable("DynamicLink");
        if (dynamicLink == null)
            return;

        // Restart the data bind on the data grid model variable to refresh data
        string dynamicLinkValue = dynamicLink.Value;
        dynamicLink.Value = string.Empty;
        dynamicLink.Value = dynamicLinkValue;
    }

    private IUAVariable alarmsDataGridModel;
    private IUAVariable actualLanguageVariable;
}
