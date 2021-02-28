using StereoKit;
using System;
using Microsoft.MixedReality.QR;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace DrumMR
{
    struct Note
    {
        public double time;
        public int pad;
    }
    class Program
    {
        static Pose[] drumLocations = new Pose[3];
        static Note[] notes;
        static string[] songs = { "SoneOne", "SongTwo", "SongThree" };
        //TODO: CHANGE ME INTO THE ACTUAL LIST OF SONGS

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

            var QRCodeWatcherAccess = GetAccessStatus().Result;
            if (QRCodeWatcherAccess != QRCodeWatcherAccessStatus.Allowed)
            {
                Debug.WriteLine("ERROR: PERMISSION TO READ QR CODES NOT GRANTED");
                return;
            }

            //Initialize drumLocations to a "default pose".  We will check if these have changed to determine if that drum has been located.
            InitializeDrumLocations();

            //Directly modifies drumLocations[i] with the location of the i'th drum.
            SetQRPoses();
            WaitFromDrumInitialization();

            // Core application loop
            while (SK.Step(() =>
            {
                if (notes is null)
                {
                    Pose windowPose = new Pose(-.4f, 0, 0, Quat.LookDir(1, 0, 1));
                    UI.WindowBegin("Window", ref windowPose, new Vec2(20, 0) * U.cm, UIWin.Normal);
                    for (int i = 0; i < songs.Length; i++)
                    {
                        if (UI.Button(songs[i]))
                        {
                            string jsonString = getJSONStringOfSong(songs[i]);
                            notes = parseJSONSong(jsonString);
                            notes = sortNotes(notes);
                            //On the next frame notes will no longer be null and the "else" clause will be entered
                        }
                    }
                }
                else
                {
                    //Play the game!
                }
            })) ;
            SK.Shutdown();
        }

        private static async Task<QRCodeWatcherAccessStatus> GetAccessStatus()
        {
            return await QRCodeWatcher.RequestAccessAsync();
        }

        //Sets an event handler to fill drumLocations[i] with the found location of the QR code with the text i.  Returns whether the initialization was successful.
        private static void SetQRPoses()
        {
            QRCodeWatcher watcher;
            DateTime watcherStart;

            watcherStart = DateTime.Now;
            watcher = new QRCodeWatcher();
            watcher.Added += (o, qr) => {
                // QRCodeWatcher will provide QR codes from before session start,
                // so we often want to filter those out.
                if (qr.Code.LastDetectedTime > watcherStart)
                {
                    drumLocations[Int32.Parse(qr.Code.Data)] = World.FromSpatialNode(qr.Code.SpatialGraphNodeId);
                    Debug.WriteLine("QR Code number " + qr.Code.Data + " has been located.  Move to the next code");
                }
            };
            watcher.Start();
        }

        //Returns whether parameter p is the "default pose" or a meaningful one
        private static bool PoseIsInitialized(Pose p)
        {
            if (p.position.x == 0 && p.position.y == 0 && p.position.z == 0 && p.orientation.Equals(Quat.Identity))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //Initializes drumLocations[] with default values
        private static void InitializeDrumLocations()
        {
            for (int i = 0; i < drumLocations.Length; i++)
            {
                drumLocations[i] = new Pose(0, 0, 0, Quat.Identity);
            }
        }

        //Sleeps the current thread until all of drumLocations[] has been initialized.
        private static void WaitFromDrumInitialization()
        {
            bool allDrumsFound;
            do
            {
                allDrumsFound = true;
                for (int i = 0; i < drumLocations.Length; i++)
                {
                    if (!PoseIsInitialized(drumLocations[i]))
                    {
                        allDrumsFound = false;
                    }
                }
                Thread.Sleep(500);
                Debug.WriteLine("sleep");
            } while (!allDrumsFound);
        }
        private static string getJSONStringOfSong(string songName)
        {
            return System.IO.File.ReadAllText(songName);
        }

        private static Note[] parseJSONSong(string json)
        {
            JArray array = JArray.Parse(json);
            Note[] toReturn = new Note[array.Count];
            int index = 0;
            foreach (JObject jobject in array) {
                Note newNote = new Note();
                newNote.time = jobject.GetValue("time").ToObject<double>();
                newNote.pad = jobject.GetValue("drum").ToObject<int>();
                toReturn[index] = newNote;
                index++;
            }
            return toReturn;
        }

        private static Note[] sortNotes(Note[] ar)
        {
            Array.Sort<Note>(ar, (x, y) => x.time.CompareTo(y.time));
            return ar;
        }
    }
}
