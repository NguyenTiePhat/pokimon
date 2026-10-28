using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

// Main-thread, bounded, non-blocking TCP transport. No Unity API or background callbacks.
public sealed class LanTransport : IDisposable
{
    public const int Port = 7777;
    public bool Connected { get; private set; }
    public bool Listening => listener != null && !Connected;
    public string Error { get; private set; }
    TcpListener listener;
    TcpClient client;
    Task connecting;
    readonly byte[] buffer = new byte[8192];
    readonly StringBuilder incoming = new StringBuilder();
    readonly Queue<byte[]> outgoing = new Queue<byte[]>();
    int offset;
    DateTime deadline, lastReceive;
    public void Host()
    {
        Dispose(); Error = null;
        try { listener = new TcpListener(IPAddress.Any, Port); listener.Start(1); }
        catch (Exception e) { Fail(e.Message); }
    }
    public void Join(string address)
    {
        Dispose(); Error = null;
        if (!IPAddress.TryParse(address, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
        { Error = "Nhập địa chỉ IPv4 hợp lệ, ví dụ 192.168.1.10"; return; }
        try { client = new TcpClient(); connecting = client.ConnectAsync(ip, Port); deadline = DateTime.UtcNow.AddSeconds(8); }
        catch (Exception e) { Fail(e.Message); }
    }
    void AcceptClient()
    {
        client.NoDelay = true; client.Client.Blocking = false;
        Connected = true; lastReceive = DateTime.UtcNow;
        listener?.Stop(); listener = null;
    }
    public void Send(string message)
    {
        if (!Connected) return;
        if (outgoing.Count >= 120 || message.Length > 65536) { Fail("Kết nối quá chậm."); return; }
        outgoing.Enqueue(Encoding.UTF8.GetBytes(message + "\n"));
    }
    public void Pump(Action<string> receive)
    {
        try
        {
            if (connecting != null)
            {
                if (connecting.IsCompleted)
                {
                    connecting.GetAwaiter().GetResult(); connecting = null; AcceptClient();
                }
                else if (DateTime.UtcNow > deadline) { Fail("Hết thời gian kết nối (8 giây)."); return; }
            }
            if (listener != null && listener.Pending()) { client = listener.AcceptTcpClient(); AcceptClient(); }
            if (!Connected) return;
            if (DateTime.UtcNow - lastReceive > TimeSpan.FromSeconds(12)) { Fail("Mất phản hồi từ máy còn lại."); return; }
            var socket = client.Client;
            for (int batch = 0; batch < 16 && socket.Poll(0, SelectMode.SelectRead); batch++)
            {
                int count = socket.Receive(buffer);
                if (count == 0) { Fail("Máy còn lại đã rời phòng."); return; }
                lastReceive = DateTime.UtcNow;
                for (int i = 0; i < count; i++)
                {
                    // JsonUtility escapes non-ASCII metadata; protocol contains only ASCII numeric fields.
                    if (buffer[i] == 10)
                    {
                        string line = incoming.ToString(); incoming.Clear(); receive(line);
                        if (!Connected) return;
                    }
                    else incoming.Append((char)buffer[i]);
                    if (incoming.Length > 65536) { Fail("Gói tin không hợp lệ."); return; }
                }
            }
            for (int batch = 0; batch < 16 && outgoing.Count > 0 && socket.Poll(0, SelectMode.SelectWrite); batch++)
            {
                byte[] data = outgoing.Peek();
                offset += socket.Send(data, offset, data.Length - offset, SocketFlags.None);
                if (offset == data.Length) { outgoing.Dequeue(); offset = 0; }
            }
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.WouldBlock) { }
        catch (Exception e) { Fail("Lỗi kết nối: " + e.Message); }
    }
    void Fail(string error) { Dispose(); Error = error; }
    public void Dispose()
    {
        Connected = false;
        listener?.Stop(); listener = null;
        client?.Close(); client = null;
        if (connecting != null) connecting.ContinueWith(t => { var observed = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        connecting = null; outgoing.Clear(); incoming.Clear(); offset = 0;
    }
    public static string LocalAddresses()
    {
        var found = new List<string>();
        try { foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
            if (address.AddressFamily == AddressFamily.InterNetwork) found.Add(address.ToString()); }
        catch (SocketException) { }
        return found.Count > 0 ? string.Join("  /  ", found) : "Dùng ipconfig để xem IPv4";
    }
}
