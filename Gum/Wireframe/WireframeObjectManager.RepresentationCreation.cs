﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary.Graphics;
using RenderingLibrary.Content;
using Gum.DataTypes.Variables;
using RenderingLibrary.Math.Geometry;
using Gum.DataTypes;
using Gum.RenderingLibrary;
using Gum.ToolStates;
using RenderingLibrary.Graphics.Fonts;
using System.Collections;
using ToolsUtilities;

namespace Gum.Wireframe
{
    public partial class WireframeObjectManager
    {

        #region Fields


        public string[] PositionAndSizeVariables = new string[]{
            "Width",
            "Height",
            "Width Units",
            "Height Units",
            "X Origin",
            "Y Origin",
            "X",
            "Y",
            "X Units",
            "Y Units",
            "Guide"
        
        };

        public string[] ColorAndAlpha = new string[]{
            "Red",
            "Green",
            "Blue",
            "Alpha"
        };

        public string[] Color = new string[]{
            "Red",
            "Green",
            "Blue"
        };

        #endregion


        private IPositionedSizedObject CreateRepresentationForInstance(InstanceSave instance, InstanceSave parentInstance, List<ElementSave> elementStack, IPositionedSizedObject parentIpso)
        {
            IPositionedSizedObject toReturn = null;

            try
            {
                List<VariableSave> exposedVariables = GetExposedVariablesForThisInstance(instance, parentInstance, elementStack);


                if (instance.BaseType == "Sprite")
                {
                    toReturn = CreateSpriteFor(instance, elementStack.Last(), parentIpso, exposedVariables);
                }
                else if (instance.BaseType == "Text")
                {
                    toReturn = CreateTextFor(instance, elementStack.Last(), parentIpso, exposedVariables);
                }
                else if (instance.BaseType == "ColoredRectangle")
                {
                    toReturn = CreateSolidRectangleFor(instance, elementStack.Last(), parentIpso, exposedVariables);
                }
                else if (instance.IsComponent())
                {
                    // Here we need to prefix the name before going any deeper, so we won't won't prefix the name at the end of the method
                    //shouldPrefixParentName = false;

                    //string prefixName = null;
                    //if (parentIpso != null)
                    //{
                    //prefixName = parentIpso.Name + ".";
                    //}
                    toReturn = CreateRepresentationsForInstanceFromComponent(instance, elementStack, parentInstance, parentIpso, ObjectFinder.Self.GetComponent(instance.BaseType));
                }
                else
                {
                    toReturn = CreateRectangleFor(instance, elementStack.Last(), parentIpso, exposedVariables);
                }
                //if (shouldPrefixParentName)
                //{
                //    string prefixName = null;
                //    if (parentIpso != null)
                //    {
                //        prefixName = parentIpso.Name + ".";
                //    }

                //    toReturn.Name = prefixName + toReturn.Name;

                //}
            }
            catch (Exception e)
            {
                int m = 3;
                throw e;
            }
            return toReturn;
        }

        private List<VariableSave> GetExposedVariablesForThisInstance(DataTypes.InstanceSave instance, InstanceSave parentInstance, List<DataTypes.ElementSave> elementStack)
        {
            List<VariableSave> exposedVariables = new List<VariableSave>();
            if (elementStack.Count > 1)
            {
                ElementSave containerOfVariables = elementStack[elementStack.Count - 2];
                ElementSave definerOfVariables = elementStack[elementStack.Count - 1];

                foreach (VariableSave variable in definerOfVariables.DefaultState.Variables)
                {
                    if (!string.IsNullOrEmpty(variable.ExposedAsName) && variable.SourceObject == instance.Name)
                    {
                        // This variable is exposed, let's see if the container does anything with it

                        VariableSave foundVariable = containerOfVariables.DefaultState.GetVariableSave(parentInstance.Name + "." + variable.ExposedAsName);

                        if (foundVariable != null)
                        {
                            VariableSave variableToAdd = new VariableSave();
                            variableToAdd.Type = variable.Type;
                            variableToAdd.Value = foundVariable.Value;
                            variableToAdd.Name = variable.Name.Substring(variable.Name.IndexOf('.') + 1);
                            exposedVariables.Add(variableToAdd);
                        }

                    }

                }

            }

            return exposedVariables;
        }

        private IPositionedSizedObject CreateRepresentationsForInstanceFromComponent(InstanceSave instance, List<ElementSave> elementStack, InstanceSave parentInstance, IPositionedSizedObject parentIpso, ComponentSave baseComponentSave)
        {
            StandardElementSave ses = ObjectFinder.Self.GetRootStandardElementSave(instance);

            List<VariableSave> exposedVariables = GetExposedVariablesForThisInstance(instance, parentInstance, elementStack);

            IPositionedSizedObject rootIpso = null;

            if (ses.Name == "Sprite")
            {
                rootIpso = CreateSpriteFor(instance, elementStack.Last(), parentIpso, exposedVariables);
            }
            else if (ses.Name == "Text")
            {
                rootIpso = CreateTextFor(instance, elementStack.Last(), parentIpso, exposedVariables);
            }
            else if (ses.Name == "ColoredRectangle")
            {
                rootIpso = CreateSolidRectangleFor(instance, elementStack.Last(), parentIpso, exposedVariables);
            }
            else
            {
                rootIpso = CreateRectangleFor(instance, elementStack.Last(), parentIpso, exposedVariables);
            }

            elementStack.Add(baseComponentSave);

            foreach (InstanceSave internalInstance in baseComponentSave.Instances)
            {
                IPositionedSizedObject createdIpso = CreateRepresentationForInstance(internalInstance, instance, elementStack, rootIpso);

            }

            elementStack.Remove(baseComponentSave);

            return rootIpso;
        }



        private static string MakeAbsoluteIfNecessary(string textureAsString)
        {
            if (!string.IsNullOrEmpty(textureAsString) && FileManager.IsRelative(textureAsString))
            {
                textureAsString = FileManager.RemoveDotDotSlash(FileManager.RelativeDirectory + textureAsString);
            }
            return textureAsString;
        }
        private IPositionedSizedObject CreateSpriteFor(ElementSave elementSave)
        {
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);
            StateSave stateSave;

            Sprite sprite = CreateSpriteInternal(elementSave, elementSave.Name, rvf, null, out stateSave);
      

            InitializeSprite(sprite, stateSave);

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(sprite, elementSave, stateSave);
            
            return sprite;
        }


        private IPositionedSizedObject CreateSpriteFor(InstanceSave instance, ElementSave parent, IPositionedSizedObject parentRepresentation, List<VariableSave> exposedVariables)
        {
            try
            {
                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, parent);
                StateSave stateSave;
                Sprite sprite = CreateSpriteInternal(instance, instance.Name, rvf, parentRepresentation, out stateSave);
                
                foreach (VariableSave variableSave in exposedVariables)
                {
                    stateSave.SetValue(variableSave.Name, variableSave.Value);
                }


                InitializeSprite(sprite, stateSave);

                // Sprite may be dependent on the texture for its location, so set the dimensions and positions *after* texture
                SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(sprite, parent, stateSave);



                return sprite;
            }
            catch (Exception e)
            {
                int m = 3;
                throw e;
            }
        }

        private Sprite CreateSpriteInternal(object tag, string name, RecursiveVariableFinder rvf, IPositionedSizedObject parentIpso, out StateSave stateSave)
        {
            Sprite sprite = new Sprite(LoaderManager.Self.InvalidTexture);

            // Add it to the manager first because the positioning code may need to access the source element/instance
            SpriteManager.Self.Add(sprite);
            sprite.Name = name;
            sprite.Tag = tag;

            mSprites.Add(sprite);

            stateSave = new StateSave();
            FillStateSaveWithFileBasedSpriteVariables(rvf, stateSave);

            stateSave.SetValue("Visible", rvf.GetValue("Visible"));

            SetParent(parentIpso, sprite, rvf.GetValue<string>("Guide"));

            // Sprite may be dependent on the texture for its location, so set the dimensions and positions *after* texture
            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);

            return sprite;
        }

        private static void FillStateSaveWithFileBasedSpriteVariables(RecursiveVariableFinder rvf, StateSave stateSave)
        {
            stateSave.SetValue("SourceFile", rvf.GetValue<string>("SourceFile"));
            stateSave.SetValue("Animate", rvf.GetValue<bool>("Animate"));
            stateSave.SetValue("AnimationFrames", rvf.GetValue<List<string>>("AnimationFrames"));
        }

        private IPositionedSizedObject CreateSolidRectangleFor(ElementSave elementSave)
        {
            SolidRectangle solidRectangle = InstantiateAndNameSolidRectangle(elementSave.Name);
            solidRectangle.Tag = elementSave;
            StateSave stateSave = new StateSave();

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.ColorAndAlpha);


            SetParent(null, solidRectangle, (string)stateSave.GetValue("Guide"));

            SetAlphaAndColorValues(solidRectangle, stateSave);
            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(solidRectangle, elementSave, stateSave);

            return solidRectangle;           
        }
        private IPositionedSizedObject CreateSolidRectangleFor(InstanceSave instance, ElementSave parent, List<VariableSave> exposedVariables)
        {

            IPositionedSizedObject parentIpso = GetRepresentation(parent);

            return CreateSolidRectangleFor(instance, parent, parentIpso, exposedVariables);
        }
        private IPositionedSizedObject CreateSolidRectangleFor(InstanceSave instance, ElementSave parent, IPositionedSizedObject parentIpso, List<VariableSave> exposedVariables)
        {
            if (exposedVariables == null)
            {
                throw new Exception("The exposedVariables argument is null when trying to create a Rectangle.  It shouldn't be.");
            }

            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            SolidRectangle solidRectangle = InstantiateAndNameSolidRectangle(instance.Name);
            solidRectangle.Tag = instance;


            StateSave stateSave = new StateSave();

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, parent);

            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.ColorAndAlpha);


            foreach (VariableSave exposed in exposedVariables)
            {
                stateSave.SetValue(exposed.Name, exposed.Value);
            }



            SetParent(parentIpso, solidRectangle, (string)stateSave.GetValue("Guide"));


            SetAlphaAndColorValues(solidRectangle, stateSave);
            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(solidRectangle, parent, stateSave);

            return solidRectangle;
        }
        private void SetAlphaAndColorValues(SolidRectangle solidRectangle, StateSave stateSave)
        {
            Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(
                (float)stateSave.GetValue("Red"),
                (float)stateSave.GetValue("Green"),
                (float)stateSave.GetValue("Blue"),
                (float)stateSave.GetValue("Alpha")
                
                );
            solidRectangle.Color = color;
        }
        private SolidRectangle InstantiateAndNameSolidRectangle(string name)
        {
            SolidRectangle solidRectangle = new SolidRectangle();

            // Add it to the manager first because the positioning code may need to access the source element/instance
            ShapeManager.Self.Add(solidRectangle);
            solidRectangle.Name = name;
            mSolidRectangles.Add(solidRectangle);
            return solidRectangle;
        }



        private IPositionedSizedObject CreateRectangleFor(ElementSave elementSave)
        {
            LineRectangle lineRectangle = InstantiateAndNameRectangle(elementSave.Name);
            lineRectangle.Tag = elementSave;
            StateSave stateSave = new StateSave();

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);


            SetParent(null, lineRectangle, (string)stateSave.GetValue("Guide"));

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(lineRectangle, elementSave, stateSave);



            return lineRectangle;
        }
        private IPositionedSizedObject CreateRectangleFor(InstanceSave instance, ElementSave parent, IPositionedSizedObject parentIpso, List<VariableSave> exposedVariables)
        {
            if (exposedVariables == null)
            {
                throw new Exception("The exposedVariables argument is null when trying to create a Rectangle.  It shouldn't be.");
            }

            ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

            LineRectangle lineRectangle = InstantiateAndNameRectangle(instance.Name);
            lineRectangle.Tag = instance;

            StateSave stateSave = new StateSave();
            // Get the variables before attaching to the guide

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, parent);

            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);


            SetParent(parentIpso, lineRectangle, (string)stateSave.GetValue("Guide"));

            if (exposedVariables == null)
            {
                int m = 3;
            }

            foreach (VariableSave exposed in exposedVariables)
            {
                stateSave.SetValue(exposed.Name, exposed.Value);
            }


            WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(lineRectangle, parent, stateSave);
            
            return lineRectangle;
        }
        private LineRectangle InstantiateAndNameRectangle(string name)
        {
            LineRectangle lineRectangle = new LineRectangle();

            // Add it to the manager first because the positioning code may need to access the source element/instance
            ShapeManager.Self.Add(lineRectangle);
            lineRectangle.Name = name;
            mLineRectangles.Add(lineRectangle);
            return lineRectangle;
        }

        private void SetParent(IPositionedSizedObject parentIpso, IPositionedSizedObject ipso, string guideName)
        {
            ipso.Parent = parentIpso;

            if (!string.IsNullOrEmpty(guideName))
            {
                IPositionedSizedObject guideIpso = GetGuide(guideName);

                if (guideIpso != null)
                {
                    ipso.Parent = guideIpso;
                }
            }
        }



        private IPositionedSizedObject CreateTextFor(ElementSave elementSave)
        {
            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(elementSave.DefaultState);

            Text text = CreateTextInternal(elementSave, elementSave.Name, rvf);

            StateSave stateSave = new StateSave();

            FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);

            SetParent(null, text, (string)stateSave.GetValue("Guide"));

            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(text, elementSave, stateSave);



            text.EnableTextureCreation();

            return text;
        }
        private IPositionedSizedObject CreateTextFor(InstanceSave instance, ElementSave parent, IPositionedSizedObject parentRepresentation, List<VariableSave> exposedVariables)
        {
            try
            {
                ElementSave instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);
                RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, parent);

                Text text = CreateTextInternal(instance, instance.Name, rvf);

                // First we get the values from the base type
                StateSave stateSave = GetStateSaveForTextVariables(instance, parent);

                if (exposedVariables == null)
                {
                    throw new ArgumentException("The exposedVariable argument is null.  It needs to be non-null");
                }

                // Then see if they're overridden by the exposed variables
                foreach (VariableSave variable in exposedVariables)
                {
                    stateSave.SetValue(variable.Name, variable.Value);
                }

                SetParent(parentRepresentation, text, (string)stateSave.GetValue("Guide"));

                WireframeObjectManager.Self.SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(text, parent, stateSave);

                return text;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private Text CreateTextInternal(object tag, string name, RecursiveVariableFinder rvf)
        {
            Text text = new Text(null);
            text.SuppressTextureCreation();
            text.Tag = tag;
            text.Name = name;

            // Add it to the manager first because the positioning code may need to access the source element/instance
            TextManager.Self.Add(text);
            mTexts.Add(text);

                //text.Alpha = (float)stateSave.GetValue("Alpha");
            text.Red = rvf.GetValue<float>("Red");
            text.Green = rvf.GetValue<float>("Green");
            text.Blue = rvf.GetValue<float>("Blue");

            text.Visible = rvf.GetValue<bool>("Visible");



            text.RawText = (string)rvf.GetValue("Text");
            text.HorizontalAlignment = (HorizontalAlignment)rvf.GetValue("HorizontalAlignment");
            text.VerticalAlignment = (VerticalAlignment)rvf.GetValue("VerticalAlignment");


            string fontName = (string)rvf.GetValue("Font");
            int fontSize = (int)rvf.GetValue("FontSize");

            BmfcSave.CreateBitmapFontFilesIfNecessary(
                fontSize,
                fontName);

            text.BitmapFont = GetBitmapFontFor(fontName, fontSize);
            text.EnableTextureCreation();
            return text;
        }



        private static StateSave GetStateSaveForTextVariables(InstanceSave instance, ElementSave parent)
        {
            StateSave stateSave = new StateSave();

            AddToStateFromInstance(stateSave, instance, parent, "Text",
                "HorizontalAlignment",
                "VerticalAlignment",
                "Font",
                "FontSize",
                //"Alpha",
                "Red",
                "Green",
                "Blue");

            RecursiveVariableFinder rvf = new DataTypes.RecursiveVariableFinder(instance, parent);
            WireframeObjectManager.Self.FillStateWithVariables(rvf, stateSave, WireframeObjectManager.Self.PositionAndSizeVariables);
            return stateSave;
        }

        static void AddToStateFromInstance(StateSave stateSave, InstanceSave instance, ElementSave parent, params string[] variables)
        {
            foreach (string variable in variables)
            {
                var value = instance.GetValueFromThisOrBase(parent, variable);

                stateSave.SetValue(variable, value);
            }
        }

        private BitmapFont GetBitmapFontFor(string fontName, int fontSize)
        {
            string fileName = FileManager.RelativeDirectory + "Font" + fontSize + fontName + ".fnt";

            if (System.IO.File.Exists(fileName))
            {
                try
                {
                    BitmapFont bitmapFont = new BitmapFont(fileName, (SystemManagers)null);

                    return bitmapFont;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }



        public void SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(IPositionedSizedObject ipso, ElementSave containerElement, StateSave stateSave)
        {
            object xUnits = stateSave.GetValue("X Units");
            object widthUnits = stateSave.GetValue("Width Units");
#if DEBUG
            if (xUnits is int)
            {
                throw new Exception("X Units must not be an int - must be an enum");
            }
            if (widthUnits is int)
            {
                throw new Exception("Width Units must not be an int - must be an enum");
            }
#endif



            SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(
                ipso,
                containerElement,
                (float)stateSave.GetValue("Width"),
                (float)stateSave.GetValue("Height"),
                stateSave.GetValue("Width Units"),
                stateSave.GetValue("Height Units"),
                (HorizontalAlignment)stateSave.GetValue("X Origin"),
                (VerticalAlignment)stateSave.GetValue("Y Origin"),
                (float)stateSave.GetValue("X"),
                (float)stateSave.GetValue("Y"),
                xUnits,
                stateSave.GetValue("Y Units")
                
                
                );
        }

        private static void SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(IPositionedSizedObject ipso, ElementSave containerElement, 
            float widthBeforePercentage, float heightBeforePercentage, 
            object widthUnitType, object heightUnitType,
            HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, float xBeforePercentage, float yBeforePercentage,
            object xUnitType, object yUnitType
            )
        {
            try
            {
                float widthAfterPercentage;
                float heightAfterPercentage;

                GumProjectSave gumProjectSave = ObjectFinder.Self.GumProjectSave;

                ipso.UpdateAccordingToPercentages(
                    containerElement,

                    widthBeforePercentage,
                    heightBeforePercentage, widthUnitType, heightUnitType,
                    gumProjectSave.DefaultCanvasWidth,
                    gumProjectSave.DefaultCanvasHeight,
                    out widthAfterPercentage, out heightAfterPercentage);

                ipso.Width = widthAfterPercentage;
                ipso.Height = heightAfterPercentage;

                float multiplierX;
                float multiplierY;
                GetMultipliersFromAlignment(horizontalAlignment, verticalAlignment, out multiplierX, out multiplierY);

                float xAfterPercentage;
                float yAfterPercentage;
                ipso.UpdateAccordingToPercentages(
                    containerElement,
                    xBeforePercentage,
                    yBeforePercentage,

                    xUnitType, yUnitType,
                    gumProjectSave.DefaultCanvasWidth,
                    gumProjectSave.DefaultCanvasHeight,
                    out xAfterPercentage, out yAfterPercentage);

                ipso.X = xAfterPercentage + multiplierX * ipso.Width;
                ipso.Y = yAfterPercentage + multiplierY * ipso.Height;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static void GetMultipliersFromAlignment(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, out float multiplierX, out float multiplierY)
        {
            multiplierX = 0;
            multiplierY = 0;
            if (horizontalAlignment == HorizontalAlignment.Center)
            {
                multiplierX = -.5f;
            }
            else if (horizontalAlignment == HorizontalAlignment.Right)
            {
                multiplierX = -1;
            }
            if (verticalAlignment == VerticalAlignment.Center)
            {
                multiplierY = -.5f;
            }
            if (verticalAlignment == VerticalAlignment.Bottom)
            {
                multiplierY = -1;
            }
        }


        //public void SetInstanceIpsoDimensionsAndPositions(IPositionedSizedObject ipso, InstanceSave instance, ElementSave parent, IPositionedSizedObject parentRepresentation)
        //{
        //    ipso.Parent = parentRepresentation;

        //    FillStateWithPositionAndSizeVariables(instance, parent, temporaryStateSave);
        //    SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, parent, temporaryStateSave);
        //}

        StateSave temporaryStateSave = new StateSave();

        //public void UpdateIpsoPositionAndScaleAccordingToParent(IPositionedSizedObject ipso, InstanceSave instance, ElementSave parent)
        //{
        //    StateSave stateSave = temporaryStateSave;

        //    FillStateWithPositionAndSizeVariables(instance, parent, stateSave);

        //    SetIpsoWidthAndPositionAccordingToUnitValueAndTypes(ipso, parent, temporaryStateSave);
        //}



        //public void FillStateWithPositionAndSizeVariables(ElementSave elementSave, StateSave stateSave)
        //{
        //    FillStateWithVariables(elementSave, stateSave, PositionAndSizeVariables);
        //}
        //public void FillStateWithPositionAndSizeVariables(InstanceSave instance, ElementSave parent, StateSave stateSave)
        //{
        //    FillStateWithVariables(instance, parent, stateSave, PositionAndSizeVariables);
        //}
        public void FillStateWithVariables(RecursiveVariableFinder rvf, StateSave stateSave, IEnumerable variables)
        {
            foreach (string variable in variables)
            {
                stateSave.SetValue(variable, rvf.GetValue(variable));
            }
        }
        
        private void InitializeSprite(Sprite sprite, StateSave stateSave)
        {
            string textureName = (string)stateSave.GetValue("SourceFile");
            bool animate = (bool)stateSave.GetValue("Animate");
            List<string> animations = (List<string>)stateSave.GetValue("AnimationFrames");

            sprite.Animate = animate;

            textureName = MakeAbsoluteIfNecessary(textureName);

            if (animations != null && animations.Count != 0)
            {
                for (int i = 0; i < animations.Count; i++)
                {
                    animations[i] = MakeAbsoluteIfNecessary(animations[i]);
                }

                TextureFlipAnimation tfa = TextureFlipAnimation.FromStringList(animations, null);
                
                sprite.Animation = tfa;

            }
            else
            {
                if (!string.IsNullOrEmpty(textureName))
                {
                    if (System.IO.File.Exists(textureName))
                    {
                        string error;
                        sprite.Texture = LoaderManager.Self.LoadOrInvalid(textureName, null, out error);
                        // Do we want to print this out somewhere?  Well, for now we'll just
                        // tolearte it silently because the invalid texture is shown
                    }
                    else
                    {
                        // The file doesn't exist, let's give this Sprite the null texture
                        sprite.Texture = LoaderManager.Self.InvalidTexture;
                    }
                }
            }

            sprite.Visible = (bool)stateSave.GetValue("Visible");

        }

    }
}