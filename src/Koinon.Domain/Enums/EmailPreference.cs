namespace Koinon.Domain.Enums;

/// <summary>
/// Email communication preferences for a person.
/// </summary>
public enum EmailPreference
{
    /// <summary>
    /// Person has opted in to all email communications.
    /// </summary>
    EmailAllowed = 0,

    /// <summary>
    /// Person wants to receive only essential emails, no mass communications.
    /// </summary>
    NoMassEmails = 1,

    /// <summary>
    /// Person has opted out of all email communications.
    /// </summary>
    DoNotEmail = 2
}
