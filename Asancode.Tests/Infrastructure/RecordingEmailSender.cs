using System.Collections.Concurrent;
using Core.Security.Sender;

namespace Asancode.Tests.Infrastructure;

public sealed record CapturedEmail(string To, string Subject, string Body);

public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<CapturedEmail> _messages = new();

    public IReadOnlyList<CapturedEmail> Messages => _messages.ToArray();

    public void Send(string to, string subject, string body)
    {
        _messages.Enqueue(new CapturedEmail(to, subject, body));
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}
