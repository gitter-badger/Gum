﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.ToolStates;

namespace Gum.DataTypes.ComponentModel
{

    public delegate object GetValueDelegate(object component);
    public delegate void SetValueDelegate(object component, object value);

    public class InstanceSavePropertyDescriptor : PropertyDescriptor
    {
        #region Fields

        Type mParameterType;

		TypeConverter mTypeConverter;


        #endregion

        #region Properties


        public TypeConverter TypeConverter
        {
            get { return Converter; }
            set { mTypeConverter = value; }
        }

        public override TypeConverter Converter
        {
            get
            {
                if (mTypeConverter == null)
                {
                    if (mParameterType == typeof(bool))
                    {
                        return TypeDescriptor.GetConverter(typeof(bool));
                    }
                    else
                    {
                        return TypeDescriptor.GetConverter(typeof(string));
                    }
                }
                else
                {
                    return mTypeConverter;
                }
            }
        }


        public object Owner
		{
			get;
			set;
		}

        #endregion

        public InstanceSavePropertyDescriptor(string name, Type type, Attribute[] attrs)
            : base(name, attrs)
        {
			mParameterType = type;
        }


        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get 
            {

                return mParameterType;
                // add more things here
				//return typeof(string);
            
            }
        }

        public override object GetValue(object component)
        {

            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            if (stateSave != null)
            {
                string name = GetVariableNameConsideringSelection();
                return stateSave.GetValue(name);
            }
            else
            {
                return null;
            }
        }

        private string GetVariableNameConsideringSelection()
        {
            string name = this.Name;
            if (SelectedState.Self.SelectedInstance != null)
            {
                name = SelectedState.Self.SelectedInstance.Name + "." + name;
            }
            return name;
        }



        public override void SetValue(object component, object value)
        {


            
            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            InstanceSave instanceSave = SelectedState.Self.SelectedInstance;

            string name = GetVariableNameConsideringSelection();

            // <None> is a reserved 
            // value for when we want
            // to allow the user to reset
            // a value through a combo box.
            // If the value is "<None>" then 
            // let's set it to null
            if (value is string && ((string)value) == "<None>")
            {
                value = null;
            }

            stateSave.SetValue(name, value, instanceSave);
        }




        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get 
			{
				if (mParameterType == typeof(bool))
				{
					return typeof(bool);
				}
				else
				{
					return typeof(string);
				}
			}
        }

        public override void ResetValue(object component)
        {
            
            // do nothing here
        }


        //private void SetEventValue(string categoryString, IEventContainer eventContainer, string value)
        //{
        //    switch (categoryString)
        //    {
        //        case "Active Events":

        //            EventResponseSave eventToModify = null;

        //            for (int i = eventContainer.Events.Count - 1; i > -1; i--)
        //            {
        //                if (eventContainer.Events[i].EventName == this.Name)
        //                {
        //                    eventToModify = eventContainer.Events[i];
        //                    break;
        //                }
        //            }

        //            if (eventToModify == null)
        //            {
        //                throw new Exception("Could not find an event by the name of " + Name);
        //            }
        //            else
        //            {
        //                string valueAsString = value;

        //                if (string.IsNullOrEmpty(valueAsString) || valueAsString == "<NONE>")
        //                {
        //                    eventContainer.Events.Remove(eventToModify);
        //                }
        //                else
        //                {
        //                    //eventToModify.InstanceMethod = valueAsString;
        //                }
        //            }
        //            //EventSave eventSave = EditorLogic.Current
        //            break;
        //        case "Unused Events":

        //            //EventSave eventSave = EventManager.AllEvents[Name];

        //            //eventSave.InstanceMethod = value;

        //            //EditorLogic.CurrentEventContainer.Events.Add(eventSave);
        //            break;
        //    }
        //}

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override string ToString()
        {
            return mParameterType + " " + Name;
        }
    
    }
}