using StereoKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace StereoKitProject1
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Main main;
            Action step;

            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "IdeaEngine",
                assetsFolder = "Assets",
            };

            bool looping = true;
            // Core application loop
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            main = new Main();
            main.Init();
            step = main.Update;

            bool firstRun = true;

            while (looping)
            {
                try
                {

                    if (firstRun == false)
                    {
                        if (!SK.Initialize(settings))
                            Environment.Exit(1);
                    }
                    firstRun = false;


                    while (SK.Step(step)) { }
                    looping = false;
                }
                catch (Exception ex)
                {
                    main.Update();
                    main.CreateHandMenu();
                    step = main.Update;
                    looping = false;
                }
            }
            
            
            main.Shutdown();

            SK.Shutdown();
            
        }






    }

    
}
