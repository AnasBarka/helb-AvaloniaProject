using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using OceanStock.Models;

namespace OceanStock.Services;

public class DatabaseServices
{
    // ✅ Collection Fish
    private readonly IMongoCollection<ProductFish> _productFish;

    // ✅ Collection Users
    private readonly IMongoCollection<User> _users;

    public DatabaseServices()
    {
        const string connectionUri =
            "mongodb://Meeeee:IAmTheBest@185.157.245.38:443/";

        var settings = MongoClientSettings.FromConnectionString(connectionUri);

        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);

        var database = client.GetDatabase("OceanStockDB");

        // Liaison collections
        _productFish = database.GetCollection<ProductFish>("Fish");
        _users       = database.GetCollection<User>("Users");

        Console.WriteLine(" MongoDB connecté -> Fish + Users OK");
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _users
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync();
    }
    
    public async Task<User?> LoginAsync(string email, string password)
    {
        var user = await _users
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (user == null)
            return null;

        // ✅ Vérifier le mot de passe
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        return isValid ? user : null;
    }
    
    public async Task<List<User>> GetUsersAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }
    
    //CRUD USERS + CREATION DE COMPTE
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
        await _users.ReplaceOneAsync(
            x => x.Id == user.Id,
            user
        );
    }

}