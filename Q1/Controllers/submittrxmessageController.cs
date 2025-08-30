using Microsoft.AspNetCore.Mvc;
using Q1.Models;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class submittrxmessageController : ControllerBase
{
    private readonly List<Partner> _allowedPartners = new List<Partner>
    {
        new Partner 
        {
          PartnerNo = "FG-00001",
          PartnerKey = "FAKEGOOGLE", 
          Password = "FAKEPASSWORD1234" 
        },
        new Partner
        {
          PartnerNo = "FG-00002",
          PartnerKey = "FAKEPEOPLE",
          Password = "FAKEPASSWORD4578" 
        }
    };

    [HttpPost]
    public IActionResult Post([FromBody] SubmitTrxRequest request)
    {
       
        var validationResult = ValidateRequest(request);
        if (validationResult != null)
            return BadRequest(validationResult);

        
        var response = ProcessTransaction(request);

        return Ok(response);
    }



   private SubmitTrxResponse? ValidateRequest(SubmitTrxRequest request)
{
    if (string.IsNullOrEmpty(request.partnerkey) ||
        string.IsNullOrEmpty(request.partnerrefno) ||
        string.IsNullOrEmpty(request.partnerpassword) ||
        string.IsNullOrEmpty(request.timestamp) ||
        string.IsNullOrEmpty(request.sig))
    {
        return new SubmitTrxResponse { 
            result = 0, 
            resultmessage = "Missing mandatory fields" 
        };
    }

    
    var partner = _allowedPartners.FirstOrDefault(p => p.PartnerKey == request.partnerkey);
    if (partner == null)
        return new SubmitTrxResponse { 
            result = 0,
            resultmessage = "Invalid partner key"
        };

    
    string decodedPassword;
    try
    {
        decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(request.partnerpassword));
    }
    catch
    {
        return new SubmitTrxResponse { 
            result = 0,
            resultmessage = "Invalid password encoding"
        };
    }

    if (decodedPassword != partner.Password)
        return new SubmitTrxResponse { 
            result = 0,
            resultmessage = "Invalid password"
        };

    
    if (request.totalamount <= 0)
        return new SubmitTrxResponse { 
            result = 0,
            resultmessage = "Total amount must be positive"
        };

   
    if (request.items != null && request.items.Count > 0)
    {
        foreach (var item in request.items)
        {
            if (string.IsNullOrEmpty(item.partneritemref) || string.IsNullOrEmpty(item.name))
                return new SubmitTrxResponse {
                    result = 0,
                    resultmessage = "Item reference or name is missing" 
                };

            if (item.qty <= 0 || item.qty > 5)
                return new SubmitTrxResponse { 
                    result = 0, 
                    resultmessage = "Invalid quantity" 
                };

            if (item.unitprice <= 0)
                return new SubmitTrxResponse { 
                    result = 0, 
                    resultmessage = "Unit price must be positive"
                };
        }
    }

        var timestampResult = ValidateTimestamp(request.timestamp);
        if (!timestampResult.IsValid)
        {
            return new SubmitTrxResponse
            {
                result = 0,
                resultmessage = timestampResult.ErrorMessage
            };
        }


        var signatureResult = ValidateSignature(request, partner);
        if (!signatureResult.IsValid)
        {
            return new SubmitTrxResponse
            {
                result = 0,
                resultmessage = signatureResult.ErrorMessage
            };
        }

        return null; 
}


    private (bool IsValid, string ErrorMessage) ValidateSignature(SubmitTrxRequest request, Partner partner)
    {
        try
        {
            var dt = DateTime.Parse(request.timestamp);
            var sigTimestamp = dt.ToString("yyyyMMddHHmmss");

            var signatureString = $"{sigTimestamp}{request.partnerkey}{request.partnerrefno}{request.totalamount}{request.partnerpassword}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
            var hashString = Convert.ToHexString(hashBytes).ToLower();

            var expectedSignature = Convert.ToBase64String(Encoding.UTF8.GetBytes(hashString));

            if (request.sig != expectedSignature)
            {
                return (false, "Access Denied!");
            }
        }
        catch
        {
            return (false, "Access Denied!");
        }

        return (true, string.Empty);
    }

    private (bool IsValid, string ErrorMessage) ValidateTimestamp(string timestamp)
    {
        try
        {
            var requestTime = DateTime.Parse(timestamp).ToUniversalTime();
            var serverTime = DateTime.UtcNow;
            var timeDifference = Math.Abs((serverTime - requestTime).TotalMinutes);

            if (timeDifference > 5)
            {
                return (false, "Expired.");
            }
        }
        catch
        {
            return (false, "timestamp is Required.");
        }

        return (true, string.Empty);
    }

    private SubmitTrxResponse ProcessTransaction(SubmitTrxRequest request)
    {
        var totalAmount = request.totalamount;
        var totalDiscount = CalculateDiscount(totalAmount);
        var finalAmount = totalAmount - totalDiscount;

        return new SubmitTrxResponse
        {
            result = 1,
            totalamount = totalAmount,
            totaldiscount = totalDiscount,
            finalamount = finalAmount
        };
    }

    private long CalculateDiscount(long totalAmount)
    {
        long baseDiscount = 0;
        long conditionalDiscount = 0;

        if (totalAmount < 200)
        {
            baseDiscount = 0;
        }
        else if (totalAmount >= 200 && totalAmount <= 500)
        {
            
            baseDiscount = (long)(totalAmount * 0.05);
        }
        else if (totalAmount >= 501 && totalAmount <= 800)
        {
            baseDiscount = (long)(totalAmount * 0.07);
        }
        else if (totalAmount >= 801 && totalAmount <= 1200)
        {
            baseDiscount = (long)(totalAmount * 0.10);
        }
        else if (totalAmount > 1200)
        {
            baseDiscount = (long)(totalAmount * 0.15);
        }

        if (totalAmount > 500 && IsPrime(totalAmount))
        {
            conditionalDiscount += (long)(totalAmount * 0.08);
        }

        if (totalAmount > 900 && EndsWithDigit5(totalAmount))
        {
            conditionalDiscount += (long)(totalAmount * 0.10); 
        }

        long totalDiscount = baseDiscount + conditionalDiscount;

        long maxDiscount = (long)(totalAmount * 0.20);
        if (totalDiscount > maxDiscount)
        {
            totalDiscount = maxDiscount;
        }

        return totalDiscount;
    }

    private bool IsPrime(long number)
    {
        if (number <= 1) return false;
        if (number <= 3) return true;
        if (number % 2 == 0 || number % 3 == 0) return false;

        for (long i = 5; i * i <= number; i += 6)
        {
            if (number % i == 0 || number % (i + 2) == 0)
                return false;
        }
        return true;
    }

    private bool EndsWithDigit5(long number)
    {
        return number % 10 == 5;
    }
}

