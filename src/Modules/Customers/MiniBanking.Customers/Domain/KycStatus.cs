namespace MiniBanking.Customers.Domain;

/// <summary>
/// Know Your Customer: has the bank verified who this person is?
/// Every customer starts as Pending; verification comes in a later step.
/// </summary>
internal enum KycStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}
