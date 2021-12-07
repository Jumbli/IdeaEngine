using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

static public class Data
{

    public struct NodeModel
    {
        public string name;
        public string filename;
        public NodeModel(string inName, string inFilename)
        {
            name = inName;
            filename = inFilename;
        }
    }

    public static int locationId = 0;
    public struct Location
    {
        public int id;
        public string name;
        public Matrix TRS;
        public int lightingId;
        public int backgroundSoundId;
       
    }

    public static int nodeId=0;
    public struct Relative
    {
        public int id;
        public string name; 
        public Color color;
        public Vec2 textSize;
        public Vec3 textPosition;
        public Quat textOrientation;
        
        public Relative(int inId, Color inColor, string inName="")
        {
            id = inId;
            name = inName;
            color = inColor;
            textSize = new Vec2();
            textPosition = new Vec3();  
            textOrientation = new Quat();  

        }
        
    }


    public class Node
    {

        public struct State
        {
            public int id;
            public string name;
            public Color color;
            public string notes;
            public float nodeScale;
            public string modelFilename;
            public string spriteFilename;
            public string musicFilename;

            public State(int inId, string inName, string inModelFilename = "")
            {
                id = inId;
                name = inName;
                color = Color.White;
                notes = "";
                nodeScale = 1;
                modelFilename = inModelFilename;
                spriteFilename = "";
                musicFilename = "";
            }
        }

        public SortedDictionary<int, State> states = new SortedDictionary<int,State>();
        public int activeStateKey = 1;
        public int maxStateKey=1;

        public bool isLocation;
        public bool isPortal;
        public int destinationId;
        public int id;
        public Dictionary<int, Relative> relatives;
        public int parent;
        public int locationId;
        public bool moveable;
        public bool movesWithParent;
        public Pose pose;
        public Model model;

        public bool visibleAtStart=true;

        public bool visible;
        public bool available=true;


        public Sprite sprite;

        public string defaultName;
       

        public float health;
        private Vec3 parentDragPoint;
        private float calculatedScale=1f;
        private Vec3 dimensions = Vec3.One;
        public float radius=1;
        public SortedDictionary<int,Action> actions = new SortedDictionary<int,Action>();
        public int maxActionKey = 1;
        private Vec3 assetDimensions = Vec3.One;
        public bool inFocus;
        public bool propertiesDisplayed;
        public bool grabbed;

        float _hue = 0;
        float _saturation = 0;
        float _value = 1;

        private int advancedStartAt=0;

        private enum EditState
        {
            ready,
            editingTitle,
            linkingParent,
            editingDescription,
            editingLink
        }

        public enum ActionType
        {
            ChangeState,
            ChangeScore,
            ChangeHealth,
            ChangeVisibility,
            Pickup,
            UseAnObject
        }

        public struct Action
        {
            public int id;
            public int useNodeid;
            public string name;
            public List<ActionStep> steps;
            public List<int> applicableStates;
            public Action(int inId, string inName="")
            {
                id = inId;
                useNodeid = 0;
                name = inName;
                steps = new List<ActionStep>();
                applicableStates = new List<int>();
            }
        }
        public struct ActionStep
        {
            public ActionType type;
            public int nodeId;
            public float value;
            public string text;

            public ActionStep(ActionType inType)
            {
                type = inType;
                nodeId = 0;
                value = 0;
                text = "";
            }
        }

        private enum MoreInfoState
        {
            collapsed,
            expanded,
            editActions
        }

        private MoreInfoState moreInfoState = MoreInfoState.collapsed;
        
        private int editingLinkId;



        private EditState editState;
        public string GetDefaultName()
        {
            string result;
            if (isLocation)
            {
                result = "location #" + id;
            }
            else
            {
                result = "Node #" + id;
            }

            return result;
        }

        public Node(string serialisedData)
        {
            UnSerialise(serialisedData);
            if (activeState.spriteFilename == "")
                OnLoadModel(activeState.modelFilename);
            else
                OnLoadImage(activeState.spriteFilename);

            Data.nodeId = Math.Max(Data.nodeId, id);
            visible = visibleAtStart;
        }
        public Node(bool inIsLocation, string inName, Pose inPose, bool inIsPortal = false, string inModelFilename="sphere.obj")
        {
            Data.nodeId++;
            id = Data.nodeId;

            isLocation = inIsLocation;
            isPortal = inIsPortal;
            pose = inPose;

            inName = GetDefaultName();
            if (isLocation)
                Main.LocationNames[id] = inName;

            defaultName = inName;

            // States
            states.Add(maxStateKey, new Node.State(maxStateKey, inName, inModelFilename));
            ChangeActiveState(maxStateKey, false);
            maxStateKey++;

            visibleAtStart = true;

            relatives = new Dictionary<int, Relative>();
            locationId = Main.locationId;
            
            health = 100;
            parentDragPoint = Vec3.Zero;
            editState = EditState.ready;
            
            OnLoadModel(activeState.modelFilename);
            destinationId = 0;
        }

        public void ChangeActiveState(int newKey, bool updateBeforeChange)
        {
            if (updateBeforeChange)
                states[activeStateKey] = activeState;

            activeStateKey = newKey;
            activeState = states[activeStateKey];
            if (activeState.modelFilename != "")
                OnLoadStandardModel(activeState.modelFilename);
            if (activeState.spriteFilename != "")
                OnLoadStandardImage(activeState.spriteFilename);
            if (isLocation)
                Main.PlayMusicForLocation();
           
        }


        private Vec2 titleSize;
        private Matrix titlePosition;
        private bool draw = false;
        private Pose moreInfoPose = new Pose();
        private Vec2 notesSize = new Vec2(.2f,.055f);

        private int currentActionKey;
        private int currentActionStepInx;
        private int sinx;
        public State activeState;
        public void Draw()
        {
            UI.PushId("" + id);

            draw = Vec3.Dot((pose.position - Input.Head.position).Normalized, Input.Head.Forward) > 0;
            titleSize = Text.Size(activeState.name, Main.titleStyle);
            actionCooldown = Math.Max(0, actionCooldown - Main.deltaTime);

            int counter = 0;
            int count=0;
            int length = 0;

            Controller c = Input.Controller(Handed.Right);
            if (c.IsX1Pressed)
            {
                Renderer.Screenshot("G:\\IdeaEngine\\scrshot" + Time.Elapsedf + ".jpg", Input.Head.position, Input.Head.Forward, 1920, 1080);

                sinx++;
            }

            if (draw)
            {

                if (activeState.spriteFilename == "")
                {
                    calculatedScale = (.12f * activeState.nodeScale) / Math.Max(Math.Max(model.Bounds.dimensions.x, model.Bounds.dimensions.y), model.Bounds.dimensions.z);
                    dimensions = model.Bounds.dimensions * calculatedScale;
                    dimensions.x = MathF.Max(.05f, dimensions.x);
                    dimensions.y = MathF.Max(.05f, dimensions.y);
                    dimensions.z = MathF.Max(.05f, dimensions.z);
                    radius = dimensions.Length /2;
                    model.Draw(Matrix.TRS(pose.position, pose.orientation, calculatedScale * .9f), activeState.color);
                }
                else
                {
                    calculatedScale = .12f * activeState.nodeScale;
                    dimensions = Vec3.One * calculatedScale;
                    dimensions.z = .05f;
                    radius = dimensions.Length;
                    sprite.Draw(Matrix.TRS(pose.position + pose.Up * (calculatedScale/2) + pose.Right * (calculatedScale / 2) * -1
                        , pose.orientation, calculatedScale * 
                        V.XYZ(sprite.NormalizedDimensions.x, sprite.NormalizedDimensions.y,1)), activeState.color);
                }

                titlePosition = Matrix.TRS(pose.position +
                    pose.Up * ((dimensions.y /2) + titleSize.y / 2 + .03f)
                    , pose.orientation, 1f);
                Text.Add(activeState.name, titlePosition, Main.titleStyle, TextAlign.Center, TextAlign.Center);

                moreInfoPose.position = pose.position + 
                    pose.Forward * ((dimensions.z / 2) + .02f) +
                    pose.Up * ((dimensions.y / -2) + -.00f);
                moreInfoPose.orientation = pose.orientation;


                switch (moreInfoState)
                {
                    case MoreInfoState.collapsed:
                        if (CheckIfMoreWindowRequired()) { 
                            UI.WindowBegin("MoreInfo", ref moreInfoPose, UIWin.Empty);
                            if (states.Count > 1 && Main.menuState == Main.MenuState.EditNodes)
                        {
                            Text.Add("State: " + activeStateKey,
                                Matrix.Identity, Main.titleStyle, TextAlign.BottomCenter, TextAlign.TopCenter);
                        }
                            if (isPortal)
                        {
                            string destinationName = "New location";
                            if (destinationId > 0)
                            {
                                if (Main.LocationNames.ContainsKey(destinationId))
                                    destinationName = Main.LocationNames[destinationId];
                            }
                            if (UI.Button("Go to\n" + destinationName))
                            {
                                Main.GoToLocation(destinationId);
                                destinationId = Main.locationId; // Update in case a new node was created
                            }
                        }
                            else
                        {
                            if (UI.Button("More"))
                                moreInfoState = MoreInfoState.expanded;
                        }
                            UI.WindowEnd();
                        }

                        break;

                    case MoreInfoState.expanded:
                        moreInfoPose.position = moreInfoPose.position + (moreInfoPose.orientation * Vec3.Forward) * .02f;
                        UI.WindowBegin("MoreInfo", ref moreInfoPose, UIWin.Body);
                        notesSize = Text.Size(activeState.notes, Main.generalTextStyle);
                        if (Main.menuState == Main.MenuState.EditNodes)
                            notesSize.x = MathF.Max(.25f, notesSize.x);
                        else
                            notesSize.x = MathF.Max(.1f, notesSize.x);
                        notesSize.y = MathF.Max(.01f, notesSize.y);
                        if (Main.menuState == Main.MenuState.EditNodes)
                            UI.Label("Type notes here. The box will expand to fit the width and height");
                        UI.LayoutReserve(notesSize);

                        Mesh.Cube.Draw(Material.Unlit,
                            Matrix.TRS(UI.LayoutLast.center + Vec3.Forward * .001f, Quat.Identity, UI.LayoutLast.dimensions + V.XYZ(.01f,.01f,0)),
                            Color.White);
                        
                        Text.Add(activeState.notes, Matrix.T(UI.LayoutLast.center + Vec3.Forward * .002f), notesSize, 
                            TextFit.Clip, Main.generalTextStyle,
                            TextAlign.Center, TextAlign.TopLeft);
                        
                        Main.keyboardInput.Update(ref activeState.notes);

                        if (Main.menuState == Main.MenuState.EditNodes)
                        {
                            UI.Label("Click buttons below to edit existing actions or add new ones");
                        }
                        length = actions.Count;
                        bool AddGeneralUseButton = false;

                        bool ignoreButton;

                        foreach (KeyValuePair<int,Action> kvp in actions)
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
                                    if (kvp.Value.applicableStates.Contains(activeStateKey))
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
                                                moreInfoState = MoreInfoState.editActions;
                                                editActionState = EditActionsState.SetApplicableStates;
                                                addingUseAndSelectingNodeToUse = true;
                                                addingUse = true;
                                                currentActionKey = kvp.Key;
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
                                                moreInfoState = MoreInfoState.editActions;
                                                editActionState = EditActionsState.SetApplicableStates;
                                                currentActionKey = kvp.Key;
                                                addingUseAndSelectingNodeToUse = false;
                                                addingUse = false;
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
                                foreach (KeyValuePair<int, Action> kvp in actions)
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
                                actions.Add(maxActionKey, new Action(maxActionKey, ""));
                                currentActionKey = maxActionKey;
                                maxActionKey++;
                                moreInfoState = MoreInfoState.editActions;
                                editActionState = EditActionsState.SetApplicableStates;
                                addingUseAndSelectingNodeToUse = false;
                                addingUse = false;
                            }
                            UI.SameLine();
                            if (UI.Button("Use held node here"))
                            {
                                actions.Add(maxActionKey, new Action(maxActionKey, ""));
                                currentActionKey = maxActionKey;
                                maxActionKey++;
                                moreInfoState = MoreInfoState.editActions;
                                editActionState = EditActionsState.SetApplicableStates;
                                addingUseAndSelectingNodeToUse = true;
                                addingUse = true;   
                            }
                        } else
                        {
                            UI.Label("");
                        }


                        UI.HSeparator();
                        if (UI.Button("Close"))
                            moreInfoState = MoreInfoState.collapsed;

                        UI.WindowEnd();
                        break;

                    case MoreInfoState.editActions:
                        EditActions();
                        break;
                }
                
            }

            DrawLines();
            propertiesDisplayed = false;

            if (Main.menuState != Main.MenuState.Play)
            {
                if (inFocus || editState != EditState.ready)
                    MakeEditable();

            }
            UI.PopId();

            if (isLocation)
            {
                if (activeState.musicFilename != "")
                    if (Main.musicInst.IsPlaying == false)
                        Main.PlayMusicForLocation();
            }

        }

        private bool CheckIfMoreWindowRequired()
        {
            if (Main.menuState == Main.MenuState.EditNodes ||
                activeState.notes.Length > 0 || isPortal)
                return true;
            
            bool response = false;
            foreach (KeyValuePair <int,Action> kvp in actions)
            {
                if (kvp.Value.applicableStates.Count == 0)
                {
                    response = true;
                    break;
                } else
                {
                    if (kvp.Value.applicableStates.Contains(activeStateKey))
                        return true;
                }
            }

            return response;

        }

        private enum EditActionsState
        {
            ShowActionSteps,
            ChooseActionType,
            SetApplicableStates,
            SelectLocation,
            SelectNode,
            ChooseVisibility,
            SetValue,
            ChooseState,
            Complete,
            ChooseObjectToUse

        }

        private void ExecuteActions(int actionKey)
        {
            bool ignore;
            if (actions.ContainsKey(actionKey))
            {
                foreach (ActionStep step in actions[actionKey].steps)
                {
                    ignore = false;
                    if (step.nodeId != 0)
                        ignore = Main.Nodes.ContainsKey(step.nodeId) == false;

                    if (!ignore)
                    {
                        switch (step.type)
                        {
                            case ActionType.ChangeScore:
                                Main.score += step.value;
                                break;
                            case ActionType.ChangeHealth:
                                Main.health += step.value;
                                break;
                            case ActionType.ChangeVisibility:
                                if (Main.Nodes[step.nodeId].available)
                                {
                                    if (step.value == 1)
                                        Main.Nodes[step.nodeId].visible = true;
                                    else
                                        Main.Nodes[step.nodeId].visible = false;
                                }
                                break;
                            case ActionType.ChangeState:
                                Main.Nodes[step.nodeId].ChangeActiveState((int)step.value,false);
                                break;
                            case ActionType.Pickup:
                                {
                                    if (Main.AddObjectToSlot(id))
                                        visible = false;
                                    available = false;
                                }

                                break;
                        }
                    }

                }
            }

        }
        private EditActionsState editActionState = EditActionsState.SetApplicableStates;
        private ActionStep workingStep = new ActionStep();
        private string stepValueLabel = "";
        private int currentSelectedLocation = 0;
        private bool addingUseAndSelectingNodeToUse = false;
        private bool addingUse = false;

        private void EditActions()
        {
         
            UI.WindowBegin("MoreInfo", ref moreInfoPose, UIWin.Body);

            int counter=0;
            int count = 0;
            int length = 0;
            string buttonText = "";
            Action newAction;

            switch (editActionState)
            {
                case EditActionsState.SetApplicableStates:
                    bool stopProcessing = false;
                    if (addingUse)
                    {
                        if (UI.Button("Delete Use"))
                        {
                            actions.Remove(currentActionKey);
                            currentActionKey = 0;
                            moreInfoState = MoreInfoState.expanded;
                            stopProcessing = true;
                        }
                    }
                    else
                    {
                        string str = actions[currentActionKey].name;
                        UI.Label("Set Button Text =>");
                        UI.SameLine();

                        UI.Input("st" + currentActionKey, ref str, V.XY(.08f, .03f));
                        UI.SameLine();
                        if (UI.Button("Delete Action"))
                        {
                            actions.Remove(currentActionKey);
                            currentActionKey = 0;
                            moreInfoState = MoreInfoState.expanded;
                            stopProcessing = true;
                        }

                        if (stopProcessing == false)
                        {
                            newAction = actions[currentActionKey];
                            newAction.name = str;
                            actions[currentActionKey] = newAction;
                        }

                    }
                    if (stopProcessing == false)
                    {
                        UI.HSeparator();
                        UI.Label("Which node states will show this button?");

                        if (UI.Radio("All states", actions[currentActionKey].applicableStates.Count == 0))
                        {
                            newAction = actions[currentActionKey];
                            newAction.applicableStates = new List<int>();
                            actions[currentActionKey] = newAction;
                        }
                        UI.SameLine();
                        counter = 1;

                        foreach (KeyValuePair<int, State> kvp in states)
                        {
                            bool toggleState = actions[currentActionKey].applicableStates.Contains(kvp.Key);
                            if (UI.Toggle(kvp.Key + ":" + states[kvp.Key].name, ref toggleState))
                            {
                                newAction = actions[currentActionKey];
                                if (toggleState)
                                    newAction.applicableStates.Add(kvp.Key);
                                else
                                    newAction.applicableStates.Remove(kvp.Key);

                                actions[currentActionKey] = newAction;
                            }

                            if (counter < 2)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                        }

                        UI.Label("");

                        if (actions[currentActionKey].name != "" || addingUse == true)
                        {
                            if (UI.Button("Next"))
                            {
                                if (addingUse == true)
                                    editActionState = EditActionsState.SelectLocation;
                                else
                                    editActionState = EditActionsState.ShowActionSteps;
                            }
                        }
                    }
                    break;

                case EditActionsState.ShowActionSteps:
                    
                    if (addingUse)
                        UI.Label("Add what outcomes using the selected node will have.");
                    else
                        UI.Label("Add what outcomes clicking this button will do.");
                    
                    List<ActionStep> steps = actions[currentActionKey].steps;
                    int inx = 0;
                    int removeInx = -1;

                    foreach (ActionStep step in steps)
                    {
                        if (UI.Button("Delete line " + (inx +1))) // Delete
                        {
                            removeInx = inx;
                        }
                        UI.SameLine();

                        string label="";
                        switch (step.type)
                        {
                            case ActionType.ChangeScore:
                                buttonText = "Change score by " + step.value.ToString();
                                break;
                            case ActionType.ChangeHealth:
                                buttonText = "Change health by " + step.value.ToString();
                                break;
                            case ActionType.ChangeVisibility:
                                if (Main.Nodes.ContainsKey(step.nodeId))
                                    label = Main.Nodes[step.nodeId].activeState.name;
                                else
                                    label = "INVALID NODE";
                                if (step.value == 1)
                                    buttonText = "Show the node " + label;
                                else
                                    buttonText = "Hide the node " + label;
                                break;
                            
                            case ActionType.ChangeState:

                                label = "INVALID";

                                if (Main.Nodes.ContainsKey(step.nodeId))
                                {
                                    if (Main.Nodes[step.nodeId].states.ContainsKey((int)step.value))
                                    {
                                        label = Main.Nodes[step.nodeId].activeState.name;
                                    }
                                }

                                buttonText = "Change node state to " + step.value.ToString() + " for " + label;
                                break;

                            case ActionType.Pickup:
                                buttonText = "Pick up this node";
                                break;

                            case ActionType.UseAnObject:
                                label = "INVALID OBJECT";
                                if (Main.Nodes.ContainsKey(step.nodeId))
                                    if (Main.Nodes[step.nodeId].states.ContainsKey((int)step.value))
                                    {
                                        label = Main.Nodes[step.nodeId].activeState.name;
                                    }

                                buttonText = "Use " + label + " here";
                                break;

                        }

                        if (UI.Button(buttonText)) // Edit
                        {
                            currentActionStepInx = inx;
                            editActionState = EditActionsState.ChooseActionType;
                        }

                        inx++;
                    }
                    if (removeInx >= 0)
                        actions[currentActionKey].steps.RemoveAt(removeInx);

                    if (UI.Button("Add an new outcome"))
                    {
                        ActionStep newStep = new ActionStep(ActionType.ChangeState);
                        actions[currentActionKey].steps.Add(newStep);
                        currentActionStepInx = actions[currentActionKey].steps.Count - 1;
                        editActionState = EditActionsState.ChooseActionType;
                    }

                    UI.Space(.02f);
                    
                    if (UI.Button("Done"))
                        moreInfoState = MoreInfoState.expanded;

                    break;

                case EditActionsState.ChooseActionType:
                    workingStep = actions[currentActionKey].steps[currentActionStepInx];
                    if (workingStep.nodeId > 0)
                    {
                        if (Main.Nodes.ContainsKey(workingStep.nodeId))
                            currentSelectedLocation = Main.Nodes[workingStep.nodeId].locationId;
                    }

                    if (UI.Radio("Change Node State", workingStep.type == ActionType.ChangeState)) {
                        workingStep.type = ActionType.ChangeState;
                        editActionState = EditActionsState.SelectLocation;
                    }
                    UI.SameLine();
                    if (UI.Radio("Change Visibility", workingStep.type == ActionType.ChangeVisibility))
                    {
                        workingStep.type = ActionType.ChangeVisibility;
                        editActionState = EditActionsState.SelectLocation;
                    }
                    if (UI.Radio("Change Score", workingStep.type == ActionType.ChangeScore)) {
                        workingStep.type = ActionType.ChangeScore;
                        stepValueLabel = "Change score by (+,-)";
                        editActionState = EditActionsState.SetValue;
                    }
                    UI.SameLine();
                    if (UI.Radio("Change User's Health", workingStep.type == ActionType.ChangeHealth)) {
                        workingStep.type = ActionType.ChangeHealth;
                        stepValueLabel = "Change health by (+,-), starts at 100";
                        editActionState = EditActionsState.SetValue;
                    }
                    if (UI.Radio("Pick up this node", workingStep.type == ActionType.Pickup))
                    {
                        workingStep.type = ActionType.Pickup;
                        editActionState = EditActionsState.Complete;
                    }

                    
                    if (UI.Button("Next"))
                    {
                        switch (workingStep.type)
                        { 
                            case ActionType.ChangeState:
                            case ActionType.ChangeVisibility:
                               editActionState = EditActionsState.SelectLocation;
                               break;
                            case ActionType.ChangeHealth:
                            case ActionType.ChangeScore:
                                editActionState = EditActionsState.SetValue;
                                break;
                            case ActionType.Pickup:
                                editActionState = EditActionsState.Complete;
                                break;

                        }
                    }
                    break;

                case EditActionsState.SelectLocation:
                case EditActionsState.SelectNode:
                    bool nodeSelected = false;
                    if (addingUseAndSelectingNodeToUse == false)
                    {
                        if (UI.Button("Affect this node"))
                        {
                            workingStep.nodeId = id;
                            currentSelectedLocation = locationId;
                            nodeSelected = true;
                        }
                        UI.SameLine();
                        UI.Label("Or...");
                        UI.HSeparator();
                    }                    

                    if (editActionState == EditActionsState.SelectLocation)
                    {
                        if (addingUseAndSelectingNodeToUse)
                            UI.Label("Select the location of the node to use here:");
                        else
                            UI.Label("select the location of the target node:");

                        length = Main.LocationNames.Count;
                        foreach (KeyValuePair <int,string> kvp in Main.LocationNames)
                        {
                            if (UI.Radio(kvp.Value, kvp.Key == currentSelectedLocation))
                            {
                                currentSelectedLocation = kvp.Key;
                                editActionState = EditActionsState.SelectNode;
                            }
                            if (counter < 2 && count < length - 1)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                            count++;
                        }
                    } else
                    {
                        if (addingUseAndSelectingNodeToUse)
                            UI.Label("Select the node to be used here:");
                        else
                            UI.Label("select the node:");

                        int targetKey = workingStep.nodeId;
                        if (addingUse)
                            targetKey = actions[currentActionKey].useNodeid;

                        foreach (KeyValuePair<int, Node> kvp in Main.Nodes)
                        {
                            if (kvp.Value.locationId == currentSelectedLocation)
                            {
                                if (UI.Radio(kvp.Value.activeState.name, kvp.Key == targetKey))
                                {
                                    if (addingUseAndSelectingNodeToUse)
                                    {
                                        newAction = actions[currentActionKey];
                                        newAction.useNodeid = kvp.Key;
                                        actions[currentActionKey] = newAction;
                                    } else
                                    {
                                        workingStep.nodeId = kvp.Key;
                                    }
                                    
                                    nodeSelected = true;
                                }
                                if (counter < 2)
                                    UI.SameLine();
                                counter++;
                                counter %= 3;
                            }
                        }
                        UI.Label("");
                    }
                    UI.HSeparator();

                    bool backShown = false;
                    if (editActionState == EditActionsState.SelectNode) { 
                        backShown = true;
                        if (UI.Button("Back"))
                        {
                            editActionState = EditActionsState.SelectLocation;
                        }
                    }
                    if (
                        (currentSelectedLocation > 0 && editActionState == EditActionsState.SelectLocation) ||
                        (workingStep.nodeId > 0 && editActionState == EditActionsState.SelectNode) ||
                        (actions[currentActionKey].useNodeid > 0 && editActionState == EditActionsState.SelectNode && addingUseAndSelectingNodeToUse))  
                    {
                        if (backShown)
                            UI.SameLine();

                        if (UI.Button("Next"))
                        {
                            if (editActionState == EditActionsState.SelectLocation)
                                editActionState = EditActionsState.SelectNode;
                            else
                                nodeSelected = true;
                        }
                    }                       

                    if (nodeSelected) {
                        if (addingUseAndSelectingNodeToUse)
                        {
                            editActionState = EditActionsState.ShowActionSteps;
                            addingUseAndSelectingNodeToUse = false;
                        }

                        else
                        {
                            if (workingStep.type == ActionType.ChangeVisibility)
                                editActionState = EditActionsState.ChooseVisibility;
                            if (workingStep.type == ActionType.ChangeState)
                                editActionState = EditActionsState.ChooseState;
                        }
                    }

                    break;
                case EditActionsState.ChooseVisibility:
                    if (UI.Radio("Show node", workingStep.value == 1f))
                    {
                        workingStep.value = 1f;
                    }
                    UI.SameLine();
                    if (UI.Radio("Hide node", workingStep.value == 0f))
                    {
                        workingStep.value = 0f;
                    }


                    if (UI.Button("Done"))
                        editActionState = EditActionsState.Complete;

                    break;

                case EditActionsState.SetValue:
                    UI.Label(stepValueLabel);

                    UI.Input("stepValue", ref workingStep.text);
                    

                    if (UI.Button("Done"))
                    {
                        try
                        {
                            workingStep.value = float.Parse(Regex.Replace(workingStep.text, "[^0-9\\.\\+\\-]", ""));
                        }
                        catch (Exception )
                        {
                            workingStep.value = 0;
                        }
                        editActionState = EditActionsState.Complete;
                    }
                    break;

                case EditActionsState.ChooseState:
                    UI.Label("Select the new state for the selected node");
                    length = Main.Nodes[workingStep.nodeId].states.Count;
                    foreach (KeyValuePair<int,Data.Node.State> kvp in Main.Nodes[workingStep.nodeId].states)
                    {
                        if (UI.Radio(kvp.Key + ":" + kvp.Value.name,workingStep.value == kvp.Key))
                            workingStep.value = kvp.Key;
                        if (counter < 2 && count < length -1)
                            UI.SameLine();
                        counter++;
                        counter %= 3;
                        count++;
                    }
                    


                    if (UI.Button("Done"))
                        editActionState = EditActionsState.Complete;
                    break;

                case EditActionsState.Complete:
                    actions[currentActionKey].steps[currentActionStepInx] = workingStep;
                    editActionState = EditActionsState.ShowActionSteps;
                    break;
                    
            }

            UI.WindowEnd();
        }

        public Vec3 ClosestPoint(Vec3 target)
        {
            if (Main.menuState == Main.MenuState.Play && visible == false)
                return Vec3.Zero;
            else
                return pose.position + (target - pose.position).Normalized * Math.Max(Math.Max(dimensions.x, dimensions.y), dimensions.z);
        }

        public void DrawLines()
        {
            Vec3 p1;
            Vec3 p2;
            Vec3 toPlayer;
            Vec3 alongLine;
            bool reverseText;
            char directionInd;
            Relative relative;
            string text;
            Vec3 lineButtonPosition;
            int removeKey = -1;
            
            List<int> keys = new List<int>(relatives.Keys);
            foreach (int key in keys)
            {
                if (Main.Nodes.ContainsKey(key))
                {
                    relative = relatives[key];
                    p1 = ClosestPoint(Main.Nodes[key].pose.position) + Vec3.Up * .04f * ((id < relative.id) ? 1f : -1f);
                    p2 = Main.Nodes[key].ClosestPoint(pose.position);

                    if (p2.Length != 0)
                    {
                        p2 += Vec3.Up * .04f * ((id < relative.id) ? 1f : -1f);
                        alongLine = (p2 - p1).Normalized;
                        reverseText = Vec3.Dot(Input.Head.Right, alongLine) > 0;

                        toPlayer = (pose.position - Input.Head.position).Normalized;

                        relative.textPosition = p1 + (alongLine * (Vec3.Distance(p1, p2) / 2));
                        relative.textOrientation = Quat.LookAt(p1, p2, Vec3.Cross(alongLine, toPlayer)) * Quat.FromAngles(0, -90, reverseText ? 0 : 180);



                        directionInd = reverseText ? '>' : '<';
                        text = "\n\r" + directionInd + " " + relative.name + " " + directionInd;

                        Lines.Add(p1, p2, relative.color, .005f);
                        Text.Add(text, Matrix.TRS(relative.textPosition, relative.textOrientation, Vec3.One * 1f),
                            Main.titleStyle, TextAlign.Center, TextAlign.Center,0,-.005f);

                        relative.textSize = Text.Size(text);
                        lineButtonPosition = relative.textPosition + (relative.textSize.y / 2) * (relative.textOrientation * Vec3.Up);

                        if (Main.menuState != Main.MenuState.Play)
                        {
                            if (editState == EditState.ready)
                            {
                                if (Vec3.Distance(Input.Hand(Handed.Right).pinchPt, lineButtonPosition) < .5f)
                                {
                                    if (EditButton(lineButtonPosition, false, "M", true, relative.textOrientation))
                                    {
                                        editState = EditState.editingLink;
                                        editingLinkId = key;
                                        actionCooldown = 1f;
                                    }
                                }

                                if (Vec3.Distance(Input.Hand(Handed.Right).pinchPt, p1) < .5f)
                                {
                                    if (EditButton(p1 + Vec3.Up * .03f, false, "J", true, relative.textOrientation))
                                    {
                                        removeKey = key;
                                        actionCooldown = 1f;
                                    }
                                }
                            }
                            if (editState == EditState.editingLink && editingLinkId == key)
                            {

                                Main.keyboardInput.Update(ref relative.name);
                                if (EditButton(lineButtonPosition, false, "H", true, relative.textOrientation))
                                {
                                    ChangeActiveState(activeStateKey,true);
                                    editState = EditState.ready;
                                    actionCooldown = 1f;
                                }
                            }
                        }
                        relatives[key] = relative;
                    }
                }
                else
                    relatives.Remove(key);
            }

            if (removeKey >= 0)
                relatives.Remove(removeKey);
            
        }

        private Color lineColour = new Color(.2f, .2f, .2f, 1);

        private float actionCooldown = 0;
        public void MakeEditable()
        {
            Hand hand = Input.Hand(Handed.Right);

            AddHandle("Model Handle" + id);

            if (hand.IsPinched == false && editState == EditState.linkingParent)
            {
                editState = EditState.ready;
                if (Main.selectedNodeRightId != -1)
                {
                    if (Main.selectedNodeRightId != id)
                    {
                        relatives[Main.selectedNodeRightId] = new Relative(Main.selectedNodeRightId,lineColour,"");
                    }
                }
            }
            if (editState == EditState.linkingParent)
            {
                Lines.Add(parentDragPoint, hand.pinchPt, lineColour, .005f);
            }

            bool actioned = false;

            if (editState == EditState.ready)
            {
                parentDragPoint = pose.position + pose.Right * ((dimensions.x/2) + .03f);
                if (EditButton(parentDragPoint, true, "N", false, Quat.Identity))
                {
                    actioned = true;
                    editState = EditState.linkingParent;
                }
            }

            Vec3 position = titlePosition.Pose.position + pose.Up * (titleSize.y + .02f);
            if (editState == EditState.ready)
            {
                if (EditButton(position, false, "M", false, Quat.Identity))
                {
                    actioned = true;
                    editState = EditState.editingTitle;
                    actionCooldown = 1f;
                    if (activeState.name == Main.nameMe)
                        activeState.name = "";

                }
            }
            else
            {
                if (editState == EditState.editingTitle)
                {
                    if (activeState.name == defaultName)
                        activeState.name = "";
                    Main.keyboardInput.Update(ref activeState.name);
                    if (isLocation)
                    {
                        Main.LocationNames[id] = activeState.name;
                    }

                    if (EditButton(position, false, "H", false, Quat.Identity))
                    {
                        if (activeState.name == "")
                            activeState.name = GetDefaultName();
                        ChangeActiveState(activeStateKey, true);
                        actioned = true;
                        editState = EditState.ready;
                        actionCooldown = 1f;

                    }

                }
            }


            if (actioned)
                Default.SoundClick.Play(parentDragPoint);

        }

        public bool EditButton(Vec3 target, bool needsPinch, string icon, bool ovedrideOrientation, Quat inOrientation)
        {
            bool activated = false;

            if (ovedrideOrientation == false)
                inOrientation = pose.orientation;

            if (actionCooldown > 0 || draw == false)
                return false;
            

            Hand hand = Input.Hand(Handed.Right);

            float distance;
            bool Highlight;

            //
            // Drag point
            //
            distance = Vec3.Distance(needsPinch ? hand.pinchPt : hand[FingerId.Index, JointId.Tip].position, target);
            Highlight = distance < .05f;

            if (needsPinch) {
                if (hand.IsPinched && Highlight)
                    activated = true;
            } else
            {
                if (distance < .01f)
                {
                    activated = true;
                }
            }
            
            Text.Add(icon, Matrix.TRS(target,
               inOrientation, Highlight ? 1.1f : 1f), Main.iconTextStyle, TextAlign.Center, TextAlign.Center);

            return activated;
        }

        public Pose propertiesPose;
        private Pose workPose2;

        private enum PropertyState
        {
            TopLevel,
            BrowsingShapes,
            BrowsingImages,
            LoadingAsset,
            SelectColour,
            BrowsingDestinations,
            BrowsingMusic,
            Advanced
        }
        private PropertyState propertyState = PropertyState.TopLevel;
        public Vec2 MaxPropertyWidth = new Vec2(.15f, .001f);
        public void AddHandle(string name)
        {
            if (draw)
                Mesh.Cube.Draw(Material.UIBox, Matrix.TRS(pose.position, pose.orientation, dimensions));
            Bounds b = new Bounds(dimensions);
            grabbed = UI.Handle(name, ref pose, b);

            if (draw)
            {
                propertiesPose.position = pose.position +
                       (pose.Right * ((dimensions.x / 2f) + (MaxPropertyWidth.x/2)  + .01f) * -1)  +
                        (pose.Up * (dimensions.y / 2f  + .04f)) +
                        (pose.Forward * dimensions.z * .5f);

                propertiesPose.orientation = pose.orientation;
                bool showColours=false;

                UI.WindowBegin("Properties", ref propertiesPose, UIWin.Empty);
                UI.LayoutReserve(MaxPropertyWidth);

                propertiesDisplayed = true;
                int counter = 0;
                switch (propertyState)
                {
                    case PropertyState.TopLevel:
                        if (isPortal)
                        {
                            if (UI.Button("Set destination"))
                            {
                                propertyState = PropertyState.BrowsingDestinations;
                            }
                        }
                        if (isLocation)
                        {
                            if (UI.Button("Set Music"))
                            {
                                propertyState = PropertyState.BrowsingMusic;
                            }
                        }

                        if (UI.Button("Select 3d Model"))
                        {
                            propertyState = PropertyState.BrowsingShapes;
                        }
 
                        if (UI.Button("Select 2d Image") && !Platform.FilePickerVisible)
                        {
                            propertyState = PropertyState.BrowsingImages;
     
                        }
                        if (UI.Button("Change Color"))
                            propertyState = PropertyState.SelectColour;

                        if (UI.Button("Advanced"))
                            propertyState = PropertyState.Advanced;

                        break;

                    case PropertyState.Advanced:
                        if (UI.Button("<"))
                            propertyState = PropertyState.TopLevel;
                        UI.SameLine();

                        UI.Toggle((visibleAtStart ? "Visible at start" : "Hidden at start"), ref visibleAtStart);

                        Text.Add("Create multiple states for a node,\nthen add buttons and logic in [More]\nallowing users to cause state changes.",
                            Matrix.Identity, Main.generalTextStyle,TextAlign.BottomLeft, TextAlign.TopLeft, .07f, 0.00f);


                        State[] aStates = states.Values.ToArray();

                        int lineCount = 0;
                        if (advancedStartAt >= states.Count)
                            advancedStartAt = Math.Max(0, advancedStartAt - 3);


                        if (actionCooldown == 0)
                        {
                            foreach (State s in aStates)
                            {
                                if (lineCount >= advancedStartAt && lineCount < advancedStartAt + 3) {
                                    if (UI.Button("Show state " + s.id))
                                        ChangeActiveState(s.id, true);
                                    if (states.Count > 1)
                                    {
                                        UI.SameLine();
                                        if (UI.Button("Delete state " + s.id))
                                        {
                                            ChangeActiveState(activeStateKey, true); // update data first, we may then delete this one
                                            actionCooldown = .5f;
                                            states.Remove(s.id);
                                            ChangeActiveState(states.Keys.ToArray()[0], false);
                                        }
                                    }
                                }
                                lineCount++;
                            }
                            
                            if (advancedStartAt > 0) { 
                                if (UI.Button("< Previous"))
                                {
                                    advancedStartAt -= 3;
                                    advancedStartAt = Math.Max(0, advancedStartAt);
                                }
                            }
                            if (lineCount > advancedStartAt + 3)
                            {
                                if (advancedStartAt > 0)
                                    UI.SameLine();
                                if (UI.Button("Next >"))
                                    advancedStartAt += 3;
                            }
                            if (UI.Button("Duplicate current state"))
                            {
                                State newState = new State(); // create new
                                newState = activeState; // copy active
                                newState.id = maxStateKey; // update id
                                states[maxStateKey] = newState;
                                ChangeActiveState(maxStateKey, true);
                                maxStateKey++;
                                actionCooldown = .5f;
                            }
                        }
                        break;
                        
                    case PropertyState.BrowsingMusic:

                        if (UI.Button("<"))
                            propertyState = PropertyState.TopLevel;

                        UI.SameLine();

                        if (UI.Button("Open music...") && !Platform.FilePickerVisible)
                        {
                            Platform.FilePicker(PickerMode.Open, OnLoadMusic, OnCancelLoad,
                                ".wav", ".mp3");
                            propertyState = PropertyState.LoadingAsset;
                        }

                        if (UI.Button("No music"))
                        {
                            activeState.musicFilename = "";
                            Main.StopMusic();
                        }

                        foreach (Data.NodeModel nodeModel in Main.standardMusic)
                        {
                            if (UI.Button(nodeModel.name))
                                OnLoadStandardMusic(nodeModel.filename);
                            if (counter < 2)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                        }
                        break;

                    case PropertyState.BrowsingShapes:
                        
                        if (UI.Button("<"))
                            propertyState = PropertyState.TopLevel;
                        
                        UI.SameLine();

                        if (UI.Button("Open model...") && !Platform.FilePickerVisible)
                        {
                            Platform.FilePicker(PickerMode.Open, OnLoadModel, OnCancelLoad,
                                ".gltf", ".glb", ".obj", ".stl", ".fbx", ".ply");
                            propertyState = PropertyState.LoadingAsset;
                        }



                        foreach (Data.NodeModel nodeModel in Main.standardModels)
                        {
                            if (UI.Button(nodeModel.name))
                                OnLoadStandardModel(nodeModel.filename);
                            if (counter < 2)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                        }
                        break;

                    case PropertyState.BrowsingImages:

                        if (UI.Button("<"))
                            propertyState = PropertyState.TopLevel;

                        UI.SameLine();

                        if (UI.Button("Open image...") && !Platform.FilePickerVisible)
                        {
                            Platform.FilePicker(PickerMode.Open, OnLoadImage, OnCancelLoad,
                                ".jpg", ".png", ".tga", ".bmp", ".psd", ".gif", ".hdr", ".pic");
                            propertyState = PropertyState.LoadingAsset;
                        }

                        foreach (Data.NodeModel nodeModel in Main.standardImages)
                        {
                            if (UI.Button(nodeModel.name))
                                OnLoadStandardImage(nodeModel.filename);
                            if (counter < 2)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                        }
                        break;


                    case PropertyState.SelectColour:
                        Vec3 hsv = activeState.color.ToHSV();
                        SetColor(hsv.x, hsv.y, hsv.z);

                        if (UI.Button("<"))
                            propertyState = PropertyState.TopLevel;

                        // Swatches are never enough by themselves! So here's some sliders to
                        // let the user HSV their color manually. We start with a fixed size
                        // label, and on the same line add a fixed size slider. Fixing the
                        // sizes here helps them to line up in columns.
                        float lineHeightAdj = UI.LineHeight * 1f;
                        UI.Label("Hue", V.XY(4 * U.cm, lineHeightAdj));
                        UI.SameLine();
                        if (UI.HSlider("Hue", ref _hue, 0, 1, 0, 10 * U.cm, UIConfirm.Pinch))
                            SetColor(_hue, _saturation, _value);

                        UI.Label("Sat.", V.XY(4 * U.cm, lineHeightAdj));
                        UI.SameLine();
                        if (UI.HSlider("Saturation", ref _saturation, 0, 1, 0, 10 * U.cm, UIConfirm.Pinch))
                            SetColor(_hue, _saturation, _value);

                        UI.Label("Val.", V.XY(4 * U.cm, lineHeightAdj));
                        UI.SameLine();
                        if (UI.HSlider("Value", ref _value, 0, 1, 0, 10 * U.cm, UIConfirm.Pinch))
                            SetColor(_hue, _saturation, _value);

                        showColours = true;
                        break;

                    case PropertyState.BrowsingDestinations:

                        if (UI.Button("<"))
                            propertyState = PropertyState.TopLevel;
                        UI.SameLine();
                        if (UI.Button("New location"))
                            destinationId = 0;

                        
                        foreach (KeyValuePair<int,string> kvp in Main.LocationNames)
                        {
                            if (UI.Button(kvp.Value))
                                destinationId = kvp.Key;
                            if (counter < 2)
                                UI.SameLine();
                            counter++;
                            counter %= 3;
                        }
                        break;



                }

                UI.WindowEnd();

                if (showColours)
                {
                    workPose2.position = propertiesPose.position + propertiesPose.Forward * - .05f + propertiesPose.Up * -.12f;
                    workPose2.orientation = propertiesPose.orientation;
                    UI.WindowBegin("Properties", ref workPose2, UIWin.Empty);
                    UI.LayoutReserve(MaxPropertyWidth);
                    UI.Space(.03f);
                    SwatchColor("White", _hue, 0, 1);
                    UI.SameLine();
                    SwatchColor("Blk", _hue, 0, SK.System.displayType == Display.Additive ? 0.25f : 0);
                    UI.SameLine();
                    SwatchColor("Green", .33f, .9f, 1);
                    UI.SameLine();
                    SwatchColor("Ylw", .14f, .9f, 1);
                    UI.SameLine();
                    SwatchColor("Blue", .66f, .9f, 1);
                    UI.WindowEnd();
                }
            }
        }

        private Vec2 swatchSize = new Vec2(.015f, .03f);
        private Vec3 swatchDimensions = new Vec3(.015f, .02f, .002f);
        private void SwatchColor(string id, float hue, float saturation, float value)
        {
            // Reserve a spot for this swatch!
            Bounds bounds = UI.LayoutReserve(swatchSize);
            bounds.dimensions.z = U.cm * 4;

            Mesh.GenerateCube(swatchDimensions).Draw(Default.Material,Matrix.T(bounds.center), Color.HSV(hue, saturation, value));

            // If the users interacts with the volume the swatch model is in,
            // then we'll set the active color right here, and play some sfx!
            BtnState state = UI.VolumeAt(id, bounds, UIConfirm.Push);
            if (state.IsJustActive())
            {
                Sound.Click.Play(Hierarchy.ToWorld(bounds.center));
                SetColor(hue, saturation, value);
            }
            if (state.IsJustInactive())
                Sound.Unclick.Play(Hierarchy.ToWorld(bounds.center));
        }

        void SetColor(float hue, float saturation, float value)
        {
            _hue = hue;
            _saturation = saturation;
            _value = value;
            activeState.color = Color.HSV(hue, saturation, value);

            //model.RootNode.Material[MatParamName.ColorTint] = color;

        }

        public void OnLoadModel(string filename)
        {
            try { 
                activeState.modelFilename = filename;
                model = Model.FromFile(filename);
                
            }
            catch (Exception)
            {
                activeState.modelFilename = "sphere.obj";
                model = Model.FromFile(filename);
            }
            activeState.spriteFilename = "";
            propertyState = PropertyState.TopLevel;
        }

        public void OnLoadMusic(string filename)
        {
            activeState.musicFilename = filename;
            Main.PlayMusicForLocation();
            propertyState = PropertyState.TopLevel;
        }

        public void OnLoadImage(string filename)
        {
            activeState.spriteFilename = filename;
            sprite = Sprite.FromFile(filename,SpriteType.Single);
            propertyState = PropertyState.TopLevel;
            activeState.modelFilename = "";
        }
        

        public void OnLoadStandardModel(string filename)
        {
            activeState.modelFilename = filename;
            activeState.spriteFilename = "";
            model = Model.FromFile(filename);
        }

        public void OnLoadStandardMusic(string filename)
        {
            activeState.musicFilename = filename;
            Main.PlayMusicForLocation();
        }

        public void OnLoadStandardImage(string filename)
        {
            activeState.spriteFilename = filename;
            activeState.modelFilename = "";
            sprite = Sprite.FromFile(filename,SpriteType.Single);
        }

        public void OnCancelLoad()
        {
            propertyState = PropertyState.TopLevel;
        }

        public void InitForNewPlay(bool saveChangesInEditMode)
        {
            moreInfoState = MoreInfoState.collapsed;
            visible = visibleAtStart;
            available = true;
            ChangeActiveState(states.Keys.ToArray()[0], saveChangesInEditMode);
        }

        private string topLevelSeperator = "|#|";
        private string arraySeperator = "{#}";
        private string arrayElementSeperator = "{,}";
        private string actionStepFieldSeperator = "[/]";
        private string actionStepArraySeperator = "[,]";
        public string Serialise()
        {
            ChangeActiveState(activeStateKey,true); //Update states

            List<string> str = new List<string>();
            str.Add(SH.Serialise(id));
            str.Add(SH.Serialise(visibleAtStart));
            str.Add(SH.Serialise(isLocation));
            str.Add(SH.Serialise(isPortal));
            str.Add(SH.Serialise(locationId));
            str.Add(SH.Serialise(destinationId));
            str.Add(SH.Serialise(pose));
            str.Add(SH.Serialise(health));
            
            List<string> subStr = new List<string>();
            // States
            foreach (KeyValuePair<int,State> kvp in states)
            {
                subStr.Add(SH.Serialise(kvp.Key) + arrayElementSeperator + SH.Serialise(kvp.Value.name) + arrayElementSeperator + 
                    SH.Serialise(kvp.Value.color) + arrayElementSeperator + SH.Serialise(kvp.Value.notes) + arrayElementSeperator +
                    SH.Serialise(kvp.Value.modelFilename) + arrayElementSeperator + SH.Serialise(kvp.Value.spriteFilename) + arrayElementSeperator +
                    SH.Serialise(kvp.Value.musicFilename) + arrayElementSeperator + SH.Serialise(kvp.Value.nodeScale));
            }
            str.Add(string.Join(arraySeperator, subStr.ToArray()));

            // Relatives
            subStr = new List<string>();
            foreach (KeyValuePair <int,Relative> kvp in relatives)
            {
                subStr.Add(SH.Serialise(kvp.Value.id) + arrayElementSeperator + SH.Serialise(kvp.Value.name) + arrayElementSeperator + SH.Serialise(kvp.Value.color));
            }
            str.Add(string.Join(arraySeperator, subStr.ToArray()));

            // Actions
            subStr = new List<string>();

            
            foreach (KeyValuePair<int,Action> kvp in actions)
            {
                List<string> subSubStr = new List<string>();
                foreach (ActionStep steps in kvp.Value.steps)
                {
                    subSubStr.Add(SH.Serialise((int)steps.type) + actionStepFieldSeperator + SH.Serialise(steps.nodeId) + actionStepFieldSeperator + SH.Serialise(steps.value) + actionStepFieldSeperator + SH.Serialise(steps.text));
                }

                subStr.Add(SH.Serialise(kvp.Value.id) + arrayElementSeperator + SH.Serialise(kvp.Value.useNodeid) + arrayElementSeperator + SH.Serialise(kvp.Value.name) + arrayElementSeperator +
                    string.Join(actionStepArraySeperator, subSubStr.ToArray()) + arrayElementSeperator +
                    SH.Serialise(kvp.Value.applicableStates));
            }
            str.Add(string.Join(arraySeperator, subStr.ToArray()));


            return string.Join(topLevelSeperator, str.ToArray());



        }

        public void UnSerialise(string data)
        {
            int i=0;
            Color col = new Color();
            string s = "";


            string[] p = data.Split(topLevelSeperator);

            int inx = 0;
            SH.UnSerialise(p[inx++], ref id);
            SH.UnSerialise(p[inx++], ref visibleAtStart);
            SH.UnSerialise(p[inx++], ref isLocation);
            SH.UnSerialise(p[inx++], ref isPortal);
            SH.UnSerialise(p[inx++], ref locationId);
            SH.UnSerialise(p[inx++], ref destinationId);
            SH.UnSerialise(p[inx++], ref pose);
            SH.UnSerialise(p[inx++], ref health);

            string[] subStr = p[inx++].Split(arraySeperator);
            string[] bits;

            // States
            states = new SortedDictionary<int, State>();
            State newState;
            foreach (string str in subStr)
            {
                bits = str.Split(arrayElementSeperator);
                newState = new State();

                SH.UnSerialise(bits[0], ref newState.id);
                SH.UnSerialise(bits[1], ref newState.name);
                SH.UnSerialise(bits[2], ref newState.color);
                SH.UnSerialise(bits[3], ref newState.notes);
                SH.UnSerialise(bits[4], ref newState.modelFilename);
                SH.UnSerialise(bits[5], ref newState.spriteFilename);
                SH.UnSerialise(bits[6], ref newState.musicFilename);
                SH.UnSerialise(bits[7], ref newState.nodeScale);
                states.Add(newState.id,newState);
            }
            maxStateKey = states.Keys.ToArray().Max() +1;


            //Relatives
            subStr = p[inx++].Split(arraySeperator);
            relatives = new Dictionary<int, Relative>();
            foreach (string str in subStr)
            {
                bits = str.Split(arrayElementSeperator);
                if (bits.Length == 3) {
                    SH.UnSerialise(bits[0], ref i);
                    SH.UnSerialise(bits[1], ref s);
                    SH.UnSerialise(bits[2], ref col);
                    if (relatives.ContainsKey(i) == false)
                        relatives.Add(i, new Relative(i, col, s));
                }
            }

            // Actions
            if (inx < p.Length)
            {
                subStr = p[inx++].Split(arraySeperator);
                Action newAction;
                ActionStep newStep;
                string[] stepBits;
                string[] subSubstr;
                int stepType = 0;
                actions = new SortedDictionary<int, Action>();
                foreach (string str in subStr)
                {
                    newAction = new Action();
                    newAction.steps = new List<ActionStep>();
                    bits = str.Split(arrayElementSeperator);
                    if (bits.Length >= 4)
                    {
                        SH.UnSerialise(bits[0], ref newAction.id);
                        SH.UnSerialise(bits[1], ref newAction.useNodeid);
                        SH.UnSerialise(bits[2], ref newAction.name);
                        subSubstr = bits[3].Split(actionStepArraySeperator);
                        foreach (string strStep in subSubstr)
                        {
                            if (strStep != "")
                            {
                                newStep = new ActionStep();
                                stepBits = strStep.Split(actionStepFieldSeperator);
                                SH.UnSerialise(stepBits[0], ref stepType);
                                newStep.type = (ActionType)stepType;
                                SH.UnSerialise(stepBits[1], ref newStep.nodeId);
                                SH.UnSerialise(stepBits[2], ref newStep.value);
                                SH.UnSerialise(stepBits[3], ref newStep.text);
                                newAction.steps.Add(newStep);
                            }
                        }
                        if (bits.Length >= 5)
                            SH.UnSerialise(bits[4], ref newAction.applicableStates);
                        else
                            newAction.applicableStates = new List<int>();
                        actions.Add(newAction.id, newAction);
                    }
                    
                }
                if (actions.Count > 0)
                    maxActionKey = actions.Keys.ToArray().Max() +1;
            }

            ChangeActiveState(states.Keys.ToArray()[0], false);
        }
    }

    
}
