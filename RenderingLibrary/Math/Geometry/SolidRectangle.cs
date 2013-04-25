﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class SolidRectangle : IPositionedSizedObject, IRenderable
    {
        #region Fields
        
        Vector2 Position;
        IPositionedSizedObject mParent;

        List<IPositionedSizedObject> mChildren;
        public Color Color;

        #endregion

        #region Properties


        public string Name
        {
            get;
            set;
        }
        public float X
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        public float Z
        {
            get;
            set;
        }

        public float Width
        {
            get;
            set;
        }

        public float Height
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


        public bool Visible
        {
            get;
            set;
        }

        public ICollection<IPositionedSizedObject> Children
        {
            get { return mChildren; }
        }

        public object Tag { get; set; }

        public BlendState BlendState
        {
            get { return BlendState.NonPremultiplied; }
        }

        #endregion

        public SolidRectangle()
        {
            mChildren = new List<IPositionedSizedObject>();
            Color = Color.White;
            Visible = true;
        }


        void IRenderable.Render(SpriteBatch spriteBatch, SystemManagers managers)
        {
            if (Visible)
            {
                Renderer renderer = null;
                if (managers == null)
                {
                    renderer = Renderer.Self;
                }
                else
                {
                    renderer = managers.Renderer;
                }

                Sprite.Render(managers, spriteBatch, this,
                    renderer.SinglePixelTexture,
                    this.Color);

            }
        }
    }
}