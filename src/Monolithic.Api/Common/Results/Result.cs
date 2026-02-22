namespace Monolithic.Api.Common.Results;

/// <summary>
/// Railway-oriented result type — replaces null-returns and thrown exceptions
/// in service/application layer methods.
///
/// A <see cref="Result{T}"/> is either:
///   - Success: carries a value of type <typeparamref name="T"/>
///   - Failure: carries an <see cref="Error"/> describing why it failed
///
/// Usage example:
/// <code>
///   Result&lt;CustomerDto&gt; result = await _service.GetByIdAsync(id, ct);
///   return result.Match(Ok, this.Problem);
/// </code>
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    public Error Error =>
        IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _error = error;
    }

    // ── Implicit conversions (allows returning T or Error directly) ───────────

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);

    // ── Functional helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Executes <paramref name="onSuccess"/> when successful,
    /// <paramref name="onFailure"/> when failed, and returns their result.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>
    /// Chains a transformation on the success path; failures propagate unchanged.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Ok(mapper(Value)) : Result<TOut>.Fail(Error);

    /// <summary>
    /// Chains an async operation on the success path; failures propagate unchanged.
    /// </summary>
    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder)
        => IsSuccess ? await binder(Value) : Result<TOut>.Fail(Error);

    // ── Static factories ──────────────────────────────────────────────────────

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);
}

/// <summary>
/// Non-generic result for operations that return no value (void / Unit).
/// </summary>
public sealed class Result
{
    private readonly Error? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error =>
        IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    private Result() => IsSuccess = true;
    private Result(Error error) { IsSuccess = false; _error = error; }

    public static implicit operator Result(Error error) => new(error);

    public static readonly Result Ok = new();
    public static Result Fail(Error error) => new(error);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error);
}
