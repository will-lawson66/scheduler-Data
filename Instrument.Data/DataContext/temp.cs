using System.Security.Cryptography;

public abstract class AuditableEntityBase
{
    public DateTimeOffset CreatedDate {get; set; }
    public string CreatedBy {get; set;} = string.Empty;    
    public DateTimeOffset ModifiedDate {get; set;}    
    public string? ModifiedBy {get; set;}
}

