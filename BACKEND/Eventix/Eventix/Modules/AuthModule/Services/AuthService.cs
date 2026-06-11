using Eventix.Common.Constants;
using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Common.Settings;
using Eventix.Data;
using Eventix.Entities;
using Eventix.Infrastructure.Email;
using Eventix.Modules.Auth.DTOs;
using Eventix.Modules.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Eventix.Modules.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;

        public AuthService(AppDbContext context, IOptions<JwtSettings> jwtSettings, IEmailService emailService)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new ApiException(SystemError.EMAIL_PASSWORD_INCORRECT);
            }

            if (!user.EmailVerified)
            {
                // In some systems, we might allow login but restrict actions, 
                // but usually, it's better to force verification.
                // For this project, let's assume they must verify first.
                throw new ApiException(SystemError.INVALID_OR_EXPIRED_RESET_TOKEN); // Or a specific "Email not verified" error if added
            }

            return await GenerateAuthResponse(user);
        }

        public async Task<AuthUserDto> RegisterAsync(RegisterRequest request)
        {
            var userExist = await _context.Users.AnyAsync(u => u.Email == request.Email.Trim().ToLower());
            if (userExist)
            {
                throw new ApiException(SystemError.EMAIL_ALREADY_EXISTS);
            }

            if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw new ApiException(SystemError.PASSWORD_REQUIRED);
            }

            if (request.Password.Length < 6)
            {
                throw new ApiException(SystemError.PASSWORD_TOO_SHORT);
            }

            var phoneExist = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber.Trim());
            if (phoneExist)
            {
                throw new ApiException(SystemError.PHONE_ALREADY_EXISTS);
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new ApiException(SystemError.PASSWORD_NOT_MATCH);
            }

            var roleName = SystemConstants.RoleConstants.CUSTOMER;
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == SystemConstants.RoleConstants.CUSTOMER);
            }


            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FullName = request.FullName ?? "",
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Status = SystemConstants.StatusAccount.INACTIVE,
                CreatedAt = DateTime.UtcNow,
                EmailVerified = false
            };

            if (role != null)
            {
                user.Roles.Add(role);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SendOtpAsync(user.Id, user.Email, SystemConstants.PurposeEmail.REGISTER);

            return new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                EmailVerified = user.EmailVerified,
                AvatarUrl = user.AvatarUrl,
            };
        }

        public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var otp = await _context.EmailOtps
                .Include(o => o.User)
                .ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(o => o.Email == request.Email.Trim().ToLower() && o.OtpCode == request.OtpCode && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

            if (otp == null)
            {
                throw new ApiException(SystemError.INVALID_OTP);
            }

            otp.User.Status = SystemConstants.StatusAccount.ACTIVE;
            otp.IsUsed = true;
            otp.User.EmailVerified = true;
            otp.User.EmailVerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GenerateAuthResponse(otp.User);
        }

        public async Task ResendOtpAsync(ResendOtpRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower());
            if (user == null)
            {
                throw new ApiException(SystemError.USER_NOT_FOUND);
            }

            if (user.EmailVerified)
            {
                throw new ApiException(SystemError.EMAIL_ALREADY_VERIFIED);
            }

            // Optional: Check if last OTP was sent too recently
            var lastOtp = await _context.EmailOtps
                .Where(o => o.UserId == user.Id && o.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                .AnyAsync();

            if (lastOtp)
            {
                throw new ApiException(SystemError.OTP_RESEND_TOO_SOON);
            }

            await SendOtpAsync(user.Id, user.Email, SystemConstants.PurposeEmail.REGISTER);
        }

        private async Task SendOtpAsync(Guid userId, string email, string purpose)
        {
            var otpCode = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();

            var otp = new EmailOtp
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = email,
                OtpCode = otpCode,
                Purpose = purpose,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ResetTokenExpireMinutes),
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailOtps.Add(otp);
            await _context.SaveChangesAsync();

            // Send actual email
            await _emailService.SendOtpEmailAsync(email, otpCode);
        }

        private async Task<AuthResponse> GenerateAuthResponse(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var accessTokenExpiresAt = DateTime.UtcNow
                .AddMinutes(_jwtSettings.AccessTokenExpireMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiresAt = DateTime.UtcNow
                .AddDays(_jwtSettings.RefreshTokenExpireDays);

            var userRefreshToken = new UserRefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = refreshTokenExpiresAt,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRefreshTokens.Add(userRefreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Token = tokenString,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                User = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    EmailVerified = user.EmailVerified,
                    AvatarUrl = user.AvatarUrl,
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var refreshTokenValue = request.RefreshToken?.Trim();

            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                throw new ApiException(SystemError.INVALID_REFRESH_TOKEN);
            }

            var refreshToken = await _context.UserRefreshTokens
                .Include(x => x.User)
                .ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(x => x.Token == refreshTokenValue);

            if (refreshToken == null)
            {
                throw new ApiException(SystemError.INVALID_REFRESH_TOKEN);
            }

            if (refreshToken.IsRevoked)
            {
                throw new ApiException(SystemError.INVALID_REFRESH_TOKEN);
            }

            if (refreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new ApiException(SystemError.REFRESH_TOKEN_EXPIRED);
            }

            if (!refreshToken.User.EmailVerified ||
                refreshToken.User.Status != SystemConstants.StatusAccount.ACTIVE)
            {
                throw new ApiException(SystemError.ACCOUNT_NOT_ACTIVE);
            }

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GenerateAuthResponse(refreshToken.User);
        }

        public async Task LogoutAsync(LogoutRequest request)
        {
            var refreshTokenValue = request.RefreshToken?.Trim();

            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                return;
            }

            var refreshToken = await _context.UserRefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshTokenValue);

            if (refreshToken == null)
            {
                return;
            }

            if (!refreshToken.IsRevoked)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }
        private static string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        public async Task ForgotPasswordAsync(ForgetPasswordRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null)
            {
                throw new ApiException(SystemError.USER_NOT_FOUND);
            }

            await SendOtpAsync(user.Id, user.Email, SystemConstants.PurposeEmail.RESET_PASSWORD);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ApiException(SystemError.PASSWORD_NOT_MATCH);
            }

            var email = request.Email.Trim().ToLower();

            var otp = await _context.EmailOtps
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Email == email &&
                    x.OtpCode == request.OtpCode &&
                    x.Purpose == SystemConstants.PurposeEmail.RESET_PASSWORD &&
                    !x.IsUsed &&
                    x.ExpiresAt > DateTime.UtcNow);

            if (otp == null)
            {
                throw new ApiException(SystemError.INVALID_OR_EXPIRED_RESET_TOKEN);
            }

            otp.IsUsed = true;

            otp.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await _context.SaveChangesAsync();
        }
    }
}
