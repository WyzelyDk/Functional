// ERRORS IN DEPTH: what an Error carries, and why.
//
// Scenario: an order cannot be found, and the failure travels up through several layers (repository, use case,
// web endpoint) before somebody logs it or shows it to a user.
//
// WHY one rich Error type:
//  - A message TEMPLATE ("Order {OrderId} was not found.") is the stable identity of the failure, the DETAILS are the
//    data. Logging systems can group by the template and still filter by the order id.
//  - A detail can be marked SENSITIVE. The redacted text is what ToString() of a result, an exception message and
//    your logs should use; the real value is still there for the code that is allowed to see it.
//  - The KIND says what sort of failure it is (an open set), and IsTransient says whether trying again may help.
//  - CONTEXT frames are added by each layer the error passes, so the final message tells where it came from.
//  - One Error can AGGREGATE many errors, so a result stays one reference wide, whatever happened.
//  - An Error is immutable: every With... method returns a modified copy.
namespace Wyzely.Functional.ResultSamples;

public static class ErrorsExample
{
    public static void Run(TextWriter output)
    {
        var error = Error.NotFound("Order {OrderId} of {Customer} was not found.")
            .WithDetail("OrderId", 7)
            .WithDetail("Customer", "anna@example.com", isSensitive: true)
            .WithCode("ORDER_NOT_FOUND");

        output.WriteLine($"Kind: {error.Kind}");
        output.WriteLine($"Template: {error.Template}");
        output.WriteLine($"Message: {error.Message}");
        output.WriteLine($"RedactedMessage: {error.RedactedMessage}");
        output.WriteLine($"Code: {error.Code}");

        // ErrorDetail.ToString() is redacted, so printing a detail is safe by default.
        foreach (var detail in error.Details)
        {
            output.WriteLine($"Detail: {detail}");
        }

        // Kinds are an open set. The built-in ones (Validation, NotFound, Conflict, Unauthorized, Forbidden,
        // Unavailable, Timeout, Cancelled, Unexpected) have factories; your own kind is just a name.
        var rateLimited = Error.Create("Slow down.", new ErrorKind("RateLimited"));
        output.WriteLine($"Custom kind: {rateLimited.Kind}");

        // Context: each layer adds a frame, outermost printed first. The original error is not changed.
        var withContext = error.WithContext("Checkout").WithContext("Web");
        output.WriteLine($"With two context frames: {withContext}");
        output.WriteLine($"The original error is unchanged: {error}");

        // A frame is a template with details of its own, like a message: the value stays structured and can be redacted.
        var withDetails = error.WithContext("Mail to {Email}", "Email", "anna@example.com", isSensitive: true);
        output.WriteLine($"Frame with a detail: {withDetails}");
        output.WriteLine($"Frame with a detail, redacted: {withDetails.ToRedactedString()}");

        // Aggregation: several errors become one. A single error stays as it is; nested aggregates are flattened.
        var name = Error.Validation("Name is missing.");
        var email = Error.Validation("Email is invalid.");
        var aggregate = Error.Aggregate([name, email]);
        output.WriteLine($"Aggregate message: {aggregate.Message}");
        output.WriteLine($"Aggregate kind when every child has the same kind: {aggregate.Kind}");
        output.WriteLine($"Aggregate children: {aggregate.Children.Count}");

        // WithContext on an aggregate adds the frame to every child, so no child loses its origin.
        output.WriteLine($"Aggregate with context, first child: {aggregate.WithContext("Signup").Children[0]}");

        // When the kinds differ, the aggregate has no single kind (the default kind, Failure).
        var mixed = Error.Aggregate([name, Error.NotFound("Customer 9 was not found.")]);
        output.WriteLine($"Aggregate kind when the kinds differ: {mixed.Kind}");

        // Transient: "trying again later might work". Unavailable and Timeout set it; you can set it yourself.
        output.WriteLine($"Unavailable is transient: {Error.Unavailable("The payment gateway is down.").IsTransient}");
        output.WriteLine($"Validation is transient: {Error.Validation("Bad input.").IsTransient}");

        // FromException keeps the exception as the cause and chooses a kind: Timeout for a TimeoutException
        // (transient), Cancelled for an OperationCanceledException, Unexpected for everything else.
        var timeout = Error.FromException(new TimeoutException("Gateway timed out."));
        output.WriteLine($"FromException(TimeoutException): kind {timeout.Kind}, transient {timeout.IsTransient}");
        output.WriteLine($"FromException keeps the cause: {timeout.Exception?.GetType().Name}");

        // An exception message may contain braces (JSON, for example). They are escaped, so they are never
        // mistaken for placeholders: the template shows them doubled, the message reads as the exception did.
        var braces = Error.FromException(new InvalidOperationException("Bad {json}"));
        output.WriteLine($"FromException escapes braces, template: {braces.Template}");
        output.WriteLine($"FromException message: {braces.Message}");

        // A failed result prints the REDACTED text; so does the exception that GetValue() throws.
        output.WriteLine($"A failed result prints the redacted text: {Result.Failure<int>(error)}");

        // Errors are compared by structure, not by reference.
        var same = Error.NotFound("Order {OrderId} of {Customer} was not found.")
            .WithDetail("OrderId", 7)
            .WithDetail("Customer", "anna@example.com", isSensitive: true)
            .WithCode("ORDER_NOT_FOUND");
        output.WriteLine($"Equal by structure: {error.Equals(same)}");
    }
}
