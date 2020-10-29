using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace FollowerV2
{
    internal class DebugHelper
    {
        public static void DrawEllipseToWorld(Camera camera, Graphics graphics, Vector3 vector3Pos, int radius,
            int points, int lineWidth, Color color)
        {
            //var camera = GameController.Game.IngameState.Camera;
            var plottedCirclePoints = new List<Vector3>();
            var slice = 2 * Math.PI / points;
            for (var i = 0; i < points; i++)
            {
                var angle = slice * i;
                var x = (decimal) vector3Pos.X + decimal.Multiply(radius, (decimal) Math.Cos(angle));
                var y = (decimal) vector3Pos.Y + decimal.Multiply(radius, (decimal) Math.Sin(angle));
                plottedCirclePoints.Add(new Vector3((float) x, (float) y, vector3Pos.Z));
            }

            for (var i = 0; i < plottedCirclePoints.Count; i++)
            {
                if (i >= plottedCirclePoints.Count - 1)
                {
                    var pointEnd1 = camera.WorldToScreen(plottedCirclePoints.Last());
                    var pointEnd2 = camera.WorldToScreen(plottedCirclePoints[0]);
                    //Graphics.DrawLine(pointEnd1, pointEnd2, lineWidth, color);
                    graphics.DrawLine(pointEnd1, pointEnd2, lineWidth, color);
                    return;
                }

                var point1 = camera.WorldToScreen(plottedCirclePoints[i]);
                var point2 = camera.WorldToScreen(plottedCirclePoints[i + 1]);
                //Graphics.DrawLine(point1, point2, lineWidth, color);
                graphics.DrawLine(point1, point2, lineWidth, color);
            }
        }
    }
}