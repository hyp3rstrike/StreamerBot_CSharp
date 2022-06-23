using System;
using System.IO;
using System.Text;
using System.Linq;

public class CPHInline
{
    public bool Execute()
    {
        try
        {
            string yourReplayPath = "V:\\";
            string yourOutputPath = "V:\\Clips\\";
            string yourFileFormat = "mkv"; // Set this to your chosen output format. Remember, you can remux MKV's to MP4's directly via OBS.
            // Variables used in the routine
            string fileNameInput = args["rawInput"].ToString(); // Name of the clip, which is used as the the new filename.
            string clipUser = args["userName"].ToString(); // Triggering User for attribution
            // Set your ReplayBuffer FilePaths Here. Remember to double backslash for each directory.
            // If there's no input from the chatter, it won't save the clip.
            if (String.IsNullOrEmpty(fileNameInput))
            {
                var errNoFn = "⚠" + args["userName"].ToString() + ", you must specify a name for the clip. Your clip has not been saved.";
                CPH.SendYouTubeMessage(errNoFn);
                CPH.SendMessage(errNoFn);
                return true;
            }

            // The actual OBS Replay Buffer save event
            CPH.ObsReplayBufferSave();
            // Wait 2 seconds in case there's a delay of whatever reason
            System.Threading.Thread.Sleep(2000);
            // Scan the initial folder where the clip is saved for the newest file created - presumably the clip
            var clipBackups = new DirectoryInfo(@"" + yourReplayPath + "");
            // Filter by MKV files - or change to whatever file output format you have your replay buffer configured to.
            var clipFile = clipBackups.GetFiles("*." + yourFileFormat).OrderByDescending(p => p.CreationTime).FirstOrDefault();
            // If no file found, quit out.
            if (clipFile == null)
            {
                return true; //
            }

            // Grab the file name by the Windows Path
            var clipPath = clipFile.FullName;
            // Rename the file
            string newFileName = @"" + yourOutputPath + fileNameInput + " (clipped by " + clipUser + ")." + yourFileFormat;
            System.IO.File.Move(clipPath, newFileName);
            // Send an alert to the YouTube chat that the clip has been successfully saved by the user.
            string msgOutput = "🎬" + args["userName"].ToString() + " has just saved a clip titled " + fileNameInput + "!";
            CPH.SendYouTubeMessage(msgOutput);
            CPH.SendMessage(msgOutput);
            return true;
        }
        catch
        {
            var errMsg = "⚠" + args["userName"].ToString() + ", something went wrong and no clip was created. Perhaps the streamer hasn't enabled replay buffer or a clip already exists with that name.";
            CPH.SendYouTubeMessage(errMsg);
            CPH.SendMessage(errMsg);
            return true;
        }

        return false;
    }
}