using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Extensions;

public static class ClaimsPrincipleExtensions
{
    public static async Task<AppUser> GetUserByEmailAsync(this UserManager<AppUser> userManager,
        ClaimsPrincipal user)
    {
        var userToReturn = await userManager.Users.FirstOrDefaultAsync(u => u.Email == user.GetEmail())
            ?? throw new UnauthorizedAccessException("User not found.");

        return userToReturn;
    }    
    
    public static async Task<AppUser> GetUserWithAddressByEmailAsync(this UserManager<AppUser> userManager,
        ClaimsPrincipal user)
    {
        var userToReturn = await userManager.Users
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Email == user.GetEmail())
                ?? throw new UnauthorizedAccessException("User not found.");

        return userToReturn;
    }

    public static string GetEmail (this ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("Email claim not found.");

        return email;
    }
}
    