using Suparco.Api.Data;
using Suparco.Api.Models;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace Suparco.Api.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _db;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public UserModel? ValidateUser(string username, string password)
        {
            var user = _db.Users.FirstOrDefault(u =>
                u.Username.ToLower() == username.ToLower());

            if (user == null)
                return null;

            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword(user.Username, user.Password, password);

            if (result == PasswordVerificationResult.Success)
                return user;

            return null;
        }


        public void SeedUsersIfEmpty()
        {
            if (!_db.Users.Any())
            {
                var hasher = new PasswordHasher<string>();

                var adminHashed = hasher.HashPassword("admin", "admin123");
                var operatorHashed = hasher.HashPassword("operator", "operator123");

                _db.Users.Add(new UserModel
                {
                    Username = "admin",
                    Password = adminHashed,
                    Role = "Admin"
                });

                _db.Users.Add(new UserModel
                {
                Username = "operator",
                Password = operatorHashed,
                Role = "Operator"
                 });

        _db.SaveChanges();
            }
        }
    }
}
