using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class Actions
{

    public Actions(Node owningNode)
    {
        try { 
            node = owningNode;

            if (Object.Equals(iconTextStyle, default(TextStyle)))
                    iconTextStyle = Text.MakeStyle(Main.iconFont, .5f * U.cm, Color.HSV(0, 0, .8f));
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
    public struct Data
    {
        public int id;
        public int useNodeid;
        public string name;
        public List<OutComes> outcomes;
        public List<int> applicableStates;
        public string prefix;
        public string feedback;

        public Data(int inId, string inName = "")
        {
            id = inId;
            useNodeid = 0;
            name = inName;
            outcomes = new List<OutComes>();
            applicableStates = new List<int>();
            prefix = "use";
            feedback = "";
        }
    }

    public Node node;


    private int maxActionKey = 1;

    public Actions()
    {
    }

    private enum EditActionsState
    {
        ShowOutcomes,
        ChooseActionType,
        SetApplicableStates,
        SelectLocation,
        SelectNode,
        ChooseVisibility,
        SetValue,
        ChooseState,
        ChooseObjectToUse,
        SelectUseNodeLocation,
        SelectUseNode
    }

    public enum ActionType
    {
        AttackWith,
        OnUse,
        OnDestroyed
    }
    
    public enum EditingFlowType
    {
        ChangeState,
        ChangeScore,
        ChangeHealth,
        ChangeVisibility,
        Pickup,
        DamageAnotherNode,
        UseAnObject,
        AddAnAction,
        ShowOutcomes,
        ChooseActionType
    }
    private EditingFlowType editingFlowType;
    
    static private TextStyle iconTextStyle;

    private List<EditActionsState> useFlow = new List<EditActionsState>
    {
        
    };

    private Dictionary<EditingFlowType, List<EditActionsState>> editingFlows = new Dictionary<EditingFlowType, List<EditActionsState>>
    {
            [EditingFlowType.ChangeState] = new List<EditActionsState> {EditActionsState.SelectLocation,EditActionsState.SelectNode, EditActionsState.ChooseState},
            [EditingFlowType.ChangeScore] = new List<EditActionsState> { EditActionsState.SetValue },
            [EditingFlowType.ChangeHealth] = new List<EditActionsState> { EditActionsState.SetValue },
            [EditingFlowType.ChangeVisibility] = new List<EditActionsState> { EditActionsState.SelectLocation, EditActionsState.SelectNode, EditActionsState.ChooseVisibility},
            [EditingFlowType.Pickup] = new List<EditActionsState> { EditActionsState.ShowOutcomes },
            [EditingFlowType.UseAnObject] = new List<EditActionsState> { EditActionsState.SetApplicableStates, EditActionsState.SelectUseNodeLocation, EditActionsState.SelectUseNode, EditActionsState.ShowOutcomes },
            [EditingFlowType.AddAnAction] = new List<EditActionsState> { EditActionsState.SetApplicableStates, EditActionsState.ShowOutcomes },
            [EditingFlowType.ShowOutcomes] = new List<EditActionsState> { EditActionsState.ShowOutcomes },
            [EditingFlowType.ChooseActionType] = new List<EditActionsState> { EditActionsState.ChooseActionType }
    };
    private int editingFlowStep;

    public struct OutComes
    {
        public EditingFlowType type;
        public int nodeId;
        public float value;
        public string text;

        public OutComes(EditingFlowType inType)
        {
            type = inType;
            nodeId = 0;
            value = 0;
            text = "";
        }
    }

    private EditActionsState editActionState = EditActionsState.SetApplicableStates;
    private OutComes workingOutcome = new OutComes();
    private bool updateWorkingOutcomeValue = false;
    private string outcomeValueLabel = "";
    private int currentSelectedLocation = 0;
    private int currentActionOutcomeInx;
    private bool nextAllowed = false;
    private bool backAllowed = false;
    private bool addingNewAction = false;
    private bool addingNewButton = false;

    public SortedDictionary<int, Actions.Data> actionList = new SortedDictionary<int, Actions.Data>();
    public int currentActionKey;
    

    public void EditActions(ref Pose pose)
    {
        try { 
            UI.WindowBegin("MoreInfo", ref pose, UIWin.Body);
            bool stopProcessing = false;
            int counter = 0;
            int count = 0;
            int length = 0;
            string buttonText = "";
            Data newAction;

            bool sameLineRequired = false;

            if (editingFlowStep == -1)
            {
                editingFlowStep = 0;
                //currentSelectedLocation = 0;
                nextAllowed = false;
            }
            backAllowed = editingFlowStep > 0;

            editActionState = editingFlows[editingFlowType][editingFlowStep];

            switch (editActionState)
            {
                case EditActionsState.SelectUseNodeLocation:

                    UI.Label("Select the location of the node to use here:");
                    ShowLocations();
                
                    break;

                case EditActionsState.SelectUseNode:
                    UI.Label("Select the node to be used here:");

                    ShowNodes(actionList[currentActionKey].useNodeid,true);
                    break;


                case EditActionsState.SetApplicableStates:

                    if (editingFlowType == EditingFlowType.UseAnObject)
                    {
                        buttonText = "Delete use button";
                        nextAllowed = true;
                    }
                    else 
                    {
                        buttonText = "Delete action";
                        string str = actionList[currentActionKey].name;
                        UI.Label("Set Button Text =>");
                        UI.SameLine();

                        if (UI.Input("st" + currentActionKey, ref str, V.XY(.08f, .03f)) ||
                            Main.keyboardInput.Update(node.id + "buttonText", true, UI.LayoutAt, Quat.Identity, -.04f, ref str))
                        {
                            newAction = actionList[currentActionKey];
                            newAction.name = str;
                            actionList[currentActionKey] = newAction;
                        }
                    

                        nextAllowed = str != "";
                    
                        UI.SameLine();
                    }

                    if (UI.Button(buttonText))
                    {
                        actionList.Remove(currentActionKey);
                        currentActionKey = 0;
                        node.moreInfoState = Node.MoreInfoState.expanded;
                        stopProcessing = true;
                    }

                    if (stopProcessing == false)
                    {
                        UI.HSeparator();
                        UI.Label("Which node states will show this button?");

                        if (UI.Radio("All states", actionList[currentActionKey].applicableStates.Count == 0))
                        {
                            newAction = actionList[currentActionKey];
                            newAction.applicableStates = new List<int>();
                            actionList[currentActionKey] = newAction;
                        }
                        UI.SameLine();
                        counter = 1;

                        foreach (KeyValuePair<int, Node.State> kvp in node.states)
                        {
                            bool toggleState = actionList[currentActionKey].applicableStates.Contains(kvp.Key);
                            if (UI.Toggle(kvp.Key + ":" + node.states[kvp.Key].name, ref toggleState))
                            {
                                newAction = actionList[currentActionKey];
                                if (toggleState)
                                    newAction.applicableStates.Add(kvp.Key);
                                else
                                    newAction.applicableStates.Remove(kvp.Key);

                                actionList[currentActionKey] = newAction;
                            }

                            if (counter < 2)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                        }

                        UI.Label("");

                        if (UI.Button("Cancel"))
                        {
                            if (addingNewButton)
                            {
                                actionList.Remove(currentActionKey);
                                currentActionKey = 0;
                            }
                            node.moreInfoState = Node.MoreInfoState.expanded;
                        }
                        sameLineRequired = true;
                    }
                    break;

                case EditActionsState.ShowOutcomes:

                    if (editingFlowType == EditingFlowType.UseAnObject)
                        UI.Label("Add what outcomes using the selected node will have.");
                    else
                        UI.Label("Add what outcomes clicking this button will have.");

                    List<OutComes> outcomes = actionList[currentActionKey].outcomes;
                    int inx = 0;
                    int removeInx = -1;

                    addingNewAction = false;

                    foreach (OutComes outcome in outcomes)
                    {
                        string label = "";
                        switch (outcome.type)
                        {
                            case EditingFlowType.ChangeScore:
                                buttonText = "Change score by " + outcome.value.ToString();
                                break;
                            case EditingFlowType.ChangeHealth:
                                buttonText = "Change health by " + outcome.value.ToString();
                                break;
                            case EditingFlowType.ChangeVisibility:
                                if (Main.Nodes.ContainsKey(outcome.nodeId))
                                    label = Main.Nodes[outcome.nodeId].activeState.name;
                                else
                                    label = "INVALID NODE";
                                if (outcome.value == 1)
                                    buttonText = "Show the node " + label;
                                else
                                    buttonText = "Hide the node " + label;
                                break;

                            case EditingFlowType.ChangeState:

                                label = "INVALID";

                                if (Main.Nodes.ContainsKey(outcome.nodeId))
                                {
                                    if (Main.Nodes[outcome.nodeId].states.ContainsKey((int)outcome.value))
                                    {
                                        label = Main.Nodes[outcome.nodeId].activeState.name;
                                    }
                                }

                                buttonText = "Change node state to " + outcome.value.ToString() + " for " + label;
                                break;

                            case EditingFlowType.Pickup:
                                buttonText = "Pick up this node";
                                break;

                            case EditingFlowType.UseAnObject:
                                label = "INVALID OBJECT";
                                if (Main.Nodes.ContainsKey(outcome.nodeId))
                                    if (Main.Nodes[outcome.nodeId].states.ContainsKey((int)outcome.value))
                                    {
                                        label = Main.Nodes[outcome.nodeId].activeState.name;
                                    }

                                buttonText = "Use " + label + " here";
                                break;

                        }

                        if (UI.Button(buttonText)) // Edit
                        {
                            currentActionOutcomeInx = inx;
                            ChangeFlowType(outcome.type);
                        }

                        UI.SameLine();
                        UI.PushTextStyle(iconTextStyle);
                        if (UI.Button("J")) // Delete
                        {
                            removeInx = inx;
                        }
                        UI.PopTextStyle();


                        inx++;
                    }
                    if (removeInx >= 0)
                        actionList[currentActionKey].outcomes.RemoveAt(removeInx);

                    if (UI.Button("Add an new outcome"))
                    {
                        addingNewAction = true;
                        OutComes newOutcome = new OutComes(EditingFlowType.ChangeState);
                        actionList[currentActionKey].outcomes.Add(newOutcome);
                        currentActionOutcomeInx = actionList[currentActionKey].outcomes.Count - 1;
                        ChangeFlowType(EditingFlowType.ChooseActionType);
                    }

                    UI.Space(.02f);

                    if (UI.Button("Done adding actions"))
                        node.moreInfoState = Node.MoreInfoState.expanded;
                
                    nextAllowed = false;
                    backAllowed = false;
                    break;

                case EditActionsState.ChooseActionType:
                    workingOutcome = actionList[currentActionKey].outcomes[currentActionOutcomeInx];
                    if (workingOutcome.nodeId > 0)
                    {
                        if (Main.Nodes.ContainsKey(workingOutcome.nodeId))
                            currentSelectedLocation = Main.Nodes[workingOutcome.nodeId].locationId;
                    }

                    if (UI.Button("Change Node State"))
                    {
                        workingOutcome.type = EditingFlowType.ChangeState;
                        ChangeFlowType(workingOutcome.type);
                    }
                    UI.SameLine();
                    if (UI.Button("Change Visibility"))
                    {
                        workingOutcome.type = EditingFlowType.ChangeVisibility;
                        ChangeFlowType(workingOutcome.type);
                    }
                    if (UI.Button("Change Score"))
                    {
                        workingOutcome.type = EditingFlowType.ChangeScore;
                        ChangeFlowType(workingOutcome.type);
                        outcomeValueLabel = "Change score by (+,-)";
                    }
                    UI.SameLine();
                    if (UI.Button("Change User's Health"))
                    {
                        workingOutcome.type = EditingFlowType.ChangeHealth;
                        ChangeFlowType(workingOutcome.type);
                        outcomeValueLabel = "Change health by (+,-), starts at 100";
                    }
                    if (UI.Button("Pick up this node"))
                    {
                        workingOutcome.type = EditingFlowType.Pickup;
                        workingOutcome.nodeId = node.id;
                        nextAllowed = true;
                        MoveNextInFlow();
                    }

                
                    if (UI.Button("Back"))
                    {
                        if (addingNewAction)
                            actionList[currentActionKey].outcomes.RemoveAt(currentActionOutcomeInx);

                        workingOutcome.type = EditingFlowType.ShowOutcomes;
                        ChangeFlowType(workingOutcome.type);
                    }
                
                    break;

                case EditActionsState.SelectLocation:
                    ShowAffectThisNodeButton(true);
                
                    UI.Label("select the location of the target node:");
                    ShowLocations();

                    if (UI.Button("Cancel"))
                    {
                        if (addingNewAction)
                            actionList[currentActionKey].outcomes.RemoveAt(currentActionOutcomeInx);
                        ChangeFlowType(EditingFlowType.ShowOutcomes);
                    }

                    sameLineRequired = true;

                    break;

                case EditActionsState.SelectNode:
                    ShowAffectThisNodeButton(false);

                    UI.Label("select the node:");
                    ShowNodes(workingOutcome.nodeId, false);
                    break;

                case EditActionsState.ChooseVisibility:
                    if (UI.Radio("Show node", workingOutcome.value == 1f))
                    {
                        workingOutcome.value = 1f;
                        MoveNextInFlow();
                    }
                    UI.SameLine();
                    if (UI.Radio("Hide node", workingOutcome.value == 0f))
                    {
                        workingOutcome.value = 0f;
                        MoveNextInFlow();
                    }

                    break;

                case EditActionsState.SetValue:
                    UI.Label(outcomeValueLabel);

                    if (UI.Input("outcomeValue", ref workingOutcome.text) ||
                        Main.keyboardInput.Update(node.id + "SetValue", true, UI.LayoutAt, Quat.Identity, -.04f, ref workingOutcome.text))
                    {
                        updateWorkingOutcomeValue = true;
                    }

                    nextAllowed = true;

                    break;

                case EditActionsState.ChooseState:
                    UI.Label("Select the new state for the selected node");
                    length = Main.Nodes[workingOutcome.nodeId].states.Count;
                    foreach (KeyValuePair<int, Node.State> kvp in Main.Nodes[workingOutcome.nodeId].states)
                    {
                        if (UI.Radio(kvp.Key + ":" + kvp.Value.name, workingOutcome.value == kvp.Key))
                        {
                            workingOutcome.value = kvp.Key;
                            MoveNextInFlow();
                        }

                        if (counter < 2 && count < length - 1)
                            UI.SameLine();
                        counter++;
                        counter %= 3;
                        count++;
                    }

                    nextAllowed = workingOutcome.value > 0;

                    break;

            }

            if (backAllowed)
            {
                if (sameLineRequired)
                {
                    UI.SameLine();
                }

                if (UI.Button("Back"))
                {
                    editingFlowStep--;
                }
                sameLineRequired = true;
            }
        
            if (nextAllowed)
            {
                if (sameLineRequired)
                    UI.SameLine();

                if (updateWorkingOutcomeValue)
                {
                    updateWorkingOutcomeValue = false;
                    try
                    {
                        workingOutcome.value = float.Parse(Regex.Replace(workingOutcome.text, "[^0-9\\.\\+\\-]", ""));
                    }
                    catch (Exception)
                    {
                        workingOutcome.value = 0;
                    }
                }
                if (editingFlowStep < editingFlows[editingFlowType].Count - 1)
                {
                    if (UI.Button("Next"))
                        MoveNextInFlow();
                }
                else
                {
                    if (UI.Button("Done"))
                    {
                        MoveNextInFlow();
                    }
                }

                if (editingFlowStep >= editingFlows[editingFlowType].Count)
                {
                    actionList[currentActionKey].outcomes[currentActionOutcomeInx] = workingOutcome;
                    ChangeFlowType(EditingFlowType.ShowOutcomes);
                }

            }

            UI.WindowEnd();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }



    private void ExecuteActions(int actionKey)
    {
        try { 
            bool ignore;
            if (actionList.ContainsKey(actionKey))
            {
                foreach (OutComes outcome in actionList[actionKey].outcomes)
                {
                    ignore = false;
                    if (outcome.nodeId != 0)
                        ignore = Main.Nodes.ContainsKey(outcome.nodeId) == false;

                    if (!ignore)
                    {
                        switch (outcome.type)
                        {
                            case EditingFlowType.ChangeScore:
                                Main.score += outcome.value;
                                break;
                            case EditingFlowType.ChangeHealth:
                                Main.health += outcome.value;
                                break;
                            case EditingFlowType.ChangeVisibility:
                                if (Main.Nodes[outcome.nodeId].available)
                                {
                                    if (outcome.value == 1)
                                        Main.Nodes[outcome.nodeId].visible = true;
                                    else
                                        Main.Nodes[outcome.nodeId].visible = false;
                                }
                                break;
                            case EditingFlowType.ChangeState:
                                Main.Nodes[outcome.nodeId].ChangeActiveState((int)outcome.value, false);
                                break;
                            case EditingFlowType.Pickup:
                                {
                                    if (Main.AddObjectToSlot(node.id))
                                        node.visible = false;
                                    node.available = false;
                                }

                                break;
                        }
                    }

                }
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void Draw()
    {
        int counter = 0;
        int length;
        int count = 0;

        try { 

            if (Main.menuState == Main.MenuState.EditNodes)
            {
                UI.Label("Click buttons below to edit existing actions or add new ones");
            }
            length = actionList.Count;
            bool AddGeneralUseButton = false;

            bool ignoreButton;

            foreach (KeyValuePair<int, Actions.Data> kvp in actionList)
            {
                ignoreButton = true;
                // Ignore buttons that are not applicable to the current state
                if (Main.menuState == Main.MenuState.EditNodes)
                    ignoreButton = false;
                else
                {
                    if (kvp.Value.applicableStates.Count == 0)
                        ignoreButton = false;
                    else
                    {
                        if (kvp.Value.applicableStates.Contains(node.activeStateKey))
                            ignoreButton = false;
                    }
                }

                if (ignoreButton == false)
                {

                    if (kvp.Value.useNodeid > 0) // This is a use button
                    {
                        if (Main.menuState == Main.MenuState.EditNodes)
                        {
                            if (Main.Nodes.ContainsKey(kvp.Value.useNodeid))
                            {
                                if (UI.Button("Use " + Main.Nodes[kvp.Value.useNodeid].activeState.name))
                                {
                                    addingNewButton = false;
                                    node.moreInfoState = Node.MoreInfoState.editActions;
                                    ChangeFlowType(EditingFlowType.UseAnObject);
                                
                                    currentActionKey = kvp.Key;
                                    currentSelectedLocation = Main.Nodes[kvp.Value.useNodeid].locationId;
                                }
                            }
                        }
                        else
                            AddGeneralUseButton = true;
                    }
                    else
                    {
                        if (kvp.Value.name != "")
                        {
                            if (UI.Button(kvp.Value.name))
                            {
                                if (Main.menuState == Main.MenuState.EditNodes)
                                {
                                    addingNewButton = false;
                                    node.moreInfoState = Node.MoreInfoState.editActions;
                                    ChangeFlowType(EditingFlowType.AddAnAction);
                                    currentActionKey = kvp.Key;
                                }
                                else
                                {
                                    ExecuteActions(kvp.Key);
                                }
                            }
                        }
                    }
                    if (counter < 2 && count < length - 1)
                        UI.SameLine();
                    counter++;
                    counter %= 3;
                    count++;
                }
            }

            if (AddGeneralUseButton)
            {
                string heldObject = "[Nothing held]";
                if (Main.inventoryNodeIdHeld > 0)
                    heldObject = Main.Nodes[Main.inventoryNodeIdHeld].activeState.name;

                if (UI.Button("Use " + heldObject))
                {
                    foreach (KeyValuePair<int, Data> kvp in actionList)
                    {
                        if (kvp.Value.useNodeid > 0 && kvp.Value.useNodeid == Main.inventoryNodeIdHeld)
                            ExecuteActions(kvp.Key);
                    }

                }
            }


            if (Main.menuState == Main.MenuState.EditNodes)
            {
                if (UI.Button("Add Action"))
                {
                    addingNewButton = true;
                    actionList.Add(maxActionKey, new Data(maxActionKey, ""));
                    currentActionKey = maxActionKey;
                    maxActionKey++;
                    node.moreInfoState = Node.MoreInfoState.editActions;
                    ChangeFlowType(EditingFlowType.AddAnAction);

                }
                UI.SameLine();
                if (UI.Button("Use held node here"))
                {
                    addingNewButton = true;
                    actionList.Add(maxActionKey, new Data(maxActionKey, ""));
                    currentActionKey = maxActionKey;
                    maxActionKey++;
                    node.moreInfoState = Node.MoreInfoState.editActions;
                    ChangeFlowType(EditingFlowType.UseAnObject);
                }
            }
            else
            {
                UI.Label("");
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public bool DoActionsExist(int activeStateKey)
    {
        bool response = false;
        try { 
            foreach (KeyValuePair<int, Data> kvp in actionList)
            {
                if (kvp.Value.applicableStates.Count == 0)
                {
                    response = true;
                    break;
                }
                else
                {
                    if (kvp.Value.applicableStates.Contains(activeStateKey))
                        return true;
                }
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
        return response;
    }

    public string[] Serialise()
    {

        List<string> subStr = new List<string>();
        try { 
            foreach (KeyValuePair<int, Data> kvp in actionList)
            {
                List<string> subSubStr = new List<string>();
                foreach (OutComes outcomes in kvp.Value.outcomes)
                {
                    subSubStr.Add(SH.Serialise((int)outcomes.type) + SH.actionOutcomeFieldSeperator + SH.Serialise(outcomes.nodeId) + SH.actionOutcomeFieldSeperator + SH.Serialise(outcomes.value) + SH.actionOutcomeFieldSeperator + SH.Serialise(outcomes.text));
                }

                subStr.Add(SH.Serialise(kvp.Value.id) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.useNodeid) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.name) + SH.arrayElementSeperator +
                    string.Join(SH.actionOutcomeArraySeperator, subSubStr.ToArray()) + SH.arrayElementSeperator +
                    SH.Serialise(kvp.Value.applicableStates));
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
        return subStr.ToArray();
    }

    public void UnSerialise(string input)
    {
        try { 

            string[] subStr = input.Split(SH.arraySeperator);
            Data newAction;
            OutComes newoutcome;
            string[] outcomeBits;
            string[] subSubstr;
            int outcomeType = 0;
            actionList = new SortedDictionary<int, Data>();
            string[] bits;

            foreach (string str in subStr)
            {
                newAction = new Data();
                newAction.outcomes = new List<OutComes>();
                bits = str.Split(SH.arrayElementSeperator);
                if (bits.Length >= 4)
                {
                    SH.UnSerialise(bits[0], ref newAction.id);
                    SH.UnSerialise(bits[1], ref newAction.useNodeid);
                    SH.UnSerialise(bits[2], ref newAction.name);
                    subSubstr = bits[3].Split(SH.actionOutcomeArraySeperator);
                    foreach (string strOutcome in subSubstr)
                    {
                        if (strOutcome != "")
                        {
                            newoutcome = new OutComes();
                            outcomeBits = strOutcome.Split(SH.actionOutcomeFieldSeperator);
                            SH.UnSerialise(outcomeBits[0], ref outcomeType);
                            newoutcome.type = (EditingFlowType)outcomeType;
                            SH.UnSerialise(outcomeBits[1], ref newoutcome.nodeId);
                            SH.UnSerialise(outcomeBits[2], ref newoutcome.value);
                            SH.UnSerialise(outcomeBits[3], ref newoutcome.text);
                            newAction.outcomes.Add(newoutcome);
                        }
                    }
                    if (bits.Length >= 5)
                        SH.UnSerialise(bits[4], ref newAction.applicableStates);
                    else
                        newAction.applicableStates = new List<int>();
                    actionList.Add(newAction.id, newAction);
                }

                if (actionList.Count > 0)
                    maxActionKey = actionList.Keys.ToArray().Max() + 1;
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void ShowAffectThisNodeButton(bool selectingLocation)
    {
        try { 
            if (UI.Button("Affect this node"))
            {
                workingOutcome.nodeId = node.id;
                currentSelectedLocation = node.locationId;
                MoveNextInFlow();
                if (selectingLocation)
                    MoveNextInFlow();

            }
            UI.SameLine();
            UI.Label("Or...");
            UI.HSeparator();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void MoveNextInFlow()
    {
        editingFlowStep++;
    }
    private void ShowLocations()
    {
        try { 
            nextAllowed = false;
            int counter=0;
            int count = 0;
            int length = Main.LocationNames.Count;
            foreach (KeyValuePair<int, string> kvp in Main.LocationNames)
            {
                if (UI.Radio(kvp.Value, kvp.Key == currentSelectedLocation))
                {
                    currentSelectedLocation = kvp.Key;
                    MoveNextInFlow();
                }
                if (counter < 2 && count < length - 1)
                    UI.SameLine();
                counter++;
                counter %= 3;
                count++;
            }

            if (currentSelectedLocation > 0)
                nextAllowed = true;

            UI.HSeparator();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void ShowNodes(int targetKey, bool selectUseNodeLocation)
    {
        try { 
            Data newAction = new Data();
            int counter = 0;
            nextAllowed = false;
            foreach (KeyValuePair<int, Node> kvp in Main.Nodes)
            {
                if (kvp.Value.locationId == currentSelectedLocation)
                {
                    if (UI.Radio(kvp.Value.activeState.name, kvp.Key == targetKey))
                    {
                        if (selectUseNodeLocation)
                        {
                            newAction = actionList[currentActionKey];
                            newAction.useNodeid = kvp.Key;
                            actionList[currentActionKey] = newAction;
                        }
                        else
                        {
                            workingOutcome.nodeId = kvp.Key;
                        }
                        MoveNextInFlow();
                        nextAllowed = true;
                    }
                    if (counter < 2)
                        UI.SameLine();
                    counter++;
                    counter %= 3;
                }
            }
            if ((selectUseNodeLocation && actionList[currentActionKey].useNodeid > 0) ||
                (selectUseNodeLocation == false && workingOutcome.nodeId > 0))
                nextAllowed = true;
        
            UI.Label("");

            UI.HSeparator();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void ChangeFlowType(EditingFlowType newType)
    {
        editingFlowType = newType;
        editingFlowStep = -1;
    }
}
