// Author: Muhammad Qasim
// Date:25-10-2025
// Student ID: c3360527
// Description: This code defines a RecordList class for a Maui application that displays a list of audio recordings.
// It allows users to play, delete, and associate photos with recordings, as well as view photos in full screen.
// The class interacts with a CRUD service to manage recording metadata and uses the Plugin.Maui.Audio library for audio playback.
// It also handles permissions for camera and storage access when associating photos with recordings.

using ECHO_PRINT.Models;
using ECHO_PRINT.Services;
using ECHO_PRINT.Helpers;
using Microsoft.Maui.Controls.Shapes;
using Plugin.Maui.Audio;
using System.IO;
using Microsoft.Maui.Media;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel.DataAnnotations;




namespace ECHO_PRINT.Resources.pages
{
	public partial class RecordList : ContentPage
	{
		private readonly EchoPrint_CRUD _crud;
		private IAudioPlayer audioPlayer;
        private Item_Records Item_Records;
        private  Button btnPlay;
        private ProgressBar? currentBar;
        private bool progressRunning;
        private RecordPage recordPage; 

        public RecordList(EchoPrint_CRUD cRUD) //constructor
        {
			InitializeComponent();
			_crud = cRUD;
		}

        // loading data each time navigate to page
        protected override async void OnAppearing() 
        {
            base.OnAppearing();
            await LoadAsync();
        }

        // loading data
        private async Task LoadAsync()
        {
            var items = await _crud.getAll();
            audioList.ItemsSource = items;
        }

        // refresh data each time user refresh the page
        protected async void OnRefresh(object sender, EventArgs e)
        {
            await LoadAsync();
            refresh.IsRefreshing = false;
        }

        //favorite function
        private async void OnFav(object sender, EventArgs e)
        {
            try
            {
                var item = (sender as BindableObject)?.BindingContext as Item_Records;
                if (item == null) return;
                bool newFavStatus = !item.IsFav;
                if (sender is Button btn)
                {
                    await btn.ScaleTo(0.8, 100, Easing.CubicOut);
                    await btn.ScaleTo(1.0, 100, Easing.CubicIn);
                }
                await _crud.setFav(item.Id, newFavStatus);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Favorite Error", ex.Message, "OK");
            }
        }

        // play function
        private async void OnPlay(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.CommandParameter as Item_Records;

            if (btn == null || item == null)
            {
                await DisplayAlert("Play", "No item selected.", "OK");
                return;
            }

            try
            {
                // If the same item is playing, stop it
                if (Item_Records == item && (audioPlayer?.IsPlaying == true))
                {
                    audioPlayer!.Stop();
                    btn.Text = "Play";
                    Item_Records = null;
                    btnPlay = null;
                    StopProgress();  //progress bar stop
                    return;
                }

                // If another item is playing, stop it and reset its button text
                if (audioPlayer != null && btnPlay != btn)
                {
                    if (audioPlayer.IsPlaying)
                        audioPlayer.Stop();

                    if (btnPlay != null) // first run btnPlay is null
                        btnPlay.Text = "Play";

                    StopProgress(); //progress bar stop
                }

                // Validate the file path
                if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
                {
                    await DisplayAlert("Play", "File not found.", "OK");
                    return;
                }

                // Start playback
                audioPlayer = AudioManager.Current.CreatePlayer(item.FilePath);
                audioPlayer.Play();

                // Update button/UI state
                btn.Text = "Stop";
                Item_Records = item;
                btnPlay = btn;

                // Setup and start progress bar
                currentBar = btn.Parent.FindByName<ProgressBar>("progressBar");
                StartProgress(); // progress bar start

                // Handle playback completion
                audioPlayer.PlaybackEnded += (s, args2) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Reset only if this is still the current button
                        if (btnPlay == btn)
                        {
                            btn.Text = "Play";
                            StopProgress(); //progress bar stop
                            Item_Records = null;
                            btnPlay = null;
                        }
                    });
                };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Play", $"Failed to play the record. {ex.Message}", "OK");
            }
        }
        private void StartProgress()
        {
            if (audioPlayer == null || currentBar == null)
                return;

            progressRunning = true;
            currentBar.IsVisible = true;
            currentBar.Progress = 0;

            Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                try
                {
                    if (!progressRunning || audioPlayer == null || currentBar == null)
                        return false;
                    var duration = audioPlayer.Duration;
                    var position = audioPlayer.CurrentPosition;
                    if (duration <= 0)
                    {
                        currentBar.Progress = 0;
                    }
                    else
                    {
                        double ratio = Math.Clamp((double)position / duration, 0, 1);
                        currentBar.Progress = ratio;
                        currentBar.ProgressColor = Colors.DarkCyan;
                    }
                    return audioPlayer.IsPlaying;
                }
                catch
                {
                    progressRunning = false;
                    return false;
                }
            });
        }

        // hide progress and reset bar
        private void StopProgress()
        {
            progressRunning = false;
            if (currentBar != null)
            {
                currentBar.IsVisible = false;
                currentBar.Progress = 0;
            }
        }



        //Media picker function
        private async void MediaPcker(object sender, EventArgs e)
        {
            // making sure an item is selected (defence code)
            if (sender is not Button btn || btn.CommandParameter is not Item_Records item)
            {
                await DisplayAlert("Error", "No item selected.", "OK");
                return;
            }

            // request camera permission
            var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();

            // request storage permission
            var storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (storageStatus != PermissionStatus.Granted)
                storageStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();

            // display alert if permissions are denied
            if (cameraStatus != PermissionStatus.Granted || storageStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("Permissions Denied",
                    "Unable to take or select photos without the required permissions.", "OK");
                return;
            }

            // display options to take or pick photo
            string action = await DisplayActionSheet(
                "Select Action", "Cancel", null, "Take Photo", "Pick Photo");
            FileResult photo = null;

            if (action == "Take Photo")
            {
                // take a new photo
                photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                {
                    Title = $"{item.Title}_photo.jpg"
                });
            }
            else if (action == "Pick Photo")
            {
                // pick an existing photo
                photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Photo"
                });
            }
            else
            {
    
                return;
            }

            // if photo is selected
            if (photo != null)
            {
                // prepare destination path
                string photoDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "Photos");
                System.IO.Directory.CreateDirectory(photoDir);

                string fileName = $"{item.Id}_{System.IO.Path.GetFileName(photo.FileName)}";
                string destPath = System.IO.Path.Combine(photoDir, fileName);

                // copy photo to app directory
                using (var src = await photo.OpenReadAsync())
                using (var dest = System.IO.File.OpenWrite(destPath))
                {
                    await src.CopyToAsync(dest);
                }

                // update the database record 
                await _crud.PhotoUpdate(item.Id, destPath);

           
                await LoadAsync(); //refresh the list
            }
        }

        //tap photo to view
        private async void OnPhotoTapped(object sender, EventArgs e)
        {
            // making sure an item is selected (defence code)
            if (sender is not Image img || img.BindingContext is not Item_Records item)
            {
                await DisplayAlert("Error", "No item selected.", "OK");
                return;
            }
            // if no photo is empty
            if (string.IsNullOrWhiteSpace(item.PhotoPath) || !File.Exists(item.PhotoPath))
            {
                await DisplayAlert("No Photo", "No photo associated with this record.", "OK");
                return;
            }
            // display the photo in a new page
            var photoPage = new ContentPage
            {
                Title = item.Title,
                Content = new Image
                {
                    Source = ImageSource.FromFile(item.PhotoPath),
                    Aspect = Aspect.AspectFit,
                    BackgroundColor = Colors.Black
                },
                BackgroundColor = Colors.Black
            };
            await Navigation.PushAsync(photoPage);
        }


        //open map button function
        private async void OpenMap(object sender, EventArgs e)
        {
            // getting item records and binding it to the button
            var item = (sender as BindableObject)?.BindingContext as Item_Records;
            if (item == null) return;

            // if the location is already collected then opening google maps
            if (item.Latitude.HasValue && !double.IsNaN(item.Latitude.Value) &&
                item.Longitude.HasValue && !double.IsNaN(item.Longitude.Value))
            {
                await Launcher.OpenAsync($"https://maps.google.com/?q={item.Latitude},{item.Longitude}");
                return;
            }

            // if the location is not been recorded then asking user to allow location permission
            bool response = await DisplayAlert("Map", "Location was not recorded.", "Allow location", "Dismiss");
            if (!response) return;

         
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>(); // checking permission status
            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                await DisplayAlert("Location", "We use your location to tag where each recording was made.", "OK");

            if (status != PermissionStatus.Granted) // requesting permission
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted) // if permission denied
            {
                bool openSettings = await DisplayAlert("Location Disabled", // prompt to open settings
                    "To tag this recording with a place, enable Location permission in Settings.",
                    "Open Settings", "Cancel");
                if (openSettings) AppInfo.ShowSettingsUI();
                return;
            }

            // getting location using helper class
            try
            {
                double lat;
                double lng;
                string? place;
                var (tLat, tLng, tPlace) = await locationHelper.GetLocationAsync(); // calling location helper

                lat = tLat;
                lng = (double)tLng;
                place = tPlace;

                // checking if location is valid
                if (double.IsNaN(lat) || double.IsNaN(lng))
                {
                    await DisplayAlert("Map", "Couldn't get current location. Try again outdoors with GPS on.", "OK");
                    return;
                }

                // updating location in db
                await _crud.LocationUpdate(item.Id, lat, lng, place);

                // refreshing list
                await LoadAsync();

                // opening google maps
                await Launcher.OpenAsync($"https://maps.google.com/?q={lat},{lng}");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Map Error", ex.Message, "OK");
            }
        }

        // delete function
        private async void OnDelete(object sender, EventArgs e)
        {
            try
            {
                // getting items
                var item = (sender as Button)?.CommandParameter as Item_Records;
                if (item == null)
                {
                    await DisplayAlert("Delete", "No item selected.", "OK");
                    return;
                }

                var confirm = await DisplayAlert("Delete", $"Delete \"{item.Title}\"?", "Delete", "Cancel");
                if (!confirm) return;

                // when deleting stop any playing audio
                if (audioPlayer?.IsPlaying == true) audioPlayer.Stop();

                // Delete from DB
                await _crud.Delete(item.Id, deleteFile: true);

                // Reload list
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Failed to delete the record", ex.Message, "OK");
            }

        }

    }

}
