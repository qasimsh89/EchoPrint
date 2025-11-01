// Author: Muhammad Qasim
// Date:25-10-2025
// Student ID: c3360527
// Description: This code defines a helper class for managing file paths related to audio recordings in a Maui application.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace ECHO_PRINT.Helpers
{
    internal class Path_helper
    {
        public static string RecordingPath() // function for creating folder to save the recordings
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "Recordings"); // defining path and folder name
            Directory.CreateDirectory(path);
            return path;
        }
        public static string RecordingName() => // declaring the date and time
                $"Echo_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
        public static string FullPath() => Path.Combine(RecordingPath(), FullPath()); // combing  recording and full path to save the file accurately.

        public static string FileName(string Name)
        {
            string userName = $"{Name}.wav";
            return Path.Combine(RecordingPath(), userName);

        }
    }
}
