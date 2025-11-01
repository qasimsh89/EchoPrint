// Author: Muhammad Qasim
// Date:25-10-2025
// Student ID: c3360527
// Description: This code defines a RecordPage class for a Maui application that allows users to record audio, save it with a custom name, and manage permissions.
// It uses the Plugin.Maui.Audio library for audio recording and playback, and it interacts with a CRUD service to store metadata about the recordings.
// The page includes functionality to request necessary permissions, handle recording start/stop, and prompt the user for saving the recording with a specified name.
// It also includes error handling and user feedback through alerts and vibrations.
// added function ask to location permission upon clicking the record button then to capture location cordinates and then reverse cordinated then update the database



using Plugin.Maui.Audio;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;
using CommunityToolkit.Maui;
using ECHO_PRINT.Helpers;
using ECHO_PRINT.Services;
using ECHO_PRINT.Models;
using System.IO;
using System.Threading;
using Microsoft.Maui.Devices.Sensors;





namespace ECHO_PRINT.Resources.pages;

public partial class RecordPage : ContentPage
{
    readonly IAudioManager audio_Manager;
    readonly IAudioRecorder audio_Recorder;
    private readonly EchoPrint_CRUD _crud;

    public RecordPage(IAudioManager audioManager, EchoPrint_CRUD cRUD)
    {
        InitializeComponent();
        _crud = cRUD;
        audio_Manager = audioManager;
        audio_Recorder = audioManager.CreateRecorder();
    }

    // main recording function starts here
    private async void recorder(object sender, EventArgs e)
    {

        PermissionStatus status1 = await Permissions.RequestAsync<Permissions.StorageWrite>();
        PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted) // waiting for the user to allow access to microphone otherwise displaying prompt
        {
            await DisplayAlert("Alert title", "required microphone access to use the app.", "OK");
            return;
        }
        if (!audio_Recorder.IsRecording)
        {
            RecordButton.Text = "tap to Stop"; // change label based on the user response
          
            await audio_Recorder.StartAsync();
            
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); // vibration when recording starts. 
            StartPulse(); // starting UI animation
        }
        else
        {
            var recordedAudio = await audio_Recorder.StopAsync();
            RecordButton.Text = "tap to Start"; // change label again when recording stops
        
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); // viration when recording stops
            await TaskHandler(recordedAudio);
         
            var player = AudioManager.Current.CreatePlayer(recordedAudio.GetAudioStream());
            player.Play();
            StopPulse(); // stopping UI animation

        }

    }

    // function for saving file 
    private async Task TaskHandler(IAudioSource tempPath)
    {
        while (true)
        {
            // Ask Save / Discard first
            bool save = await DisplayAlert(
                "Recording Finished",
                "Would you like to save the recording?",
                "Save", "Discard");

            if (!save)
            {
                // Discard: nothing persisted yet (tempPath is an IAudioSource, not a file)
                return;
            }

            // Ask for a name
            string action = await DisplayPromptAsync(
                "Save Recording",
                "Enter a name for your recording:",
                "Save", "Cancel",
                "My Recording");

            // User cancelled
            if (action == null)
                return;

            action = action.Trim();

            // Empty name → warn and retry
            if (string.IsNullOrWhiteSpace(action))
            {
                await DisplayAlert("Invalid Name", "Recording name cannot be empty.", "OK");
                continue;
            }

            // buiding path to save the file
            string Path = Path_helper.RecordingPath();
            string fileName = $"{action}.wav";
            string finalPath = System.IO.Path.Combine(Path, fileName);

            // Duplicate name checker
            if (File.Exists(finalPath))
            {
                await DisplayAlert("File name already exists", "Please enter a different name.", "OK");
                continue;
            }
            // Saving the file to the location.
            System.IO.Directory.CreateDirectory(Path);
            using (var input = tempPath.GetAudioStream())
            using (var output = System.IO.File.Create(finalPath))
            {
                await input.CopyToAsync(output);
            }

            // Adding entry to the database
            var newItem = new Item_Records
            {
                Title = action,
                FilePath = finalPath,
                CreatedOn = DateTime.Now,
                IsFav = false,
            };

            await _crud.Add(newItem);
            _ = Task.Run(async () => //running this process in background for better UI experience and optimication.
            {
                try
                {
                  
                     var (lat, lng, place) = await locationHelper.GetLocationAsync();

                     await _crud.LocationUpdate(newItem.Id, lat, lng, place);

                   
                }
                catch
                {

                }
             
            });

            await DisplayAlert("Successfully Saved",
              $"Saved as: {System.IO.Path.GetFileName(finalPath)}",
              "OK");
            return;
        }
    }


    //UI animation
    CancellationTokenSource? _pulse;
    void StartPulse()
    {
        Pulse.IsVisible = true;
        _pulse?.Cancel();
        _pulse = new CancellationTokenSource();
        _ = PulseLoop(_pulse);
    }
    async Task PulseLoop(CancellationTokenSource token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Pulse.ScaleTo(1.12, 380, Easing.CubicOut);
                await Pulse.FadeTo(0.35, 300);
                await Pulse.ScaleTo(1.00, 380, Easing.CubicIn);
                await Pulse.FadeTo(0.15, 380);
            }
        }
        catch
        {
            //ignore
        }
    }
    void StopPulse()
    {
        _pulse?.Cancel();
        Pulse.IsVisible = false;
        Pulse.Opacity = 0.15;
        Pulse.Scale = 1.0;
    }

}