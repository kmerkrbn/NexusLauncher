using CmlLib.Core.Auth;

namespace MClauncher.Services
{
    public interface ISessionService
    {
        MSession? GetSession();
        void SetSession(MSession session);
        void ClearSession();
        bool IsAuthenticated { get; }
        bool IsPremium { get; }
    }
}
