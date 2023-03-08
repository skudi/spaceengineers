
IMyMotorStator rotor;
IMyMotorStator hinge;
List<IMyPistonBase> pistons = new List<IMyPistonBase>();
List<IMyShipDrill> drills = new List<IMyShipDrill>();
IMyCockpit cockpit;

enum State {
    IDLE,
    MINE,
    PAUSE,
    RETRACT
};

struct Limits<T> {
    public T Min;
    public T Max;
    public Limits(T min, T max) {
        this.Min = min;
        this.Max = max;
    }
};

State state = State.IDLE;
float hingeAngle = 0.1F;
Limits<float> rotorLimits = new Limits<float>(0.01F, (float)(1.99 * Math.PI));
Limits<float> lengthLimits = new Limits<float>(0.0F, 10.0F);
int ts = 0;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    string mineGroupName = Me.CustomData;
    IMyBlockGroup mineGroup = GridTerminalSystem.GetBlockGroupWithName(mineGroupName) as IMyBlockGroup;
    if (mineGroup == null) {
        Echo ($@"group [{mineGroupName}] not found");
    }
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    mineGroup.GetBlocks(blocks);
    foreach (var blk in blocks) {
        if (blk is IMyPistonBase) {
            pistons.Add((IMyPistonBase)blk);
            Echo($@"Piston {blk.CustomName}");
        } else if (blk is IMyMotorStator) {
            if (blk.CustomName.Contains("Hinge")) {
                hinge = (IMyMotorStator)blk;
                Echo($@"Hinge v: {hinge.CustomName}");
            } else if (blk.CustomName.Contains("Rotor")) {
                rotor = (IMyMotorStator)blk;
                Echo($@"Rotor v: {rotor.CustomName}");
            }
        } else if (blk is IMyCockpit) {
            cockpit = (IMyCockpit)blk;
            Echo($@"Cockpit v: {rotor.CustomName}");
        } else if (blk is IMyShipDrill) {
            drills.Add((IMyShipDrill)blk);
            Echo($@"Drill v: {blk.CustomName}");
        } else {
        Echo($@"object: {blk.CustomName} [{blk.GetType().Name}]");
        }
    }
}

public void Save()
{
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

void Retract() {
    foreach (var drill in drills) {
        drill.Enabled = false;
    }
    foreach (var piston in pistons) {
        piston.Velocity = -0.5F;
        piston.MinLimit = lengthLimits.Min / pistons.Count;
    }
    rotor.LowerLimitRad = 0;
    rotor.TargetVelocityRPM = -1;
    rotor.UpperLimitRad = rotorLimits.Max;
    hinge.LowerLimitRad = 0;
    hinge.TargetVelocityRPM = -1;
    hinge.UpperLimitRad = 1;
}

void Mine() {
    foreach (var drill in drills) {
        drill.Enabled = true;
    }
    rotor.UpperLimitRad = rotorLimits.Max;
    rotor.TargetVelocityRPM = 2;
    rotor.Enabled=true;
    hinge.UpperLimitRad = hingeAngle;
    hinge.TargetVelocityRPM = 3;
    hinge.Enabled=true;
    foreach (var piston in pistons) {
        piston.Enabled=true;
        piston.MaxLimit = lengthLimits.Min / pistons.Count;
        piston.Velocity = 0.03F / pistons.Count;
    }
}

void Report() {
    StringBuilder statmsg=new StringBuilder();
    statmsg.AppendLine($@"{ts}");
    statmsg.AppendLine($@"{state}");
    IMyTextSurface lcd = cockpit.GetSurface(0);
    statmsg.AppendLine($@"H:{hinge.Angle*180.0F/Math.PI:0}");
    foreach (var piston in pistons) {
        statmsg.AppendLine($@"Px:{piston.CurrentPosition:0.00} <{piston.MinLimit:0.00} / {lengthLimits.Min/pistons.Count:0.00} : {piston.MaxLimit:0.00} / {lengthLimits.Max/pistons.Count:0.00}>");
    }
    statmsg.AppendLine($@"R:{rotor.Angle:0.00} <{rotor.LowerLimitRad:0.00} / {rotorLimits.Min:0.00} : {rotor.UpperLimitRad:0.00} / {rotorLimits.Max:0.00}>");
    Echo(statmsg.ToString());
    lcd.WriteText(statmsg.ToString());
}

public void Main(string argument, UpdateType updateSource)
{
    ts += 1;
    Echo($@"argument: {argument}");
    if (argument.ToLower() == "stop") {
        Retract();
        state=State.IDLE;
    } else if (argument.ToLower() == "start") {
        Mine();
        state=State.MINE;
    } else if (argument.ToLower() == "status") {
        Report();
    } else if (argument.ToLower() == "resume") {
        state = State.MINE;
        rotor.TargetVelocityRPM=2;
    } else if (argument.ToLower() == "retract") {
        Retract();
        state = State.RETRACT;
    } else {
        //update Tick
        Report();
        if (state == State.MINE ) {
            if (pistons[0].CurrentPosition >= lengthLimits.Max / pistons.Count) {
                Retract();
                state = State.RETRACT;
            }
            if  (( rotor.Angle >= rotorLimits.Max && rotor.TargetVelocityRPM > 0
                 || rotor.Angle <= rotorLimits.Min && rotor.TargetVelocityRPM < 0)
            ) {
                rotor.TargetVelocityRPM = -rotor.TargetVelocityRPM;
                foreach (var piston in pistons) {
                    piston.MaxLimit += 0.1F;
                }
            }
        }
        if (state == State.RETRACT ) {
            if (pistons[0].CurrentPosition <= pistons[0].MinLimit) {
                hingeAngle += 0.1F;
                Mine();
                state = State.MINE;
            }
        }
    }
}
