using System;
using System.Collections.Generic;
using System.Text;

namespace Fundoo.User.Domain.Entities
{
    public class PasswordResetToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? UsedAtUtc { get; set; }
    }
}