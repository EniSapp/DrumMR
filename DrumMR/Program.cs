using StereoKit;
using System;
using Microsoft.MixedReality.QR;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO.Ports;

namespace DrumMR
{
    struct Note
    {
        public double time;
        public int pad;
    }
    class Program
    {
        static Pose[] drumLocations = new Pose[4];
        static Note[] notes;
        static string[] songs = { "Istanbul", "particle", "tmbg", "wheel", "whistling" };
        const double timeLengthOfGameBoard = 1.5;
        static bool[] buffer = new bool[4];

        static void Main(string[] args)
        {
            Sound song;
            Queue<Note>[] noteQueues = null;
            int positionInNotes = 0;
            double songStartTime = 0;
            DateTime watcherStart = DateTime.Now;

            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "DrumMR",
                assetsFolder = "Assets",
            };
            if (!SK.Initialize(settings))
                Environment.Exit(1);
            
            

            SerialPort port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
            port.DataReceived += (sender, dataArgs) =>
            {
                SerialPort sp = (SerialPort)sender;
                buffer[Int32.Parse(sp.ReadExisting())] = true;
            };




            Vec3[] unitVectors = createNewUnitVectors(); //x, y, and z
            Mesh boardMesh = Mesh.GenerateCube(new Vec3(.30f, .20f, .1f));
            Mesh noteMesh = Mesh.GenerateCube(new Vec3(.04f, .048f, .1f));
            Model boardModel = Model.FromMesh(boardMesh, Default.Material);
            Model noteModel = Model.FromMesh(noteMesh, Default.Material);
            Debug.WriteLine(unitVectors[0]);



            Quat boardQuat = new Quat(1, 1, 1, 1);

            Vec3 boardLocation = new Vec3(drumLocations[1].orientation.x + .05f*(unitVectors[0].x+unitVectors[1].x+unitVectors[2].x), drumLocations[1].orientation.y + .05f*((unitVectors[0].y + unitVectors[1].y + unitVectors[2].y)), drumLocations[1].orientation.z + (((unitVectors[0].z + unitVectors[1].z + unitVectors[2].z))));
            Pose boardPose = new Pose(boardLocation, boardQuat);
            boardModel.Draw(boardPose.ToMatrix(), Color.Black);
            //TODO: SET THIS QUAT TO BE PARALLEL TO UNITVECTORS[0]
            //TODO: MAKE DIFFERENTLY COLORED NOTE MESHES FOR EACH LANE
            //TODO: ROTATION IS GOING TO NEED TO BE FIGURED OUT.  IT CAN'T TURN DYNAMICALLY BECAUSE THE NOTES WOULD NEED TO TURN AS WELL

            // Core application loop

            InitializeDrumLocations();
            while (SK.Step(() =>
            {
                if (!PoseIsInitialized(drumLocations[0]))
                {
                    //getDrumLocation(0);
                } else if (!PoseIsInitialized(drumLocations[1]))
                {
                    //getDrumLocation(1);
                } else if (!PoseIsInitialized(drumLocations[2]))
                {
                    //getDrumLocation(2);
                } else if (!PoseIsInitialized(drumLocations[3]))
                {
                   // getDrumLocation(3);
                }
                else if (notes is null)
                {
                    //TODO: CHANGE THIS TO MOVE WITH THE USER USING INPUT.HEAD.POSITION?
                    Pose windowPose = new Pose(-.4f, 0, 0, Quat.LookDir(1, 0, 1));
                    UI.WindowBegin("Window", ref windowPose, new Vec2(20, 0) * U.cm, UIWin.Normal);
                    for (int i = 0; i < songs.Length; i++)
                    {
                        if (UI.Button(songs[i]))
                        {
                            string jsonString = getJSONStringOfSong(songs[i] + ".json");
                            notes = parseJSONSong(jsonString);
                            notes = sortNotes(notes);
                            //song = Sound.FromFile(songs[i] + ".wav");
                            //song.Play(Input.Head.position);
                            songStartTime = Time.Total;
                            noteQueues = new Queue<Note>[3];
                        }
                    }
                    UI.WindowEnd();
                }
                else
                {
                    float midpoint = (drumLocations[0].position.x + drumLocations[3].position.x)/2;
                    float notePoint = midpoint / 4;
                    while (positionInNotes < notes.Length && notes[positionInNotes].time-(songStartTime-Time.Total) > timeLengthOfGameBoard)
                    {
                        Note noteToPush = notes[positionInNotes];
                        noteQueues[noteToPush.pad].Enqueue(noteToPush);
                        positionInNotes++;
                    }
                    boardModel.Draw(boardPose.ToMatrix(), Color.Black);
                    for (int i = 0; i < noteQueues.Length; i++)
                    {
                        for (int j = 0; j < noteQueues[i].Count; j++)
                        {
                            Note noteToRender = noteQueues[i].Dequeue();
                            Pose notePose = new Pose(notePoint * i, (float)(noteToRender.time - Time.Total) *(float)( .30/1.5) , boardLocation.z+ (float).1,boardQuat);
                            noteModel.Draw(notePose.ToMatrix(), Color.Black);
                            //TODO: RENDER THE NOTE HERE
                            //unitVectors[3] contains vectors representing 





                            if (j == 0 && buffer[i])
                            {
                                buffer[i] = false;
                            } else
                            {
                                noteQueues[i].Enqueue(noteToRender);
                            }
                        }
                    }



                }
            })) ;
            SK.Shutdown();
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

        private static Vec3[] createNewUnitVectors()
        {
            Vec3 dOneLocation = drumLocations[1].position;
            Vec3 dTwoLocation = drumLocations[2].position;
            Vec3 xUnit = new Vec3((dTwoLocation.x - dOneLocation.x)*1/.22f, (dTwoLocation.y - dOneLocation.y) * 1 / .22f, (dTwoLocation.z - dOneLocation.z) * 1 / .22f);
            Vec3 yUnit = new Vec3(-xUnit.y, xUnit.x, xUnit.z);
            Vec3 zUnit = new Vec3(-xUnit.z, xUnit.y, xUnit.x);
            return new Vec3[] { xUnit, yUnit, zUnit };
        }

        private static void getDrumLocation(int index)
        {
            Debug.WriteLine("Touch the controller to drum " + index + " now");
            if (Input.Hand((Handed)0).IsJustGripped)
            {
                Debug.WriteLine("Pinched!");
                drumLocations[index] = Input.Hand((Handed)0).palm;
            }
        }
    }






}
