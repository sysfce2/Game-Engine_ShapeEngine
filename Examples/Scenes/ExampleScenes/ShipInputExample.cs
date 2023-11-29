﻿using Raylib_CsLo;
using ShapeEngine.Core;
using ShapeEngine.Lib;
using ShapeEngine.Random;
using ShapeEngine.Screen;
using System.Numerics;
using ShapeEngine.Core.Interfaces;
using ShapeEngine.Core.Structs;
using ShapeEngine.Core.Shapes;
using ShapeEngine.Input;

namespace Examples.Scenes.ExampleScenes
{
    
    public class ShipInputExample : ExampleScene
    {
        internal readonly struct ColorScheme
        {
            public readonly Color Hull;
            public readonly Color Outline;
            public readonly Color Cockpit;

            public ColorScheme(Color hull, Color outline, Color cockpit)
            {
                this.Hull = hull;
                this.Outline = outline;
                this.Cockpit = cockpit;
            }
        }
        internal class SpaceShip : ICameraFollowTarget
        {
            public const float Speed = 500f;
            public const float Size = 30f;
            public const float MaxDistance = 2500f;
            public const float MinDistance = 250f;

            public static readonly ColorScheme[] ColorSchemes = new[]
            {
                new ColorScheme(DARKGRAY, GRAY, GREEN),
                new ColorScheme(DARKGRAY, GRAY, BLUE),
                new ColorScheme(DARKGRAY, GRAY, YELLOW),
                new ColorScheme(DARKGRAY, GRAY, RED),
                new ColorScheme(DARKGRAY, GRAY, ORANGE),
                new ColorScheme(DARKGRAY, GRAY, PURPLE),
                new ColorScheme(DARKGRAY, GRAY, PINK),
                new ColorScheme(DARKGRAY, GRAY, SKYBLUE),

            };
            
            private Circle hull;
            private Vector2 movementDir;
            
            private readonly InputAction iaMoveHor;
            private readonly InputAction iaMoveVer;
            private readonly ColorScheme colorScheme;
            public readonly Gamepad Gamepad;

            public SpaceShip(Vector2 pos, Gamepad gamepad)
            {
                hull = new(pos, Size);
                movementDir = ShapeRandom.randVec2();
                
                // var moveHorKB = new InputTypeKeyboardButtonAxis(ShapeKeyboardButton.A, ShapeKeyboardButton.D);
                var moveHorGP = new InputTypeGamepadAxis(ShapeGamepadAxis.RIGHT_X, 0.1f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyGamepadReversed);
                // var moveHorMW = new InputTypeMouseWheelAxis(ShapeMouseWheelAxis.HORIZONTAL, 0.2f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyMouseReversed);
                iaMoveHor = new(moveHorGP);
                
                // var moveVerKB = new InputTypeKeyboardButtonAxis(ShapeKeyboardButton.W, ShapeKeyboardButton.S);
                var moveVerGP = new InputTypeGamepadAxis(ShapeGamepadAxis.RIGHT_Y, 0.1f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyGamepadReversed);
                // var moveVerMW = new InputTypeMouseWheelAxis(ShapeMouseWheelAxis.VERTICAL, 0.2f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyMouseReversed);
                iaMoveVer = new(moveVerGP);

                Gamepad = gamepad;
                colorScheme = ColorSchemes[gamepad.Index];
            }

            public bool Overlap(SpaceShip other)
            {
                // if (invisibleTimer > 0f) return false;
                
                return hull.OverlapShape(other.hull);
            }
            public string GetInputDescription(InputDevice inputDevice)
            {
                string hor = iaMoveHor.GetInputTypeDescription(inputDevice, true, 1, false, false);
                string ver = iaMoveVer.GetInputTypeDescription(inputDevice, true, 1, false, false);
                return $"Move Horizontal [{hor}] Vertical [{ver}]";
            }
            
            public void Destroy()
            {
                
            }
            public void Update(float dt, Vector2 target)
            {
                int gamepadIndex = GAMELOOP.CurGamepad?.Index ?? -1;
                iaMoveHor.Gamepad = gamepadIndex;
                iaMoveVer.Gamepad = gamepadIndex;
                
                iaMoveHor.Update(dt);
                iaMoveVer.Update(dt);
                    
                Vector2 dir = new(iaMoveHor.State.AxisRaw, iaMoveVer.State.AxisRaw);
                    
                float lsq = dir.LengthSquared();
                if (lsq > 0f)
                {
                    movementDir = dir.Normalize();
                    var movement = movementDir * Speed * dt;
                    hull = new Circle(hull.Center + movement, Size);
                }
                else
                {
                    hull = new Circle(hull.Center, Size);
                }
            }

            public Vector2 GetRandomSpawnPosition()
            {
                return GetPosition() + ShapeRandom.randVec2(0, hull.Radius * 2);
            }
            public void Draw()
            {
                var rightThruster = movementDir.RotateDeg(-25);
                var leftThruster = movementDir.RotateDeg(25);

                var outlineColor = colorScheme.Outline;
                var hullColor = colorScheme.Hull;
                var cockpitColor = colorScheme.Cockpit;
                DrawCircleV(hull.Center - rightThruster * hull.Radius, hull.Radius / 6, outlineColor);
                DrawCircleV(hull.Center - leftThruster * hull.Radius, hull.Radius / 6, outlineColor);
                hull.Draw(hullColor);
                DrawCircleV(hull.Center + movementDir * hull.Radius * 0.66f, hull.Radius * 0.33f, cockpitColor);

                hull.DrawLines(4f, outlineColor);
            }


            public Vector2 GetPosition()
            {
                return hull.Center;
            }

            public void FollowStarted()
            {
            }

            public void FollowEnded()
            {
            }

            public Vector2 GetCameraFollowPosition()
            {
                return GetPosition();
            }
        }

        
        private readonly Font font;
        private readonly Rect universe = new(new Vector2(0f), new Vector2(10000f), new Vector2(0.5f));
        private readonly List<Star> stars = new();

        private readonly CameraFollowerMulti cameraFollower = new();
        private readonly ShapeCamera camera = new();
        private readonly List<SpaceShip> spaceShips = new();
        private readonly Gamepad[] gamepads = new Gamepad[8];
        private readonly List<int> connectedGamepadIndices = new();

        private readonly List<Gamepad> availableGamepads = new();
        // private readonly InputAction iaAddShip;
        // private readonly InputAction iaNextShip;
        // private readonly InputAction iaCenterTarget;
        // private bool centerTargetActive = false;
        
        
        public ShipInputExample()
        {
            Title = "Multiple Gamepads Example";

            font = GAMELOOP.GetFont(FontIDs.JetBrains);
                
            GenerateStars(2500);
            camera.Follower = cameraFollower;
            
            GamepadSetup();
            // AddShip();
            
            // var addShipKB = new InputTypeKeyboardButton(ShapeKeyboardButton.SPACE);
            // var addShipGP = new InputTypeGamepadButton(ShapeGamepadButton.RIGHT_FACE_DOWN);
            // var addshipMB = new InputTypeMouseButton(ShapeMouseButton.LEFT);
            // iaAddShip = new(addShipKB, addshipMB, addShipGP);
            //     
            // var nextShipKB = new InputTypeKeyboardButton(ShapeKeyboardButton.Q);
            // var nextShipGP = new InputTypeGamepadButton(ShapeGamepadButton.RIGHT_FACE_RIGHT);
            // var nextShipMB = new InputTypeMouseButton(ShapeMouseButton.RIGHT);
            // iaNextShip = new(nextShipKB, nextShipMB, nextShipGP);
            //
            // var centerTargetKB = new InputTypeKeyboardButton(ShapeKeyboardButton.E);
            // var centerTargetGP = new InputTypeGamepadButton(ShapeGamepadButton.RIGHT_TRIGGER_TOP);
            // iaCenterTarget = new(centerTargetKB, centerTargetGP);
        }

       
        // private void AddShip()
        // {
        //     var pos = ActiveSpaceShip?.GetRandomSpawnPosition() ?? new();
        //     var spaceShip = new SpaceShip(pos);
        //     spaceShips.Add(spaceShip);
        //     cameraFollower.AddTarget(spaceShip);
        //     if (ActiveSpaceShip == null)
        //     {
        //         SelectShip(spaceShip);
        //     }
        // }
        
        // private void RemoveShip()
        // {
        //     foreach (var ship in spaceShips)
        //     {
        //         ship.Destroy();
        //     }
        //     ActiveSpaceShip = null;
        //     
        //     spaceShips.Clear();
        //     cameraFollower.Reset();
        // }
        private void GenerateStars(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                var pos = universe.GetRandomPointInside();

                ChanceList<float> sizes = new((45, 1.5f), (25, 2f), (15, 2.5f), (10, 3f), (3, 3.5f), (2, 4f));
                float size = sizes.Next();
                Star star = new(pos, size);
                stars.Add(star);
            }
        }
        
        public override void Activate(IScene oldScene)
        {
            GAMELOOP.Camera = camera;
            
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
            stars.Clear();
            GenerateStars(2500);
            
            camera.Reset();
            cameraFollower.Reset();
            
        }

        protected override void HandleInputExample(float dt, Vector2 mousePosGame, Vector2 mousePosUI)
        {
            // int gamepadIndex = GAMELOOP.CurGamepad?.Index ?? -1;
            // iaAddShip.Gamepad = gamepadIndex;
            // iaNextShip.Gamepad = gamepadIndex;
            // iaCenterTarget.Gamepad = gamepadIndex;
            //     
            // iaAddShip.Update(dt);
            // iaNextShip.Update(dt);
            // iaCenterTarget.Update(dt);
            //
            // if (iaCenterTarget.State.Pressed)
            // {
            //     centerTargetActive = !centerTargetActive;
            //     if (!centerTargetActive)
            //     {
            //         cameraFollower.CenterTarget = null;
            //     }
            //     else
            //     {
            //         if (ActiveSpaceShip != null) cameraFollower.CenterTarget = ActiveSpaceShip;
            //     }
            // }
            //
            // if (iaNextShip.State.Pressed)
            // {
            //     if (ActiveSpaceShip != null)
            //     {
            //         var next = FindClosestShip(ActiveSpaceShip);
            //         if(next != null) SelectShip(next);
            //     }
            // }
            //
            // if (iaAddShip.State.Pressed)
            // {
            //     AddShip();
            // }
        }

        
        protected override void UpdateExample(float dt, float deltaSlow, ScreenInfo game, ScreenInfo ui)
        {
            CheckGamepadConnections();
            // var targetPos = ActiveSpaceShip?.GetPosition() ?? new();
            //
            // SpaceShip? next = null;
            // for (int i = spaceShips.Count - 1; i >= 0; i--)
            // {
            //     var ship = spaceShips[i];
            //     ship.Update(dt, targetPos);
            //
            //
            //     if (ActiveSpaceShip != null && ship != ActiveSpaceShip && ship.Overlap(ActiveSpaceShip))
            //     {
            //         DestroyShip(ship);
            //     }
            // }
            // if(next != null) SelectShip(next);
        }
        protected override void DrawGameExample(ScreenInfo game)
        {
            foreach (var star in stars)
            {
                star.Draw();
            }

            foreach (var ship in spaceShips)
            {
                ship.Draw();
            }
            
            cameraFollower.Draw();
            
        }
        protected override void DrawGameUIExample(ScreenInfo ui)
        {
            
        }
        protected override void DrawUIExample(ScreenInfo ui)
        {
            var rects = GAMELOOP.UIRects.GetRect("bottom center").SplitV(0.35f);
            DrawDescription(rects.top);
            DrawInputDescription(rects.bottom);
           
            DrawGamepadInfo(GAMELOOP.UIRects.GetRect("bottom right"));
        }

        private void DrawGamepadInfo(Rect rect)
        {
            var text = $"Connected {connectedGamepadIndices.Count} | Available {availableGamepads.Count}";
            font.DrawText(text, rect, 1f, new Vector2(0.5f, 0.5f), ColorHighlight3);
        }
        private void DrawDescription(Rect rect)
        {
            font.DrawText("Press a button on a new controller to add a ship.", rect, 1f, new Vector2(0.5f, 0.5f), ColorMedium);
        }
        private void DrawInputDescription(Rect rect)
        {
            var curDevice = Input.CurrentInputDevice;
            var curDeviceNoMouse = Input.CurrentInputDeviceNoMouse;
            string addShipText = "No Text"; // iaAddShip.GetInputTypeDescription(curDevice, true, 1, false);
            string moveText = "No Text"; //ActiveSpaceShip != null ? ActiveSpaceShip.GetInputDescription(curDevice) : "";
            
            string textBottom = $"{moveText} | Add Ship {addShipText}";
            font.DrawText(textBottom, rect, 1f, new Vector2(0.5f, 0.5f), ColorLight);
        }

        private void OnGamepadConnectionChanged(Gamepad gamepad, bool connected)
        {
            
        }
        private void CheckGamepadConnections()
        {
            connectedGamepadIndices.Clear();
            for (var i = 0; i < gamepads.Length; i++)
            {
                var gamepad = gamepads[i];
                if (Raylib.IsGamepadAvailable(i))
                {
                    if (!gamepad.Connected)
                    {
                        gamepad.Connect();
                        OnGamepadConnectionChanged(gamepad, true);
                    }
                    connectedGamepadIndices.Add(i);
                }
                else
                {
                    if (gamepad.Connected)
                    {
                        gamepad.Disconnect();
                        OnGamepadConnectionChanged(gamepad, false);
                    }
                }
            }
        
        }
        private void GamepadSetup()
        {
            for (var i = 0; i < gamepads.Length; i++)
            {
                gamepads[i] = new Gamepad(i, Raylib.IsGamepadAvailable(i));
            }
        }
        private bool HasGamepad(int index) => index >= 0 && index < gamepads.Length;
        private bool IsGamepadConnected(int index) => HasGamepad(index) && gamepads[index].Connected;
        private Gamepad? GetGamepad(int index)
        {
            if (!HasGamepad(index)) return null;
            return gamepads[index];
        }
        private Gamepad? RequestGamepad()
        {
            // var preferredGamepad = GetGamepad(preferredIndex);
            // if (preferredGamepad is { Connected: true, Available: true })
            // {
            //     preferredGamepad.Claim();
            //     return preferredGamepad;
            // }

            foreach (var gamepad in gamepads)
            {
                if (gamepad is { Connected: true, Available: true })
                {
                    gamepad.Claim();
                    return gamepad;
                }
            }
            return null;
        }
        private void ReturnGamepad(int index) => GetGamepad(index)?.Free();
        private void ReturnGamepad(Gamepad gamepad) => GetGamepad(gamepad.Index)?.Free();

        
    }
    
    
    
    
    
    
    //added ships checks gamepad index
    //if free gamepad is found than ship is in group a 
    //all ships that do not have a gamepad index are in group b and can be switched through with a button
    
    // public class ShipInputExample : ExampleScene
    // {
    //     internal class SpaceShip : ICameraFollowTarget
    //     {
    //         public const float Speed = 500;
    //         public bool Selected = false;
    //         public bool Selectable => gamepad == null;
    //         
    //         private Circle hull;
    //         private Vector2 movementDir;
    //
    //         private readonly Color hullColorActive = ColorMedium;
    //         private readonly Color outlineColorActive = ColorHighlight1;
    //         private readonly Color cockpitColorActive = ColorHighlight3;
    //
    //         private readonly Color hullColorInactive = ColorMedium;
    //         private readonly Color outlineColorInactive = ColorLight;
    //         private readonly Color cockpitColorInactive = ColorRustyRed;
    //         
    //         private readonly InputAction iaMoveHor;
    //         private readonly InputAction iaMoveVer;
    //         private readonly InputAction iaAddShip;
    //         private readonly InputAction iaAddShipGamepad;
    //         private readonly InputAction iaMoveHorGamepad;
    //         private readonly InputAction iaMoveVerGamepad;
    //         
    //         private Gamepad? gamepad;
    //
    //         public event Action<Vector2>? OnSpawnShipRequested;
    //         public SpaceShip(Vector2 pos, float r)
    //         {
    //             hull = new(pos, r);
    //             
    //             
    //             var moveHorKB = new InputTypeKeyboardButtonAxis(ShapeKeyboardButton.A, ShapeKeyboardButton.D);
    //             var moveHorMW = new InputTypeMouseWheelAxis(ShapeMouseWheelAxis.HORIZONTAL, 0.2f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyMouseReversed);
    //             iaMoveHor = new(moveHorKB, moveHorMW);
    //             
    //             var moveVerKB = new InputTypeKeyboardButtonAxis(ShapeKeyboardButton.W, ShapeKeyboardButton.S);
    //             var moveVerMW = new InputTypeMouseWheelAxis(ShapeMouseWheelAxis.VERTICAL, 0.2f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyMouseReversed);
    //             iaMoveVer = new(moveVerKB, moveVerMW);
    //             
    //             var moveHorGP = new InputTypeGamepadAxis(ShapeGamepadAxis.RIGHT_X, 0.1f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyGamepadReversed);
    //             iaMoveHorGamepad = new(moveHorGP);
    //         
    //             var moveVerGP = new InputTypeGamepadAxis(ShapeGamepadAxis.RIGHT_Y, 0.1f, ModifierKeyOperator.Or, GameloopExamples.ModifierKeyGamepadReversed);
    //             iaMoveVerGamepad = new(moveVerGP);
    //             
    //             var addShipKB = new InputTypeKeyboardButton(ShapeKeyboardButton.SPACE);
    //             var addshipMB = new InputTypeMouseButton(ShapeMouseButton.LEFT);
    //             iaAddShip = new(addShipKB, addshipMB);
    //             
    //             var addShipGP = new InputTypeGamepadButton(ShapeGamepadButton.RIGHT_FACE_DOWN);
    //             iaAddShipGamepad = new(addShipGP);
    //         }
    //
    //         public string GetInputDescription(InputDevice inputDevice)
    //         {
    //             string hor = iaMoveHor.GetInputTypeDescription(inputDevice, true, 1, false, false);
    //             string ver = iaMoveVer.GetInputTypeDescription(inputDevice, true, 1, false, false);
    //             return $"Move Horizontal [{hor}] Vertical [{ver}]";
    //         }
    //         public void Reset(Vector2 pos, float r)
    //         {
    //             hull = new(pos, r);
    //         }
    //
    //         public void Destroy()
    //         {
    //             //if(gamepad != null) ShapeLoop.Input.ReturnGamepad(gamepad);
    //         }
    //         public void Update(float dt)
    //         {
    //             if (gamepad != null)
    //             {
    //                 if (!gamepad.Available || !gamepad.Connected)
    //                 {
    //                     gamepad = null;
    //                 }
    //             }
    //
    //             if (gamepad == null)
    //             {
    //                 gamepad = ShapeLoop.Input.RequestGamepad();
    //             }
    //
    //             
    //
    //             Vector2 dir;
    //             if (gamepad != null)
    //             {
    //                 iaMoveHorGamepad.Gamepad = gamepad.Index;
    //                 iaMoveVerGamepad.Gamepad = gamepad.Index;
    //                 iaAddShipGamepad.Gamepad = gamepad.Index;
    //                 
    //                 iaAddShipGamepad.Update(dt);
    //                 iaMoveHorGamepad.Update(dt);
    //                 iaMoveVerGamepad.Update(dt);
    //                 
    //                 if(iaAddShipGamepad.State.Pressed) OnSpawnShipRequested?.Invoke(GetPosition());
    //                 dir = new(iaMoveHorGamepad.State.AxisRaw, iaMoveVerGamepad.State.AxisRaw);
    //             }
    //             else
    //             {
    //                 if (Selectable && Selected)
    //                 {
    //                     iaMoveHor.Update(dt);
    //                     iaMoveVer.Update(dt);
    //                     iaAddShip.Update(dt);
    //                     
    //                     if(iaAddShip.State.Pressed) OnSpawnShipRequested?.Invoke(GetPosition());
    //                     dir = new(iaMoveHor.State.AxisRaw, iaMoveVer.State.AxisRaw);
    //                 }
    //                 else dir = new();
    //             }
    //             
    //             float lsq = dir.LengthSquared();
    //             if (lsq > 0f)
    //             {
    //                 movementDir = dir.Normalize();
    //                 var movement = movementDir * Speed * dt;
    //                 hull = new Circle(hull.Center + movement, hull.Radius);
    //             }
    //         }
    //         public void Draw()
    //         {
    //             var rightThruster = movementDir.RotateDeg(-25);
    //             var leftThruster = movementDir.RotateDeg(25);
    //             
    //             var outlineColor = Selected ? outlineColorActive : outlineColorInactive;
    //             var hullColor = Selected ? hullColorActive : hullColorInactive;
    //             var cockpitColor = Selected ? cockpitColorActive : cockpitColorInactive;
    //             if (!Selectable) outlineColor = RED;
    //             DrawCircleV(hull.Center - rightThruster * hull.Radius, hull.Radius / 6, outlineColor);
    //             DrawCircleV(hull.Center - leftThruster * hull.Radius, hull.Radius / 6, outlineColor);
    //             hull.Draw(hullColor);
    //             DrawCircleV(hull.Center + movementDir * hull.Radius * 0.66f, hull.Radius * 0.33f, cockpitColor);
    //
    //             hull.DrawLines(4f, outlineColor);
    //         }
    //
    //
    //         public Vector2 GetPosition()
    //         {
    //             return hull.Center;
    //         }
    //
    //         public void FollowStarted()
    //         {
    //         }
    //
    //         public void FollowEnded()
    //         {
    //         }
    //
    //         public Vector2 GetCameraFollowPosition()
    //         {
    //             return GetPosition();
    //         }
    //     }
    //
    //     // internal interface IFollowTarget
    //     // {
    //     //     public Vector2 GetPosition();
    //     // }
    //     // internal class CameraFollower : ICameraFollowTarget
    //     // {
    //     //     private readonly List<IFollowTarget> targets = new();
    //     //     private readonly ShapeCamera camera;
    //     //     private Vector2 followPosition = new();
    //     //     // public CameraFollower()
    //     //     // {
    //     //     //     camera = new()
    //     //     //     {
    //     //     //         Follower =
    //     //     //         {
    //     //     //             FollowSpeed = SpaceShip.Speed * 2f
    //     //     //         }
    //     //     //     };
    //     //     // }
    //     //     public Vector2 GetRandomPosition()
    //     //     {
    //     //         return camera.Area.GetRandomPointInside();
    //     //     }
    //     //     
    //     //     public void Reset()
    //     //     {
    //     //         camera.Reset();
    //     //         // camera.Follower.SetTarget(this);
    //     //         SetCameraValues();
    //     //     }
    //     //     public void Activate()
    //     //     {
    //     //         GAMELOOP.Camera = camera;
    //     //         // camera.Follower.SetTarget(this);
    //     //         SetCameraValues();
    //     //     }
    //     //     public void Deactivate()
    //     //     {
    //     //         GAMELOOP.ResetCamera();
    //     //     }
    //     //     
    //     //     
    //     //     public bool AddTarget(IFollowTarget newTarget)
    //     //     {
    //     //         if (targets.Contains(newTarget)) return false;
    //     //         targets.Add(newTarget);
    //     //         return true;
    //     //     }
    //     //
    //     //     public bool RemoveTarget(IFollowTarget target)
    //     //     {
    //     //         return targets.Remove(target);
    //     //     }
    //     //
    //     //     public void Update(float dt)
    //     //     {
    //     //         SetCameraValues();
    //     //     }
    //     //     public void FollowStarted()
    //     //     {
    //     //         
    //     //     }
    //     //     public void FollowEnded()
    //     //     {
    //     //         
    //     //     }
    //     //     public Vector2 GetCameraFollowPosition()
    //     //     {
    //     //         return followPosition;
    //     //     }
    //     //     private void UpdateCameraFollowBoundary()
    //     //     {
    //     //         var rect = camera.Area;
    //     //         float size = rect.Size.Min();
    //     //         var boundary = new Vector2(size * 0.15f, size * 0.4f);
    //     //         // camera.Follower.BoundaryDis = new(boundary);
    //     //     }
    //     //
    //     //     private void SetCameraValues()
    //     //     {
    //     //         var cameraArea = camera.Area;
    //     //         var totalPosition = new Vector2();
    //     //         var newCameraRect = new Rect(cameraArea.Center, new(), new(0.5f));
    //     //         
    //     //         foreach (var target in targets)
    //     //         {
    //     //             var pos = target.GetPosition();
    //     //             totalPosition += pos;
    //     //             newCameraRect = newCameraRect.Enlarge(pos);
    //     //         }
    //     //
    //     //         var curSize = cameraArea.Size;
    //     //         var newSize = newCameraRect.Size;
    //     //         float f = 1f - (newSize.GetArea() / curSize.GetArea());
    //     //         camera.SetZoom(f);
    //     //         followPosition = totalPosition / targets.Count;
    //     //         
    //     //         UpdateCameraFollowBoundary();
    //     //     }
    //     // }
    //     //
    //     
    //     private readonly Font font;
    //     private readonly Rect universe = new(new Vector2(0f), new Vector2(10000f), new Vector2(0.5f));
    //     private readonly List<Star> stars = new();
    //
    //     private readonly CameraFollowerMulti cameraFollower = new();
    //     private readonly ShapeCamera camera = new();
    //     // private readonly List<SpaceShip> spaceShips = new();
    //     private readonly List<SpaceShip> gamepadShips = new();
    //     private readonly List<SpaceShip> selectableShips = new();
    //     //private bool setupFinished = false;
    //     private SpaceShip? ActiveSpaceShip = null;
    //
    //     
    //     private readonly InputAction iaChangeShip;
    //     
    //     
    //     public ShipInputExample()
    //     {
    //         Title = "Ship Input Example";
    //
    //         font = GAMELOOP.GetFont(FontIDs.JetBrains);
    //             
    //         GenerateStars(2500);
    //
    //         // var spaceShip1 = new SpaceShip(new Vector2(), 30f);
    //         // var spaceShip2 = new SpaceShip(new Vector2(), 30f);
    //         // spaceShips.Add(spaceShip1);
    //         // spaceShips.Add(spaceShip2);
    //         // cameraFollower.AddTarget(spaceShip1);
    //         // cameraFollower.AddTarget(spaceShip2);
    //         camera.Follower = cameraFollower;
    //
    //         AddShip(new(0));
    //         
    //         
    //         var nextShipKB = new InputTypeKeyboardButton(ShapeKeyboardButton.Q);
    //         //var nextShipGP = new InputTypeGamepadButton(ShapeGamepadButton.RIGHT_FACE_LEFT);
    //         var nextShipMB = new InputTypeMouseButton(ShapeMouseButton.RIGHT);
    //         iaChangeShip = new(nextShipKB, nextShipMB);
    //         // SetShipActive(spaceShip1);
    //     }
    //
    //     private void SelectShip(SpaceShip ship)
    //     {
    //         if (!ship.Selectable || !selectableShips.Contains(ship)) return;
    //         if (ActiveSpaceShip == ship) return;
    //         
    //         if (ActiveSpaceShip != null) ActiveSpaceShip.Selected = false;
    //         ActiveSpaceShip = ship;
    //         ActiveSpaceShip.Selected = true;
    //     }
    //
    //     private void NextShip()
    //     {
    //         if (selectableShips.Count <= 0) return;
    //         
    //         if(ActiveSpaceShip == null) SelectShip(selectableShips[0]);
    //         else
    //         {
    //             var index = selectableShips.IndexOf(ActiveSpaceShip);
    //             index = ShapeMath.WrapIndex(selectableShips.Count, index + 1);
    //             SelectShip(selectableShips[index]);
    //         }
    //     }
    //
    //     private void AddShip(Vector2 pos)
    //     {
    //         var spaceShip = new SpaceShip(pos, 30f);
    //         spaceShip.OnSpawnShipRequested += AddShip;
    //         if(spaceShip.Selectable) selectableShips.Add(spaceShip);
    //         else gamepadShips.Add(spaceShip);
    //         cameraFollower.AddTarget(spaceShip);
    //         if (ActiveSpaceShip == null && spaceShip.Selectable)
    //         {
    //             SelectShip(spaceShip);
    //         }
    //     }
    //     private void DestroyShips()
    //     {
    //         // var area = camera.Area;
    //         // foreach (var ship in spaceShips)
    //         // {
    //         //     ship.Reset(area.GetRandomPointInside(), 30f);
    //         // }
    //         foreach (var ship in selectableShips)
    //         {
    //             ship.Destroy();
    //             ship.OnSpawnShipRequested -= AddShip;
    //         }
    //         foreach (var ship in gamepadShips)
    //         {
    //             ship.Destroy();
    //             ship.OnSpawnShipRequested -= AddShip;
    //         }
    //         ActiveSpaceShip = null;
    //         
    //         selectableShips.Clear();
    //         gamepadShips.Clear();
    //         cameraFollower.Reset();
    //     }
    //     private void GenerateStars(int amount)
    //     {
    //         for (int i = 0; i < amount; i++)
    //         {
    //             var pos = universe.GetRandomPointInside();
    //
    //             ChanceList<float> sizes = new((45, 1.5f), (25, 2f), (15, 2.5f), (10, 3f), (3, 3.5f), (2, 4f));
    //             float size = sizes.Next();
    //             Star star = new(pos, size);
    //             stars.Add(star);
    //         }
    //     }
    //     
    //     public override void Activate(IScene oldScene)
    //     {
    //         GAMELOOP.Camera = camera;
    //         
    //     }
    //
    //     public override void Deactivate()
    //     {
    //         GAMELOOP.ResetCamera();
    //     }
    //     public override GameObjectHandler? GetGameObjectHandler()
    //     {
    //         return null;
    //     }
    //     public override void Reset()
    //     {
    //         GAMELOOP.ScreenEffectIntensity = 1f;
    //         DestroyShips();
    //         stars.Clear();
    //         GenerateStars(2500);
    //         
    //         camera.Reset();
    //         cameraFollower.Reset();
    //         
    //         AddShip(new(0));
    //     }
    //
    //     
    //     // private void ShakeCamera()
    //     // {
    //     //     camera.Shake(ShapeRandom.randF(0.8f, 2f), new Vector2(100, 100), 0, 25, 0.75f);
    //     // }
    //     //
    //     
    //     protected override void HandleInputExample(float dt, Vector2 mousePosGame, Vector2 mousePosUI)
    //     {
    //         
    //         iaChangeShip.Update(dt);
    //         if (iaChangeShip.State.Pressed)
    //         {
    //              NextShip();       
    //         }
    //         // int gamepadIndex = GAMELOOP.CurGamepad?.Index ?? -1;
    //         // iaShakeCamera.Gamepad = gamepadIndex;
    //         // iaShakeCamera.Update(dt);
    //         //
    //         // iaRotateCamera.Gamepad = gamepadIndex;
    //         // iaRotateCamera.Update(dt);
    //         //
    //         // iaToggleDrawCameraFollowBoundary.Gamepad = gamepadIndex;
    //         // iaToggleDrawCameraFollowBoundary.Update(dt);
    //         //
    //         // if (iaToggleDrawCameraFollowBoundary.State.Pressed)
    //         // {
    //         //     drawCameraFollowBoundary = !drawCameraFollowBoundary;
    //         // }
    //         //
    //         // HandleRotation(dt);
    //         //
    //         // if (iaShakeCamera.State.Pressed) ShakeCamera();
    //
    //     }
    //
    //     
    //     protected override void UpdateExample(float dt, float deltaSlow, ScreenInfo game, ScreenInfo ui)
    //     {
    //         for (int i = selectableShips.Count - 1; i >= 0; i--)
    //         {
    //             var ship = selectableShips[i];
    //             if (!ship.Selectable)
    //             {
    //                 selectableShips.RemoveAt(i);
    //                 if (ActiveSpaceShip == ship)
    //                 {
    //                     if (selectableShips.Count > 0)
    //                     {
    //                         NextShip();
    //                     }
    //                     else ActiveSpaceShip = null;
    //                 }
    //                 gamepadShips.Add(ship);
    //             }
    //             else ship.Update(dt);
    //         }
    //         for (int i = gamepadShips.Count - 1; i >= 0; i--)
    //         {
    //             var ship = gamepadShips[i];
    //             if (ship.Selectable)
    //             {
    //                 gamepadShips.RemoveAt(i);
    //                 selectableShips.Add(ship);
    //                 if(ActiveSpaceShip == null) SelectShip(ship);
    //             }
    //             else ship.Update(dt);
    //         }
    //     }
    //     protected override void DrawGameExample(ScreenInfo game)
    //     {
    //         foreach (var star in stars)
    //         {
    //             star.Draw();
    //         }
    //
    //         foreach (var ship in selectableShips)
    //         {
    //             ship.Draw();
    //         }
    //         foreach (var ship in gamepadShips)
    //         {
    //             ship.Draw();
    //         }
    //         
    //         cameraFollower.Draw();
    //         
    //     }
    //     protected override void DrawGameUIExample(ScreenInfo ui)
    //     {
    //         
    //     }
    //     protected override void DrawUIExample(ScreenInfo ui)
    //     {
    //         // var rects = GAMELOOP.UIRects.GetRect("bottom center").SplitV(0.5f);
    //         // DrawInputDescription(GAMELOOP.UIRects.GetRect("bottom center"));
    //         // DrawCameraSizeInfo(rects.top);
    //         // DrawCameraInfo(rects.bottom);
    //        
    //     }
    //
    //     // private void DrawCameraSizeInfo(Rect rect)
    //     // {
    //     //     var targetSize = ShapeMath.RoundToDecimals(camera.TargetSize, 2);
    //     //     var curSize = ShapeMath.RoundToDecimals(camera.CurSize, 2);
    //     //     var dif = ShapeMath.RoundToDecimals(camera.Dif, 2);
    //     //     
    //     //     string text = $"Size: Cur {curSize} | Target {targetSize} | Dif {dif}";
    //     //     font.DrawText(text, rect, 1f, new Vector2(0.5f, 0.5f), ColorHighlight3);
    //     // }
    //     // private void DrawCameraInfo(Rect rect)
    //     // {
    //     //     //var pos = camera.Position;
    //     //     // var x = (int)pos.X;
    //     //     // var y = (int)pos.Y;
    //     //     // var rot = (int)camera.RotationDeg;
    //     //     var zoomLevel = ShapeMath.RoundToDecimals(camera.ZoomLevel, 2);// (int)(ShapeUtils.GetFactor(camera.ZoomLevel, 0.1f, 5f) * 100f);
    //     //     var zoomFactor = ShapeMath.RoundToDecimals(camera.ZoomFactor, 2);
    //     //     // var prevZoomLevel = ShapeMath.RoundToDecimals(cameraFollower.prevZoomLevel, 2);
    //     //     string text = $"Zoom: Level {zoomLevel} | Factor {zoomFactor}";
    //     //     font.DrawText(text, rect, 1f, new Vector2(0.5f, 0.5f), ColorHighlight3);
    //     // }
    //    
    //     // private void DrawInputDescription(Rect rect)
    //     // {
    //     //     var rects = rect.SplitV(0.35f);
    //     //     var curDevice = Input.CurrentInputDevice;
    //     //     // var curDeviceNoMouse = Input.CurrentInputDeviceNoMouse;
    //     //     string shakeCameraText = iaShakeCamera.GetInputTypeDescription(curDevice, true, 1, false);
    //     //     string rotateCameraText = iaRotateCamera.GetInputTypeDescription(curDevice, true, 1, false);
    //     //     string toggleDrawText = iaToggleDrawCameraFollowBoundary.GetInputTypeDescription(Input.CurrentInputDeviceNoMouse, true, 1, false);
    //     //     string moveText = ship.GetInputDescription(curDevice);
    //     //     string onText = drawCameraFollowBoundary ? "ON" : "OFF";
    //     //     string textTop = $"Draw Camera Follow Boundary {onText} - Toggle {toggleDrawText}";
    //     //     string textBottom = $"{moveText} | Shake {shakeCameraText} | Rotate {rotateCameraText}";
    //     //     font.DrawText(textTop, rects.top, 1f, new Vector2(0.5f, 0.5f), ColorMedium);
    //     //     font.DrawText(textBottom, rects.bottom, 1f, new Vector2(0.5f, 0.5f), ColorLight);
    //     // }
    // }

}
