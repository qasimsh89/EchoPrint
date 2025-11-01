using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Plugin.Maui.Audio;
using ECHO_PRINT.Models;
using ECHO_PRINT.Services;
using IOPath = System.IO.Path;

namespace ECHO_PRINT.Resources.pages;

public partial class FavRecordings : ContentPage
{
    private readonly EchoPrint_CRUD _crud;

    // assigning variables
    private IAudioPlayer? audioPlayer;
    private Item_Records? Item;
    private Button? PlayBtn;
    private ProgressBar? currentBar;
    private bool progressRunning;
    public FavRecordings(EchoPrint_CRUD crud) // constructor
    {
        InitializeComponent();
        _crud = crud;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _crud.getFav(); // list-returning overload
        favList.ItemsSource = items;
    }

    protected async void OnRefresh(object sender, EventArgs e)
    {
        await LoadAsync();
        refresh.IsRefreshing = false;
    }

    // Play button function

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
            // stop audio
            if (Item == item && (audioPlayer?.IsPlaying == true))
            {
                audioPlayer!.Stop();
                btn.Text = "Play";
                Item = null;
                PlayBtn = null;
                StopProgress();
                return;
            }

            // stop previous if another audio is playing
            if (audioPlayer != null && PlayBtn != btn)
            {
                if (audioPlayer.IsPlaying)
                    audioPlayer.Stop();

                if (PlayBtn != null)
                    PlayBtn.Text = "Play";
                StopProgress();
            }

            // validate file
            if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
            {
                await DisplayAlert("Play", "File not found.", "OK");
                return;
            }

            // start playback
            audioPlayer = AudioManager.Current.CreatePlayer(item.FilePath);
            audioPlayer.Play();

            btn.Text = "Stop";
            Item = item;
            PlayBtn = btn;
            // Setup and start progress bar
            currentBar = btn.Parent.FindByName<ProgressBar>("progressBar");
            StartProgress(); // progress bar start

            audioPlayer.PlaybackEnded += (s, args2) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (PlayBtn == btn)
                    {
                        btn.Text = "Play";
                        StopProgress(); //progress bar stop
                        Item = null;
                        PlayBtn = null;
                    }
                });
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Play", $"Failed to play the record. {ex.Message}", "OK");
        }
    }

    //progress bar function
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

    // fav button 
    private async void OnFav(object sender, EventArgs e)
    {
        try
        {
            var item = (sender as BindableObject)?.BindingContext as Item_Records;
            if (item == null) return;
             
            bool newVal = !item.IsFav;
            item.IsFav = false; // remove from fav
            await _crud.setFav(item.Id, newVal); // update db

            await _crud.getFav(); // getting updated db

            await LoadAsync(); // refreshing the list
        }
        catch (Exception ex)
        {
            await DisplayAlert("Favorites", $"Failed to update favorite: {ex.Message}", "OK");
        }
    }
    
    // delete function
    private async void OnDelete(object sender, EventArgs e)
    {
        try
        {
            var item = (sender as Button)?.CommandParameter as Item_Records;
            if (item == null)
            {
                await DisplayAlert("Delete", "No item selected.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Delete", $"Delete \"{item.Title}\"?", "Delete", "Cancel");
            if (!confirm) return;

            if (audioPlayer?.IsPlaying == true && Item?.Id == item.Id)
            {
                audioPlayer.Stop();
                if (PlayBtn != null) PlayBtn.Text = "Play";
                Item = null; 
                PlayBtn = null;
                
            }

            await _crud.Delete(item.Id, deleteFile: true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Delete", $"Failed to delete the record: {ex.Message}", "OK");
        }
    }


    // opening maps function
    private async void OpenMap(object sender, EventArgs e)
    {
        try
        {
            var item = (sender as Button)?.CommandParameter as Item_Records;
            if (item == null) return;

            var lat = item.Latitude;
            var lng = item.Longitude;

            if (lat.HasValue && lng.HasValue && !double.IsNaN(lat.Value) && !double.IsNaN(lng.Value))
            {
                await Launcher.OpenAsync($"https://maps.google.com/?q={lat.Value},{lng.Value}");
            }
            else
            {
                await DisplayAlert("Map", "Location was not recorded for this item.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Map", ex.Message, "OK");
        }
    }


    // media picker
    private async void MediaPcker(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn || btn.CommandParameter is not Item_Records item)
            {
                await DisplayAlert("Error", "No item selected.", "OK");
                return;
            }

            // permissions
            var cam = await Permissions.CheckStatusAsync<Permissions.Camera>();
            var write = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (cam != PermissionStatus.Granted) cam = await Permissions.RequestAsync<Permissions.Camera>();
            if (write != PermissionStatus.Granted) write = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (cam != PermissionStatus.Granted || write != PermissionStatus.Granted)
            {
                await DisplayAlert("Permissions", "Camera/Storage permissions are required.", "OK");
                return;
            }

            // action
            string action = await DisplayActionSheet("Attach Photo", "Cancel", null, "Take Photo", "Pick Photo");
            FileResult? result = null;

            if (action == "Take Photo")
                result = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions { Title = $"{item.Title}_photo.jpg" });
            else if (action == "Pick Photo")
                result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions { Title = "Select Photo" });
            else
                return;

            if (result == null) return;

            var photosDir = IOPath.Combine(FileSystem.AppDataDirectory, "Photos");
            Directory.CreateDirectory(photosDir);

            var safeName = $"{item.Id}_{IOPath.GetFileName(result.FileName)}";
            var destPath = IOPath.Combine(photosDir, safeName);

            using (var src = await result.OpenReadAsync())
            using (var dest = File.OpenWrite(destPath))
                await src.CopyToAsync(dest);

            await _crud.PhotoUpdate(item.Id, destPath);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Photo", ex.Message, "OK");
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
        // if photo is empty
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
}
