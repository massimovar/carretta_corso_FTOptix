#region Using directives
using UAManagedCore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Linq;
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

public class NetworkConfiguratorEditModelsLogic : BaseNetLogic
{
    public static void CreateEditModels(FTOptix.System.System systemNode, IUAObject parentNode)
    {
        NetworkInterfacesEditModel.CreateEditModel(systemNode, parentNode);
    }

    public static IUAObject GetNetworkInterfacesEditModel(IUAObject parentNode)
    {
        var networkInterfacesEditModel = parentNode.GetObject(editModelNetworkInterfacesBrowseName);
        if (networkInterfacesEditModel == null)
            throw new CoreConfigurationException("Edit model for network interfaces not found");

        return networkInterfacesEditModel;
    }

    public static void DeleteEditModels(IUAObject parentNode)
    {
        NetworkInterfacesEditModel.DeleteEditModel(parentNode);
    }

    private static class NetworkInterfacesEditModel
    {
        public static IUAObject CreateEditModel(FTOptix.System.System systemNode, IUAObject parentNode)
        {
            var editModelNetworkInterfaces = parentNode.FindObject(editModelNetworkInterfacesBrowseName);
            if (editModelNetworkInterfaces == null)
            {
                editModelNetworkInterfaces = InformationModel.MakeObject(editModelNetworkInterfacesBrowseName);
                var systemToNetworkInterfacesEditModelWriter = new SystemToNetworkInterfacesEditModelWriterLogic(editModelNetworkInterfaces);
                systemToNetworkInterfacesEditModelWriter.InitializeEditModel(systemNode.NetworkInterfaces.ToList());
                parentNode.Add(editModelNetworkInterfaces);
            }

            return editModelNetworkInterfaces;
        }

        public static void DeleteEditModel(IUAObject parentNode)
        {
            var editModelNetworkInterfaces = parentNode.FindObject(editModelNetworkInterfacesBrowseName);
            if (editModelNetworkInterfaces != null)
                parentNode.Remove(editModelNetworkInterfaces);
        }
    }

    private static readonly string editModelNetworkInterfacesBrowseName = "NetworkInterfacesEditModel";
}
