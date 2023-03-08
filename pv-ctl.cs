//Edit the value below to adjust the sensitivity
int sensitivity = 40;
List<IMyTerminalBlock> trackingPanels = new List<IMyTerminalBlock>(); 

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    GridTerminalSystem.SearchBlocksOfName("[SENSOR]", trackingPanels);  
    Echo($@"Found panels: {trackingPanels.Count}");
}

void Main() {   
 
    for(int i = 0; i < trackingPanels.Count; i++){ 
        IMySolarPanel trackingPanel = trackingPanels[i] as IMySolarPanel; 
        Echo($@"panel {trackingPanel.CustomName}");
 
        IMyMotorStator r = GridTerminalSystem.GetBlockWithName(trackingPanel.CustomData) as IMyMotorStator; 
        Echo($@"rotor {r.CustomName}");
        if(r != null) trackSun(trackingPanel, r); 
    } 
}   
   
public int GetPanelPower(IMySolarPanel panel) {   
    var _d = panel.DetailedInfo;    
    Echo(_d);
    string _power = _d.Split(new string[] {"\n"}, StringSplitOptions.None)[1].Split(' ')[2]; //Checking the MAX Output   
    int _powerOutput = Convert.ToInt32(Math.Round(Convert.ToDouble(_power)));     
    return _powerOutput;   
}   
   
public void RotatePanel(IMyMotorStator rotor, int maxSpeed) {  
   if (rotor.TargetVelocityRPM < maxSpeed) rotor.SetValueFloat("Velocity", maxSpeed);  
} 
 
public void trackSun(IMySolarPanel panel, IMyMotorStator rotor) { 
    if (panel != null){      
        int powerOutput = GetPanelPower(panel);  
        if (powerOutput >= sensitivity) rotor.GetActionWithName("ResetVelocity").Apply(rotor); 
        else RotatePanel(rotor,1);  
    }  
}