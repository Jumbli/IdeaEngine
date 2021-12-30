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
            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "IdeaEngine",
                assetsFolder = "Assets",
            };

            // Core application loop
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            Main main;
            System.Action step;
            main = new Main();
            main.Init();
            step = main.Update;

            while (SK.Step(step)) { }
            
            main.Shutdown();

            SK.Shutdown();
            
        }






    }

    
}
