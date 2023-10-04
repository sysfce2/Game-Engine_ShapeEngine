using Raylib_CsLo;
using ShapeEngine.Core;
using ShapeEngine.Lib;
using ShapeEngine.Random;
using ShapeEngine.Screen;
using System.Net.NetworkInformation;
using System.Numerics;
using ShapeEngine.Core.Interfaces;
using ShapeEngine.Core.Structs;
using ShapeEngine.UI;
using ShapeEngine.Core.Shapes;

namespace Examples.Scenes.ExampleScenes
{
    public class CameraAreaDrawExample : ExampleScene
    {
        Font font;
        Vector2 movementDir = new();
        Rect universe = new(new Vector2(0f), new Vector2(10000f), new Vector2(0.5f));
        List<Star> stars = new();
        private List<Star> drawStars = new();
        
        private Ship ship = new(new Vector2(0f), 30f, ColorMedium, ColorLight, ColorHighlight1);
        private Ship ship2 = new(new Vector2(100, 0), 30f, ColorMedium, ColorHighlight2, ColorHighlight3);
        private Ship currentShip;
        private uint prevCameraTweenID = 0;
        
        private ShapeCamera camera = new ShapeCamera();
        public CameraAreaDrawExample()
        {
            Title = "Camera Area Draw Example";

            font = GAMELOOP.GetFont(FontIDs.JetBrains);
                
            GenerateStars(ShapeRandom.randI(15000, 30000));
            camera.Follower.BoundaryDis = new(200, 400);
            camera.Follower.FollowSpeed = ship.Speed * 1.1f;

            currentShip = ship;
        }

        private void GenerateStars(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector2 pos = universe.GetRandomPointInside();

                //ChanceList<float> sizes = new((45, 1.5f), (25, 2f), (15, 2.5f), (10, 3f), (3, 3.5f), (2, 4f));
                float size = ShapeRandom.randF(1.5f, 3f);// sizes.Next();
                Star star = new(pos, size);
                stars.Add(star);
            }
        }
        
        public override void Activate(IScene oldScene)
        {
            GAMELOOP.Camera = camera;
            camera.Follower.SetTarget(ship);
            currentShip = ship;
        }

        public override void Deactivate()
        {
            GAMELOOP.ResetCamera();
        }
        public override GameObjectHandler? GetGameObjectHandler()
        {
            return null;
        }
        public override void Reset()
        {
            GAMELOOP.ScreenEffectIntensity = 1f;
            camera.Reset();
            ship.Reset(new Vector2(0), 30f);
            ship2.Reset(new Vector2(100, 0), 30f);
            camera.Follower.SetTarget(ship);
            currentShip = ship;
            stars.Clear();
            GenerateStars(ShapeRandom.randI(15000, 30000));

        }
        protected override void HandleInputExample(float dt, Vector2 mousePosGame, Vector2 mousePosUI)
        {
            HandleZoom(dt);

            if (IsKeyPressed(KeyboardKey.KEY_B))
            {
                camera.StopTweenSequence(prevCameraTweenID);
                CameraTweenZoomFactor zoomFactorOut = new(1f, 0.5f, 0.25f, TweenType.LINEAR);
                CameraTweenZoomFactor zoomFactorIn = new(0.5f, 1.25f, 0.75f, TweenType.LINEAR);
                CameraTweenZoomFactor zoomFactorFinal = new(1.25f, 1f, 0.25f, TweenType.LINEAR);
                prevCameraTweenID = camera.StartTweenSequence(zoomFactorOut, zoomFactorIn, zoomFactorFinal);
                
                if (currentShip == ship)
                {
                    currentShip = ship2;
                    camera.Follower.ChangeTarget(ship2, 1f);
                }
                else
                {
                    currentShip = ship;
                    camera.Follower.ChangeTarget(ship, 1f);
                }
            }

        }
        private void HandleZoom(float dt)
        {
            float zoomSpeed = 1f;
            int zoomDir = 0;
            if (IsKeyDown(KeyboardKey.KEY_Z)) zoomDir = -1;
            else if (IsKeyDown(KeyboardKey.KEY_X)) zoomDir = 1;

            if (zoomDir != 0)
            {
                camera.Zoom(zoomDir * zoomSpeed * dt);
            }
        }
        protected override void UpdateExample(float dt, float deltaSlow, ScreenInfo game, ScreenInfo ui)
        {
            currentShip.Update(dt, camera.RotationDeg);

            drawStars.Clear();
            Rect cameraArea = game.Area;
            foreach (var star in stars)
            {
                if(cameraArea.OverlapShape(star.GetBoundingBox())) drawStars.Add(star);
            }
        }

        protected override void DrawGameExample(ScreenInfo game)
        {
            foreach (var star in drawStars)
            {
                star.Draw(new Color(20, 150, 150, 200));
            }
            ship.Draw();
            ship2.Draw();
            
            // Circle cameraBoundaryMin = new(camera.Position, camera.Follower.BoundaryDis.Min);
            // cameraBoundaryMin.DrawLines(2f, ColorHighlight3);
            // Circle cameraBoundaryMax = new(camera.Position, camera.Follower.BoundaryDis.Max);
            // cameraBoundaryMax.DrawLines(2f, ColorHighlight2);
        }
        protected override void DrawGameUIExample(ScreenInfo ui)
        {
            Rect shipInfoRect = ui.Area.ApplyMargins(0.1f, 0.1f, 0.08f, 0.88f);// new Rect(uiSize * new Vector2(0.5f, 1f), uiSize * new Vector2(0.95f, 0.11f), new Vector2(0.5f, 1f));
            string shipInfoText = $"Move Ship [W A S D] | Switch Ship [B]";
            font.DrawText(shipInfoText, shipInfoRect, 1f, new Vector2(0.5f, 0.5f), ColorHighlight3);
        }

        protected override void DrawUIExample(ScreenInfo ui)
        {
            Vector2 uiSize = ui.Area.Size;
            Rect infoRect = new Rect(uiSize * new Vector2(0.5f, 1f), uiSize * new Vector2(0.95f, 0.1f), new Vector2(0.5f, 1f));
            string infoText = $"Zoom [Y X] | Total Stars {stars.Count} | Drawn Stars {drawStars.Count} | Camera Size {camera.Area.Size.Round()}";
            font.DrawText(infoText, infoRect, 1f, new Vector2(0.5f, 0.5f), ColorLight);

            
        }
    }

}