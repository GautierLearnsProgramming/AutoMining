MyCommandLine _commandLine = new MyCommandLine();

List<IMyShipController> _controllers = new List<IMyShipController>();
IMyShipController _controller;

int moveStepNumber = 0;
int mineStepNumber = 0;

MatrixD wdm;

public Program()
{
    GridTerminalSystem.GetBlocksOfType(_controllers, x => x.IsSameConstructAs(Me));
    _controller = _controllers[0];
}

public void Save()
{
}

public void Main(string argument, UpdateType updateSource)
{
    if (_commandLine.TryParse(argument))
    {
        if (argument.Equals("AddMoveStep"))
        {
            wdm = _controller.WorldMatrix;
            Echo(wdm.Translation.ToString());

            Me.CustomData += $"\nmove{moveStepNumber.ToString()}={wdm.M11},{wdm.M12},{wdm.M13},{wdm.M14},{wdm.M21},{wdm.M22},{wdm.M23},{wdm.M24},{wdm.M31},{wdm.M32},{wdm.M33},{wdm.M34},{wdm.M41},{wdm.M42},{wdm.M43},{wdm.M44}";

            Echo("Move Step Added");
            Echo($"Move Step Number : {moveStepNumber.ToString()}");
            moveStepNumber++;
        }
        else if (argument.Equals("AddMineStep"))
        {
            wdm = _controller.WorldMatrix;
            Me.CustomData += $"\nmine{mineStepNumber.ToString()}={wdm.M11},{wdm.M12},{wdm.M13},{wdm.M14},{wdm.M21},{wdm.M22},{wdm.M23},{wdm.M24},{wdm.M31},{wdm.M32},{wdm.M33},{wdm.M34},{wdm.M41},{wdm.M42},{wdm.M43},{wdm.M44};PUT MINING DEPTH HERE";

            Echo("Mine Step added, don't forget to put the mining depth");
            Echo($"Mine Step Number : {mineStepNumber.ToString()}");
            mineStepNumber++;
        }
        else if (argument.Equals("AddMineStepB"))
        {
            wdm = _controller.WorldMatrix;
            Me.CustomData += $"\nmine{mineStepNumber.ToString()}={wdm.M11},{wdm.M12},{wdm.M13},{wdm.M14},{wdm.M21},{wdm.M22},{wdm.M23},{wdm.M24},{wdm.M31},{wdm.M32},{wdm.M33},{wdm.M34},{wdm.M41},{wdm.M42},{wdm.M43},{wdm.M44};";
            Echo($"Mine Step Number : {mineStepNumber.ToString()}");
            mineStepNumber++;
        }
        else if (argument.Equals("FinishMineStepB"))
        {
            wdm = _controller.WorldMatrix;
            Me.CustomData += $"{wdm.M11},{wdm.M12},{wdm.M13},{wdm.M14},{wdm.M21},{wdm.M22},{wdm.M23},{wdm.M24},{wdm.M31},{wdm.M32},{wdm.M33},{wdm.M34},{wdm.M41},{wdm.M42},{wdm.M43},{wdm.M44};";
        }
        else if (argument.Equals("IncrementMoveStep"))
        {
            moveStepNumber++;
        }
        else if (argument.Equals("IncrementMineStep"))
        {
            mineStepNumber++;
        }
    }
}