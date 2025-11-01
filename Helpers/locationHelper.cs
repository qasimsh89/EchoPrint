// Author: Muhammad Qasim
// Date:26-10-2025
// Student ID: c3360527
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECHO_PRINT.Helpers
{
    internal class locationHelper
    {


        //getting location cordinates and reverse location 
        internal static async Task<(double lat, double? lang, string? place)> GetLocationAsync()
        {
            try
            {
                // checking last known location
                var location = await Geolocation.Default.GetLastKnownLocationAsync().ConfigureAwait(false);

                // if last know location is available then quickly save the location for (its an optimze solution)
                if (location is null)
                {
                    var quickReq = new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(3));
                    using var quickCts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                    location = await Geolocation.Default.GetLocationAsync(quickReq, quickCts.Token).ConfigureAwait(false);
                }

                // if location null then save nothing
                if (location is null)
                    return (double.NaN, null, null);

                // reverse geo decoding
                string? placeName = null;
                try
                {
                    var placemarks = await Geocoding.Default.GetPlacemarksAsync(location).ConfigureAwait(false);
                    var pm = placemarks?.FirstOrDefault();

                    string Join(string sep, params string?[] parts) =>
                        string.Join(sep, parts.Where(s => !string.IsNullOrWhiteSpace(s)));


                    string Street = Join(" ", pm?.SubThoroughfare, pm?.Thoroughfare); // st name

                    string suburb = pm?.Locality ?? pm?.SubLocality; // suburb

                    string state = pm?.AdminArea; // area


                    //formating the address
                    placeName = Join(", ", Street, suburb, state);
                    if (string.IsNullOrWhiteSpace(placeName))
                        placeName = pm?.CountryName ?? "Unknown Place";
                }
                catch
                {
                    // if reverse geo cordinate fails then just add cordinates instead of breaking
                }

                return (location.Latitude, location.Longitude, placeName);
            }
            catch
            {
                return (double.NaN, null, null);
            }



        }
    }
}
