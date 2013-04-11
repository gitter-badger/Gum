﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Content;
using Microsoft.Xna.Framework;
using RenderingLibrary.Math.Geometry;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.ObjectModel;

namespace RenderingLibrary.Graphics
{
    #region HorizontalAlignment Enum

    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    #endregion

    #region VerticalAlignment Enum

    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom
    }

    #endregion

    public class Text : IPositionedSizedObject, IRenderable, IVisible
    {
        #region Fields

        public Vector2 Position;

        public Color Color = Color.White;

        string mRawText;
        List<string> mWrappedText = new List<string>();
        float mWidth = 200;
        float mHeight = 200;
        LinePrimitive mBounds;

        BitmapFont mBitmapFont;
        Texture2D mTextureToRender;

        IPositionedSizedObject mParent;

        List<IPositionedSizedObject> mChildren;

        float mAlpha = 1;
        float mRed = 1;
        float mGreen = 1;
        float mBlue = 1;

        public bool mIsTextureCreationSuppressed;

        SystemManagers mManagers;

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }

        public string RawText
        {
            get
            {
                return mRawText;
            }
            set
            {
                mRawText = value;
                UpdateWrappedText();

                UpdateTextureToRender();
            }
        }

        public List<string> WrappedText
        {
            get
            {
                return mWrappedText;
            }
        }

        public float X
        {
            get
            {
                return Position.X;
            }
            set
            {
                Position.X = value;
            }
        }

        public float Y
        {
            get
            {
                return Position.Y;
            }
            set
            {
                Position.Y = value;
            }
        }

        public float Width
        {
            get
            {
                return mWidth;
            }
            set
            {
                mWidth = value;
                UpdateWrappedText();
                UpdateLinePrimitive();
            }
        }

        public float Height
        {
            get
            {
                return mHeight;
            }
            set
            {
                mHeight = value;
                UpdateLinePrimitive();
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get;
            set;
        }

        public VerticalAlignment VerticalAlignment
        {
            get;
            set;
        }

        public IPositionedSizedObject Parent
        {
            get { return mParent; }
            set 
            {
                if (mParent != value)
                {
                    if (mParent != null)
                    {
                        mParent.Children.Remove(this);
                    }
                    mParent = value;
                    if (mParent != null)
                    {
                        mParent.Children.Add(this);
                    }
                }
            }
        }

        public float Z
        {
            get;
            set;
        }

        public BitmapFont BitmapFont
        {
            get
            {
                return mBitmapFont;
            }
            set
            {
                mBitmapFont = value;

                UpdateWrappedText();
                UpdateTextureToRender();
            }
        }

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public float Alpha
        {
            get { return mAlpha; }
            set { mAlpha = value; }
        }

        public float Red
        {
            get { return mRed; }
            set { mRed = value; }
        }

        public float Green
        {
            get { return mGreen; }
            set { mGreen = value; }
        }

        public float Blue
        {
            get { return mBlue; }
            set { mBlue = value; }
        }

        public object Tag { get; set; }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }

        Renderer Renderer
        {
            get
            {
                if (mManagers == null)
                {
                    return Renderer.Self;
                }
                else
                {
                    return mManagers.Renderer;
                }
            }
        }

        public bool RenderBoundary
        {
            get;
            set;
        }
        #endregion

        #region Methods

        public Text(SystemManagers managers, string text = "Hello")
        {
            Visible = true;
            RenderBoundary = true;

            mManagers = managers;
            mChildren = new List<IPositionedSizedObject>();

            mRawText = text;
            UpdateWrappedText();
            mBounds = new LinePrimitive(Renderer.SinglePixelTexture);
            mBounds.Color = Color.LightGreen;
            
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            mBounds.Add(0, 0);
            HorizontalAlignment = Graphics.HorizontalAlignment.Left;
            VerticalAlignment = Graphics.VerticalAlignment.Top;

#if !TEST
            if (LoaderManager.Self.DefaultBitmapFont != null)
            {
                this.BitmapFont = LoaderManager.Self.DefaultBitmapFont;
            }
            else
            {
                this.BitmapFont = new Graphics.BitmapFont(@"Content\TestFont.fnt", managers);
            }
#endif
            UpdateLinePrimitive();
        }

        char[] whatToSplitOn = new char[] { ' '};
        private void UpdateWrappedText()
        {
            mWrappedText.Clear();

            // This allocates like crazy but we're
            // on the PC and prob won't be calling this
            // very frequently so let's 
            String line = String.Empty;
            String returnString = String.Empty;

            // The user may have entered "\n" in the string, which would 
            // be written as "\\n".  Let's replace that, shall we?
            string stringToUse = null;
            List<string> wordArray = new List<string>();

            if (mRawText != null)
            {
                stringToUse = mRawText.Replace("\\n", "\n");
                wordArray.AddRange(stringToUse.Split(whatToSplitOn));
            }


            while(wordArray.Count != 0)
            {
                string wordUnmodified = wordArray[0];

                string word = wordUnmodified;

                bool containsNewline = false;

                if (word.Contains('\n'))
                {
                    word = word.Substring(0, word.IndexOf('\n'));
                    containsNewline = true;
                }

                string whatToMeasure = line + word;

                float lineWidth = MeasureString(whatToMeasure);

                if (lineWidth > mWidth)
                {
                    while (line.EndsWith(" "))
                    {
                        line = line.Substring(0, line.Length - 1);
                    }
                    if (!string.IsNullOrEmpty(line))
                    {
                        mWrappedText.Add(line);
                    }

                    //returnString = returnString + line + '\n';
                    line = String.Empty;
                }

                // If it's the first word and it's empty, don't add anything
                if (!string.IsNullOrEmpty(word) || !string.IsNullOrEmpty(line))
                {
                    line = line + word + ' ';
                }

                wordArray.RemoveAt(0);

                if (containsNewline)
                {
                    mWrappedText.Add(line);
                    line = string.Empty;
                    int indexOfNewline = wordUnmodified.IndexOf('\n');
                    wordArray.Insert(0, wordUnmodified.Substring(indexOfNewline + 1, wordUnmodified.Length - (indexOfNewline + 1)));
                }
            }
            while (line.EndsWith(" "))
            {
                line = line.Substring(0, line.Length - 1);
            }
            mWrappedText.Add(line);

            UpdateTextureToRender();

        }

        private float MeasureString(string whatToMeasure)
        {
            if (this.BitmapFont != null)
            {
                return BitmapFont.MeasureString(whatToMeasure);
            }
            else if (LoaderManager.Self.DefaultBitmapFont != null)
            {
                return LoaderManager.Self.DefaultBitmapFont.MeasureString(whatToMeasure);
            }
            else
            {
#if TEST
                return 0;
#else
                float wordWidth = LoaderManager.Self.DefaultFont.MeasureString(whatToMeasure).X;
                return wordWidth;
#endif
            }
        }

        void UpdateTextureToRender()
        {
            if (!mIsTextureCreationSuppressed)
            {
                BitmapFont fontToUse = mBitmapFont;
                if (mBitmapFont == null)
                {
                    fontToUse = LoaderManager.Self.DefaultBitmapFont;
                }

                if (fontToUse != null)
                {
                    if (mTextureToRender != null)
                    {
                        mTextureToRender.Dispose();
                        mTextureToRender = null;
                    }

                    mTextureToRender = fontToUse.RenderToTexture2D(WrappedText, this.HorizontalAlignment, mManagers);
                }
                else if (mBitmapFont == null)
                {
                    if (mTextureToRender != null)
                    {
                        mTextureToRender.Dispose();
                        mTextureToRender = null;
                    }
                }
            }
        }

        void UpdateLinePrimitive()
        {
            LineRectangle.UpdateLinePrimitive(mBounds, this);

        }


        public void Render(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (AbsoluteVisible)
            {
                if (RenderBoundary)
                {
                    LineRectangle.RenderLinePrimitive(mBounds, spriteBatch, this, managers);
                }

                if (mTextureToRender == null)
                {
                    RenderUsingSpriteFont(spriteBatch);
                }
                else
                {
                    RenderUsingBitmapFont(spriteBatch, managers);
                }
            }
        }

        private void RenderUsingBitmapFont(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (mTempForRendering == null)
            {
                mTempForRendering = new LineRectangle(managers);
            }

            mTempForRendering.X = this.X;
            mTempForRendering.Y = this.Y;
            mTempForRendering.Width = this.mTextureToRender.Width;
            mTempForRendering.Height = this.mTextureToRender.Height;
            mTempForRendering.Parent = this.Parent;

            float widthDifference = this.mWidth - mTempForRendering.Width;

            if (this.HorizontalAlignment == Graphics.HorizontalAlignment.Center)
            {
                mTempForRendering.X += widthDifference / 2.0f;
            }
            else if (this.HorizontalAlignment == Graphics.HorizontalAlignment.Right)
            {
                mTempForRendering.X += widthDifference;
            }

            if (this.VerticalAlignment == Graphics.VerticalAlignment.Center)
            {
                mTempForRendering.Y += (this.Height - mTextureToRender.Height) / 2.0f;
            }
            else if (this.VerticalAlignment == Graphics.VerticalAlignment.Bottom)
            {
                mTempForRendering.Y += this.Height - mTempForRendering.Height;
            }

            Sprite.Render(managers, spriteBatch, mTempForRendering, mTextureToRender, 
                new Color(mRed, mGreen, mBlue, mAlpha));
        }

        IPositionedSizedObject mTempForRendering;

        private void RenderUsingSpriteFont(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {

            Vector2 offset = new Vector2(Renderer.Self.Camera.RenderingXOffset, Renderer.Self.Camera.RenderingYOffset);

            float leftSide = offset.X + this.GetAbsoluteX();
            float topSide = offset.Y + this.GetAbsoluteY();

            SpriteFont font = LoaderManager.Self.DefaultFont;

            switch (this.VerticalAlignment)
            {
                case Graphics.VerticalAlignment.Top:
                    offset.Y = topSide;
                    break;
                case Graphics.VerticalAlignment.Bottom:
                    {
                        float requiredHeight = (this.mWrappedText.Count) * font.LineSpacing;

                        offset.Y = topSide + (this.Height - requiredHeight);

                        break;
                    }
                case Graphics.VerticalAlignment.Center:
                    {
                        float requiredHeight = (this.mWrappedText.Count) * font.LineSpacing;

                        offset.Y = topSide + (this.Height - requiredHeight) / 2.0f;
                        break;
                    }
            }



            float offsetY = offset.Y;

            for (int i = 0; i < mWrappedText.Count; i++)
            {
                offset.X = leftSide;
                offset.Y = (int)offsetY;

                string line = mWrappedText[i];

                if (HorizontalAlignment == Graphics.HorizontalAlignment.Right)
                {
                    offset.X = leftSide + (Width - font.MeasureString(line).X);
                }
                else if (HorizontalAlignment == Graphics.HorizontalAlignment.Center)
                {
                    offset.X = leftSide + (Width - font.MeasureString(line).X) / 2.0f;
                }

                offset.X = (int)offset.X; // so we don't have half-pixels that render weird

                spriteBatch.DrawString(font, line, offset, Color);
                offsetY += LoaderManager.Self.DefaultFont.LineSpacing;
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

        public void SuppressTextureCreation()
        {
            mIsTextureCreationSuppressed = true;
        }

        public void EnableTextureCreation()
        {
            mIsTextureCreationSuppressed = false;
            UpdateTextureToRender();
        }
        #endregion

        #region IVisible Implementation

        public bool Visible
        {
            get;
            set;
        }

        public bool AbsoluteVisible
        {
            get
            {
                if (((IVisible)this).Parent == null)
                {
                    return Visible;
                }
                else
                {
                    return Visible && ((IVisible)this).Parent.AbsoluteVisible;
                }
            }
        }

        IVisible IVisible.Parent
        {
            get
            {
                return ((IPositionedSizedObject)this).Parent as IVisible;
            }
        }

        #endregion
    }
}
