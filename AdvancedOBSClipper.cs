using System;
using System.IO;
using System.Text;
using System.Linq;

public class CPHInline
{
	public bool Execute()
	{
        try // Attempts the clipping
        {
            // Set your ReplayBuffer FilePaths Here. Remember to double backslash for each directory.
            string yourReplayPath = "C:\\";
            string yourOutputPath = "C:\\Clips\\";

            // Variables used in the routine
            string fileNameInput = args["rawInput"].ToString(); // Name of the clip, which is used as the the new filename.
            string clipUser = args["userName"].ToString(); // Triggering User for attribution
            var platform = args["eventSource"].ToString();
            string twitch = "twitch";
            string youtube = "youtube";

            // If there's no input from the chatter, it won't save the clip.
            if (String.IsNullOrEmpty(fileNameInput))
            {
                var errMsg = "⚠" + args["userName"].ToString() + ", you must specify a name for the clip. Your clip has not been saved.";
                if (platform == youtube) {
                    CPH.SendYouTubeMessage(errMsg);
                    return true;
                } 
                else if(platform == twitch)
                {
                    CPH.SendMessage(errMsg);
                    return true;
                }
            }
            // The actual OBS Replay Buffer save event
            CPH.ObsReplayBufferSave();
            // Wait 2 seconds in case there's a delay of whatever reason
            System.Threading.Thread.Sleep(2000);
            // Scan the initial folder where the clip is saved for the newest file created - presumably the clip
            var clipBackups = new DirectoryInfo(@"" + yourReplayPath + "");
            // Filter by MKV files - or change to whatever file output format you have your replay buffer configured to.
            var clipFile = clipBackups.GetFiles("*.mkv").OrderByDescending(p => p.CreationTime).FirstOrDefault();
            // If no file found, quit out.
            if (clipFile == null)
                return true; //
            // Grab the file name by the Windows Path
            var clipPath = clipFile.FullName;
            // Rename the file
            string newFileName = @"" + yourOutputPath + fileNameInput + " (clipped by " + clipUser + ").mkv";
            System.IO.File.Move(clipPath, newFileName);
            // Send an alert to the YouTube chat that the clip has been successfully saved by the user.
            string msgOutput = "🎬" + args["userName"].ToString() + " has just saved a clip titled " + fileNameInput + "!";
            if (platform == youtube) 
            {
                CPH.SendYouTubeMessage(msgOutput);
                return true;
            } 
            else if(platform == twitch)
            {
                CPH.SendMessage(msgOutput);
                return true;
            }
            // Finish up.
            return true;
        }
        catch // If something doesn't work as expected.
        {
            var platform = args["eventSource"].ToString();
            string twitch = "twitch";
            string youtube = "youtube";
            var errMsg = "⚠" + args["userName"].ToString() + ", something went wrong and no clip was created. Perhaps the streamer hasn't enabled OBS replay buffer.";
            if (platform == youtube) 
            {
                CPH.SendYouTubeMessage(errMsg);
                return true;
            } 
            else if(platform == twitch)
            {
                CPH.SendMessage(errMsg);
                return true;
            }
            return false;
        }
	}
}