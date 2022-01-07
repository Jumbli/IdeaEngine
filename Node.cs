using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


public class Node
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

    public struct Relative
    {
        public int id;
        public string name;
        public Color color;
        public Vec2 textSize;
        public Vec3 textPosition;
        public Quat textOrientation;

        public Relative(int inId, Color inColor, string inName = "")
        {
            id = inId;
            name = inName;
            color = inColor;
            textSize = new Vec2();
            textPosition = new Vec3();
            textOrientation = new Quat();

        }

    }

    public static int maxNodeId = 0;
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

    public bool forceDraw = false;
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
    
    public Actions actions;
        
    private Vec3 assetDimensions = Vec3.One;
    public bool inFocus;
    public bool propertiesDisplayed;
    public bool grabbed;

    float _hue = 0;
    float _saturation = 0;
    float _value = 1;

    static public int activeNodeId;

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

    public enum MoreInfoState
    {
        collapsed,
        expanded,
        editActions
    }

    public MoreInfoState moreInfoState = MoreInfoState.collapsed;
        
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
        init();

        UnSerialise(serialisedData);
        if (activeState.spriteFilename == "")
            OnLoadModel(activeState.modelFilename);
        else
            OnLoadImage(activeState.spriteFilename);

        Node.maxNodeId = Math.Max(Node.maxNodeId, id);
        visible = visibleAtStart;
    }
    public Node(bool inIsLocation, string inName, Pose inPose, bool inIsPortal = false, string inModelFilename="sphere.obj")
    {
        try { 
            Node.maxNodeId++;
            id = Node.maxNodeId;

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

            init();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    ~Node()
    {
        //Log.Info("Node" + states[activeStateKey].name + "Exiting");
    }

    private void init()
    {
        try { 
            actions = new Actions(this);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void ChangeActiveState(int newKey, bool updateBeforeChange)
    {
        try { 
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
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    private Vec2 titleSize;
    private Matrix titlePosition;
    private bool draw = false;
    private Pose moreInfoPose = new Pose();
    private Pose moreInfoWindowPose = new Pose();
    private Vec2 notesSize = new Vec2(.2f,.055f);

    private int sinx;
    public State activeState;
    private Bounds scaledBounds;

    private Matrix modeTransform = new Matrix();
    public void Draw()
    {
        try { 
            UI.PushId("" + id);

            draw = Vec3.Dot((pose.position - Input.Head.position).Normalized, Input.Head.Forward) > 0 || forceDraw;
            forceDraw = false;
        

            titleSize = Text.Size(activeState.name, Main.titleStyle);
            actionCooldown = Math.Max(0, actionCooldown - Main.deltaTime);


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
                    scaledBounds = model.Bounds * calculatedScale;
                    dimensions.x = MathF.Max(.05f, dimensions.x);
                    dimensions.y = MathF.Max(.05f, dimensions.y);
                    dimensions.z = MathF.Max(.05f, dimensions.z);

                    modeTransform = Matrix.TRS(pose.position, pose.orientation, calculatedScale * .9f);
                    modeTransform.Translation = modeTransform.Transform(model.Bounds.center * -1);
                    model.Draw(modeTransform, activeState.color);
                }
                else
                {
                    calculatedScale = .12f * activeState.nodeScale;
                    dimensions = Vec3.One * calculatedScale;
                    dimensions.z = .05f;
                    scaledBounds = new Bounds(Vec3.Zero,dimensions);
                    sprite.Draw(Matrix.TRS(pose.position + pose.Up * (calculatedScale/2) + pose.Right * (calculatedScale / 2) * -1
                        , pose.orientation, calculatedScale * 
                        V.XYZ(sprite.NormalizedDimensions.x, sprite.NormalizedDimensions.y,1)), activeState.color);
                }

                radius = MathF.Max(dimensions.x, dimensions.y);
                radius = MathF.Max(radius, dimensions.z);
                radius /= 2;

                titlePosition = Matrix.TRS(pose.position +
                    pose.Up * ((dimensions.y /2) + titleSize.y / 2 + .03f)
                    , pose.orientation, 1f);
                Text.Add(activeState.name, titlePosition, Main.titleStyle, TextAlign.Center, TextAlign.Center);

                moreInfoPose.position = pose.position +
                    pose.Forward * ((dimensions.z / 2) + .02f) +
                    pose.Up * ((dimensions.y / -2) + -.00f);
                moreInfoPose.orientation = pose.orientation;

                if (moreInfoState == MoreInfoState.expanded && Node.activeNodeId != id)
                    moreInfoState = MoreInfoState.collapsed;
                
                switch (moreInfoState)
                {
                    case MoreInfoState.collapsed:
                        if (Main.editingNodeId == id && Main.editingNodeDescription == "MoreInfo")
                            Main.ResetEditingMode();

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
                                    {
                                        moreInfoState = MoreInfoState.expanded;
                                        moreInfoWindowPose.position = moreInfoPose.position + (moreInfoPose.orientation * Vec3.Forward) * .02f;
                                        moreInfoWindowPose.orientation = moreInfoPose.orientation;
                                        Main.editingNodeId = id;
                                        Main.editingNodeDescription = "MoreInfo";
                                        Node.activeNodeId = id;
                                    }
                            }
                            UI.WindowEnd();
                        }

                        break;

                    case MoreInfoState.expanded:
                        UI.WindowBegin("MoreInfo", ref moreInfoWindowPose, UIWin.Body);
                        notesSize = Text.Size(activeState.notes, Main.generalTextStyle);
                        if (Main.menuState == Main.MenuState.EditNodes)
                            notesSize.x = MathF.Max(.25f, notesSize.x);
                        else
                            notesSize.x = MathF.Max(.1f, notesSize.x);
                        notesSize.y = MathF.Max(.01f, notesSize.y + .003f);
                        if (Main.menuState == Main.MenuState.EditNodes)
                            UI.Label("Type notes here. The box will expand to fit the width and height");
                        UI.LayoutReserve(notesSize);

                        Mesh.Cube.Draw(Material.Unlit,
                            Matrix.TRS(UI.LayoutLast.center + Vec3.Forward * .001f, Quat.Identity, UI.LayoutLast.dimensions + V.XYZ(.01f,.01f,0)),
                            Color.White);
                        
                        Text.Add(activeState.notes, Matrix.T(UI.LayoutLast.center + Vec3.Forward * .002f), notesSize, 
                            TextFit.Clip, Main.generalTextStyle,
                            TextAlign.Center, TextAlign.TopLeft);

                        actions.Draw();


                        UI.HSeparator();
                        if (UI.Button("Close"))
                            moreInfoState = MoreInfoState.collapsed;
                        if (Main.menuState == Main.MenuState.EditNodes)
                        {
                            Main.keyboardInput.Update(id + "notes", true, UI.LayoutAt, Quat.Identity, -.04f, ref activeState.notes);
                            forceDraw = true;
                        }

                        UI.WindowEnd();

                        

                        break;

                    case MoreInfoState.editActions:
                        actions.EditActions(ref moreInfoWindowPose);
                        break;
                }
                
            }

            DrawLines();
            propertiesDisplayed = false;

            if (Main.menuState == Main.MenuState.EditNodes)
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
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private bool CheckIfMoreWindowRequired()
    {
        try {
            if (Main.editingNodeId > -1 && Main.menuState == Main.MenuState.EditNodes)
                if (Main.editingNodeId != id || Main.editingNodeDescription != "MoreInfo")
                    return false;

            if (Main.menuState == Main.MenuState.EditNodes ||
                activeState.notes.Length > 0 || isPortal)
                return true;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

        return actions.DoActionsExist(activeStateKey);

    }

    private Vec3 newPoint = new Vec3();
    private Vec3 lineDirection = new Vec3();

    public Vec3 ClosestPoint(Vec3 lineSource)
    {
        try {    
            if (Main.menuState == Main.MenuState.Play && visible == false)
                return Vec3.Zero;

            lineDirection = (lineSource - pose.position).Normalized;

            newPoint = pose.position + lineDirection * radius;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

        return newPoint;
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

        try
        {
            foreach (int key in keys)
            {
                if (Main.Nodes.ContainsKey(key))
                {
                    relative = relatives[key];
                    p1 = ClosestPoint(Main.Nodes[key].pose.position) + Vec3.Up * .04f * ((id < relative.id) ? 1f : -1f);
                    p2 = Main.Nodes[key].ClosestPoint(pose.position);

                    if (Vec3.Dot((p2 - p1).Normalized, (pose.position - Main.Nodes[key].pose.position).Normalized) < 0)
                    {
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
                                Main.titleStyle, TextAlign.Center, TextAlign.Center, 0, -.005f);

                            relative.textSize = Text.Size(text);
                            lineButtonPosition = relative.textPosition + (relative.textSize.y / 2) * (relative.textOrientation * Vec3.Up);

                            if (Main.menuState == Main.MenuState.EditNodes)
                            {
                                if (editState == EditState.ready)
                                {
                                    if (Vec3.Distance(Input.Hand(Handed.Right).pinchPt, lineButtonPosition) < .5f)
                                    {
                                        if (EditButton("editLink", ChangeEditMode.setEditMode, lineButtonPosition, false, "M", true, relative.textOrientation))
                                        {
                                            editState = EditState.editingLink;
                                            editingLinkId = key;
                                            actionCooldown = 1f;
                                        }
                                    }

                                    if (Vec3.Distance(Input.Hand(Handed.Right).pinchPt, p1) < .5f)
                                    {
                                        if (EditButton("deleteLink", ChangeEditMode.ignoreEditMode, p1 + Vec3.Up * .03f, false, "J", true, relative.textOrientation))
                                        {
                                            removeKey = key;
                                            actionCooldown = 1f;
                                        }
                                    }
                                }
                                if (editState == EditState.editingLink && editingLinkId == key)
                                {

                                    Main.keyboardInput.Update(id + "link" + key, false, lineButtonPosition, relative.textOrientation, -.04f, ref relative.name);
                                    forceDraw = true;

                                    if (EditButton("editLink", ChangeEditMode.clearEditMode, lineButtonPosition, false, "H", true, relative.textOrientation))
                                    {
                                        ChangeActiveState(activeStateKey, true);
                                        editState = EditState.ready;
                                        actionCooldown = 1f;
                                    }
                                }
                            }
                            relatives[key] = relative;
                        }
                    }
                }
                else
                    relatives.Remove(key);
            }

            if (removeKey >= 0)
                relatives.Remove(removeKey);
        }
        catch (Exception ex)
        {
            Log.Err(ex.Source + ":" + ex.Message);
        }
    }

    private Color lineColour = new Color(.2f, .2f, .2f, 1);

    private float actionCooldown = 0;
    private int linkingHand;
    public void MakeEditable()
    {
        try {

            if (Main.editingNodeId == -1)
                AddHandle("Model Handle" + id);

            bool actioned = false;

            for (int i = 0; i < 2; i++)
            {

                Hand hand = Input.Hand(i);

                if (editState == EditState.linkingParent && linkingHand == i)
                {
                    if (hand.IsPinched == false)
                    {
                        editState = EditState.ready;
                        Main.ResetEditingMode();
                        if (Main.selectedNodes[i] != -1 && Main.selectedNodes[i] != id)
                        {
                            relatives[Main.selectedNodes[i]] = new Relative(Main.selectedNodes[i], lineColour, "");
                        }
                    }
                    else
                    {
                        Lines.Add(parentDragPoint, hand.pinchPt, lineColour, .005f);
                    }
                }
            }

            if (editState == EditState.ready)
            {
                parentDragPoint = pose.position + pose.Right * ((dimensions.x / 2) + .03f);
                if (EditButton("linkNode", ChangeEditMode.setEditMode, parentDragPoint, true, "N", false, Quat.Identity))
                {
                    actioned = true;
                    editState = EditState.linkingParent;
                }
            }

            Vec3 position = titlePosition.Pose.position + pose.Up * (titleSize.y + .02f);
            if (editState == EditState.ready)
            {
                if (EditButton("editTitle", ChangeEditMode.setEditMode, position, false, "M", false, Quat.Identity))
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
                    Main.keyboardInput.Update(id + "title", false, position, Quat.Identity, -.04f, ref activeState.name);
                    forceDraw = true;

                    if (isLocation)
                    {
                        Main.LocationNames[id] = activeState.name;
                    }

                    if (EditButton("editTitle", ChangeEditMode.clearEditMode, position, false, "H", false, Quat.Identity))
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
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public enum ChangeEditMode
    {
        ignoreEditMode,
        setEditMode,
        clearEditMode
    }
    public bool EditButton(string description, ChangeEditMode editMode, Vec3 target, bool needsPinch, string icon, bool ovedrideOrientation, Quat inOrientation)
    {
        
        bool activated = false;

        try {
            if (Main.editingNodeId > -1)
                if (Main.editingNodeId != id || Main.editingNodeDescription != description)
                    return false;

            if (ovedrideOrientation == false)
                inOrientation = pose.orientation;

            if (actionCooldown > 0 || draw == false)
                return false;

            float distance;
            bool Highlight = false;

            //
            // Drag point
            //
            for (int i = 0; i < 2; i++)
            {
                Hand hand = Input.Hand(i);
                distance = Vec3.Distance(needsPinch ? hand.pinchPt : hand[FingerId.Index, JointId.Tip].position, target);
                if (Highlight == false)
                    Highlight = distance < .05f;

                if (needsPinch)
                {
                    if (hand.IsJustPinched && Highlight)
                    {
                        activated = true;
                        linkingHand = i;
                    }
                }
                else
                {
                    if (distance < .01f)
                    {
                        activated = true;
                    }
                }
            }
            
            Text.Add(icon, Matrix.TRS(target,
                inOrientation, Highlight ? 1.1f : 1f), Main.iconTextStyle, TextAlign.Center, TextAlign.Center);

            if (activated)
            {
                switch (editMode)
                {
                    case ChangeEditMode.clearEditMode:
                        Main.ResetEditingMode();
                        break;
                    case ChangeEditMode.setEditMode:
                        Main.editingNodeId = id;
                        Main.editingNodeDescription = description;
                        break;
                    case ChangeEditMode.ignoreEditMode:
                        break;
                    default:
                        break;
                }
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

        return activated;
    }

    public Pose propertiesPose;
    private Pose workPose2;

    private bool rotateOnly = false;

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
        try { 
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
                        //UI.Toggle(rotateOnly?"Rotate only":"Move enabled", ref rotateOnly);
                    
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

                        foreach (Node.NodeModel nodeModel in Main.standardMusic)
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
                                ".gltf", ".glb", ".obj", ".stl", ".ply");
                            propertyState = PropertyState.LoadingAsset;
                        }



                        foreach (Node.NodeModel nodeModel in Main.standardModels)
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

                        foreach (Node.NodeModel nodeModel in Main.standardImages)
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
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private Vec2 swatchSize = new Vec2(.015f, .03f);
    private Vec3 swatchDimensions = new Vec3(.015f, .02f, .002f);
    private void SwatchColor(string id, float hue, float saturation, float value)
    {
        try { 
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
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    void SetColor(float hue, float saturation, float value)
    {
        try { 
            _hue = hue;
            _saturation = saturation;
            _value = value;
            activeState.color = Color.HSV(hue, saturation, value);

            //model.RootNode.Material[MatParamName.ColorTint] = color;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

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
        try { 
            activeState.musicFilename = filename;
            Main.PlayMusicForLocation();
            propertyState = PropertyState.TopLevel;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void OnLoadImage(string filename)
    {
        try { 
            activeState.spriteFilename = filename;
            sprite = Sprite.FromFile(filename,SpriteType.Single);
            propertyState = PropertyState.TopLevel;
            activeState.modelFilename = "";
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
        

    public void OnLoadStandardModel(string filename)
    {
        try { 
            activeState.modelFilename = filename;
            activeState.spriteFilename = "";
            model = Model.FromFile(filename);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void OnLoadStandardMusic(string filename)
    {
        try { 
            activeState.musicFilename = filename;
            Main.PlayMusicForLocation();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void OnLoadStandardImage(string filename)
    {
        try { 
            activeState.spriteFilename = filename;
            activeState.modelFilename = "";
            sprite = Sprite.FromFile(filename,SpriteType.Single);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void OnCancelLoad()
    {
        propertyState = PropertyState.TopLevel;
    }

    public void InitForNewPlay(bool saveChangesInEditMode)
    {
        try { 
            moreInfoState = MoreInfoState.collapsed;
            visible = visibleAtStart;
            available = true;
            ChangeActiveState(states.Keys.ToArray()[0], saveChangesInEditMode);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    public string Serialise()
    {
        List<string> str = new List<string>();

        try { 
            ChangeActiveState(activeStateKey,true); //Update states

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
                subStr.Add(SH.Serialise(kvp.Key) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.name) + SH.arrayElementSeperator + 
                    SH.Serialise(kvp.Value.color) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.notes) + SH.arrayElementSeperator +
                    SH.Serialise(kvp.Value.modelFilename) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.spriteFilename) + SH.arrayElementSeperator +
                    SH.Serialise(kvp.Value.musicFilename) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.nodeScale));
            }
            str.Add(string.Join(SH.arraySeperator, subStr.ToArray()));

            // Relatives
            subStr = new List<string>();
            foreach (KeyValuePair <int,Relative> kvp in relatives)
            {
                subStr.Add(SH.Serialise(kvp.Value.id) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.name) + SH.arrayElementSeperator + SH.Serialise(kvp.Value.color));
            }
            str.Add(string.Join(SH.arraySeperator, subStr.ToArray()));

            // Actions
            str.Add(string.Join(SH.arraySeperator, actions.Serialise()));
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

        return string.Join(SH.topLevelSeperator, str.ToArray());



    }

    public void UnSerialise(string data)
    {
        try
        {
            int i = 0;
            Color col = new Color();
            string s = "";


            string[] p = data.Split(SH.topLevelSeperator);

            int inx = 0;
            SH.UnSerialise(p[inx++], ref id);
            SH.UnSerialise(p[inx++], ref visibleAtStart);
            SH.UnSerialise(p[inx++], ref isLocation);
            SH.UnSerialise(p[inx++], ref isPortal);
            SH.UnSerialise(p[inx++], ref locationId);
            SH.UnSerialise(p[inx++], ref destinationId);
            SH.UnSerialise(p[inx++], ref pose);
            SH.UnSerialise(p[inx++], ref health);

            string[] subStr = p[inx++].Split(SH.arraySeperator);
            string[] bits;

            // States
            states = new SortedDictionary<int, State>();
            State newState;
            foreach (string str in subStr)
            {
                bits = str.Split(SH.arrayElementSeperator);
                newState = new State();

                SH.UnSerialise(bits[0], ref newState.id);
                SH.UnSerialise(bits[1], ref newState.name);
                SH.UnSerialise(bits[2], ref newState.color);
                SH.UnSerialise(bits[3], ref newState.notes);
                SH.UnSerialise(bits[4], ref newState.modelFilename);
                SH.UnSerialise(bits[5], ref newState.spriteFilename);
                SH.UnSerialise(bits[6], ref newState.musicFilename);
                SH.UnSerialise(bits[7], ref newState.nodeScale);
                states.Add(newState.id, newState);
            }
            maxStateKey = states.Keys.ToArray().Max() + 1;


            //Relatives
            subStr = p[inx++].Split(SH.arraySeperator);
            relatives = new Dictionary<int, Relative>();
            foreach (string str in subStr)
            {
                bits = str.Split(SH.arrayElementSeperator);
                if (bits.Length == 3)
                {
                    SH.UnSerialise(bits[0], ref i);
                    SH.UnSerialise(bits[1], ref s);
                    SH.UnSerialise(bits[2], ref col);
                    if (relatives.ContainsKey(i) == false)
                        relatives.Add(i, new Relative(i, col, s));
                }
            }

            if (inx < p.Length)
                actions.UnSerialise(p[inx++]);

            ChangeActiveState(states.Keys.ToArray()[0], false);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
}
