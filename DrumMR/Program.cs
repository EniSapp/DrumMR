using StereoKit;
using System;

namespace DrumMR
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "DrumMR",
                assetsFolder = "Assets",
            };
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            // Core application loop
            while (SK.Step(() =>
            {
                //APPLICATION LOGIC GOES HERE
            })) ;
            SK.Shutdown();
        }
    }
}
