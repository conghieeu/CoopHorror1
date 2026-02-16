// Stub types for Dissonance Voice Chat (not installed in project)
// These provide minimal implementations so code that references Dissonance can compile

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// Stub for DissonanceComms. Provides no voice chat functionality.
    /// </summary>
    public class DissonanceComms : MonoBehaviour
    {
        public string LocalPlayerName { get; set; } = "";
        public event Action<string> LocalPlayerNameChanged;
        public bool IsMuted { get; set; }

        public VoicePlayerState FindPlayer(string playerName)
        {
            return new VoicePlayerState(playerName);
        }

        public static void TestDependencies() { }
        public void TrackPlayerPosition(IDissonancePlayer player) { }
        public void StopTracking(IDissonancePlayer player) { }

        protected virtual void OnLocalPlayerNameChanged(string name)
        {
            LocalPlayerNameChanged?.Invoke(name);
        }
    }

    /// <summary>
    /// Stub for VoicePlayerState.
    /// </summary>
    public class VoicePlayerState
    {
        public string Name { get; private set; }
        public float Amplitude { get; set; }
        public bool IsSpeaking { get; set; }

        public VoicePlayerState(string name = "")
        {
            Name = name;
        }
    }

    /// <summary>
    /// Stub for IDissonancePlayer interface.
    /// </summary>
    public interface IDissonancePlayer
    {
        string PlayerId { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        NetworkPlayerType Type { get; }
        bool IsTracking { get; }
    }

    public enum NetworkPlayerType
    {
        Unknown,
        Local,
        Remote
    }

    public enum NetworkMode
    {
        None,
        Host,
        DedicatedServer,
        Client
    }

    public enum LogCategory
    {
        Core,
        Network,
        Playback,
        Recording
    }

    /// <summary>
    /// Stub for Dissonance Log.
    /// </summary>
    public class Log
    {
        private string _category;
        private string _name;

        public Log(string category, string name)
        {
            _category = category;
            _name = name;
        }

        public Exception CreateUserErrorException(string problem, string likelyCause, string documentationLink, string guid)
        {
            return new Exception($"[Dissonance] {problem} - {likelyCause}");
        }

        public Exception CreatePossibleBugException(string problem, string guid)
        {
            return new Exception($"[Dissonance] Bug: {problem}");
        }

        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warn(string message) { }
        public void Error(string message) { }
    }

    public static class Logs
    {
        public static Log Create(LogCategory category, string name)
        {
            return new Log(category.ToString(), name);
        }
    }
}

namespace Dissonance.Audio.Playback
{
    /// <summary>
    /// Stub for VoicePlayback. Does nothing without Dissonance.
    /// </summary>
    public class VoicePlayback : MonoBehaviour
    {
        public string PlayerName { get; set; } = "";
        public float Amplitude { get; set; }
    }
}

namespace Dissonance.Networking
{
    /// <summary>
    /// Stub for ICommsNetworkState.
    /// </summary>
    public interface ICommsNetworkState { }

    /// <summary>
    /// Stub for BaseCommsNetwork.
    /// </summary>
    public abstract class BaseCommsNetwork<TServer, TClient, TConn, TServerParam, TClientParam> : MonoBehaviour, ICommsNetworkState
        where TServer : class
        where TClient : class
    {
        public bool IsInitialized { get; protected set; }
        public NetworkMode Mode { get; protected set; }
        public TServer Server { get; protected set; }
        public TClient Client { get; protected set; }

        protected abstract TClient CreateClient(TClientParam connectionParameters);
        protected abstract TServer CreateServer(TServerParam connectionParameters);

        protected void RunAsHost(TServerParam serverParam, TClientParam clientParam) { Mode = NetworkMode.Host; }
        protected void RunAsDedicatedServer(TServerParam serverParam) { Mode = NetworkMode.DedicatedServer; }
        protected void RunAsClient(TClientParam clientParam) { Mode = NetworkMode.Client; }
        protected void Stop() { Mode = NetworkMode.None; }

        protected virtual void Update() { }
    }

    /// <summary>
    /// Stub for BaseClient.
    /// </summary>
    public abstract class BaseClient<TServer, TClient, TConn>
        where TServer : class
        where TClient : class
    {
        protected BaseClient(ICommsNetworkState network) { }

        public virtual void Connect() { }
        public virtual void Disconnect() { }

        public void NetworkReceivedPacket(ArraySegment<byte> data) { }
        protected virtual void ReadMessages() { }
        protected virtual void SendReliable(ArraySegment<byte> packet) { }
        protected virtual void SendUnreliable(ArraySegment<byte> packet) { }
        protected void Connected() { }
    }

    /// <summary>
    /// Stub for BaseServer.
    /// </summary>
    public abstract class BaseServer<TServer, TClient, TConn>
        where TServer : class
        where TClient : class
    {
        public virtual void Connect() { }
        public virtual void Disconnect() { }

        public void NetworkReceivedPacket(TConn client, ArraySegment<byte> data) { }
        protected void ClientDisconnected(TConn client) { }
        protected virtual void ReadMessages() { }
        protected virtual void SendReliable(TConn destination, ArraySegment<byte> packet) { }
        protected virtual void SendUnreliable(TConn destination, ArraySegment<byte> packet) { }
    }
}

namespace Dissonance.Datastructures
{
    /// <summary>
    /// Stub for ConcurrentPool.
    /// </summary>
    public class ConcurrentPool<T>
    {
        private readonly Func<T> _factory;
        private readonly Queue<T> _pool = new Queue<T>();

        public ConcurrentPool(int initialCount, Func<T> factory)
        {
            _factory = factory;
            for (int i = 0; i < initialCount; i++)
                _pool.Enqueue(factory());
        }

        public T Get()
        {
            return _pool.Count > 0 ? _pool.Dequeue() : _factory();
        }

        public void Put(T item)
        {
            _pool.Enqueue(item);
        }
    }
}

namespace Dissonance.Extensions
{
    /// <summary>
    /// Stub extension methods for Dissonance.
    /// </summary>
    public static class DissonanceExtensions
    {
        public static bool IsServerEnabled(this NetworkMode mode)
        {
            return mode == NetworkMode.Host || mode == NetworkMode.DedicatedServer;
        }

        public static bool IsClientEnabled(this NetworkMode mode)
        {
            return mode == NetworkMode.Host || mode == NetworkMode.Client;
        }

        public static ArraySegment<byte> CopyToSegment(this ArraySegment<byte> source, byte[] destination)
        {
            if (source.Array != null)
            {
                Buffer.BlockCopy(source.Array, source.Offset, destination, 0, source.Count);
            }
            return new ArraySegment<byte>(destination, 0, source.Count);
        }
    }
}

namespace Dissonance
{
    /// <summary>
    /// Stub for Unit type (void equivalent for generics).
    /// </summary>
    public struct Unit
    {
        public static readonly Unit None = default;
    }
}
