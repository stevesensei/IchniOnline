namespace IchniOnline.Server.Models.Responses;

public enum ResponseCode
{
    Ok = 10000,
    BadRequest = 10400,
    Unauthorized = 10401,
    Forbidden = 10403,
    NotFound = 10404,
    InternalServerError = 10500
}