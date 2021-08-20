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
        List<PathStep> _baseDock = new List<PathStep>();
        List<PathStep> _baseUndock = new List<PathStep>();
        List<string> _flightPlan = new List<string>();
        Dictionary<string, List<PathStep>> _paths = new Dictionary<string, List<PathStep>>();
        IMyShipController _controller;

        //Used to regulate main behaviour
        int _flightPlanControl = 0;

        // Used to regulate behavior in flyStepPath
        int _flyPathPartControl = 0;
        int _stepControl = 0;
        // Used to regulate behavior in flyToCoordinate
        int _flightControl = 0;
        // Used to regulate behavior in makeParkStep
        int _unparkControl = 0;
        // Used to regulate behavior in makeUnparkStep
        int _parkControl = 0;
        // Used to regulate behavior in mineTunnel
        int _mineControl = 0;
        // Used to regulate sleep behaviour
        int _sleepControl = 0;

        MyCommandLine _commandLine = new MyCommandLine();
        MyIni _ini = new MyIni();

        public void Main(string argument, UpdateType updateSource)
        {
            if (_commandLine.TryParse(argument))
            {
                if (argument.Equals("start"))
                {
                    _flightPlanControl = 0;
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                }
                else if (argument.Equals("forceStop"))
                {
                    resetOverrides();
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                }
                else if (argument.Equals("debug"))
                {
                    foreach(var path in _flightPlan)
                    {
                        Echo(path);
                        Echo(_paths[path][0].coord.ToString());
                    }
                }
            } else
            {
                if (_sleepControl > 0)
                {
                    _sleepControl--;
                }
                else {
                    Echo(_flightPlan.Count.ToString());
                    Echo(_paths.Keys.Count.ToString());
                   flyFlightPlan();
                }
            }
        }

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

            loadPaths();
            loadFligthPlan();
            loadBase();

            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void loadFligthPlan()
        {
            List<MyIniKey> flightPlanKeys = new List<MyIniKey>();
            _ini.GetKeys("flightPlan", flightPlanKeys);

            foreach(var key in flightPlanKeys)
            {
                _flightPlan.Add(key.Name);
            }
        }

        public void loadBase()
        {
            List<MyIniKey> baseDockKeys = new List<MyIniKey>();
            List<MyIniKey> baseUndockKeys = new List<MyIniKey>();
            _ini.GetKeys("baseDock", baseDockKeys);
            _ini.GetKeys("baseUndock", baseUndockKeys);
            loadSteps(baseDockKeys, _baseDock);
            loadSteps(baseUndockKeys, _baseUndock);
        }

        public void loadPaths()
        {
            List<string> sections = new List<string>();
            _ini.GetSections(sections);

            foreach(var section in sections)
            {
                if (!section.Contains("path")) continue;
                Echo(section);
                List<MyIniKey> pathKeys = new List<MyIniKey>();
                List<PathStep> _steps = new List<PathStep>();
                _ini.GetKeys(section, pathKeys);

                loadSteps(pathKeys, _steps);

                _paths[section] = _steps;
            }
        }

        public void loadSteps(List<MyIniKey> pathKeys, List<PathStep> _steps)
        {
            foreach (var key in pathKeys)
            {
                if (key.Name.Contains("move"))
                {
                    var coordString = _ini.Get(key).ToString();
                    _steps.Add(new PathStep(key.Name, parseCoord(coordString), PathStepType.Move));
                }
                else if (key.Name.Contains("mine"))
                {
                    var mineStepString = _ini.Get(key).ToString();
                    var values = mineStepString.Split(';');
                    _steps.Add(new PathStep(key.Name, parseCoord(values[0]), PathStepType.Mine, Double.Parse(values[1])));
                }
                else if (key.Name.Contains("unpark"))
                {
                    var coordString = _ini.Get(key).ToString();
                    _steps.Add(new PathStep(key.Name, parseCoord(coordString), PathStepType.Unpark));
                }
                else if (key.Name.Contains("park"))
                {
                    var coordString = _ini.Get(key).ToString();
                    _steps.Add(new PathStep(key.Name, parseCoord(coordString), PathStepType.Park));
                }
            }
        }

        public void Save()
        {
        }

        public void Sleep(int sleepCount)
        {
            _sleepControl = sleepCount;
        }

        public MatrixD parseCoord(string coordString)
        {
            var values = coordString.Split(',');
            return new MatrixD(Double.Parse(values[0]), Double.Parse(values[1]), Double.Parse(values[2]), Double.Parse(values[3]), Double.Parse(values[4]), Double.Parse(values[5]), Double.Parse(values[6]), Double.Parse(values[7]), Double.Parse(values[8]), Double.Parse(values[9]), Double.Parse(values[10]), Double.Parse(values[11]), Double.Parse(values[12]), Double.Parse(values[13]), Double.Parse(values[14]), Double.Parse(values[15]));
        }

        public bool flyFlightPlan()
        {
            if (_flightPlanControl == _flightPlan.Count)
            {
                Echo("Flight Plan Finished");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return true;
            }
            else
            {
                List<PathStep> steps;
                Echo(_flightPlan[_flightPlanControl]);
                _paths.TryGetValue(_flightPlan[_flightPlanControl], out steps);
                if (flyTotalPath(steps)) {
                    Sleep(1000);
                    _flightPlanControl++; }
            }
            return false;
        }

        public void resetOverrides()
        {
            foreach (var gyro in _gyros)
            {
                gyro.GyroOverride = false;
            }
            foreach (var thruster in _thrusters)
            {
                thruster.ThrustOverridePercentage = 0f;
            }
        }

        public bool flyTotalPath(List<PathStep> steps)
        {
            switch (_flyPathPartControl)
            {
                case 0:
                    if (flyStepPath(_baseUndock)) _flyPathPartControl++;
                    break;
                case 1:
                    if (flyStepPath(steps)) _flyPathPartControl++;
                    break;
                case 2:
                    if (flyStepPath(_baseDock)) _flyPathPartControl++;
                    break;
                case 3:
                    Echo("Fly path finished");
                    _flyPathPartControl = 0;
                    return true;
                default:
                    Echo("Problem with FlightPathPartControl number");
                    return false;
            }
            return false;
        }

        public bool flyStepPath(List<PathStep> steps)
        {
            if (_stepControl == steps.Count)
            {
                resetOverrides();
                _stepControl++; ;
                return false;
            }
            else if (_stepControl == steps.Count + 1)
            {
                Echo("Step path finished");
                _stepControl = 0;
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
            Echo($"Executing step {step.name}");
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
                    if (move(returnVec, getTargetMineReturnSpeed(returnVec), _controller, _thrusters, mass)) _mineControl++;
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
            

            double maxBackwardThrust = 0;
            double maxBackwardAcceleration;
            Vector3D moveDirection = SafeNormalize(distanceVec);
            foreach(var thruster in thrusters)
            {
                maxBackwardThrust += Math.Max(0, thruster.MaxEffectiveThrust * Vector3D.Dot(thruster.WorldMatrix.Backward, moveDirection));
            }
            maxBackwardAcceleration = maxBackwardThrust / mass;

            double distance = distanceVec.Length();
            if (distance < 50)
            {
                return Math.Log(distance + 1) * Math.Sqrt(maxBackwardAcceleration) * 0.8;
            }

            return MathHelper.Clamp(Math.Sqrt(2 * maxBackwardAcceleration * distance) * 0.6, 0, 250);
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
            public PathStep(string name, MatrixD coord, PathStepType pathStepType)
            {
                this.stepType = pathStepType;
                this.coord = coord;
                this.name = name;
            }

            public PathStep(string name, MatrixD coord, PathStepType pathStepType, double depth)
            {
                this.stepType = pathStepType;
                this.coord = coord;
                this.mineDepth = depth;
                this.name = name;
            }
            public PathStepType stepType {get; set;}
            public MatrixD coord { get; set; }
            public double mineDepth { get; set; }
            public string name { get; set; }
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
