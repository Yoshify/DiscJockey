namespace YoutubeDLSharp;

/// <summary>
///     Encapsulates the output of a yt-dlp download operation.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RunResult<T>
{
    /// <summary>
    ///     Creates a new instance of class RunResult.
    /// </summary>
    public RunResult(bool success, string[] error, T result)
    {
        Success = success;
        ErrorOutput = error;
        Data = result;
    }

    /// <summary>
    ///     Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    ///     The accumulated error output.
    /// </summary>
    public string[] ErrorOutput { get; }

    /// <summary>
    ///     The output data.
    /// </summary>
    public T Data { get; }
}