﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System.Collections;
using Gum.Reflection;
using ToolsUtilities;

namespace Gum.DataTypes.Variables
{
    public static class StateSaveExtensionMethods
    {
        public static void Initialize(this StateSave stateSave)
        {
            foreach (VariableSave variable in stateSave.Variables)
            {
                variable.FixEnumerations();
            }
        }

        public static object GetValueRecursive(this StateSave stateSave, string variableName)
        {
            object value = stateSave.GetValue(variableName);

            if (value == null)
            {
                // Is this thing the default?
                ElementSave parent = stateSave.ParentContainer;

                if (parent != null && stateSave != parent.DefaultState)
                {
                    throw new NotImplementedException();
                }
                else if (parent != null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, parent);
                    
                    if (baseElement != null)
                    {
                        string nameInBase = variableName;

                        if (variableName.Contains('.'))
                        {
                            // this variable is set on an instance, but we're going into the
                            // base type, so we want to get the raw variable and not the variable
                            // as tied to an instance.
                            nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
                        }

                        return baseElement.DefaultState.GetValueRecursive(nameInBase);
                    }
                    else
                    {
                        return null;
                    }

                }
                else
                {
                    return null;
                }
            }
            else
            {
                return value;
            }
        }

        private static ElementSave GetBaseElementFromVariable(string variableName, ElementSave parent)
        {
            // this thing is the default state
            // But it's null, so we have to look
            // to the parent
            ElementSave baseElement = null;

            if (variableName.Contains('.'))
            {
                string instanceToSearchFor = variableName.Substring(0, variableName.IndexOf('.'));

                InstanceSave instanceSave = parent.GetInstance(instanceToSearchFor);

                baseElement = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);
            }
            else
            {
                baseElement = ObjectFinder.Self.GetElementSave(parent.BaseType);
            }
            return baseElement;
        }


        public static VariableSave GetVariableRecursive(this StateSave stateSave, string variableName)
        {
            VariableSave variableSave = stateSave.GetVariableSave(variableName);

            if (variableSave == null)
            {
                // Is this thing the default?
                ElementSave parent = stateSave.ParentContainer;

                if (parent != null && stateSave != parent.DefaultState)
                {
                    throw new NotImplementedException();
                }
                else if (parent != null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, parent);

                    if (baseElement != null)
                    {
                        string nameInBase = variableName;

                        if (variableName.Contains('.'))
                        {
                            // this variable is set on an instance, but we're going into the
                            // base type, so we want to get the raw variable and not the variable
                            // as tied to an instance.
                            nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
                        }

                        return baseElement.DefaultState.GetVariableRecursive(nameInBase);
                    }
                }
                    
                return null;
            }
            else
            {
                return variableSave;
            }
        }

        public static VariableListSave GetVariableListRecursive(this StateSave stateSave, string variableName)
        {
            VariableListSave variableListSave = stateSave.GetVariableListSave(variableName);

            if (variableListSave == null)
            {
                // Is this thing the default?
                ElementSave parent = stateSave.ParentContainer;

                if (parent != null && stateSave != parent.DefaultState)
                {
                    throw new NotImplementedException();
                }
                else if (parent != null)
                {
                    ElementSave baseElement = GetBaseElementFromVariable(variableName, parent);

                    if (baseElement != null)
                    {
                        string nameInBase = variableName;

                        if (variableName.Contains('.'))
                        {
                            // this variable is set on an instance, but we're going into the
                            // base type, so we want to get the raw variable and not the variable
                            // as tied to an instance.
                            nameInBase = variableName.Substring(nameInBase.IndexOf('.') + 1);
                        }

                        return baseElement.DefaultState.GetVariableListRecursive(nameInBase);
                    }
                }

                return null;
            }
            else
            {
                return variableListSave;
            }
        }


        public static void ReactToInstanceNameChange(this StateSave stateSave, string oldName, string newName)
        {
            foreach (VariableSave variable in stateSave.Variables)
            {
                if(variable.SourceObject == oldName)
                {
                    variable.Name = newName + "." +  variable.Name.Substring((oldName + ".").Length);
                    variable.SourceObject = newName;
                }
            }

            foreach (VariableListSave variableList in stateSave.VariableLists)
            {
                if (variableList.SourceObject == oldName)
                {
                    if (variableList.SourceObject == oldName)
                    {
                        variableList.Name = newName + "." + variableList.Name.Substring((oldName + ".").Length);
                        variableList.SourceObject = newName;
                    }
                }
            }
        }


        public static void SetValue(this StateSave stateSave, string variableName, object value, InstanceSave instanceSave = null, string variableType = null)
        {
            bool isReservedName = TrySetReservedValues(stateSave, variableName, value, instanceSave);

            VariableSave variableSave = stateSave.GetVariableSave(variableName);

            string rootName = variableName;
            if (variableName.Contains('.'))
            {
                rootName = variableName.Substring(variableName.IndexOf('.') + 1);
            }

            bool isFile = false;

            // Why might instanceSave be null?
            // The reason is because StateSaves
            // are used both for actual game data
            // as well as temporary variable containers.
            // If a StateSave is a temporary container then
            // instanceSave may (probably will be) null.
            if (instanceSave != null)
            {
                VariableSave tempVariable = instanceSave.GetVariableFromThisOrBase(stateSave.ParentContainer, rootName);

                if (tempVariable != null)
                {
                    isFile = tempVariable.IsFile;
                }
            }
            else if (variableSave != null)
            {
                isFile = variableSave.IsFile;
            }

            if (isFile && 
                value is string &&
                !FileManager.IsRelative((string)value))
            {
                string directoryToMakeRelativeTo = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);
                value = FileManager.MakeRelative((string)value, directoryToMakeRelativeTo);
            }


            if (!isReservedName)
            {
                if (value != null && value is IList)
                {
                    stateSave.AssignVariableListSave(variableName, value, instanceSave);
                }
                else
                {

                    stateSave.AssignVariableSave(variableName, value, instanceSave, variableType );
                }
            }

        }

        private static bool TrySetReservedValues(StateSave stateSave, string variableName, object value, InstanceSave instanceSave)
        {
            bool isReservedName = false;

            // Check for reserved names
            if (variableName == "Name")
            {
                stateSave.ParentContainer.Name = value as string;
                isReservedName = true;
            }
            else if (variableName == "Base Type")
            {
                stateSave.ParentContainer.BaseType = value.ToString();
                isReservedName = true; // don't do anything
            }

            if (variableName.Contains('.'))
            {
                string instanceName = variableName.Substring(0, variableName.IndexOf('.'));

                ElementSave elementSave = stateSave.ParentContainer;

                // This is a variable on an instance
                if (variableName.EndsWith(".Name"))
                {
                    instanceSave.Name = (string)value;
                    isReservedName = true;
                }
                else if (variableName.EndsWith(".Base Type"))
                {
                    instanceSave.BaseType = value.ToString();
                    isReservedName = true;
                }
                else if (variableName.EndsWith(".Locked"))
                {
                    instanceSave.Locked = (bool)value;
                    isReservedName = true;
                }
            }
            return isReservedName;
        }


        private static void AssignVariableListSave(this StateSave stateSave, string variableName, object value, InstanceSave instanceSave)
        {
            VariableListSave variableListSave = stateSave.GetVariableListSave(variableName);

            if (variableListSave == null)
            {
                if (value is List<string>)
                {
                    variableListSave = new VariableListSave<string>();
                }
                variableListSave.Type = "string";

                variableListSave.Name = variableName;

                //if (instanceSave != null)
                //{
                //    variableListSave.SourceObject = instanceSave.Name;
                //}

                stateSave.VariableLists.Add(variableListSave);
            }

            // See comments in AssignVariableSave about why we do this outside of the above if-statement.

            if (variableName.Contains('.'))
            {
                string rootName = variableListSave.Name.Substring(variableListSave.Name.IndexOf('.') + 1);

                string sourceObjectName = variableListSave.Name.Substring(0, variableListSave.Name.IndexOf('.'));

                if (instanceSave == null && stateSave.ParentContainer != null)
                {
                    instanceSave = stateSave.ParentContainer.GetInstance(sourceObjectName);
                }
                if (instanceSave != null)
                {
                    VariableListSave baseVariableListSave = ObjectFinder.Self.GetRootStandardElementSave(instanceSave).DefaultState.GetVariableListSave(rootName);
                    variableListSave.IsFile = baseVariableListSave.IsFile;
                }
                variableListSave.SourceObject = sourceObjectName;
            }

            variableListSave.ValueAsIList = value as IList;
        }

        /// <summary>
        /// Assigns a value to a variable.  If the variable doesn't exist then the variable is instantiated, then the value is assigned.
        /// </summary>
        /// <param name="stateSave">The StateSave that contains the variable.  The variable will be added to this StateSave if it doesn't exist.</param>
        /// <param name="variableName">The name of the variable to look for.</param>
        /// <param name="value">The value to assign to the variable.</param>
        /// <param name="instanceSave">The instance that owns this variable.  This may be null.</param>
        /// <param name="variableType">The type of the variable.  This is only needed if the value is null.</param>
        private static void AssignVariableSave(this StateSave stateSave, string variableName, object value, 
            InstanceSave instanceSave, string variableType = null)
        {
            // Not a reserved variable, so use the State's variables
            VariableSave variableSave = stateSave.GetVariableSave(variableName);

            if (variableSave == null)
            {
                variableSave = new VariableSave();

                if (value is bool)
                {
                    variableSave.Type = "bool";
                }
                else if (value is float)
                {
                    variableSave.Type = "float";
                }
                else if (value is int)
                {
                    variableSave.Type = "int";
                }
                // account for enums
                else if (value is string)
                {
                    variableSave.Type = "string";
                }
                else if (value == null)
                {
                    variableSave.Type = variableType;
                }
                else
                {
                    variableSave.Type = value.GetType().ToString();
                }

                // Let's get the variable that this comes from to see if it's a file

                variableSave.Name = variableName;

                stateSave.Variables.Add(variableSave);
            }




            // There seems to be
            // two ways to indicate
            // that a variable has a
            // source object.  One is
            // to pass a InstanceSave to
            // this method, another is to
            // include a '.' in the name.  If
            // an instanceSave is passed, then
            // a dot MUST be present.  I don't think
            // we allow a dot to exist without a variable
            // representing a variable on an instance save,
            // so I'm not sure why we even require an InstanceSave.
            // Also, it seems like code (especially plugins) may not
            // know to pass an InstanceSave and may assume that the dot
            // is all that's needed.  If so, we shouldn't be strict and require
            // a non-null InstanceSave.  
            //if (instanceSave != null)
            // Update:  We used to only check this when first creating a Variable, but
            // there's no harm in forcing the source object.  Let's do that.
            // Update:  Turns out we do need the instance so that we can get the base type
            // to find out if the variable IsFile or not.  If the InstanceSave is null, but 
            // we have a sourceObjectName that we determine by the presence of a dot, then let's
            // try to find the InstanceSave
            if (variableName.Contains('.'))
            {
                string rootName = variableSave.Name.Substring(variableSave.Name.IndexOf('.') + 1);
                string sourceObjectName = variableSave.Name.Substring(0, variableSave.Name.IndexOf('.'));

                if (instanceSave == null && stateSave.ParentContainer != null)
                {
                    instanceSave = stateSave.ParentContainer.GetInstance(sourceObjectName);
                }

                //ElementSave baseElement = ObjectFinder.Self.GetRootStandardElementSave(instanceSave);

                //VariableSave baseVariableSave = baseElement.DefaultState.GetVariableSave(rootName);
                if (instanceSave != null)
                {
                    VariableSave baseVariableSave = ObjectFinder.Self.GetRootStandardElementSave(instanceSave).DefaultState.GetVariableSave(rootName);
                    variableSave.IsFile = baseVariableSave.IsFile;
                }
                variableSave.SourceObject = sourceObjectName;
            }

            variableSave.SetsValue = true;

            variableSave.Value = value;
        }

        public static StateSave Clone(this StateSave whatToClone)
        {
            return whatToClone.Clone<StateSave>();

        }

        public static T Clone<T>(this StateSave whatToClone) where T : StateSave
        {
            T toReturn = FileManager.CloneSaveObjectCast<StateSave, T>(whatToClone);

            toReturn.Variables.Clear();
            foreach (VariableSave vs in whatToClone.Variables)
            {
                toReturn.Variables.Add(vs.Clone());
            }



            // do we also want to clone VariableSaveLists?  Not sure at this point

            return toReturn;

        }

        public static void SetFrom(this StateSave stateSave, StateSave otherStateSave)
        {
            stateSave.Name = otherStateSave.Name;
            // We don't want to do this because the otherStateSave may not have a parent
            //stateSave.ParentContainer = otherStateSave.ParentContainer;

            stateSave.Variables.Clear();
            stateSave.VariableLists.Clear();

            foreach (VariableSave variable in otherStateSave.Variables)
            {
                stateSave.Variables.Add(FileManager.CloneSaveObject(variable));
            }

            foreach (VariableListSave variableList in otherStateSave.VariableLists)
            {
                stateSave.VariableLists.Add(FileManager.CloneSaveObject(variableList));
            }

            stateSave.FixEnumerations();
        }

        public static void FixEnumerations(this StateSave stateSave)
        {
            foreach (VariableSave variable in stateSave.Variables)
            {
                variable.FixEnumerations();
            }

            // Do w want to fix enums here?
            //foreach (VariableListSave variableList in otherStateSave.VariableLists)
            //{
            //    stateSave.VariableLists.Add(FileManager.CloneSaveObject(variableList));
            //}


        }
    }
}