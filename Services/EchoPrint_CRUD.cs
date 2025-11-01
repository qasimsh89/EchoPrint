// Author: Muhammad Qasim
// Date:25-10-2025
// Student ID: c3360527
// Description: This code defines a CRUD service for a Maui application that manages audio recording metadata using SQLite.
// It provides methods to add, retrieve, update, and delete records in the database, including handling favorite status, location updates, and photo paths.
// It also includes functionality to delete associated audio files when a record is deleted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using ECHO_PRINT.Models;
using System.IO;
using Microsoft.Maui.Storage;
namespace ECHO_PRINT.Services
{
    public class EchoPrint_CRUD
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbConeection;

        public EchoPrint_CRUD() // building connection between file system and db
        {
            _dbConeection = Path.Combine(FileSystem.AppDataDirectory, "EchoPrint.db");

        }

        private async Task TaskAsync() // initalizing 
        {
            if (_db != null) return;
            _db = new SQLiteAsyncConnection(_dbConeection);
            await _db.CreateTableAsync<Item_Records>();
        }
        //adding in the db
        public async Task<int> Add(Item_Records item)
        {
            await TaskAsync();
            return await _db!.InsertAsync(item);
        }
        //getting all the list from db
        public async Task<List<Item_Records>> getAll()
        {
            await TaskAsync();
            return await _db!.Table<Item_Records>().OrderByDescending(x => x.CreatedOn).ToListAsync();
        }
        //setting fav and updating fav
        public async Task<int> setFav(int Id, bool isfav)
        {
            await TaskAsync();
            var item = await _db!.FindAsync<Item_Records>(Id);
            if (item == null) return 0;
            item.IsFav = isfav;
            return await _db.UpdateAsync(item);
        }
        //get fav list
        public async Task<List<Item_Records>> getFav()
        {
            await TaskAsync();
            return await _db!.Table<Item_Records>().Where(x => x.IsFav == true).OrderByDescending(x => x.CreatedOn).ToListAsync();
        }

        //updating location
        public async Task<int> LocationUpdate(int Id, double? lat, double? lng, string? place)
        {
            await TaskAsync();
            var Item = await _db.FindAsync<Item_Records>(Id);
            if (Item == null) return 0;
            Item.Latitude = lat;
            Item.Longitude = lng;
            Item.PlaceName = place;
            return await _db.UpdateAsync(Item);
        }

        //photo update
        public async Task<int> PhotoUpdate(int Id, string? photoPath)
        {
            await TaskAsync();
            var Item = await _db.FindAsync<Item_Records>(Id);
            if (Item == null) return 0;
            Item.PhotoPath = photoPath;
            return await _db.UpdateAsync(Item);
        }

        //delete function with option to delete the associated file
        public async Task<int> Delete(int Id, bool deleteFile = true)
        {


            await TaskAsync();

                var data = await _db!.FindAsync<Item_Records>(Id);
                if (data == null) 
                    return 0;
                if (deleteFile)
                {
                    try
                    {
                        var path = data.FilePath;
                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deleting file: {ex.Message}");
                    }
                }
               
                return await _db!.DeleteAsync(data);
              
         
        }
        
    }
}
