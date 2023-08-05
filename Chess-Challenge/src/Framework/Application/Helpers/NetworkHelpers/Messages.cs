using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChessChallenge.Application.NetworkHelpers;


public struct ServerHelloMsg : ISerializableMessage
{
    public string ProtocolVersion;
    public string ServerVersion;
    public string SessionId;
    public long ServerTime;

    public byte RefCode => 0;

    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(ProtocolVersion);
        writer.Write(ServerVersion);
        writer.Write(SessionId);
        writer.Write(ServerTime);
    }
    
    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        ProtocolVersion = reader.ReadString();
        ServerVersion = reader.ReadString();
        SessionId = reader.ReadString();
        ServerTime = reader.ReadInt64();
    }
}

public struct ClientHelloMsg : ISerializableMessage
{
    public string ProtocolVersion;
    public string ClientVersion;
    public string UserName; // This client's username

    // RoomId: No weird characters (maybe)
    public string RoomId; // Create or connect to this room  


    public byte RefCode => 1;

    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(ProtocolVersion);
        writer.Write(ClientVersion);
        writer.Write(UserName);
        writer.Write(RoomId);
    }
    
    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        ProtocolVersion = reader.ReadString();
        ClientVersion = reader.ReadString();
        UserName = reader.ReadString();
        RoomId = reader.ReadString();
    }
    
}


public struct Ack : ISerializableMessage
{
    public byte RefCode => 2;
    public void SerializeIntoStream(Stream stream)
    {
        return;
    }

    public void ReadFromStream(Stream stream)
    {
        return;
    }
}

public struct Reject : ISerializableMessage
{
    public byte RefCode => 3;
    public void SerializeIntoStream(Stream stream)
    {
        return;
    }

    public void ReadFromStream(Stream stream)
    {
        return;
    }
}


public struct ShutdownMsg : ISerializableMessage
{
    public byte RefCode => 4;
    public string Reason;
    
    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(Reason);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        Reason = reader.ReadString();
    }
}


public struct PingMsg : ISerializableMessage
{
    public byte RefCode => 5;
    public void SerializeIntoStream(Stream stream)
    {
        
    }

    public void ReadFromStream(Stream stream)
    {
        
    }
}


public struct GiveYourPrefs : ISerializableMessage
{
    public byte RefCode => 9;
    public void SerializeIntoStream(Stream stream)
    {
        
    }

    public void ReadFromStream(Stream stream)
    {
        
    }
}


public struct ClientPrefs : ISerializableMessage
{
    public byte RefCode => 10;

    public long PreferredClockMillis; // Time for each player
    
    public string StartFen; // The starting fen string for all games

    public int Games => 1; // For now only 1 is supported!
    
    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(PreferredClockMillis);
        writer.Write(StartFen);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        PreferredClockMillis = reader.ReadInt64();
        StartFen = reader.ReadString();
    }
}


public struct PlayerJoined : ISerializableMessage
{
    public byte RefCode => 20;

    public string? UserName; 
    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(UserName);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        UserName = reader.ReadString();
    }
}



public struct PlayerLeft : ISerializableMessage
{
    public byte RefCode => 21;
    public void SerializeIntoStream(Stream stream)
    {
        
    }

    public void ReadFromStream(Stream stream)
    {
        
    }
}



public struct GetReady : ISerializableMessage
{
    public byte RefCode => 25;

    public bool IsWhite;
    public long ClockTimeMillis;
    public string GameStartFen;

    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(IsWhite);
        writer.Write(ClockTimeMillis);
        writer.Write(GameStartFen);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        IsWhite = reader.ReadBoolean();
        ClockTimeMillis = reader.ReadInt64();
        GameStartFen = reader.ReadString();
    }
}

public struct IsReady : ISerializableMessage
{
    public byte RefCode => 26;
    public void SerializeIntoStream(Stream stream)
    {
        
    }

    public void ReadFromStream(Stream stream)
    {
        
    }
}


public struct GameStart : ISerializableMessage
{
    public byte RefCode => 30;
    public void SerializeIntoStream(Stream stream)
    {
        
    }

    public void ReadFromStream(Stream stream)
    {
        
    }
}


public struct MoveMessage : ISerializableMessage
{
    public string MoveName;
    public long YourClockElapsed; // Time left on clock for mybot
    public long OpponentClockElapsed; // Time left on clock for networked
    
    public byte RefCode => 40;

    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(MoveName);
        writer.Write(YourClockElapsed);
        writer.Write(OpponentClockElapsed);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        MoveName = reader.ReadString();
        YourClockElapsed = reader.ReadInt64();
        OpponentClockElapsed = reader.ReadInt64();
    }
}

public struct GameOver : ISerializableMessage
{
    public byte RefCode => 50;

    public string Reason;
    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(Reason);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        Reason = reader.ReadString();
    }
}

public struct TimeOut : ISerializableMessage
{
    public byte RefCode => 99;
    public bool ItWasYou;
    public void SerializeIntoStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write(ItWasYou);
    }

    public void ReadFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8,  true);
        
        ItWasYou = reader.ReadBoolean();
    }
}

public static class MessageHelper
{
    /// <summary>
    /// Returning null means that the read failed (without throwing any exceptions) for some reason!
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static ISerializableMessage? DecodeNextMessage(this Stream stream, CancellationToken? cancellationToken = null)
    {
        var buffer = new byte[1];

        int bytesRead;
        if (cancellationToken is not null)
        {
            try
            {
                bytesRead = stream.ReadAsync(buffer, 0, 1, cancellationToken.Value).Result;
            }
            catch (TaskCanceledException)
            {
                bytesRead = 0;
            }
            catch (AggregateException e)
            {
                if (e.InnerExceptions?.Any(x => x is OperationCanceledException) ?? false)
                {
                    bytesRead = 0;
                }

                if(cancellationToken.Value.IsCancellationRequested)
                    return null;
                throw;
            }
            catch
            {
                if(cancellationToken.Value.IsCancellationRequested)
                    return null;
                throw; // TODO: Handle exceptions later
            }
        }
        else
            bytesRead = stream.Read(buffer, 0, 1);

        if (cancellationToken is not null && cancellationToken.Value.IsCancellationRequested && bytesRead != 1)
            return null;

        if (bytesRead != 1)
            return null;

        if (MessageTypeLookup.TryGetValue(buffer[0], out var obj) == false)
            return null;
        
        obj.ReadFromStream(stream);

        return obj;
    }

    public static void EncodeMessage(this Stream stream, ISerializableMessage message)
    {
        //TODO: Merge this into SerializeIntoStream function or get rid of the SerializeIntoStream function
        stream.Write(new[]{message.RefCode}, 0, 1);
        
        message.SerializeIntoStream(stream);
    }


    private static readonly Dictionary<byte, ISerializableMessage> MessageTypeLookup = new()
    {
        [new ServerHelloMsg().RefCode] = new ServerHelloMsg(),
        [new ClientHelloMsg().RefCode] = new ClientHelloMsg(),
        [new Ack().RefCode] = new Ack(),
        [new Reject().RefCode] = new Reject(),
        [new ShutdownMsg().RefCode] = new ShutdownMsg(),
        [new GiveYourPrefs().RefCode] = new GiveYourPrefs(),
        [new ClientPrefs().RefCode] = new ClientPrefs(),
        [new PlayerJoined().RefCode] = new PlayerJoined(),
        [new PlayerLeft().RefCode] = new PlayerLeft(),
        [new GetReady().RefCode] = new GetReady(),
        [new IsReady().RefCode] = new IsReady(),
        [new GameStart().RefCode] = new GameStart(),
        [new MoveMessage().RefCode] = new MoveMessage(),
        [new GameOver().RefCode] = new GameOver(),
        [new TimeOut().RefCode] = new TimeOut(),
        [new PingMsg().RefCode] = new PingMsg(),
    };
}



// Always both server and client should wait for ack or another packet after sending a message. Do not proceed to send two messages at once
// make all the below ISeri..Message like IsReady dont use enums
public enum MessageType
{
    // Use two threads. One for sending and one for receiving.
    // Use ReadAsync to listen for any messages and occasionally check if the buffer has any complete data. (use a bool flag or something)
    // Also use cancellation token to cancel read when sending something.
    // Make sure not to interrupt processing a message! Only pass cancellation token when waiting for new content and dont quit if already in the middle of receinving one.
    
    ServerHello, // Contain a timestamp. 
    ClientHello, // Should contain a username too
    
    //This is based on when server hello was sent. All time sent to this client will be based on an offset of that timestamp.
    // So, the reference starting timestamp is different for different clients.
    TimeSyncRequest,
    TimeSyncResponse,
    
    // No ack messages for the above
    
    // Wait for 2 minute for an ack message. If not received, connection is lost!
    
    ClientPrefs, // Prefs sent by each client. Server sends back final settings in game start. Needs ack
    Ack, // This is used as a place holder to say nothing
    PlayerJoined, // needs ack
    GameSettings, // Should contain max clocks for the players and the room id. Needs ack from all clients 
    PlayerLeft, // Needs ack from the other client
    
    // Send a shutdown signal from the server if two clients aren't compatible. The shutdown should have a reason attached
    
    NotifyWhenReady, // From server to clients: No ack needed
    Ready, // No ack
    GameStart, // From server to clients: No ack needed!
    MoveMade, // should contain a bool for lastMove. Usually should be sending a gameover packet from the server after ack to all clients
    // MoveMade should contain the exact time left for the player.
    Shutdown, // send before client or server disconnect. Should contain a reason. No ACK needed!
    GameOver, // This should contain the reason
    
    QueryTimeLeftOnClock, // Client can send this to query the server for time left on clock for a player.
    TimeLeftOnClock, // Server send this to the client requested. Contains clock for all players and the timestamp it was recorded (Based on when connection with that client started)

    // IMP: Server shouldn't block on other clients when waiting for ack from one client
}

// Reserve a room id for matchmaking. After two players are connected send them a new room id

// Update  UI to not show the board unless a game is going on. Show some text when waiting for an opponenet