namespace InvoiceApp.Domain;

/// <summary>A successful approval action, in the order it was recorded.</summary>
public sealed record ApprovalRecord(string Username, ApprovalAuthority Authority, DateTime ApprovedAt);
