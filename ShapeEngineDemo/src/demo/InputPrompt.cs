﻿using System.Numerics;
using Raylib_CsLo;
using ShapeEngineCore.Globals;
using ShapeEngineCore.Globals.UI;
using ShapeEngineCore.Globals.Input;
using ShapeEngineCore.Globals.Screen;

namespace ShapeEngineDemo
{
    public class InputPrompt : UIElement
    {
        float radius = 0f;
        float angleDeg = 0f;
        Color textColor = WHITE;
        Color bgColor = DARKGRAY;
        Color barColor = YELLOW;
        string inputAction = "";
        float f = 0f;

        public InputPrompt(Vector2 center, float radius, string inputAction, float angleDeg, Color textColor, Color barColor, Color bgColor)
        {
            this.rect = new(center.X + radius, center.Y + radius, radius, radius);
            this.radius = radius;
            this.inputAction = inputAction;
            this.angleDeg = angleDeg;
            this.textColor = textColor;
            this.barColor = barColor;
            this.bgColor = bgColor;
        }
        public InputPrompt(Vector2 center, float radius, string inputAction, Color textColor, Color barColor, Color bgColor)
        {
            this.rect = new(center.X + radius, center.Y + radius, radius, radius);
            this.radius = radius;
            this.inputAction = inputAction;
            this.textColor = textColor;
            this.barColor = barColor;
            this.bgColor = bgColor;
        }
        public InputPrompt(Vector2 center, float radius, string inputAction, Color textColor, Color barColor)
        {
            this.rect = new(center.X + radius, center.Y + radius, radius, radius);
            this.radius = radius;
            this.inputAction = inputAction;
            this.textColor = textColor;
            this.barColor = barColor;
            this.bgColor = new(0,0,0,0);
        }
        public InputPrompt(Vector2 center, float radius, string inputAction, Color textColor)
        {
            this.rect = new(center.X + radius, center.Y + radius, radius, radius);
            this.radius = radius;
            this.inputAction = inputAction;
            this.textColor = textColor;
            this.barColor = new(0,0,0,0);
            this.bgColor = new(0, 0, 0, 0);
        }
        public InputPrompt(Vector2 center, float radius, string inputAction, float angleDeg, Color textColor)
        {
            this.rect = new(center.X + radius, center.Y + radius, radius, radius);
            this.radius = radius;
            this.inputAction = inputAction;
            this.textColor = textColor;
            this.angleDeg = angleDeg;
            this.barColor = new(0, 0, 0, 0);
            this.bgColor = new(0, 0, 0, 0);
        }
        public InputPrompt(Vector2 center, float radius, string inputAction, float angleDeg, Color textColor, Color bgColor)
        {
            this.rect = new(center.X + radius, center.Y + radius, radius, radius);
            this.radius = radius;
            this.inputAction = inputAction;
            this.angleDeg = angleDeg;
            this.textColor = textColor;
            this.barColor = new(0,0,0,0);
            this.bgColor = bgColor;
        }
       
        public void SetBarF(float value)
        {
            f = value;
        }

        public void Pressed()
        {

        }

        public override void Draw(Vector2 uiSize, Vector2 stretchFactor)
        {
            Vector2 center = GetPos(Alignement.CENTER);
            string text = InputHandler.GetInputActionKeyNames(0, inputAction, true).keyboard;
            float r = GetSize().X;
            float thickness = r * 0.25f;
            if(bgColor.a > 0) DrawCircleV(center, r, bgColor);
            UIHandler.DrawTextAlignedPro(text, center, angleDeg, GetSize(), 1, textColor, Alignement.CENTER);
            if(barColor.a > 0) Drawing.DrawCircleOutlineBar(center, r + thickness, -90, thickness, f, barColor);
        }
    }

}
