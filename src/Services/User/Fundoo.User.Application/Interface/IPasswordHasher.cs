using System;
using System.Collections.Generic;
using System.Text;

namespace Fundoo.User.Application.Interface;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
