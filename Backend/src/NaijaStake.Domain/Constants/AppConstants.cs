namespace NaijaStake.Domain.Constants;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class AppConstants
{
    public const string ApplicationName = "StakeIt";
    
    public static class Cache
    {
        public const string PopularBetsKey = "popular_bets";
        public const string BetDetailsKeyPrefix = "bet_details:";
        public const string UserWalletKeyPrefix = "wallet:";
        
        // Cache durations
        public const int PopularBetsCacheDurationSeconds = 300; // 5 minutes
        public const int BetDetailsCacheDurationSeconds = 600;   // 10 minutes
        public const int WalletCacheDurationSeconds = 60;        // 1 minute
    }

    public static class Redis
    {
        public const string StakeLockKeyPrefix = "stake_lock:";
        public const string BetLockKeyPrefix = "bet_lock:";
        public const string ResolutionLockKeyPrefix = "resolution_lock:";
        
        // Lock durations to prevent race conditions
        public const int StakeLockDurationSeconds = 5;
        public const int BetLockDurationSeconds = 5;
        public const int ResolutionLockDurationSeconds = 30;
    }

    public static class Validation
    {
        public const int MinimumPasswordLength = 8;
        public const int MaximumPasswordLength = 255;
        public const decimal MinimumStakeAmount = 100;
        public const decimal MaximumStakeAmount = 10_000_000;
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaximumPageSize = 100;
    }
}
