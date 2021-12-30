using StereoKit;
using System;
using System.Collections.Generic;
using System.Linq;

public class KeyboardInput
{
    private string[] keys = {
        "1234567890" + (char)Key.Backspace + " <>*-",
        "qwertyuiop! #£$+" ,
        "asdfghjkl:" + (char)Key.Return + " {}[]",
        "zxcvbnm,.?; /" +  "@\\\"",
        (char)Key.Shift + " " + (char)Key.Tab + "" + (char)Key.Up +  "" + (char)Key.Left + "" + (char)Key.Right + "" + (char)Key.Down
    };
    private string[] symbols =
    {

        "1234567890",
        "!\"£$%^&*()_=",
        "`¬;:'@#~<>/\\|",
        "+-?"
    };

    private Dictionary<Key,string> specialKeys = new Dictionary<Key, string>();

    private string virtualKeystrokes="";

    public Pose pose;

    private bool capsLock = false;

    private TextStyle iconTextStyle;
    private Vec2 buttonSize;
	public KeyboardInput()
	{
        iconTextStyle = Text.MakeStyle(Main.iconFont, .5f * U.cm, Color.HSV(0, 0, .8f));
        buttonSize = new Vec2(.025f, .025f);
        specialKeys.Add(Key.Return, "Q");
        specialKeys.Add(Key.Shift, "O");
        specialKeys.Add(Key.Tab, " ");
        specialKeys.Add(Key.Up, "U");
        specialKeys.Add(Key.Down, "T");
        specialKeys.Add(Key.Right, "S");
        specialKeys.Add(Key.Left, "R");
        specialKeys.Add(Key.Backspace, "P");
    }

    struct Request
    {
        public float timeAdded;
        public float timelastRequested;
        public Pose pose;
        public Request(Pose newPose)
        {
            timelastRequested = timeAdded = Time.TotalUnscaledf;
            pose = newPose;
        }

        public Request UpdateRequest()
        {
            timelastRequested = Time.TotalUnscaledf;
            return this;
        }

    }
    Dictionary<string, Request> requests = new Dictionary<string, Request>();

    Stack<string> requestStack = new Stack<string>();
    

    public void CheckValidInputs()
    {
        // Find latest request
        string[] keys = requests.Keys.ToArray();
        float frameTime = Time.TotalUnscaledf;

        mostRecentlyAddedRequestKey = "";
        mostRecentlyAddedRequestTime = 0;

        foreach (string key in keys)
        {
            if (requests[key].timelastRequested < frameTime) // Input no longer being requested
            {
                requests.Remove(key);
            }
            else
            {
                if (requests[key].timeAdded > mostRecentlyAddedRequestTime)
                {
                    mostRecentlyAddedRequestKey = key;
                    mostRecentlyAddedRequestTime = requests[key].timeAdded;
                }
            }
        }


    }
    public string mostRecentlyAddedRequestKey = "";
    public float mostRecentlyAddedRequestTime;

    public bool Update(string inputKey, bool relative, Vec3 position, Quat orientation, float offset, ref string subject)
    {
        bool keyPressed = false;
        char key;

        if (requests.ContainsKey(inputKey) == false) // Is this a new request
        {
            requestStack.Push(inputKey);
            requests.Add(inputKey, new Request(pose));
            if (relative)
            {
                Main.keyboardInput.pose.position = position + Vec3.Up * offset;
                Main.keyboardInput.pose.orientation = orientation * Quat.LookAt(position, Hierarchy.ToLocal(Input.Head.position) + new Vec3(0, .5f, 0));
            } else
            {
                Main.keyboardInput.pose.orientation = Quat.Identity * Quat.LookAt(position, Input.Head.position + new Vec3(0, .5f, 0));
                Main.keyboardInput.pose.position = position + (Main.keyboardInput.pose.orientation * Vec3.Up) * offset;
            }
            return false;
        }
        requests[inputKey] = requests[inputKey].UpdateRequest(); // Confirm it's still in use

        if (inputKey != mostRecentlyAddedRequestKey)
            return false;


        if (virtualKeystrokes != "") {
            key = virtualKeystrokes[0];
            virtualKeystrokes = virtualKeystrokes.Substring(1);
        } else 
            key = Input.TextConsume();

        if (key != '\0')
        {
            keyPressed = true;
            switch (key)
            {
                case (char)Key.Backspace:
                    if (subject.Length > 0)
                        subject = subject.Substring(0, subject.Length - 1);
                    break;
                case (char)Key.Left:
                    break;
                case (char)Key.Right:
                    break;
                case (char)Key.End:
                    break;
                case (char)Key.Home:
                    break;
                case (char)Key.Del:
                    break;
                case (char)Key.Tab:
                    subject += " ";
                    break;
                case (char)Key.Return:
                    subject += "\n\r";
                    break;
                default:
                    subject += key;
                    break;
            }
        }
        //pose.position = Input.Head.position - Vec3.Up * -.2f + Vec3.Forward * -.1f;
        //pose.orientation = Input.Head.orientation;
        int inx;
        int rowCount = 0;
        string rowOfKeys;
        UI.WindowBegin("KeyboardInput", ref pose, UIWin.Body,UIMove.Exact);
        foreach (string row in keys)
        {
            inx = 0;

            rowCount++;
            if (capsLock)
                rowOfKeys = row.ToUpper();
            else
                rowOfKeys = row;

            foreach (char c in rowOfKeys)
            {
                if (c == ' ')
                    UI.Label("  ");
                else
                {
                    if (specialKeys.ContainsKey((Key)c))
                    {
                        switch ((Key)c)
                        {
                            case Key.Shift:
                                UI.PushTextStyle(iconTextStyle);
                                UI.Toggle(specialKeys[(Key)c], ref capsLock);
                                UI.PopTextStyle();
                                break;

                            case Key.Tab:
                                if (UI.Button(" ", buttonSize * V.XY(13, 1)))
                                    virtualKeystrokes += " ";
                                break;
                            default:
                                UI.PushTextStyle(iconTextStyle);
                                if (UI.Button("" + specialKeys[(Key)c]))
                                {
                                    virtualKeystrokes += c;
                                }
                                UI.PopTextStyle();
                                break;
                        }
                        
                    }
                    else
                    {
                        if (UI.Button("" + c))
                        {
                            virtualKeystrokes += c;
                        }
                    }
                }
                if (inx++ < row.Length - 1)
                    UI.SameLine();

            }
        }

        UI.WindowEnd();

        return keyPressed;

    }
    
}
