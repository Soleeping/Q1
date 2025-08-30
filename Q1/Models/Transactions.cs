using System.ComponentModel.DataAnnotations;

namespace Q1.Models;

#nullable disable warnings

public class SubmitTrxRequest
{
    [StringLength(50)]
    public string partnerkey { get; set; }
    [StringLength(50)]
    public string partnerrefno { get; set; }
    [StringLength(50)]
    public string partnerpassword { get; set; }
    public long totalamount { get; set; }
    public List<ItemDetail>? items { get; set; }
    public string timestamp { get; set; }
    public string sig { get; set; }
}

public class ItemDetail
{
    [StringLength(50)]
    public string partneritemref { get; set; }
    [StringLength(100)]
    public string name { get; set; }
    public int qty { get; set; }
    public long unitprice { get; set; }
}

public class SubmitTrxResponse
{
    public int result { get; set; }
    public long? totalamount { get; set; }
    public long? totaldiscount { get; set; }
    public long? finalamount { get; set; }
    public string? resultmessage { get; set; }
}

public class Partner
{
    public string? PartnerNo { get; set; }
    public string? PartnerKey { get; set; }
    public string? Password { get; set; }
}