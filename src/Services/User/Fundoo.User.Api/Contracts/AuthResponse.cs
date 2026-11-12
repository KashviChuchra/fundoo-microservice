namespace Fundoo.User.Api.Contracts
{
	public class AuthResponse
	{
		public string Message { get; set; } = string.Empty;

		public string? Token { get; set; }

		public Guid? ChallengeId { get; set; }
	}
}