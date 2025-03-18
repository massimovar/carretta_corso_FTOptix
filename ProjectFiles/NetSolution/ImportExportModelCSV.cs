#region Using directives

using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UAManagedCore;
using UAManagedCore.Logging;
using FTOptix.Alarm;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.EventLogger;
using OpcUa = UAManagedCore.OpcUa;

#endregion

public class ImportExportModelCSV : BaseNetLogic
{
    [ExportMethod]
    public void BuildVariablesCSVFile()
    {
        string filePath = GenerateFullCSVFilePath("variablesList.csv", true);
        IUANode variablesNodeToBuildCSV = InformationModel.Get(LogicObject.GetVariable("VariablesNodeToBuildCSV").Value);
        if (filePath == string.Empty)
            return;

        LogNewEntry(LogLevel.Info, "Starting writing variables to CSV file...");
        WriteCSVFile(GenerateVariablesEntries(variablesNodeToBuildCSV), filePath);
    }

    [ExportMethod]
    public void ExportModel()
    {
        IUANode modelsVariableFolder;
        string filePath = GenerateFullCSVFilePath(ModelDynamicLinkCSVFileName, true);

        modelsVariableFolder = InformationModel.Get(LogicObject.GetVariable("ModelNodeToExport").Value);
        if (filePath == string.Empty)
            return;

        LogNewEntry(LogLevel.Info, "Starting writing model variables dynamic links to CSV file...");
        WriteCSVFile(GenerateModelValueEntries(modelsVariableFolder), filePath);
    }

    [ExportMethod]
    public void ImportModel()
    {
        string filePath = GenerateFullCSVFilePath(ModelDynamicLinkCSVFileName, false);
        if (filePath == string.Empty)
            return;

        LogNewEntry(LogLevel.Info, "Starting reading model variables dynamic links from CSV file...");
        CreateOrUpdateModelEntry(ReadCSVFile(filePath));
    }

    private List<string[]> ReadCSVFile(string filePath)
    {
        try
        {
            if (filePath == string.Empty)
                throw new NullReferenceException("File path is null or empty, cannot read CSV file");

            char fieldDelimiter = (char)GetFieldDelimiter();
            List<string[]> entries = new List<string[]>();
            LogNewEntry(LogLevel.Info, $"Working on CSV file {@"" + filePath + ""}");
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    entries.Add(reader.ReadLine().Split(fieldDelimiter));
                }
            }
            return entries;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, "Unable to read nodes from CSV file, error: " + ex.Message);
            return null;
        }
    }

    private void WriteCSVFile(List<string[]> entries, string filePath)
    {
        try
        {
            if (entries == null)
                throw new NullReferenceException("Entries list is null, cannot write CSV file");
            if (filePath == string.Empty)
                throw new NullReferenceException("File path is null or empty, cannot write CSV file");

            using (CSVFileWriter CSVFileWriter = new CSVFileWriter(filePath) { FieldDelimiter = GetFieldDelimiter().Value })
            {
                foreach (string[] entry in entries)
                {
                    CSVFileWriter.WriteLine(entry);
                }
            }
            LogNewEntry(LogLevel.Info, $"Successfully wrote {entries.Count} lines in {filePath}");
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, "Unable to export node, error: " + ex.Message);
        }
    }

    private List<string[]> GenerateVariablesEntries(IUANode startingNode)
    {
        try
        {
            if (startingNode == null)
                throw new NullReferenceException("Starting node is null");

            List<string[]> entries = new List<string[]>
            {
                new string[3] { "Variable Name", "Variable Path" , "Variable Array size" }
            };
            foreach (IUANode childrenNode in startingNode.Children)
            {
                GetVariableNodeEntry(childrenNode, ref entries);
            }
            return entries;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return null;
        }
    }

    private void GetVariableNodeEntry(IUANode nodeToCheck, ref List<string[]> entries)
    {
        if (nodeToCheck.NodeClass == NodeClass.Variable && !nodeToCheck.GetType().IsAssignableTo(typeof(TagStructure)))
        {
            string arrayDimension = "";
            var nodeToCheckIUAVariable = nodeToCheck as IUAVariable;
            if (nodeToCheckIUAVariable.ArrayDimensions.Length > 0) arrayDimension = nodeToCheckIUAVariable.ArrayDimensions[0].ToString();
            entries.Add(new string[3] { nodeToCheck.BrowseName, MakeBrowsePath(nodeToCheck), arrayDimension });
        }
        else
        {
            if (nodeToCheck.Children.Count > 0)
            {
                foreach (IUANode childrenNode in nodeToCheck.Children)
                {
                    GetVariableNodeEntry(childrenNode, ref entries);
                }
            }
        }
    }

    private List<string[]> GenerateModelValueEntries(IUANode startingNode)
    {
        try
        {
            if (startingNode == null)
                throw new NullReferenceException("Starting node is null");

            List<string[]> entries = new List<string[]>
            {
                CSVModelDynamicLinkHeader
            };
            foreach (IUANode childrenNode in startingNode.Children)
            {
                GetModelVariableValueEntry(childrenNode, entries);
            }
            return entries;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return null;
        }
    }

    private void GetModelVariableValueEntry(IUANode nodeToCheck, List<string[]> entries)
    {
        switch (nodeToCheck.NodeClass)
        {
            case NodeClass.Variable:
                string variableFullPath = MakeBrowsePath(nodeToCheck);
                string variableDataType = InformationModel.Get(((IUAVariable)nodeToCheck).DataType).BrowseName;
                var nodeToCheckIUAVariable = nodeToCheck as IUAVariable;
                uint variableArrayDimension = nodeToCheckIUAVariable.ArrayDimensions.Length > 0 ? nodeToCheckIUAVariable.ArrayDimensions[0] : 0;
                string sourceArrayIndex = string.Empty;
                string dynamicLinkMode = string.Empty;
                if (nodeToCheck.GetType().IsAssignableTo(typeof(Alias)))
                {
                    variableDataType = MakeBrowsePath(InformationModel.Get(((Alias)nodeToCheck).Kind));
                    NodeId aliasPointedNode = ((Alias)nodeToCheck).Value;
                    string aliasValue = aliasPointedNode != null ? MakeBrowsePath(InformationModel.Get(aliasPointedNode)) : string.Empty;
                    entries.Add(new string[7] { variableFullPath, MarkerAliasTypeIdentifier + variableDataType, "", "", aliasValue, "", "" });
                }
                else
                {
                    int j = nodeToCheckIUAVariable.ArrayDimensions.Length > 0 ? 0 : -1;
                    for (int i = j; i < variableArrayDimension; i++)
                    {
                        string dynamicLinkName = "DynamicLink" + (i >= 0 ? $"_{i}" : string.Empty);
                        DynamicLink dynamicLink = nodeToCheck.GetVariable(dynamicLinkName) as DynamicLink;
                        string variableValue;
                        if (dynamicLink != null && !string.IsNullOrWhiteSpace(dynamicLink.Value))
                        {
                            PathResolverResult resolvePathResult = LogicObject.Context.ResolvePath(nodeToCheck, dynamicLink.Value);
                            if (resolvePathResult.AliasSpecification != null && resolvePathResult.AliasSpecification.AliasTokenPath != "")
                                variableValue = dynamicLink.Value;
                            else
                            {
                                variableValue = MakeBrowsePath(resolvePathResult.ResolvedNode);
                                if (Regex.IsMatch(dynamicLink.Value, "\\.\\d*?\\z"))
                                {
                                    string[] splitDynamicLinkValue = dynamicLink.Value.Value.ToString().Split(".");
                                    sourceArrayIndex += "." + splitDynamicLinkValue[^1];
                                }
                            }
                            foreach (uint index in resolvePathResult.Indexes)
                            {
                                sourceArrayIndex += $"{index}%";
                            }
                            if (sourceArrayIndex.Length > 1) sourceArrayIndex = sourceArrayIndex.Remove(sourceArrayIndex.Length - 1);

                            dynamicLinkMode = ((int)dynamicLink.Mode).ToString();
                        }
                        else
                        {
                            if (variableArrayDimension > 0)
                            {
                                Array arrayValue = (Array)nodeToCheckIUAVariable.Value.Value;
                                variableValue = arrayValue.GetValue(i).ToString();
                            }
                            else
                                variableValue = nodeToCheckIUAVariable.Value.Value.ToString();
                        }
                        string variableArrayIndex = variableArrayDimension > 0 ? i.ToString() : string.Empty;
                        entries.Add(new string[7] { variableFullPath, variableDataType, variableArrayDimension.ToString(), variableArrayIndex, variableValue, sourceArrayIndex, dynamicLinkMode });
                    }
                }
                return;

            case NodeClass.Object:
                entries.Add(new string[7] { MakeBrowsePath(nodeToCheck), MakeBrowsePath(((IUAObject)nodeToCheck).ObjectType), "", "", "", "", "" });
                break;

            case NodeClass.ObjectType:
                entries.Add(new string[7] { MakeBrowsePath(nodeToCheck), MarkerObjectTypeIdentifier + MakeBrowsePath(((IUAObjectType)nodeToCheck).SuperType), "", "", "", "", "" });
                break;
        }
        if (nodeToCheck.Children.Count > 0)
        {
            foreach (IUANode childrenNode in nodeToCheck.Children)
            {
                GetModelVariableValueEntry(childrenNode, entries);
            }
        }
    }

    private void ResetDynamicLink(IUAVariable targetVariable, uint arrayDimension, int arrayIndex)
    {
        if (arrayDimension > 0 && arrayIndex >= 0)
        {
            IUAVariable dynamicLinkToDelete = targetVariable.GetVariable($"DynamicLink_{arrayIndex}");
            if (dynamicLinkToDelete != null)
                targetVariable.Refs.RemoveReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, dynamicLinkToDelete.NodeId, false);

            try
            {
                dynamicLinkToDelete.Delete();
            }
            catch
            {
                // No DynamicLink to remove
            }
        }
        else
            targetVariable.ResetDynamicLink();
    }

    private void SetValueVariable(IUAVariable targetVariable, uint arrayDimension, int arrayIndex, object value)
    {
        if (arrayDimension > 0 && arrayIndex >= 0)
        {
            Array arrayValue = (Array)targetVariable.Value.Value;
            arrayValue.SetValue(ManageObjectValue(arrayValue.GetValue(arrayIndex), value), arrayIndex);
            targetVariable.Value = new UAValue(arrayValue);
        }
        else
            targetVariable.Value = new UAValue(value);

        setOrUpdateValueCounter++;
    }

    private void SetDynamicLink(IUAVariable targetVariable, IUAVariable targetDynamicLink, uint arrayVariableDimension, int arrayVariableIndex, string dynamicLinkArrayIndex, string dynamicLinkMode, string aliasDynamicLink)
    {
        int arrayIndex = -1;
        int mode = (int)DynamicLinkMode.ReadWrite;
        if (!string.IsNullOrWhiteSpace(dynamicLinkArrayIndex) &&
            !dynamicLinkArrayIndex.Contains("%") &&
            !dynamicLinkArrayIndex.StartsWith('.') &&
            !int.TryParse(dynamicLinkArrayIndex, out arrayIndex))
        {
            LogNewEntry(LogLevel.Warning, $"Unable to parse the array index {dynamicLinkArrayIndex}, dynamic link target {targetDynamicLink.BrowseName} for the variable {targetVariable.BrowseName} will be set without array specification");
        }

        if (!string.IsNullOrWhiteSpace(dynamicLinkMode) && !int.TryParse(dynamicLinkMode, out mode) && mode < (int)DynamicLinkMode.Read && mode > (int)DynamicLinkMode.ReadWrite)
        {
            LogNewEntry(LogLevel.Warning, $"Unable to parse the dynamic link mode {dynamicLinkMode}, dynamic link {targetDynamicLink.BrowseName} mode will be set to Read/Write");
            mode = (int)DynamicLinkMode.ReadWrite;
        }

        string dynamicLinkVariableBrowseName = "DynamicLink";
        if (arrayVariableDimension > 0 && arrayVariableIndex >= 0)
            dynamicLinkVariableBrowseName += $"_{arrayVariableIndex}";

        DynamicLink newDynamicLink = InformationModel.MakeVariable<DynamicLink>(dynamicLinkVariableBrowseName, FTOptix.Core.DataTypes.NodePath);

        newDynamicLink.Value = aliasDynamicLink.Length > 0 ? aliasDynamicLink : DynamicLinkPath.MakePath(targetVariable, targetDynamicLink);
        newDynamicLink.Mode = (DynamicLinkMode)mode;

        if (dynamicLinkArrayIndex.Contains("%"))
        {
            string[] splittedArrayMultiIndex = dynamicLinkArrayIndex.Split("%");
            string arrayMultiDimensionIndex = string.Empty;
            foreach (string singleArrayDimensionIndex in splittedArrayMultiIndex)
            {
                uint parsedIndex = 0;
                if (uint.TryParse(singleArrayDimensionIndex, out parsedIndex))
                    arrayMultiDimensionIndex += $"{parsedIndex},";
            }
            if (arrayMultiDimensionIndex.Length > 2)
                newDynamicLink.Value = $"{newDynamicLink.Value.Value}[{arrayMultiDimensionIndex.Remove(arrayMultiDimensionIndex.Length - 1)}]";
        }
        else if (dynamicLinkArrayIndex.StartsWith('.'))
        {
            newDynamicLink.Value = newDynamicLink.Value.Value + dynamicLinkArrayIndex;
        }
        else
            if (arrayIndex >= 0) newDynamicLink.Value = $"{newDynamicLink.Value.Value}[{arrayIndex}]";

        if (arrayVariableDimension > 0 && arrayVariableIndex >= 0)
            newDynamicLink.ParentArrayIndexVariable.Value = arrayVariableIndex;

        targetVariable.Refs.AddReference(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink, newDynamicLink);
        newDynamicLink.SetModellingRuleRecursive();
        setOrUpdateDynamicLinkCounter++;
    }

    private void CreateOrUpdateModelEntry(List<string[]> entries)
    {
        try
        {
            if (entries == null)
                throw new NullReferenceException("Entries list is null, cannot start the reading process");

            int entriesCount = -1;
            foreach (string[] entry in entries)
            {
                if (entry.Length != CSVModelDynamicLinkHeader.Length)
                    throw new InvalidDataException($"The elements count ({entry.Length}) of the entry is not equal to {CSVModelDynamicLinkHeader.Length} as it should be, the reading process is halted");

                entriesCount++;
                if (entriesCount == 0)
                {
                    continue;
                }
                uint variableArrayDimension = 0;
                int variableArrayDynamicLinkIndex = -1;
                string nodeBrowsePath = entry[0];
                string dataType = entry[1];
                string dynamicLinkPath = entry[4];
                string dynamicLinkArrayIndex = entry[5];
                string dynamicLinkMode = entry[6];
                uint.TryParse(entry[2], out variableArrayDimension);
                int.TryParse(entry[3], out variableArrayDynamicLinkIndex);
                NodeId dataTypeNodeId = GetVariableTypeNodeId(dataType);
                if (dataTypeNodeId != null)
                {
                    IUAVariable targetVariable = Project.Current.GetVariable(nodeBrowsePath);
                    if (targetVariable == null)
                    {
                        if (!MakeVariable(nodeBrowsePath, dataTypeNodeId, variableArrayDimension) || Project.Current.GetVariable(nodeBrowsePath) == null)
                        {
                            LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: variable with path {nodeBrowsePath} does not exist.");
                            continue;
                        }
                        else
                            targetVariable = Project.Current.GetVariable(nodeBrowsePath);
                    }
                    if (string.IsNullOrWhiteSpace(dynamicLinkPath))
                    {
                        ResetDynamicLink(targetVariable, variableArrayDimension, variableArrayDynamicLinkIndex);
                        resetDynamicLinkCounter++;
                        continue;
                    }

                    IUANode targetDynamicLink = Project.Current.Get(dynamicLinkPath);
                    bool isAlias = dynamicLinkPath.Contains("{") && dynamicLinkPath.Contains("}");
                    bool isValue = targetDynamicLink == null && !isAlias;

                    if (!isValue && !isAlias && targetDynamicLink.NodeClass != NodeClass.Variable)
                    {
                        LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: dynamic link {dynamicLinkPath} is not a variable.");
                        continue;
                    }

                    ResetDynamicLink(targetVariable, variableArrayDimension, variableArrayDynamicLinkIndex);

                    if (isValue)
                    {
                        try
                        {
                            SetValueVariable(targetVariable, variableArrayDimension, variableArrayDynamicLinkIndex, dynamicLinkPath);
                        }
                        catch (Exception ex)
                        {
                            LogNewEntry(LogLevel.Warning, $"Unable to set value for {targetVariable}, Default value will be set ({ex.Message})");
                        }
                    }
                    else
                        SetDynamicLink(targetVariable, (IUAVariable)targetDynamicLink, variableArrayDimension, variableArrayDynamicLinkIndex, dynamicLinkArrayIndex, dynamicLinkMode, isAlias ? dynamicLinkPath : string.Empty);
                }
                else
                {
                    if (dataType.StartsWith(MarkerAliasTypeIdentifier))
                    {
                        Alias targetAlias = Project.Current.Get(nodeBrowsePath) as Alias;
                        NodeId aliasKind = GetDataTypeNodeId(dataType.Replace(MarkerAliasTypeIdentifier, string.Empty));
                        if (targetAlias == null)
                        {
                            if (aliasKind == null)
                            {
                                LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: cannot find kind Data type with path {dataType} to create Alias with path {nodeBrowsePath}");
                                continue;
                            }
                            if (!MakeAlias(nodeBrowsePath, aliasKind))
                                LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: kind for alias type with path {nodeBrowsePath} does not exist.");
                        }
                        else
                        {
                            if (aliasKind != null && targetAlias.Kind != aliasKind)
                                targetAlias.Kind = aliasKind;

                            IUANode targetDynamicLink = Project.Current.Get(dynamicLinkPath);

                            if (targetDynamicLink != null)
                                targetAlias.Owner.SetAlias(targetAlias.BrowseName, targetDynamicLink);
                        }
                    }
                    else if (dataType.StartsWith(MarkerObjectTypeIdentifier))
                    {
                        IUAObjectType targetObjectType = (IUAObjectType)Project.Current.Get(nodeBrowsePath);
                        if (targetObjectType == null)
                        {
                            NodeId objectSuperType = GetObjectTypeNodeId(dataType.Replace(MarkerObjectTypeIdentifier, string.Empty));
                            if (objectSuperType == null)
                            {
                                LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: cannot find Object supertype with path {dataType} to create object type with path {nodeBrowsePath}");
                                continue;
                            }
                            if (!MakeObjectType(nodeBrowsePath, objectSuperType)) LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: object type with path {nodeBrowsePath} does not exist.");
                        }
                    }
                    else
                    {
                        IUAObject targetObject = Project.Current.GetObject(nodeBrowsePath);
                        if (targetObject == null)
                        {
                            NodeId objectType = GetObjectTypeNodeId(dataType);
                            if (objectType == null)
                            {
                                LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: cannot find Object type with path {dataType} to create object with path {nodeBrowsePath}");
                                continue;
                            }
                            if (!MakeObject(nodeBrowsePath, objectType))
                                LogNewEntry(LogLevel.Error, $"Error in line {entriesCount} of CSV file: object with path {nodeBrowsePath} does not exists.");
                        }
                    }
                }
            }
            LogNewEntry(LogLevel.Info, $"Successfully terminated the import of {entries.Count - 1} lines to merge dynamic link on models. {setOrUpdateDynamicLinkCounter} links set or updated, {resetDynamicLinkCounter} links reset, {setOrUpdateValueCounter} value set");
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
        }
    }

    private NodeId GetDataTypeNodeId(string dataTypePath)
    {
        IUANode nodeType = Project.Current.Get(dataTypePath);

        if (nodeType != null)
            return nodeType.NodeId;

        string[] splitTypePath = dataTypePath.Split('/');
        string nodeName = string.Empty;

        if (splitTypePath.Length <= 0)
            nodeName = dataTypePath;
        else
            nodeName = splitTypePath[splitTypePath.Length - 1];

        FieldInfo fieldType = typeof(FTOptix.Core.VariableTypes).GetField(nodeName);
        if (fieldType != null)
            return (NodeId)fieldType.GetValue(null);

        fieldType = typeof(FTOptix.CommunicationDriver.VariableTypes).GetField(nodeName);
        if (fieldType != null)
            return (NodeId)fieldType.GetValue(null);

        return null;
    }

    private NodeId GetVariableTypeNodeId(string dataTypePath)
    {
        if (DataTypesHelper.GetDataTypeIdByName(dataTypePath) != null)
            return DataTypesHelper.GetDataTypeIdByName(dataTypePath);

        string[] splitTypePath = dataTypePath.Split('/');
        string nodeName = string.Empty;

        if (splitTypePath.Length <= 0)
            nodeName = dataTypePath;
        else
            nodeName = splitTypePath[splitTypePath.Length - 1];

        FieldInfo fieldType = typeof(FTOptix.Core.DataTypes).GetField(nodeName);

        if (fieldType != null)
            return (NodeId)fieldType.GetValue(null);
        else
            return null;
    }

    private NodeId GetObjectTypeNodeId(string dataTypePath)
    {
        IUANode nodeType = Project.Current.Get(dataTypePath);

        if (nodeType != null)
            return nodeType.NodeId;

        string[] splitTypePath = dataTypePath.Split('/');
        string nodeName = string.Empty;

        if (splitTypePath.Length <= 0)
            nodeName = dataTypePath;
        else
            nodeName = splitTypePath[splitTypePath.Length - 1];

        FieldInfo fieldType = typeof(OpcUa.ObjectTypes).GetField(nodeName);

        if (fieldType != null)
            return (NodeId)fieldType.GetValue(null);
        else
            return null;
    }

    private IUANode GetNodeOwnerFromPath(string nodeBrowsePath, out string nodeName)
    {
        try
        {
            string[] splitBrowsePath = nodeBrowsePath.Split('/');

            if (splitBrowsePath.Length <= 0)
                throw new NullReferenceException("Missing path");

            nodeName = splitBrowsePath[splitBrowsePath.Length - 1];
            string nodeOwner = string.Empty;

            for (int i = 0; i < splitBrowsePath.Length - 1; i++)
            {
                nodeOwner += splitBrowsePath[i] + (i == splitBrowsePath.Length - 2 ? "" : "/");
            }

            IUANode ownerNode = Project.Current.Get(nodeOwner);

            if (ownerNode == null)
                throw new NullReferenceException($"Cannot find owner {nodeOwner} for node {nodeName}. Full path: {nodeBrowsePath}");

            return ownerNode;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            nodeName = string.Empty;
            return null;
        }
    }

    private bool MakeAlias(string aliasBrowsePath, NodeId kindType)
    {
        try
        {
            string aliasName = string.Empty;
            IUANode ownerObjectType = GetNodeOwnerFromPath(aliasBrowsePath, out aliasName);
            Alias newAlias = InformationModel.MakeVariable<Alias>(aliasName, UAManagedCore.OpcUa.DataTypes.NodeId);
            newAlias.Kind = kindType;
            ownerObjectType.Add(newAlias);
            return true;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return false;
        }
    }

    private bool MakeObjectType(string objectBrowsePath, NodeId objectSuperType)
    {
        try
        {
            string objectTypeName = string.Empty;
            IUANode ownerObjectType = GetNodeOwnerFromPath(objectBrowsePath, out objectTypeName);
            IUAObjectType newObjectType = InformationModel.MakeObjectType(objectTypeName, objectSuperType);
            ownerObjectType.Add(newObjectType);
            return true;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return false;
        }
    }

    private bool MakeObject(string objectBrowsePath, NodeId objectType)
    {
        try
        {
            string objectName = string.Empty;
            IUANode ownerObject = GetNodeOwnerFromPath(objectBrowsePath, out objectName);
            IUAObject newObject = InformationModel.MakeObject(objectName, objectType);
            ownerObject.Add(newObject);
            return true;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return false;
        }
    }

    private bool MakeVariable(string variableBrowsePath, NodeId variableType, uint variableArrayDimension)
    {
        try
        {
            string variableName = string.Empty;
            IUANode ownerVariable = GetNodeOwnerFromPath(variableBrowsePath, out variableName);
            uint[] arrayDimension = null;

            if (variableArrayDimension > 0)
                arrayDimension = new uint[] { variableArrayDimension };

            IUAVariable newVariable = InformationModel.MakeVariable(variableName, variableType, arrayDimension);
            ownerVariable.Add(newVariable);
            return true;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return false;
        }
    }

    private void LogNewEntry(LogLevel entrySeverity, string message, [CallerMemberName] string callerMethod = "")
    {
        switch (entrySeverity)
        {
            case LogLevel.Info:
                Log.Info($"{LogicObject.BrowseName}.{callerMethod}", message);
                break;

            case LogLevel.Warning:
                Log.Warning($"{LogicObject.BrowseName}.{callerMethod}", message);
                break;

            case LogLevel.Error:
                Log.Error($"{LogicObject.BrowseName}.{callerMethod}", message);
                break;

            case LogLevel.Debug:
                Log.Debug($"{LogicObject.BrowseName}.{callerMethod}", message);
                break;

            case LogLevel.Verbose1:
                Log.Verbose1($"{LogicObject.BrowseName}.{callerMethod}", message);
                break;

            case LogLevel.Verbose2:
                Log.Verbose2($"{LogicObject.BrowseName}.{callerMethod}", message);
                break;
        }
    }

    private string GenerateFullCSVFilePath(string fileName, bool isWriting)
    {
        try
        {
            string folderPath = Path.GetDirectoryName(new ResourceUri(LogicObject.GetVariable(CSVFilePathVariableName)?.Value).Uri);

            if (string.IsNullOrEmpty(folderPath))
                throw new Exception("CSV Folder path not set properly");

            Directory.CreateDirectory(folderPath);
            string fullFilePath = Path.Combine(folderPath, fileName);

            if (File.Exists(fullFilePath) && isWriting)
            {
                LogNewEntry(LogLevel.Warning, $"File {fullFilePath} already exists, will be deleted");
                File.Delete(fullFilePath);
            }
            return fullFilePath;
        }
        catch (Exception ex)
        {
            LogNewEntry(LogLevel.Error, ex.Message);
            return string.Empty;
        }
    }

    private string MakeBrowsePath(IUANode node, IUANode ancestor = null)
    {
        if (ancestor == null)
            ancestor = Project.Current;

        if (node == null)
            return string.Empty;

        string path = node.BrowseName;
        var current = node.Owner;

        while (current != null && current != ancestor)
        {
            path = $"{current.BrowseName}/{path}";
            current = current.Owner;
        }
        return path;
    }

    private char? GetFieldDelimiter()
    {
        var separatorVariable = LogicObject.GetVariable(CharacterSeparatorVariableName);
        if (separatorVariable == null)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, "CharacterSeparator variable not found");
            return null;
        }

        string separator = separatorVariable.Value;

        if (separator.Length != 1 || separator == String.Empty)
        {
            Log.Error(MethodBase.GetCurrentMethod().Name, "Wrong CharacterSeparator configuration. Please insert a char");
            return null;
        }

        if (char.TryParse(separator, out char result))
            return result;

        return null;
    }

    private object ManageObjectValue(object targetType, object inputValue)
    {
        switch (Type.GetTypeCode(targetType.GetType()))
        {
            case TypeCode.Byte:
                return Convert.ToByte(inputValue);

            case TypeCode.SByte:
                return Convert.ToSByte(inputValue);

            case TypeCode.UInt16:
                return Convert.ToUInt16(inputValue);

            case TypeCode.UInt32:
                return Convert.ToUInt32(inputValue);

            case TypeCode.UInt64:
                return Convert.ToUInt64(inputValue);

            case TypeCode.Int16:
                return Convert.ToInt16(inputValue);

            case TypeCode.Int32:
                return Convert.ToInt32(inputValue);

            case TypeCode.Int64:
                return Convert.ToInt64(inputValue);

            case TypeCode.Decimal:
                return Convert.ToDecimal(inputValue);

            case TypeCode.Double:
                return Convert.ToDouble(inputValue);

            case TypeCode.Single:
                return Convert.ToSingle(inputValue);

            case TypeCode.String:
                return Convert.ToString(inputValue);

            default:
                return inputValue;
        }
    }

    private class CSVFileWriter : IDisposable
    {
        public char FieldDelimiter { get; set; } = ',';

        public char QuoteChar { get; set; } = '"';

        public bool WrapFields { get; set; } = false;

        public CSVFileWriter(string filePath)
        {
            CSVStreamWriter = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        }

        public void WriteLine(string[] fields)
        {
            var stringBuilder = new StringBuilder();

            for (var i = 0; i < fields.Length; ++i)
            {
                if (WrapFields)
                    stringBuilder.AppendFormat("{0}{1}{0}", QuoteChar, EscapeField(fields[i]));
                else
                    stringBuilder.AppendFormat("{0}", fields[i]);

                if (i != fields.Length - 1)
                    stringBuilder.Append(FieldDelimiter);
            }

            CSVStreamWriter.WriteLine(stringBuilder.ToString());
            CSVStreamWriter.Flush();
        }

        private string EscapeField(string field)
        {
            var quoteCharString = QuoteChar.ToString();
            return field.Replace(quoteCharString, quoteCharString + quoteCharString);
        }

        private readonly StreamWriter CSVStreamWriter;

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                CSVStreamWriter.Dispose();

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    private const string CSVFilePathVariableName = "CSVFilePath";
    private const string CharacterSeparatorVariableName = "CharacterSeparator";
    private const string MarkerObjectTypeIdentifier = "%TYPE%";
    private const string MarkerAliasTypeIdentifier = "%ALIAS%";
    private readonly string[] CSVModelDynamicLinkHeader = { "Model path", "Type", "Variable Array Dimension", "Variable Array Index", "Dynamic link or Value", "Dynamic link array index", "Mode (0=R 1=W 2=RW)" };
    private const string ModelDynamicLinkCSVFileName = "modelList.csv";
    private int setOrUpdateDynamicLinkCounter = 0;
    private int setOrUpdateValueCounter = 0;
    private int resetDynamicLinkCounter = 0;
}
