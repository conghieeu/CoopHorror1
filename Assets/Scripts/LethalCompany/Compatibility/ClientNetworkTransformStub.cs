// Stub for ClientNetworkTransform (from Unity.Netcode.Components)
namespace Unity.Netcode.Components
{
    public class ClientNetworkTransform : NetworkTransform
    {
        // NetworkTransform.InLocalSpace may already exist in NGO,
        // but we add it here in case the version doesn't have it
        public new bool InLocalSpace { get; set; }
    }
}