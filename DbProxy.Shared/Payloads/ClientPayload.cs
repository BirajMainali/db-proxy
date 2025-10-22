namespace DbProxy.Shared.Payloads;

public class ClientPayload
{
    public string Requester { get; set; }
    public string Payload { get; set; }
    public string Signature { get; set; }
}