namespace SailNora.Foundation.Models;

public class ApiResult<T>
{
    public string Code { get; set; } = ApiResultCodes.Success;

    public string Msg { get; set; } = "success";

    public T? Data { get; set; }

    public static ApiResult<T> Ok(T? data, string msg = "success")
        => new() { Code = ApiResultCodes.Success, Msg = msg, Data = data };

    public static ApiResult<T> Fail(string msg, string code = ApiResultCodes.Error)
        => new() { Code = code, Msg = msg, Data = default };
}

public class ApiResult : ApiResult<object>
{
    public static ApiResult Ok(string msg = "success")
        => new() { Code = ApiResultCodes.Success, Msg = msg };

    public static new ApiResult Fail(string msg, string code = ApiResultCodes.Error)
        => new() { Code = code, Msg = msg };
}
