using Domain.Enums;

namespace Application.Common.Exceptions;

public sealed class FeatureUnavailableException : Exception
{
    public Feature Feature { get; }
    public FeatureUnavailableReason Reason { get; }

    public FeatureUnavailableException(Feature feature, FeatureUnavailableReason reason)
        : base($"Feature '{feature}' is unavailable ({reason}).")
    {
        Feature = feature;
        Reason = reason;
    }
}
