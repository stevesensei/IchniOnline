namespace IchniOnline.Server.Models.Responses;

public class GlobalResponse<T>
{
    public required ResponseCode Code { get; set; }
    public required string Message { get; set; }
    public T? Data { get; set; }

    public static GlobalResponse<T> Ok(T? data, string message = "Success") =>
        new() { Code = ResponseCode.Ok, Message = message, Data = data };

    public static GlobalResponse<T> Fail(ResponseCode code, string message) =>
        new() { Code = code, Message = message, Data = default };

    public static GlobalResponse<T> BadRequest(string message = "Bad request") =>
        Fail(ResponseCode.BadRequest, message);

    public static GlobalResponse<T> Unauthorized(string message = "Unauthorized") =>
        Fail(ResponseCode.Unauthorized, message);

    public static GlobalResponse<T> Forbidden(string message = "Forbidden") =>
        Fail(ResponseCode.Forbidden, message);

    public static GlobalResponse<T> NotFound(string message = "Not found") =>
        Fail(ResponseCode.NotFound, message);

    public static GlobalResponse<T> InternalServerError(string message = "Internal server error") =>
        Fail(ResponseCode.InternalServerError, message);
}

public class GlobalResponse : GlobalResponse<object>
{
    public static GlobalResponse Ok(string message = "Success") =>
        new() { Code = ResponseCode.Ok, Message = message, Data = default };

    public new static GlobalResponse Fail(ResponseCode code, string message) =>
        new() { Code = code, Message = message, Data = default };
}