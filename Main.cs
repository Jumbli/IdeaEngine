using StereoKit;
using StereoKit.Framework;
using System;
using System.Collections.Generic;

public class Main
{

    static public float score;
    static public float health = 100;

    public static Node.NodeModel[] standardModels =
    {
        new Node.NodeModel("Cube", "cube.obj"),
        new Node.NodeModel("Sphere", "sphere.obj"),
        new Node.NodeModel("Torus", "torus.obj"),
        new Node.NodeModel("Cone", "cone.obj"),
        new Node.NodeModel("Cylinder", "cylinder.obj"),
        new Node.NodeModel("Portal", "portal.obj")
    };

    public static Node.NodeModel[] standardImages =
    {
        new Node.NodeModel("Start", "images\\start.png"),
        new Node.NodeModel("Back", "images\\back.png"),
        new Node.NodeModel("Up arrow", "images\\upArrow.png"),
        new Node.NodeModel("Move", "images\\moveIcon.png"),
        new Node.NodeModel("Cross", "images\\close.png"),
        new Node.NodeModel("Tick", "images\\tick.png"),
        new Node.NodeModel("Bin", "images\\delete.png"),
        new Node.NodeModel("Gear", "images\\settings.png"),
        new Node.NodeModel("Human", "images\\stand.png"),
        new Node.NodeModel("Sound", "images\\volume.png")
    };

    public static Node.NodeModel[] standardMusic =
    {
        new Node.NodeModel("Dream On", "music\\DreamOn.wav"),
        new Node.NodeModel("My Inspiration", "music\\MyInspiration.wav")
    };


    static public Dictionary<int, Node> Nodes = new Dictionary<int, Node>();
    static public Dictionary<int, string> LocationNames = new Dictionary<int, string>();
    static public List<int> inventoryNodeIds = new List<int>();
    static public Sound Music;
    static public SoundInst musicInst;

    Material floorMaterial;
    Matrix floorTransform;
    static string mindMapFilename = "";

    static public int locationId = 0;

    Dictionary<int, SphericalHarmonics> Lightings = new Dictionary<int, SphericalHarmonics>();

    static private My.Framework.HandMenuRadial handMenu;

    private Sprite iconOpen;


    static public Font iconFont;
    static public TextStyle generalTextStyle;
    static public TextStyle titleStyle;
    static public TextStyle uiTitleStyle;
    static public TextStyle uiBodyStyle;
    static public TextStyle iconTextStyle;
    static public float deltaTime;

    private TextStyle handMenuTextStyle;

    public static KeyboardInput keyboardInput;


    public Main()
    {
    }

    ~Main()
    {
        Log.Info("MainExiting");
    }

    public enum MenuState
    {
        EditNodes,
        Play,
        Files,
        Demos
    }

    static private int HandMenuInventoryLayer = 3;
    public static MenuState menuState = MenuState.Files;
    private Sprite feet;
    private Matrix feetTransform;

    public void Init()
    {

        try { 

            //Tex cubemap = Tex.FromCubemapEquirectangular("chineseGarden.jpg", out SphericalHarmonics lighting);
            //Renderer.SkyTex = cubemap;
            //Renderer.SkyLight = lighting;

            iconFont = Font.FromFile("Assets\\iconFont.ttf");

            generalTextStyle = Text.MakeStyle(Font.Default, .6f * U.cm, Color.HSV(0, 0, 0));


            titleStyle = Text.MakeStyle(Font.Default, 1f * U.cm, Color.HSV(0, 0, 0));

            handMenuTextStyle = Text.MakeStyle(Font.Default, .6f * U.cm, Color.HSV(0, 0, 0));
            iconTextStyle = Text.MakeStyle(iconFont, 1f * U.cm, Color.HSV(0, 0, 0));

            uiTitleStyle = Text.MakeStyle(Font.Default, .8f * U.cm, Color.HSV(0, 0, .6f));
            uiBodyStyle = Text.MakeStyle(Font.Default, .6f * U.cm, Color.HSV(0, 0, .8f));
            UI.PushTextStyle(uiBodyStyle);

            floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            feet = Sprite.FromFile("footprints.png",SpriteType.Single);
            feetTransform = Matrix.TRS(V.XYZ(0, -1.45f, 0), Quat.FromAngles(90,180,0) ,.35f * new Vec3(feet.NormalizedDimensions.x, feet.NormalizedDimensions.y,1));

            iconOpen = Sprite.FromFile("open.png", StereoKit.SpriteType.Single);
            Sprite iconNew = Sprite.FromFile("new.png", StereoKit.SpriteType.Single);

            CreateHandMenu();

            keyboardInput = new KeyboardInput();


            Material handleMaterial = Material.UIBox;
            handleMaterial.SetFloat("border_size", 0.002f);
            handleMaterial.SetFloat("border_size_grow", 0.003f);
            handleMaterial.SetFloat("border_affect_raduis", 0f);

            Startup();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    public void CreateHandMenu()
    {
        try { 
            if (handMenu != null)
            {
                handMenu.Shutdown();
            }
            handMenu = SK.AddStepper(new My.Framework.HandMenuRadial(
                       new HandRadialLayer("Files",
                           new HandMenuItem("New|C", null, NewMindMap),
                           new HandMenuItem("Open|D", null, LoadMindMap, "Nodes"),
                           new HandMenuItem("Demo|E", null, Demos, "Nodes"),
                           new HandMenuItem("Quit|F", null, QuitRequest, "Confirm")),
                       new HandRadialLayer("Confirm",
                           new HandMenuItem("Cancel|G", null, null, HandMenuAction.Back),
                           new HandMenuItem("Confirm XXX|H", null, ConfirmAction, HandMenuAction.Back)),
                       new HandRadialLayer("Nodes",
                           new HandMenuItem("Add node|B", null, AddNode),
                           new HandMenuItem("Add portal|A", null, AddPortal),
                           new HandMenuItem("Save|K", null, SaveMindMap),
                           new HandMenuItem("Play|I", null, Play, "Play"),
                           new HandMenuItem("Exit|F", null, ExitMindMapRequest, "Confirm"),
                           new HandMenuItem("Delete Node|J", null, DeleteNode, "orphan")
                           ),
                       new HandRadialLayer("Play",
                           new HandMenuItem("Slot 1", null, UseObject),
                           new HandMenuItem("Slot 2", null, UseObject),
                           new HandMenuItem("Slot 3", null, UseObject),
                           new HandMenuItem("Slot 4", null, UseObject),
                           new HandMenuItem("Slot 5", null, UseObject),
                           new HandMenuItem("Edit|M", null, Edit, HandMenuAction.Back)
                          ),
                       new HandRadialLayer("orphan",
                           new HandMenuItem("Cancel|G", null, CancelDelete, HandMenuAction.Back),
                           new HandMenuItem("Delete nodes|J", null, DeleteSelectedNode, HandMenuAction.Back)
                           )));


            handMenu.textStyle = handMenuTextStyle;
            handMenu.iconStyle = iconTextStyle;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    static public int[] selectedNodes = new int[2];

    private Pose errorPose = new Pose();
    private float pinchScaleInitialDistance = 0;
    private float pinchScaleInitialScale = 1;

    private Vec3 relativeLocomotionOrigin;
    //private Vec3 relativeLocomotionLookAtOrigin;
    private Vec3 locomotionDirection;


    private bool scalingNode = false;
    private Hand leftHand;


    private float[] closestDistance = new float[2];

    static public int editingNodeId = -1;
    static public string editingNodeDescription = "";

    //private Vec3 scaleHorizontal = new Vec3(1, 0, 1);

    static public void ResetEditingMode()
    {
        editingNodeId = -1;
        editingNodeDescription = "";
    }
    public void Update()
    {
        
        try
        {
            deltaTime = Time.ElapsedUnscaledf;

            HandleLocomotion();

            if (editingNodeId >= 0)
            {
                if (Nodes.ContainsKey(editingNodeId) == false)
                    ResetEditingMode();
            }

            try
            {
                musicInst.Position = Input.Head.position + Vec3.Up;
            }
            catch (Exception ex)
            {
                Log.Err(ex.Source + ":" + ex.Message);
            }

            if (errorMessage != "")
            {
                if (handMenu.active)
                    handMenu.Close();

                errorPose.position = Input.Head.position + Input.Head.Forward * .5f;
                errorPose.orientation = Quat.LookAt(errorPose.position, Input.Head.position);
                UI.WindowBegin("ErrorWindow", ref errorPose, UIWin.Body); ;
                UI.LayoutReserve(V.XY(.2f, .0001f));

                UI.PushTextStyle(uiTitleStyle);
                UI.Label(errorMessage);
                UI.PopTextStyle();

                if (UI.Button("Close"))
                    errorMessage = "";
                UI.WindowEnd();
                return;
            }

            if (Platform.FilePickerVisible == false)
            {
                List<int> keys = new List<int>(Nodes.Keys);

                if (SK.System.displayType == Display.Opaque)
                    Default.MeshCube.Draw(floorMaterial, floorTransform);

                feet.Draw(feetTransform);

                if (menuState == MenuState.EditNodes)
                {
                    int nodeCount = Nodes.Count;

                    closestDistance[0] = closestDistance[1] = 99999999999;

                    float distance = 0;

                    if (handMenu.active == false && scalingNode == false)
                    {
                        selectedNodes[0] = selectedNodes[1] = -1;
                        
                        foreach (int key in keys) // First find closest
                        {
                            Nodes[key].inFocus = false;
                            if (Nodes[key].locationId == locationId)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    distance = Vec3.Distance(Input.Hand(i).pinchPt, Nodes[key].pose.position);
                                    if (distance < closestDistance[i] && distance < Nodes[key].radius + .1f)
                                    {
                                        selectedNodes[i] = key;
                                        closestDistance[i] = distance;
                                    }
                                    else
                                    {

                                        if (Nodes[key].propertiesDisplayed)
                                        {
                                            distance = Vec3.Distance(Input.Hand(i).pinchPt,
                                                Nodes[key].propertiesPose.position +
                                                Nodes[key].propertiesPose.Up * -.12f);


                                            if (distance < closestDistance[i] && distance < Nodes[key].MaxPropertyWidth.Length)
                                            {
                                                selectedNodes[i] = key;
                                                closestDistance[i] = distance;
                                            }

                                        }
                                    }
                                }

                            }
                        }
                    }
                    scalingNode = false;
                    for (int i = 0; i < 2; i++)
                    {
                        if (selectedNodes[i] >= 0)
                            Nodes[selectedNodes[i]].inFocus = true;
                    }
                    if (selectedNodes[0] >= 0 && selectedNodes[0] == selectedNodes[1])
                    {
                        
                        if (Input.Hand(Handed.Right).pinchPt.Length != 0f && Input.Hand(Handed.Left).pinchPt.Length != 0)
                        {
                            if (Input.Hand(Handed.Right).IsJustPinched == true ||
                                Input.Hand(Handed.Left).IsJustPinched == true)
                            {
                                pinchScaleInitialDistance = Vec3.Distance(Input.Hand(Handed.Right).pinchPt, Input.Hand(Handed.Left).pinchPt) * 6;
                                pinchScaleInitialScale = Nodes[selectedNodes[0]].activeState.nodeScale;
                            }

                            if (Input.Hand(Handed.Right).IsPinched == true &&
                                Input.Hand(Handed.Left).IsPinched == true)
                            {
                                scalingNode = true;
                                Nodes[selectedNodes[0]].activeState.nodeScale = pinchScaleInitialScale +
                                    (Vec3.Distance(Input.Hand(Handed.Right).pinchPt, Input.Hand(Handed.Left).pinchPt) * 6f)
                                    - pinchScaleInitialDistance;
                            }
                        }
                        
                    }
                }

                foreach (int key in keys) // Now draw
                {
                    if (Nodes[key].locationId == locationId)
                    {
                        if (Nodes[key].visible || menuState == MenuState.EditNodes)
                            Nodes[key].Draw();
                    }
                }

                keyboardInput.CheckValidInputs();




            }
            HandText();

        } catch (Exception ex)
        {
            Log.Err(ex.Source + ":" + ex.Message);
        }

    }
    static bool deletingNode = false;


    private Pose locomotionPose = new Pose();
    private bool locomotionActive = false;
    private void HandleLocomotion()
    {
        leftHand = Input.Hand(Handed.Left);

        float activation = Math.Max(0.2f, leftHand.gripActivation * .3f);
        bool aligned = Vec3.Dot(leftHand.palm.orientation * Vec3.Forward, (leftHand.palm.position - Input.Head.position).Normalized) < -.99f;


        locomotionPose.position = leftHand.palm.position;
        if (locomotionActive || (leftHand.IsJustGripped && aligned))
            locomotionPose.position += leftHand.palm.orientation * Vec3.Forward * .05f;

        locomotionPose.orientation = Quat.LookAt(locomotionPose.position, Input.Head.position);
        locomotionPose.position += locomotionPose.Forward * U.cm * 1.5f;


        

        if (My.Framework.HandMenuRadial.HandFacingHead(leftHand) || locomotionActive)
        {
            string text;


            if (locomotionActive == false)
            {
                text = aligned ? "Grip" : "Align";
                Hierarchy.Push(locomotionPose.ToMatrix(activation));
                Lines.Add(handMenu.circle);
                Hierarchy.Push(Matrix.S(1.6f));
                Text.Add(text, Matrix.Identity, TextAlign.Center);
                Hierarchy.Pop();
                Hierarchy.Pop();
            
                Hierarchy.Push(Matrix.TRS(leftHand.palm.position + (leftHand.palm.orientation * Vec3.Forward * .03f), leftHand.palm.orientation, .16f));
                Lines.Add(handMenu.circle);
                Hierarchy.Pop();
            }
        }

        Hierarchy.Push(Renderer.CameraRoot);
        if (leftHand.IsGripped == false)
            locomotionActive = false;

        if (leftHand.IsJustGripped && aligned)
        {
            relativeLocomotionOrigin = Hierarchy.ToLocal(locomotionPose.position);
            locomotionActive = true;
        }

        if (locomotionActive && leftHand.palm.position.Length != 0)
        {
            
            Hierarchy.Push(Matrix.TRS(relativeLocomotionOrigin, locomotionPose.orientation, activation));
            Lines.Add(handMenu.circle);
            Hierarchy.Push(Matrix.S(1.6f));
            Text.Add("Drag", Matrix.Identity, TextAlign.Center);
            Hierarchy.Pop();
            Hierarchy.Pop();


            locomotionDirection = Hierarchy.ToLocal(locomotionPose.position) - relativeLocomotionOrigin;
            locomotionDirection = Hierarchy.ToWorldDirection(locomotionDirection);

            Renderer.CameraRoot = Matrix.T(
                Renderer.CameraRoot.Pose.position + locomotionDirection * deltaTime * locomotionDirection.Length * 50
                );

        }

        Hierarchy.Pop();
    }


    private void HandText()
    {
        try { 
            Hand hand = Input.Hand(Handed.Right);
            Vec3 pos = hand.palm.position + hand.palm.Forward * -.05f;

            string text = "";
            text = "Health: " + health + "%\n"
                + "Score: " + score;

            if (Vec3.Dot(hand.palm.Forward, (hand.palm.position - Input.Head.position).Normalized) > .6f)
            {
                Text.Add(text, Matrix.TRS(
                    pos,Quat.LookAt(pos, Input.Head.position),Vec3.One * 1f), titleStyle, TextAlign.Center, TextAlign.Center);
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    public void Shutdown()
    {

    }


    private void AddNode()
    {
        try { 
            Hand hand = Input.Hand(Handed.Right);
            Vec3 position = hand[FingerId.Ring, JointId.Tip].position;
            Node newNode = new Node(false, "", new Pose(position, Quat.LookAt(position, Input.Head.position)));

            Nodes.Add(newNode.id, newNode);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void AddPortal()
    {
        try { 
            Hand hand = Input.Hand(Handed.Right);
            Vec3 position = hand[FingerId.Ring, JointId.Tip].position;
            Node newNode = new Node(false, "", new Pose(position, Quat.LookAt(position, Input.Head.position)), true, "portal.obj");

            Nodes.Add(newNode.id, newNode);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    private void Files()
    {
        try { 
            menuState = MenuState.Files;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void Demos()
    {
        try { 
            handMenu.Close();
            LoadMindMap("Assets\\demo.dat");
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

    }

    private void Play()
    {
        try { 
            Main.menuState = MenuState.Play;
            handMenu.Close();
            ResetNodeStatus(true);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void Edit()
    {
        try
        {
            Main.menuState = MenuState.EditNodes;
            handMenu.Close();
            ResetNodeStatus(false);
        }
            catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void ResetNodeStatus(bool saveChangesInEditMode)
    {
        try { 
            inventoryNodeIds = new List<int>();
            inventoryNodeIdHeld = -1;
            score = 0;
            health = 100;
            foreach (KeyValuePair<int, Node> kvp in Nodes)
                kvp.Value.InitForNewPlay(saveChangesInEditMode);
            RedrawInventory();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
    private void Back()
    {
        try { 
            switch (menuState)
            {
                case MenuState.Demos:
                case MenuState.EditNodes:
                case MenuState.Play:
                    menuState = MenuState.Files;
                    break;
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void QuitRequest()
    {
        actionRequested = "quit";
    }

    private void ExitMindMapRequest()
    {
        actionRequested = "exit";
    }


    private string nodeSeparator = "@~@";

    private void LoadMindMap()
    {
        try { 
            handMenu.Close();
            if (Platform.FilePickerVisible == false)
            {
                Platform.FilePicker(PickerMode.Open, LoadMindMap, cancelLoadMindMap, ".dat");
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void LoadMindMap(string filename)
    {
        try
        {
            ResetNodes();
            mindMapFilename = filename;
            Default.SoundClick.Play(Input.Head.position);
            byte[] bytes = Platform.ReadFileBytes(filename);
            StopMusic();

            string input = System.Text.Encoding.UTF8.GetString(bytes);

            Nodes = new Dictionary<int, Node>();
            Node newNode;
            string[] nodesData = input.Split(nodeSeparator);
            locationId = 0;
            foreach (string nodeData in nodesData)
            {
                newNode = new Node(nodeData);
                Nodes.Add(newNode.id, newNode);
                if (newNode.isLocation)
                {
                    Main.LocationNames[newNode.id] = newNode.activeState.name;
                    if (locationId == 0)
                        locationId = newNode.id;
                }
            }

            if (filename == "Assets\\demo.dat")
            {
                handMenu.Reset();
                handMenu.SelectLayer("Nodes");
                //handMenu.SelectLayer("Play");
                Main.menuState = MenuState.EditNodes;
            }
            else
            {
                if (filename == "Assets\\startup.dat")
                {
                    handMenu.Reset();
                    menuState = MenuState.Files;
                }
                else
                    menuState = MenuState.EditNodes;
            }

            GoToLocation(locationId);
        } catch (Exception ex)
        {
            Log.Err(ex.Source + ":" + ex.Message);
        }
    }

    private void cancelLoadMindMap()
    {
        try { 
            handMenu.Back();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
    private void SaveMindMap()
    {
        try { 
            SaveMindMap(mindMapFilename);
            Default.SoundClick.Play(Input.Head.position);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
    private void SaveMindMap(string filename)
    {
        try { 
            List<string> nodeDetails = new List<string>();
            foreach (KeyValuePair<int, Node> kvp in Nodes)
                nodeDetails.Add(kvp.Value.Serialise());

            string output = string.Join(nodeSeparator, nodeDetails.ToArray());

            Platform.WriteFile(filename, System.Text.Encoding.UTF8.GetBytes(output));
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }



    private void NewMindMap()
    {
        try { 
            if (Platform.FilePickerVisible == false) {
                Platform.FilePicker(PickerMode.Save, NewFilenameSelected, null, "");
                handMenu.Back();
                handMenu.Close();
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    private void NewFilenameSelected(string filename)
    {
        try { 
            ResetNodes();
            if (filename.ToLower().EndsWith(".dat") == false)
                filename += ".dat";

            mindMapFilename = filename;
        
            handMenu.SelectLayer("Nodes");
            NewLocation();
            menuState = MenuState.EditNodes;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }

    }

    private void ResetNodes()
    {
        try {    
            LocationNames = new Dictionary<int, string>();
            Nodes = new Dictionary<int, Node>();
            Node.maxNodeId = 0;
            selectedNodes[0] = selectedNodes[1] = -1;

            inventoryNodeIds = new List<int>();
            inventoryNodeIdHeld = -1;
            score = 0;
            health = 100;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    static public string actionRequested;
    private void ConfirmAction()
    {
        try { 
            switch (actionRequested)
            {
                case "quit":
                    StopMusic();
                    SK.Quit();
                    break;
                case "exit":
                    StopMusic();
                    handMenu.Back();
                    handMenu.Close();
                    Startup();
                    break;
            }
            actionRequested = "";
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void Startup()
    {
        try { 
            handMenu.Reset();
            LoadMindMap("Assets\\startup.dat");
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    static public string nameMe = "Name me!";
    static public void NewLocation()
    {
        try { 
            StopMusic();
            Vec3 position = Input.Head.position + Input.Head.Forward * .4f;

            Node newNode = new Node(true, nameMe, new Pose(position, Quat.LookAt(position, Input.Head.position)), false, "cube.obj");
            Main.Nodes.Add(newNode.id, newNode);
            Main.locationId = newNode.locationId = newNode.id;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


    static public void GoToLocation(int destinationId)
    {
        try { 
            if (destinationId == 0)
            {
                int savedLocation = locationId;
                Main.NewLocation();
                Vec3 position = Input.Head.position + Input.Head.Forward * .4f + Input.Head.Right * .4f;
                Node newNode = new Node(false, "", new Pose(position, Quat.LookAt(position, Input.Head.position)), true, "portal.obj");
                newNode.destinationId = savedLocation;

                Nodes.Add(newNode.id, newNode);
            }
            else
            {
                Main.locationId = destinationId;
            }
            PlayMusicForLocation();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    static public string errorMessage = "";
    private void DeleteNode()
    {
        try { 
            bool confirmDelete = false;

            if (selectedNodes[1] <= 0)
            {
                errorMessage = "Place your right hand closer to the\nnode before selecting delete";
            }
            else
            {
                if (selectedNodes[1] >= 0)
                {
                    if (Nodes[selectedNodes[1]].isLocation)
                    {
                        errorMessage = "This is the root node for this\nlocation and cannot be deleted";
                    }
                    else
                    {
                        confirmDelete = true;
                    }
                }
            }
            if (confirmDelete)
                deletingNode = true;
            else
                handMenu.Back();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private int OrphanCheck(int locationId)
    {
        
        int portalCount = 0;

        try { 
            foreach (KeyValuePair<int, Node> kvp in Nodes)
            {
                if (kvp.Value.isPortal && kvp.Value.destinationId == locationId)
                    portalCount++;
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
        return portalCount;
    }

    private void DeleteSelectedNode()
    {
        try { 
            DeleteOnlyNode(selectedNodes[1]);
            selectedNodes[1] = -1;
            handMenu.Close();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }
    private void DeleteOnlyNode(int nodeId)
    {
        try { 
            Nodes.Remove(nodeId);
            deletingNode = false;
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void DeleteOrphans()
    {
        try { 
            List<int> keys = new List<int>();
            foreach (KeyValuePair<int, Node> kvp in Nodes)
            {
                if (kvp.Value.locationId == Nodes[selectedNodes[1]].destinationId)
                    keys.Add(kvp.Key);
            }

            foreach (int key in keys)
                DeleteOnlyNode(key);
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    private void CancelDelete()
    {
        deletingNode = false;
    }

    static public void MenuClosed()
    {
        try { 
            if (Main.deletingNode)
            {
                Main.handMenu.Back();
                Main.deletingNode = false;
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    static public void PlayMusicForLocation()
    {
        try
        {
            Main.StopMusic();
            if (Main.Nodes.ContainsKey(locationId))
            {
                string filename = Main.Nodes[locationId].activeState.musicFilename;
                if (filename != "")
                {
                    Main.musicInst = Sound.FromFile(filename).Play(Main.Nodes[locationId].pose.position, .2f);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Err(ex.Source + ":" + ex.Message);
        }
    }

    static public void StopMusic()
    {
        try
        {
            Main.musicInst.Stop();
        } catch (Exception ex)
        {
            Log.Err(ex.Source + ":" + ex.Message);
        }
    }


    static private int inventoryInxInUse = 0;
    static public int inventoryNodeIdHeld = 0;
    private void UseObject()
    {
        try { 
            inventoryInxInUse = handMenu.lastIdSelected;
            RedrawInventory();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }

    static public bool AddObjectToSlot(int nodeId)
    {
        try { 
            if (Nodes[nodeId].isLocation)
            {
                errorMessage = "You can't carry a location";
                return false;
            }
            inventoryNodeIds.Add(nodeId);
            if (inventoryNodeIds.Count > 5)
                inventoryNodeIds.RemoveAt(0);
            RedrawInventory();
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
        return true;
        
    }

    static public void RedrawInventory()
    {
        try { 
            inventoryNodeIdHeld = 0;
            int count = 0;
            foreach (int id in inventoryNodeIds)
            {
                handMenu.layers[HandMenuInventoryLayer].items[count].name = Nodes[id].activeState.name;
                if (count == inventoryInxInUse)
                {
                    handMenu.layers[HandMenuInventoryLayer].items[count].name += "|B";
                    inventoryNodeIdHeld = id;
                }
                count++;
            }
            for (int i = count; i < 5; i++)
            {
                handMenu.layers[HandMenuInventoryLayer].items[i].name = "Slot " + (i + 1);
                if (i == inventoryInxInUse)
                    handMenu.layers[HandMenuInventoryLayer].items[i].name += "|B";
            }
        }
        catch (Exception ex) { Log.Err(ex.Source + ":" + ex.Message); }
    }


}
