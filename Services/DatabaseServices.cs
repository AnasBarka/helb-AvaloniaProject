using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using OceanStock.Models;

namespace OceanStock.Services;

public class DatabaseServices
{
    private readonly IMongoCollection<ProductFish> _productFish;
    private readonly IMongoCollection<User> _users;

    public DatabaseServices()
    {
        const string connectionUri = "mongodb://Meeeee:IAmTheBest@185.157.245.38:443/";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);
        var database = client.GetDatabase("OceanStockDB");

        _productFish = database.GetCollection<ProductFish>("Fish");
        _users = database.GetCollection<User>("Users");
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _users.Find(x => x.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _users.Find(x => x.Email == email).FirstOrDefaultAsync();

        if (user == null)
            return null;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }

    public async Task CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }

    public async Task DeleteUserAsync(string id)
    {
        await _users.DeleteOneAsync(x => x.Id == id);
    }

    public async Task UpdateUserAsync(User user)
    {
        await _users.ReplaceOneAsync(x => x.Id == user.Id, user);
    }

    public async Task<List<ProductFish>> GetFishAsync()
    {
        return await _productFish.Find(_ => true).ToListAsync();
    }

    public async Task<ProductFish?> GetFishByCustomIdAsync(string customId)
    {
        return await _productFish.Find(x => x.Id == customId).FirstOrDefaultAsync();
    }

    public async Task CreateFishAsync(ProductFish fish)
    {
        await _productFish.InsertOneAsync(fish);
    }

    public async Task UpdateFishAsync(ProductFish fish)
    {
        await _productFish.ReplaceOneAsync(x => x.Id == fish.Id, fish);
    }

    public async Task DeleteFishAsync(string customId)
    {
        await _productFish.DeleteOneAsync(x => x.Id == customId);
    }
}
