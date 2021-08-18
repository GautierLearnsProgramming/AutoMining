using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyGyro> _gyros = new List<IMyGyro>();
        List<IMyThrust> _thrusters = new List<IMyThrust>();
        List<IMyShipDrill> _drills = new List<IMyShipDrill>();
        List<IMyShipController> _controllers = new List<IMyShipController>();
        List<IMyShipConnector> _connectors = new List<IMyShipConnector>();
        List<PathStep> _steps = new List<PathStep>();
        IMyShipController _controller;


        int _stepControl = 0;

        int _flightControl = 0;
        int _unparkControl = 0;
        int _parkControl = 0;
        int _mineControl = 0;

        MyCommandLine _commandLine = new MyCommandLine();
        MyIni _ini = new MyIni();

        public Program()
        {
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());

            GridTerminalSystem.GetBlocksOfType(_gyros, x => x.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(_thrusters, x => x.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(_controllers, x => x.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(_drills, x => x.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(_connectors, x => x.IsSameConstructAs(Me));
            _controller = _controllers[0];

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            List<MyIniKey> path1keys = new List<MyIniKey>();
            _ini.GetKeys("path", path1keys);
            foreach (var key in path1keys)
            {
                if (key.Name.Contains("move"))
                {
                    var coordString = _ini.Get(key).ToString();
                    _steps.Add(new PathStep(parseCoord(coordString), PathStepType.Move));
                }
                else if (key.Name.Contains("mine"))
                {
                    var mineStepString = _ini.Get(key).ToString();
                    var values = mineStepString.Split(';');
                    _steps.Add(new PathStep(parseCoord(values[0]), PathStepType.Mine, Double.Parse(values[1])));
                }
                else if (key.Name.Contains("unpark"))
                {
                    var coordString = _ini.Get(key).ToString();
                    _steps.Add(new PathStep(parseCoord(coordString), PathStepType.Unpark));
                }
                else if (key.Name.Contains("park"))
                {
                    var coordString = _ini.Get(key).ToString();
                    _steps.Add(new PathStep(parseCoord(coordString), PathStepType.Park));
                }
            }
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            
            
            
            //MatrixD startPos = new MatrixD(nullMatrix);
            //startPos.Translation = new Vector3D(908822.28, -9627.49, 1590485.59);
            //startPos.Forward = new Vector3D(0, 0, 1);
            //startPos.Up = new Vector3D(0, 1, 0);
            //mineTunnel(startPos, 150);

            flyStepPath(_steps);

            //flyToCoordinate(new MatrixD(-0.872052431106567, -0.403872519731522, -0.27642548084259, 0, 0.487992972135544, -0.760527789592743, -0.42832225561142, 0, -0.0372417196631432, -0.508413255214691, 0.860307455062866, 0, 908658.584301433, -9764.90295774968, 1590674.94891754, 1));

            //orientShip(new Vector3D(-0.0372417196631432, -0.508413255214691, 0.860307455062866), new Vector3D(0.487992972135544, -0.760527789592743, -0.42832225561142), _controller);
            //                        (0.872053146362305, 0.403874188661575, 0.276421844959259, 0, 0.487992495298386, -0.760528206825256, -0.428321957588196, 0, 0.0372384786605835, 0.508411288261414, -0.860308527946472, 0, 908658.478502589, -9764.2468889776, 1590674.33268479, 1)
            //flyToCoordinate(_coordinates[1]);

            //GPS:Mythologiga #1:912123.29:-9627.34:1595693.03:#FF75C9F1:
            //move(new Vector3D(912123.29, -9627.34, 1595693.03));

            //GPS:Mythologiga #2:912513.87:-10192.73:1594600.02:#FF75C9F1:
            //move(new Vector3D(912513.87, -10192.73, 1594600.02));

            //GPS:Mythologiga #3:912123.29:-9627.34:1595693.03:#FF75C9F1:
            //move(new Vector3D(912123.29, -9607.34, 1595693.03));

            //orientShip(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), _controller);
            //orientShip(new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), _controller);
            
        }

        public MatrixD parseCoord(string coordString)
        {
            var values = coordString.Split(',');
            return new MatrixD(Double.Parse(values[0]), Double.Parse(values[1]), Double.Parse(values[2]), Double.Parse(values[3]), Double.Parse(values[4]), Double.Parse(values[5]), Double.Parse(values[6]), Double.Parse(values[7]), Double.Parse(values[8]), Double.Parse(values[9]), Double.Parse(values[10]), Double.Parse(values[11]), Double.Parse(values[12]), Double.Parse(values[13]), Double.Parse(values[14]), Double.Parse(values[15]));
        }

        public bool flyStepPath(List<PathStep> steps)
        {
            if (_stepControl == steps.Count)
            {
                foreach (var gyro in _gyros)
                {
                    gyro.GyroOverride = false;
                }
                foreach (var thruster in _thrusters)
                {
                    thruster.ThrustOverridePercentage = 0f;
                }
                _stepControl++; ;
                return false;
            }
            else if (_stepControl == steps.Count + 1)
            {
                Echo("Flight path finished");
                return true;
            }
            else if (makeStep(steps[_stepControl]))
            {
                _stepControl++;
            }
            return false;
        }

        public bool makeStep(PathStep step)
        {
            switch (step.stepType)
            {
                case PathStepType.Move:
                    {
                            return flyToCoordinate(step.coord);
                    }
                case PathStepType.Mine:
                    {
                        return mineTunnel(step.coord, step.mineDepth);
                    }
                case PathStepType.Unpark:
                    {
                        return makeUnparkStep(step);
                    }
                case PathStepType.Park:
                    {
                        return makeParkStep(step);
                    }
                default:
                    Echo("Problem with step type");
                    return false;
            }
        }

        public bool makeParkStep(PathStep step)
        {
            switch (_parkControl)
            {
                case 0:
                    var parkable = false;
                    flyToCoordinate(step.coord);
                    foreach (var connector in _connectors)
                    {
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                        {
                            parkable = true;
                        }
                    }
                    if (parkable) _parkControl++;
                    return false;
                case 1:
                    bool parked = false;
                    Echo("Trying to connect");
                    foreach(var connector in _connectors)
                    {
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                        {
                            connector.Connect();
                            parked = true;
                        }
                        if (parked) _parkControl++;
                    }
                    return false;
                case 2:
                    _parkControl = 0;
                    return true;
                default:
                    Echo("Problem with the park control number");
                    return false;
            }
        }

        public bool makeUnparkStep(PathStep step)
        {
            switch (_unparkControl)
            {
                case 0:
                    foreach (var connector in _connectors)
                    {
                        connector.Disconnect();
                    }
                    _unparkControl++;
                    return false;
                case 1:
                    if (flyToCoordinate(step.coord, false)) _unparkControl++;
                    return false;
                case 2:
                    _unparkControl = 0;
                    return true;
                default:
                    Echo("Problem with the park control number");
                    return false;
            }
        }


        public double getTargetMiningSpeed(Vector3D distanceVec)
        {
            return MathHelper.Clamp(Math.Log(distanceVec.Length() + 1), 0, 0.8);
        }

        public bool mineTunnel(MatrixD startingPosition, double depth)
        {
            var destination = startingPosition.Translation + depth * SafeNormalize(startingPosition.Forward);
            var distanceVec = destination - _controller.GetPosition();
            var returnVec = startingPosition.Translation - _controller.GetPosition();
            var mass = _controller.CalculateShipMass().PhysicalMass;
            switch (_mineControl)
            {
                case 0:
                    if (flyToCoordinate(startingPosition))
                    {
                        drillOn();
                        _mineControl++;
                    }
                    break;
                case 1:
                    if (move(distanceVec, getTargetMineSpeed(distanceVec), _controller, _thrusters, mass)) _mineControl++;
                    break;
                case 2:
                    if (move(returnVec, getTargetMineReturnSpeed(distanceVec), _controller, _thrusters, mass)) _mineControl++;
                    break;
                case 3:
                    drillOff();
                    _mineControl = 0;
                    Echo("Finished mining !");
                    return true;
                default:
                    Echo("There is a problem with the mine control number !");
                    break;
            }
            return false;
        }

        public double getTargetMineSpeed(Vector3D distanceVec)
        {
            return MathHelper.Clamp(Math.Log(distanceVec.Length() + 1), 0, 1.5);
        }

        public double getTargetMineReturnSpeed(Vector3D distanceVec)
        {
            return MathHelper.Clamp(Math.Log(distanceVec.Length() + 1), 0, 8);
        }

        public bool drillOn()
        {
            foreach(var drill in _drills)
            {
                drill.Enabled = true;
            }
            return true;
        }

        public bool drillOff()
        {
            foreach (var drill in _drills)
            {
                drill.Enabled = false;
            }
            return true;
        }

        public bool flyToCoordinate(MatrixD coordinate, bool orientFirst)
        {
            Vector3D distanceVec = coordinate.Translation - _controller.GetPosition();
            double mass = _controller.CalculateShipMass().PhysicalMass;
            var targetSpeed = getTargetSpeed(distanceVec, _thrusters, mass);
            Echo(targetSpeed.ToString());
            switch (_flightControl)
            {
                case 0:
                    if (orientFirst)
                    {
                        if (orientShip(coordinate.Forward, coordinate.Up, _controller)) _flightControl++;
                        break;
                    }
                    else
                    {
                        _flightControl++;
                        break;
                    }
                case 1:
                    if(move(distanceVec, targetSpeed, _controller, _thrusters, mass)) _flightControl++;
                    break;
                case 2:
                    if(orientShip(coordinate.Forward, coordinate.Up, _controller)) _flightControl++;
                    break;
                case 3:
                    if (move(distanceVec, targetSpeed, _controller, _thrusters, mass)) _flightControl++;
                    break;
                case 4:
                    Echo("Finished path !");
                    _flightControl = 0;
                    return true;
                default:
                    Echo("There is a problem with the flight control number !");
                    return false;
            }
            return false;
        }

        public bool flyToCoordinate(MatrixD coordinate)
        {
            return flyToCoordinate(coordinate, true);
        }
        public bool move(Vector3D distanceVec, double targetSpeed, IMyShipController _controller, List<IMyThrust> _thrusters, double mass)
        { 
            if (distanceVec.Length() < 0.03)
            {
                foreach(var thruster in _thrusters)
                {
                    thruster.ThrustOverride = 0f;
                }
                _controller.DampenersOverride = true;
                Echo("We've arrived");
                return true;
            }
            Vector3D desiredDirectionVec = SafeNormalize(distanceVec);
            var myVelocityVec = _controller.GetShipVelocities().LinearVelocity;
            var targetVelocityVec = targetSpeed * desiredDirectionVec;
            var relativeVelocity = myVelocityVec - targetVelocityVec;
            ApplyThrustCustom(_thrusters, relativeVelocity, _controller, mass);
            return false;
        }

        public double getTargetSpeed(Vector3D distanceVec, List<IMyThrust> thrusters, double mass)
        {
            double distance = distanceVec.Length();
            if (distance < 50)
            {
                return Math.Log(distance + 1);
            }

            double maxBackwardThrust = 0;
            double maxBackwardAcceleration;
            Vector3D moveDirection = SafeNormalize(distanceVec);
            foreach(var thruster in thrusters)
            {
                maxBackwardThrust += Math.Max(0, thruster.MaxEffectiveThrust * Vector3D.Dot(thruster.WorldMatrix.Backward, moveDirection));
            }
            maxBackwardAcceleration = maxBackwardThrust / mass;
            
            return MathHelper.Clamp(Math.Sqrt(2 * maxBackwardAcceleration * distance) * 0.6, 0, 100);
        }

        public void ApplyThrustCustom(List<IMyThrust> thrusters, Vector3D travelVec, IMyShipController thisController, double mass)
        {
            var gravity = thisController.GetNaturalGravity();

            var desiredThrust = mass * (2 * travelVec + gravity);
            var thrustToApply = desiredThrust;

            foreach (IMyThrust thisThrust in thrusters)
            {
                if (Vector3D.Dot(thisThrust.WorldMatrix.Forward, thrustToApply) > 0)
                {
                    var neededThrust = Vector3D.Dot(thrustToApply, thisThrust.WorldMatrix.Forward);
                    Echo("Needed thrust");
                    Echo(neededThrust.ToString());
                    var outputProportion = MathHelper.Clamp(neededThrust / thisThrust.MaxEffectiveThrust, 0, 1);
                    Echo("Output Proportion");
                    Echo((Convert.ToSingle(outputProportion).ToString()));
                    thisThrust.ThrustOverridePercentage = (float)outputProportion;
                    thrustToApply -= thisThrust.WorldMatrix.Forward * outputProportion * thisThrust.MaxEffectiveThrust;
                }
                else
                {
                    thisThrust.ThrustOverridePercentage = 0.000001f;
                }

            }
        }

        public Vector3D VectorRejection(Vector3D a, Vector3D b) //reject a on b    
        {
            if (Vector3D.IsZero(b))
                return Vector3D.Zero;

            return a - a.Dot(b) / b.LengthSquared() * b;
        }

        public bool orientShip(Vector3D desiredForwardVector, Vector3D desiredUpVector, IMyShipController _controller)
        {
            var angularVelocity = _controller.GetShipVelocities().AngularVelocity.Length();
            Echo("Current angular velocity");
            Echo(angularVelocity.ToString());

            double pitch, yaw, roll, angle = 0;
            GetRotationAnglesSimultaneous(desiredForwardVector, desiredUpVector, _controller.WorldMatrix, out yaw, out pitch, out roll, out angle);
            Echo("Angle left to cover");
            Echo(angle.ToString());
            if (angle < 0.00015 && angularVelocity < 0.002)
            {
                foreach (var gyro in _gyros)
                {
                    gyro.GyroOverride = false;
                }
                Echo("Ship Oriented");
                return true;
            }


            ApplyGyroOverride(pitch, yaw, roll, _gyros, _controller.WorldMatrix);

            Echo("Orienting Ship");
            return false;
        }

        //Whip's ApplyGyroOverride Method v12 - 11/02/2019
        void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed); //because keen does some weird stuff with signs 
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList)
            {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));

                thisGyro.GyroOverride = true;
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                
            }
        }

        /*
        Whip's GetRotationAnglesSimultaneous - Last modified: 07/05/2020
        Gets axis angle rotation and decomposes it upon each cardinal axis.
        Has the desired effect of not causing roll oversteer. Does NOT use
        sequential rotation angles.
        Set desiredUpVector to Vector3D.Zero if you don't care about roll.
        Dependencies:
        SafeNormalize
        */
        void GetRotationAnglesSimultaneous(Vector3D desiredForwardVector, Vector3D desiredUpVector, MatrixD worldMatrix, out double yaw, out double pitch, out double roll, out double angle)
        {
            desiredForwardVector = SafeNormalize(desiredForwardVector);

            MatrixD transposedWm;
            MatrixD.Transpose(ref worldMatrix, out transposedWm);
            Vector3D.Rotate(ref desiredForwardVector, ref transposedWm, out desiredForwardVector);
            Vector3D.Rotate(ref desiredUpVector, ref transposedWm, out desiredUpVector);

            Vector3D leftVector = Vector3D.Cross(desiredUpVector, desiredForwardVector);
            Vector3D axis;
            if (Vector3D.IsZero(desiredUpVector) || Vector3D.IsZero(leftVector))
            {
                axis = new Vector3D(desiredForwardVector.Y, -desiredForwardVector.X, 0);
                angle = Math.Acos(MathHelper.Clamp(-desiredForwardVector.Z, -1.0, 1.0));
            }
            else
            {
                leftVector = SafeNormalize(leftVector);
                Vector3D upVector = Vector3D.Cross(desiredForwardVector, leftVector);

                // Create matrix
                MatrixD targetMatrix = MatrixD.Zero;
                targetMatrix.Forward = desiredForwardVector;
                targetMatrix.Left = leftVector;
                targetMatrix.Up = upVector;

                axis = new Vector3D(targetMatrix.M23 - targetMatrix.M32,
                                    targetMatrix.M31 - targetMatrix.M13,
                                    targetMatrix.M12 - targetMatrix.M21);

                double trace = targetMatrix.M11 + targetMatrix.M22 + targetMatrix.M33;
                angle = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1, 1));
            }

            if (Vector3D.IsZero(axis))
            {
                angle = desiredForwardVector.Z < 0 ? 0 : Math.PI;
                yaw = angle;
                pitch = 0;
                roll = 0;
                return;
            }

            axis = SafeNormalize(axis);
            yaw = -axis.Y * angle;
            pitch = -axis.X * angle;
            roll = -axis.Z * angle;
        }

        public static Vector3D SafeNormalize(Vector3D a)
        {
            if (Vector3D.IsZero(a))
                return Vector3D.Zero;

            if (Vector3D.IsUnit(ref a))
                return a;

            return Vector3D.Normalize(a);
        }

        public bool IsClosed(IMyTerminalBlock b)
        {
            return GridTerminalSystem.GetBlockWithId(b.EntityId) == null;
        }

        public class PathStep
        {
            public PathStep(MatrixD coord, PathStepType pathStepType)
            {
                this.stepType = pathStepType;
                this.coord = coord;
            }

            public PathStep(MatrixD coord, PathStepType pathStepType, double depth)
            {
                this.stepType = pathStepType;
                this.coord = coord;
                this.mineDepth = depth;
            }
            public PathStepType stepType {get; set;}
            public MatrixD coord { get; set; }
            public double mineDepth { get; set; }
        }

        public enum PathStepType
        {
            Move,
            Mine,
            Park,
            Unpark
        }
    }
}
