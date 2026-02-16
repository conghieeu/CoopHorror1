// Stub types for Discord Game SDK (not installed at the expected path)
// These provide empty implementations so DiscordController.cs can compile

using System;

namespace Discord
{
    public enum Result
    {
        Ok = 0,
        ServiceUnavailable = 1,
        InvalidVersion = 2,
        LockFailed = 3,
        InternalError = 4,
        InvalidPayload = 5,
        InvalidCommand = 6,
        InvalidPermissions = 7,
        NotFetched = 8,
        NotFound = 9,
        Conflict = 10,
        InvalidSecret = 11,
        InvalidJoinSecret = 12,
        NoEligibleActivity = 13,
        InvalidInvite = 14,
        NotAuthenticated = 15,
        InvalidAccessToken = 16,
        ApplicationMismatch = 17,
        InvalidDataUrl = 18,
        InvalidBase64 = 19,
        NotFiltered = 20,
        LobbyFull = 21,
        InvalidLobbySecret = 22,
        InvalidFilename = 23,
        InvalidFileSize = 24,
        InvalidEntitlement = 25,
        NotInstalled = 26,
        NotRunning = 27,
        InsufficientBuffer = 28,
        PurchaseCanceled = 29,
        InvalidGuild = 30,
        InvalidEvent = 31,
        InvalidChannel = 32,
        InvalidOrigin = 33,
        RateLimited = 34,
        OAuth2Error = 35,
        SelectChannelTimeout = 36,
        GetGuildTimeout = 37,
        SelectVoiceForceRequired = 38,
        CaptureShortcutAlreadyListening = 39,
        UnauthorizedForAchievement = 40,
        InvalidGiftCode = 41,
        PurchaseError = 42,
        TransactionAborted = 43
    }

    public struct Activity
    {
        public string Details;
        public string State;
        public ActivityAssets Assets;
        public ActivityParty Party;
    }

    public struct ActivityAssets
    {
        public string LargeImage;
        public string LargeText;
        public string SmallImage;
        public string SmallText;
    }

    public struct ActivityParty
    {
        public string Id;
        public PartySize Size;
    }

    public struct PartySize
    {
        public int CurrentSize;
        public int MaxSize;
    }

    public class ActivityManager
    {
        public void RegisterSteam(uint steamId) { }

        public void UpdateActivity(Activity activity, Action<Result> callback)
        {
            callback?.Invoke(Result.Ok);
        }

        public void ClearActivity(Action<Result> callback)
        {
            callback?.Invoke(Result.Ok);
        }
    }

    public class Discord : IDisposable
    {
        public Discord(long clientId, ulong flags) { }

        public void RunCallbacks() { }

        public ActivityManager GetActivityManager()
        {
            return new ActivityManager();
        }

        public void Dispose() { }
    }
}
