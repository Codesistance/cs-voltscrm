namespace VoltsCRM.Domain.Enums;

/// <summary>
/// The kind of user a login represents. Drives which UI area the user lands in and which
/// profile table holds their details. Authentication is unified (one <c>AspNetUsers</c> store);
/// this discriminator + a 1:1 profile table per type provides the separation.
/// </summary>
public enum UserType { Customer, Agent, Administration }
