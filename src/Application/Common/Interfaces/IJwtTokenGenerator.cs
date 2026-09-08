using System;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
