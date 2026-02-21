using CmlLib.Core.Auth;
using System.Text.RegularExpressions;

namespace MClauncher.Services
{
    public class SessionService : ISessionService
    {
        private MSession? _session;

        // UUID pattern for Microsoft accounts
        private static readonly Regex UuidPattern = new Regex(
            @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        public MSession? GetSession() => _session;

        public void SetSession(MSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public void ClearSession()
        {
            _session = null;
        }

        public bool IsAuthenticated => _session != null;

        public bool IsPremium
        {
            get
            {
                if (!IsAuthenticated || _session == null)
                    return false;

                // Check if AccessToken is valid and not offline markers
                if (string.IsNullOrEmpty(_session.AccessToken) || _session.AccessToken == "0")
                    return false;

                // Microsoft accounts have UUID format username
                // This is more reliable than checking AccessToken length
                if (!string.IsNullOrEmpty(_session.Username) && 
                    UuidPattern.IsMatch(_session.Username))
                {
                    return true;
                }

                return false;
            }
        }
    }
}

