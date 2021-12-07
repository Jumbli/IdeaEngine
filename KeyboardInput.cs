using StereoKit;
using System;

public class KeyboardInput
{
	public KeyboardInput()
	{
	}

	public void Update(ref string subject)
    {
        char key = Input.TextConsume();
        if (key != '\0')
        {
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
                case (char)Key.Return:
                    subject += "\n\r";
                    break;
                default:
                    subject += key;
                    break;
            }
        }
    }
}
