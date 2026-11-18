using System;
using System.Collections.Generic;
using System.Text;
using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Interface;

public interface ITokenService
{
    string CreateAccessToken(AppUser user);
}
