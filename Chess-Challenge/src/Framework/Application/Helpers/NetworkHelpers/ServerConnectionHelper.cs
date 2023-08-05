using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.API;
using ChessChallenge.Example;
using static ChessChallenge.Application.ConsoleHelper;

namespace ChessChallenge.Application.NetworkHelpers;

public static class ServerConnectionHelper
{
    public static bool StartsOffWhite { get; private set; }

    public static async Task<TcpClient?> ConnectToServerAsync(string host, int port)
    {

        Log($"Connecting to {host}:{port}");
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port);
        }
        catch (Exception e)
        {
            Log($"Failed to connect to {host} on port {port} due to {e.ToString()}");
            return null;
        }
        
        Log("Connection established!");
        
        return client;
    }

    public static void Disconnect(TcpClient? client)
    {
        try
        {
            client?.Close();
        }
        catch
        {
            //ignored
        }
    }

    private static bool VerifyServer(ServerHelloMsg serverHelloMsg, string protocolVersion) 
        => serverHelloMsg.ProtocolVersion == protocolVersion;
    
    
    public static bool InitializeCommunication(TcpClient client, string roomId, string protocolVersion, out ShutdownMsg? shutdownMsg)
    {
        var stream = client.GetStream();
        shutdownMsg = null;
        
        try
        {
            Log("Waiting for Server's Hello");

            var msg = stream.DecodeNextMessage();
            if (msg is not ServerHelloMsg helloMsg)
            {
                Log($"Expected a ServerHelloMsg but got {msg.GetType()}", true, ConsoleColor.Red);
                return false;
            }
            
            var isCompatible = VerifyServer(helloMsg, protocolVersion);
            
            if (!isCompatible)
            {
                Log("Incompatible server version! Disconnecting...");
                //TODO: Properly shutdown connection by sending shutdown packet first
                client.Dispose();
                return false;
            }
            
            stream.EncodeMessage(new ClientHelloMsg
            {
                RoomId = roomId,
                ProtocolVersion = protocolVersion,
                ClientVersion = "0.1",
                UserName = NetworkedBot.UserName
            });
            
            Log("Connected!");

            msg = stream.DecodeNextMessage();

            switch (msg)
            {
                case Ack:
                    return true;
                case Reject:
                {
                    var cancelSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                    msg = stream.DecodeNextMessage(cancelSource.Token); // We wait for the next message for a minute.

                    if (msg is ShutdownMsg sMsg)
                        shutdownMsg = sMsg;

                    return false; // Return false anyway even if we don't recognise that packet. There's nothing else to do here
                }
                default:
                    // If the message was anything else or even null, we don't know how to handle it so we return false.
                    return false;
            }
        }
        catch (Exception e)
        {
            Log("Error occured while initialising client-server connection! Room probably full!", isError: true, ConsoleColor.Red);
            Log(e.ToString());
            client.Dispose();
            return false;
        }
    }


}