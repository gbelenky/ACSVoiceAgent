using System.Text.Json;

namespace ACSVoiceAgent.Services;

/// <summary>
/// Business logic for agent tool calls. All functions are stateless with simulated data.
/// Replace with real service calls in production.
/// </summary>
public static class AgentFunctions
{
    private static readonly Dictionary<string, object> Customers = new()
    {
        ["12345"] = new { name = "Genady Belenky", id = "12345", phone = "+14255551234", email = "genady@example.com", tier = "Gold", balance = 150.00 },
        ["67890"] = new { name = "John Smith", id = "67890", phone = "+14255555678", email = "john@example.com", tier = "Silver", balance = 45.50 },
        ["+14255551234"] = new { name = "Genady Belenky", id = "12345", phone = "+14255551234", email = "alice@example.com", tier = "Gold", balance = 150.00 },
        ["+14255555678"] = new { name = "John Smith", id = "67890", phone = "+14255555678", email = "john@example.com", tier = "Silver", balance = 45.50 }
    };

    private static readonly Dictionary<string, object> Orders = new()
    {
        ["001"] = new { orderId = "001", status = "Shipped", estimatedDelivery = "2026-03-27", items = "Wireless Headphones x1", trackingNumber = "1Z999AA10123456784" },
        ["002"] = new { orderId = "002", status = "Processing", estimatedDelivery = "2026-03-30", items = "USB-C Hub x2", trackingNumber = "" },
        ["003"] = new { orderId = "003", status = "Delivered", estimatedDelivery = "2026-03-20", items = "Laptop Stand x1", trackingNumber = "1Z999AA10987654321" }
    };

    private static readonly Dictionary<string, object> Appointments = new()
    {
        ["12345"] = new { appointmentId = "APT-100", customerId = "12345", date = "2026-03-28", time = "10:00", type = "Service Review", status = "Confirmed" },
        ["67890"] = new { appointmentId = "APT-101", customerId = "67890", date = "2026-03-29", time = "14:30", type = "Account Setup", status = "Confirmed" }
    };

    private static readonly List<(string[] Keywords, string Question, string Answer)> FaqEntries =
    [
        (["return", "refund", "money back"], "What is the return policy?", "We offer a 30-day return policy for all unused items in original packaging. Refunds are processed within 5-7 business days."),
        (["shipping", "delivery", "ship"], "How long does shipping take?", "Standard shipping takes 5-7 business days. Express shipping takes 2-3 business days. Free shipping on orders over $50."),
        (["hours", "open", "close", "business hours"], "What are your business hours?", "Our customer service is available Monday-Friday 8am-8pm EST, and Saturday 9am-5pm EST. We are closed on Sundays."),
        (["warranty", "guarantee"], "What warranty do you offer?", "All products come with a 1-year manufacturer warranty. Extended warranties are available for purchase at checkout."),
        (["cancel", "subscription"], "How do I cancel my subscription?", "You can cancel your subscription anytime from your account settings or by calling us. No cancellation fees apply."),
        (["payment", "pay", "credit card", "billing"], "What payment methods do you accept?", "We accept Visa, Mastercard, American Express, PayPal, and Apple Pay."),
        (["contact", "email", "phone", "support"], "How can I contact support?", "You can reach us by phone at 1-800-555-0199, email at support@example.com, or through live chat on our website.")
    ];

    public static string LookupCustomer(JsonElement args)
    {
        var identifier = args.GetProperty("identifier").GetString() ?? "";
        return Customers.TryGetValue(identifier, out var customer)
            ? JsonSerializer.Serialize(customer)
            : JsonSerializer.Serialize(new { error = "Customer not found", identifier });
    }

    public static string CheckOrderStatus(JsonElement args)
    {
        var orderId = args.GetProperty("order_id").GetString() ?? "";
        return Orders.TryGetValue(orderId.ToUpperInvariant(), out var order)
            ? JsonSerializer.Serialize(order)
            : JsonSerializer.Serialize(new { error = "Order not found", orderId });
    }

    public static string CheckAppointment(JsonElement args)
    {
        var customerId = args.GetProperty("customer_id").GetString() ?? "";
        return Appointments.TryGetValue(customerId, out var existing)
            ? JsonSerializer.Serialize(existing)
            : JsonSerializer.Serialize(new { message = "No upcoming appointments found", customerId });
    }

    public static string BookAppointment(JsonElement args)
    {
        var customerId = args.GetProperty("customer_id").GetString() ?? "";
        return JsonSerializer.Serialize(new
        {
            appointmentId = $"APT-{Random.Shared.Next(200, 999)}",
            customerId,
            date = args.TryGetProperty("date", out var d) ? d.GetString() : "TBD",
            time = args.TryGetProperty("time", out var t) ? t.GetString() : "TBD",
            type = "General",
            status = "Confirmed"
        });
    }

    public static string CancelAppointment(JsonElement args)
    {
        var appointmentId = args.TryGetProperty("appointment_id", out var a) ? a.GetString() : "unknown";
        return JsonSerializer.Serialize(new { message = "Appointment cancelled successfully", appointmentId });
    }

    public static string SearchKnowledgeBase(JsonElement args)
    {
        var query = (args.GetProperty("query").GetString() ?? "").ToLowerInvariant();
        var results = FaqEntries
            .Where(e => e.Keywords.Any(k => query.Contains(k)))
            .Select(e => new { question = e.Question, answer = e.Answer })
            .ToList();

        return results.Count > 0
            ? JsonSerializer.Serialize(new { results, count = results.Count })
            : JsonSerializer.Serialize(new { message = "No FAQ entries found. Consider transferring to a human agent.", query });
    }
}
