# Verdict: Developer Quick Reference

## Common Patterns

### 1. Basic Success/Failure

```csharp
using Verdict;

// Success
public Result<User> GetUser(int id)
{
    var user = _db.Users.Find(id);
    if (user == null)
        return Result<User>.Failure("NOT_FOUND", "User not found");
    
    return Result<User>.Success(user);
}

// Implicit conversion (cleaner)
public Result<User> GetUser(int id)
{
    var user = _db.Users.Find(id);
    if (user == null)
        return new Error("NOT_FOUND", "User not found");
    
    return user; // Implicit conversion
}
```

### 2. Validation (Multi-Error)

```csharp
using Verdict.Extensions;

public MultiResult<User> ValidateUser(User user)
{
    return ValidationExtensions.ValidateAll(user,
        (u => !string.IsNullOrEmpty(u.Email), "REQUIRED_EMAIL", "Email is required"),
        (u => u.Email.Contains("@"), "INVALID_EMAIL", "Email must be valid"),
        (u => u.Age >= 18, "UNDERAGE", "Must be 18 or older"),
        (u => u.Username.Length >= 3, "SHORT_USERNAME", "Username too short")
    );
}

// Usage
var result = ValidateUser(user);
if (result.IsFailure)
{
    // Returns ALL errors at once
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"[{error.Code}] {error.Message}");
    }
}
```

### 3. Async Operations

```csharp
using Verdict.Async;

public async Task<Result<OrderDto>> ProcessOrderAsync(int orderId)
{
    return await GetOrderAsync(orderId)
        .BindAsync(order => ValidateStockAsync(order))
        .BindAsync(order => ChargePaymentAsync(order))
        .BindAsync(order => CreateShipmentAsync(order))
        .MapAsync(order => order.ToDto())
        .TapAsync(dto => SendConfirmationEmailAsync(dto))
        .TapErrorAsync(error => LogErrorAsync(error));
}
```

### 4. Success Metadata (Audit Trails)

```csharp
using Verdict.Rich;

public Result<User> CreateUser(CreateUserDto dto)
{
    var user = new User(dto);
    _db.Users.Add(user);
    _db.SaveChanges();
    
    return Result<User>.Success(user)
        .WithSuccess("User created")
        .WithSuccess("Welcome email queued")
        .WithSuccess(new SuccessInfo("Audit logged")
            .WithMetadata("UserId", user.Id)
            .WithMetadata("Timestamp", DateTime.UtcNow));
}

// Access success messages
var result = CreateUser(dto);
foreach (var success in result.GetSuccesses())
{
    _logger.LogInformation(success.Message);
}
```

### 5. Error Metadata (Debugging)

```csharp
using Verdict.Rich;

public Result<Payment> ProcessPayment(PaymentRequest request)
{
    try
    {
        var payment = _paymentGateway.Charge(request);
        return payment;
    }
    catch (PaymentException ex)
    {
        return Result<Payment>.Failure("PAYMENT_FAILED", ex.Message)
            .WithErrorMetadata("TransactionId", request.TransactionId)
            .WithErrorMetadata("Amount", request.Amount)
            .WithErrorMetadata("Gateway", "Stripe")
            .WithErrorMetadata("Timestamp", DateTime.UtcNow);
    }
}
```

### 6. ASP.NET Core Integration

```csharp
using Verdict.AspNetCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        return _userService.GetUser(id)
            .ToActionResult(user => Ok(user));
        // Auto-converts to BadRequest on failure
    }
    
    [HttpPost]
    public IActionResult CreateUser(CreateUserDto dto)
    {
        return _userService.CreateUser(dto)
            .ToActionResult(user => CreatedAtAction(
                nameof(GetUser), 
                new { id = user.Id }, 
                user
            ));
    }
    
    // ProblemDetails (RFC 7807)
    [HttpPut("{id}")]
    public IActionResult UpdateUser(int id, UpdateUserDto dto)
    {
        return _userService.UpdateUser(id, dto)
            .ToProblemDetails(HttpContext);
    }
}
```

### 7. Combining Results

```csharp
using Verdict.Extensions;

// Combine multiple results
var emailResult = ValidateEmail(email);
var passwordResult = ValidatePassword(password);
var usernameResult = ValidateUsername(username);

var combined = CombineExtensions.CombineAll(
    emailResult,
    passwordResult,
    usernameResult
);

if (combined.IsSuccess)
{
    var (email, password, username) = combined.Value;
    // All validations passed
}
else
{
    // Returns ALL validation errors
    foreach (var error in combined.Errors)
    {
        Console.WriteLine($"[{error.Code}] {error.Message}");
    }
}
```

### 8. Try/Catch Helpers

```csharp
using Verdict.Extensions;

// Convert exceptions to Results
public Result<User> GetUserSafe(int id)
{
    return TryExtensions.Try(() => {
        return _db.Users.Find(id) 
            ?? throw new NotFoundException("User not found");
    });
}

// Custom error handler
public Result<Data> LoadData(string path)
{
    return TryExtensions.Try(
        () => File.ReadAllText(path),
        ex => new Error("FILE_ERROR", $"Failed to load {path}", ex)
    );
}
```

### 9. Pattern Matching

```csharp
// Deconstruction
var (isSuccess, value, error) = GetUser(id);
if (isSuccess)
{
    Console.WriteLine($"User: {value.Name}");
}

// Switch expression
var message = GetUser(id) switch
{
    { IsSuccess: true } result => $"Found: {result.Value.Name}",
    { IsFailure: true } result => $"Error: {result.Error.Message}",
    _ => "Unknown"
};

// ValueOrDefault (no exception)
var user = GetUser(id).ValueOrDefault; // null if failed
var user = GetUser(id).ValueOr(User.Guest); // fallback value
```

### 10. Logging Integration

```csharp
using Verdict.Logging;

public Result<User> GetUser(int id)
{
    return _db.Users.Find(id)
        .ToResult("NOT_FOUND", "User not found")
        .Log(_logger, "User retrieved")
        .LogOnFailure(_logger, LogLevel.Warning);
}
```

---

## Best Practices

### ✅ DO

```csharp
// Use error codes (not just messages)
return Result<User>.Failure("NOT_FOUND", "User not found");

// Use implicit conversions
return user; // Instead of Result<User>.Success(user)
return new Error("CODE", "Message"); // Instead of Result.Failure(...)

// Use ValueOrDefault for safe access
var user = GetUser(id).ValueOrDefault;

// Chain operations
return GetUser(id)
    .Map(u => u.ToDto())
    .Tap(dto => _logger.LogInformation("User loaded"));

// Use MultiResult for validation
return ValidationExtensions.ValidateAll(user, ...);
```

### ❌ DON'T

```csharp
// Don't throw exceptions for expected errors
if (user == null)
    throw new NotFoundException(); // ❌ BAD

// Don't access Value without checking
var user = GetUser(id).Value; // ❌ Throws if failed

// Don't use exceptions for flow control
try {
    var user = GetUser(id);
} catch (NotFoundException) {
    // ❌ BAD
}

// Don't mix Result and exceptions
public Result<User> GetUser(int id)
{
    if (id < 0)
        throw new ArgumentException(); // ❌ Inconsistent
    
    return _db.Users.Find(id);
}
```

---

## Migration from FluentResults

### Before (FluentResults)

```csharp
using FluentResults;

public Result<User> GetUser(int id)
{
    var user = _db.Users.Find(id);
    if (user == null)
        return Result.Fail("User not found");
    
    return Result.Ok(user);
}

// Validation
var result = Result.Ok(user)
    .Ensure(u => u.Email.Contains("@"), "Invalid email")
    .Ensure(u => u.Age >= 18, "Must be 18+");

// Success messages
var result = Result.Ok(user)
    .WithSuccess("User created")
    .WithSuccess("Email sent");
```

### After (Verdict)

```csharp
using Verdict;

public Result<User> GetUser(int id)
{
    var user = _db.Users.Find(id);
    if (user == null)
        return Result<User>.Failure("NOT_FOUND", "User not found");
    
    return Result<User>.Success(user);
}

// Validation (Verdict.Extensions)
using Verdict.Extensions;

var result = Result<User>.Success(user)
    .Ensure(u => u.Email.Contains("@"), "INVALID_EMAIL", "Invalid email")
    .Ensure(u => u.Age >= 18, "UNDERAGE", "Must be 18+");

// Success messages (Verdict.Rich)
using Verdict.Rich;

var result = Result<User>.Success(user)
    .WithSuccess("User created")
    .WithSuccess("Email sent");
```

**Key Differences:**
1. Verdict requires error codes (better for APIs)
2. Verdict has separate packages for features (opt-in)
3. Verdict is 230x faster with zero allocation

---

## Performance Tips

### 1. Use Core for Hot Paths

```csharp
// Hot path (called millions of times)
public Result<decimal> CalculateTotal(Order order)
{
    // Use core Result<T> - zero allocation
    return order.Items.Sum(i => i.Price);
}
```

### 2. Use Extensions for Validation

```csharp
// Warm path (called thousands of times)
public MultiResult<Order> ValidateOrder(Order order)
{
    // Use Verdict.Extensions - minimal allocation
    return ValidationExtensions.ValidateAll(order, ...);
}
```

### 3. Use Rich for Audit Trails

```csharp
// Cold path (called rarely, needs audit)
public Result<User> CreateUser(CreateUserDto dto)
{
    // Use Verdict.Rich - metadata overhead acceptable
    return Result<User>.Success(new User(dto))
        .WithSuccess("User created")
        .WithSuccess(new SuccessInfo("Audit logged")
            .WithMetadata("UserId", user.Id));
}
```

### 4. Avoid Unnecessary Conversions

```csharp
// ❌ BAD - unnecessary boxing
Result<User> result = GetUser(id);
var richResult = RichResult<User>.From(result); // Don't do this

// ✅ GOOD - use same type
Result<User> result = GetUser(id)
    .WithSuccess("User loaded"); // Same type, no conversion
```

---

## Common Scenarios

### API Endpoint

```csharp
[HttpPost("users")]
public IActionResult CreateUser(CreateUserDto dto)
{
    return ValidateUser(dto)
        .Bind(validDto => _userService.CreateUser(validDto))
        .Tap(user => _eventBus.Publish(new UserCreated(user.Id)))
        .ToActionResult(user => CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            user
        ));
}
```

### Background Job

```csharp
public async Task<Result> ProcessOrdersAsync()
{
    var orders = await _db.Orders.Where(o => o.Status == "Pending").ToListAsync();
    
    var results = new List<Result>();
    foreach (var order in orders)
    {
        var result = await ProcessOrderAsync(order);
        results.Add(result);
    }
    
    return CombineExtensions.Merge(results.ToArray());
}
```

### Form Validation

```csharp
public MultiResult<RegistrationDto> ValidateRegistration(RegistrationDto dto)
{
    return ValidationExtensions.ValidateAll(dto,
        (d => !string.IsNullOrEmpty(d.Email), "REQUIRED_EMAIL", "Email is required"),
        (d => d.Email.Contains("@"), "INVALID_EMAIL", "Email must be valid"),
        (d => d.Password.Length >= 8, "SHORT_PASSWORD", "Password must be 8+ characters"),
        (d => d.Password == d.ConfirmPassword, "PASSWORD_MISMATCH", "Passwords must match"),
        (d => d.AcceptedTerms, "TERMS_NOT_ACCEPTED", "Must accept terms")
    );
}
```

---

## Cheat Sheet

| Task            | Code                                                                                     |
| --------------- | ---------------------------------------------------------------------------------------- |
| **Success**     | `return Result<T>.Success(value);` or `return value;`                                    |
| **Failure**     | `return Result<T>.Failure("CODE", "Message");` or `return new Error("CODE", "Message");` |
| **Check**       | `if (result.IsSuccess)` or `if (result.IsFailure)`                                       |
| **Get Value**   | `result.Value` (throws if failed) or `result.ValueOrDefault` (safe)                      |
| **Transform**   | `result.Map(x => x.ToDto())`                                                             |
| **Chain**       | `result.Bind(x => GetRelated(x))`                                                        |
| **Side Effect** | `result.Tap(x => Log(x))`                                                                |
| **Validate**    | `ValidationExtensions.ValidateAll(value, ...)`                                           |
| **Combine**     | `CombineExtensions.CombineAll(r1, r2, r3)`                                               |
| **Try/Catch**   | `TryExtensions.Try(() => RiskyOperation())`                                              |
| **Async**       | `await result.BindAsync(x => GetAsync(x))`                                               |
| **Metadata**    | `result.WithSuccess("Message")` or `result.WithErrorMetadata("Key", value)`              |
| **ASP.NET**     | `result.ToActionResult(x => Ok(x))`                                                      |

---

## The Verdict

**Start simple:** Use core `Result<T>` for basic success/failure.  
**Add features:** Use extension packages as needed.  
**Stay fast:** Zero allocation, 230x faster than FluentResults.

**Questions?** Check the [full documentation](https://github.com/BaryoDev/Verdict) or [Architect's Decision Guide](./architects_decision_guide.md).
