// Author: Muhammad Qasim
// Date:25-10-2025
// Student ID: c3360527
// Description: This code defines a data model for audio recording metadata in a Maui application using SQLite.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace ECHO_PRINT.Models
{
    [Table("Item_Records")]
    public class Item_Records
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed] 
        public string FilePath { get; set; } = ""; // link to the recorded file
        [Indexed] 
        public string Title { get; set; } = ""; // user entered name
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? PhotoPath { get; set; } // path to the photo associated with the recording

        //favorties
        [Indexed] public bool IsFav { get; set; }

        //location
        public double? Latitude { get; set; } = double.NaN;
        public double? Longitude { get; set;} = double.NaN;
        public string? PlaceName { get; set; }

        //UI related
        [Ignore] public bool IsPlaying { get; set; } = false; // to show playing status in UI

    }
}
