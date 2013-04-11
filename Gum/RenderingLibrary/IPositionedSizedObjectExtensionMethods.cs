﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Managers;
using Gum.Converters;

namespace Gum.RenderingLibrary
{
    public static class IPositionedSizedObjectExtensionMethods
    {
        public static void UpdateAccordingToPercentages(this IPositionedSizedObject ipso, 
            ElementSave containerElement,
            float unmodifiedX, float unmodifiedY, 
            object xUnitType, object yUnitType,
            //string xVariable, string yVariable,
            float canvasWidth, float canvasHeight,
            out float x, out float y)
        {

            string qualifiedVariablePrefixWithDot = GetQualifiedPrefixWithDot(ipso, WireframeObjectManager.Self.GetElement(ipso), containerElement);

            float parentWidth;
            float parentHeight;
            GetParentWidthAndHeight(ipso, canvasWidth, canvasHeight, out parentWidth, out parentHeight);


            UnitConverter.Self.ConvertToPixelCoordinates(unmodifiedX, unmodifiedY, xUnitType, yUnitType, parentWidth, parentHeight, out x, out y);

        }

        private static void GetParentWidthAndHeight(IPositionedSizedObject ipso, float canvasWidth, float canvasHeight, out float parentWidth, out float parentHeight)
        {


            if (ipso.Parent == null)
            {
                parentWidth = canvasWidth;
                parentHeight = canvasHeight;
            }
            else
            {
                parentWidth = ipso.Parent.Width;
                parentHeight = ipso.Parent.Height;
            }
        }

        public static string GetQualifiedPrefixWithDot(IPositionedSizedObject ipso, ElementSave elementSaveForIpso, ElementSave containerElement)
        {
            string qualifiedVariablePrefixWithDot = ipso.Name + ".";

            IPositionedSizedObject parent = ipso.Parent;

            while (parent != null)
            {
                qualifiedVariablePrefixWithDot = parent.Name + "." + qualifiedVariablePrefixWithDot;
                parent = parent.Parent;
            }

            if (
                // Not sure why we checked to make sure it doesn't contain a dot.  It always will because
                // of the first line:
                //!qualifiedVariablePrefixWithDot.Contains(".") && 
                elementSaveForIpso == containerElement)
            {
                qualifiedVariablePrefixWithDot = "";
            }
            if (containerElement is ComponentSave || containerElement is StandardElementSave)
            {
                // If the containerElement is a ComponentSave, then we assume (currently) that the ipso is attached to it.  Since we'll be using
                // the container element to get the variable, we don't want to include the name of the element, so let's get rid of the first part.
                // Update June 13, 2012
                // If we pass a Text object
                // that is the child of a Button
                // and the Button is the container
                // element, we want to return "TextInstance."
                // as the prefix.  The code below prevents that.
                // I now have unit tests for this so I'm going to
                // remove the code below and modify it later if it
                // turns out we really do need it.
                // Update June 13, 2012
                // Now we climb up the parent/child
                // relationship until we get to the root
                // If the containerElement is a ComponentSave
                // or StandardElementSave (although maybe this
                // case will never happen) we need to remove the
                // first name before the dot because it's the element
                // itself.  If it's a Screen there is no attachment so
                // the first name won't be the name of the Screen.
                int indexOfDot = qualifiedVariablePrefixWithDot.IndexOf('.');
                if (indexOfDot != -1)
                {
                    qualifiedVariablePrefixWithDot = qualifiedVariablePrefixWithDot.Substring(indexOfDot + 1, qualifiedVariablePrefixWithDot.Length - (indexOfDot + 1));
                }
            }


            return qualifiedVariablePrefixWithDot;
        }




    }
}