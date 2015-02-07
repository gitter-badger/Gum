﻿using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.Managers
{
    public class NameVerifier
    {

        public static char[] InvalidCharacters =
                new char[] 
            { 
                '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', 
                '(', ')', '-', '=', '+', ';', '\'', ':', '"', '<', 
                ',', '>', '.', '/', '\\', '?', '[', '{', ']', '}', 
                '|', 
                // Spaces are handled separately
            //    ' ' 
            };

        static NameVerifier mSelf;

        public static NameVerifier Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new NameVerifier();
                }
                return mSelf;
            }
        }


        public bool IsFolderNameValid(string folderName, out string whyNotValid)
        {

            IsNameValidCommon(folderName, out whyNotValid);

            return string.IsNullOrEmpty(whyNotValid);

        }

        public bool IsScreenNameValid(string screenName, ScreenSave screen, out string whyNotValid)
        {
            IsNameValidCommon(screenName, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsNameAnExistingElement(screenName, screen, out whyNotValid);
            }

            return string.IsNullOrEmpty(whyNotValid);

        }

        public bool IsComponentNameValid(string componentName, ComponentSave component, out string whyNotValid)
        {
            IsNameValidCommon(componentName, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsNameAnExistingElement(componentName, component, out whyNotValid);
            }

            return string.IsNullOrEmpty(whyNotValid);
        }

        public bool IsInstanceNameValid(string instanceName, InstanceSave instanceSave, ElementSave elementSave, out string whyNotValid)
        {
            IsNameValidCommon(instanceName, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsNameAlreadyUsed(instanceName, instanceSave, elementSave, out whyNotValid);
            }
            return string.IsNullOrEmpty(whyNotValid);
        }

        public bool IsExposedVariableNameValid(string variableName, ElementSave elementSave, out string whyNotValid)
        {
            whyNotValid = null;

            IsNameValidCommon(variableName, out whyNotValid);

            if (string.IsNullOrEmpty(whyNotValid))
            {
                IsNameAlreadyUsed(variableName, null, elementSave, out whyNotValid);
            }

            return string.IsNullOrEmpty(whyNotValid);
        }

        private void IsNameValidCommon(string name, out string whyNotValid)
        {
            whyNotValid = null;
            if (string.IsNullOrEmpty(name))
            {
                whyNotValid = "Empty names are not valid";
            }
            else if (name.IndexOfAny(InvalidCharacters) != -1)
            {
                whyNotValid = "The name can't contain invalid character " + name[name.IndexOfAny(InvalidCharacters)];
            }
        }

        private void IsNameAlreadyUsed(string name, object objectToIgnore, ElementSave elementSave, out string whyNotValid)
        {
            whyNotValid = null;

            if (objectToIgnore != elementSave && name == elementSave.Name)
            {
                whyNotValid = "The element is named " + elementSave.Name;
            }

            var instance = elementSave.Instances.FirstOrDefault(item => item != objectToIgnore && item.Name == name);
            if (instance != null)
            {
                whyNotValid = "There is already an instance named " + instance.ToString();
            }

            var state = elementSave.States.FirstOrDefault(item => item != objectToIgnore && item.Name == name);
            if (state != null)
            {
                whyNotValid = "There is already a state named " + state.ToString();
            }
            
            var variable = elementSave.AllStates.SelectMany(item => item.Variables).FirstOrDefault(item => item != objectToIgnore && item.ExposedAsName == name);
            if (variable != null)
            {
                whyNotValid = "There is already a variable named " + variable.ToString();
            }

            //element = ObjectFinder.Self.GumProjectSave.StandardElements.FirstOrDefault(item=>item != objectToIgnore && item.Name == name)
            //{
            //    whyNotValid = "There is a standard element named " + element.Name + " so this name can't be used.";
            //}

        }

        private void IsNameAnExistingElement(string name, object objectToIgnore, out string whyNotValid)
        {
            whyNotValid = null;

            var standardElement = ObjectFinder.Self.GumProjectSave.StandardElements.FirstOrDefault(item =>
                item != objectToIgnore && item.Name == name);
            if (standardElement != null)
            {
                whyNotValid = "There is a standard element named " + standardElement.Name + " so this name can't be used.";
            }

            var component = ObjectFinder.Self.GumProjectSave.Components.FirstOrDefault(item =>
                item != objectToIgnore && item.Name == name);
            if (component != null)
            {
                whyNotValid = "There is a component named " + component.Name + " so this name can't be used.";
            }

            var screen = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item =>
                item != objectToIgnore && item.Name == name);
            if (screen != null)
            {
                whyNotValid = "There is a screen named " + screen.Name + " so this name can't be used.";
            }
        }

    }
}
