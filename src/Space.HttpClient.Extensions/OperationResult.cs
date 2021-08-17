namespace Space.HttpClient.Extensions
{
    public class OperationResult<T> where T : ErrorMessage
    {
        /// <summary>
        /// 错误编码
        /// </summary>
        public long Code { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 返回对象
        /// </summary>
        public T Result { get; set; }
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        public static OperationResult<T> FromResult(T result) => new OperationResult<T>() { Code = result.Code, Result = result, Message = result.Message, Success = result.Success };
    }

    public class ErrorMessage
    {
        /// <summary>
        /// 错误编码
        /// </summary>
        public long Code { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
    }
}
