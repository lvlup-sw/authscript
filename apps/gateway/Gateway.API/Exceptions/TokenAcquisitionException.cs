// =============================================================================
// <copyright file="TokenAcquisitionException.cs" company="Levelup Software">
// Copyright (c) Levelup Software. All rights reserved.
// </copyright>
// =============================================================================

namespace Gateway.API.Exceptions;

/// <summary>
/// Exception thrown when token acquisition fails due to transient errors (network, auth server unavailable).
/// Configuration errors should throw <see cref="InvalidOperationException"/> instead.
/// </summary>
public sealed class TokenAcquisitionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenAcquisitionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TokenAcquisitionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenAcquisitionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TokenAcquisitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
