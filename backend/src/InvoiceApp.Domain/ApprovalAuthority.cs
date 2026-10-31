namespace InvoiceApp.Domain;

/// <summary>Business approval authority, independent of API endpoint access roles.</summary>
public enum ApprovalAuthority
{
    Manager,
    Director
}
