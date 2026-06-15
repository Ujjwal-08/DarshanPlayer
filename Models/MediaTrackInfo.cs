namespace DarshanPlayer.Models
{
    /// <summary>Lightweight track info — decouples UI from LibVLC types.</summary>
    public record MediaTrackInfo(int Id, string Name)
    {
        /// <summary>True when this is the currently active track. Set by ViewModel before exposing to UI.</summary>
        public bool IsActive { get; init; } = false;
        public override string ToString() => Name;
    }
}
