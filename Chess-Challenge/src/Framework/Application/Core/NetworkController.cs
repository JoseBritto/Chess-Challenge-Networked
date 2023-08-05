using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.Application.NetworkHelpers;
using ChessChallenge.Chess;
using ChessChallenge.Example;
using Ping = System.Net.NetworkInformation.Ping;

namespace ChessChallenge.Application;

public class NetworkController
{
    public static NetworkController Instance;

    public bool NetworkConnectionReady { get; private set; }

    public NetworkState State { get; private set; }
    private Stream _stream;
    
    private Task? _readingTask = null;
    private ISerializableMessage? unReadMessage = null;
    private CancellationTokenSource? _streamReadCancellationSource;
    
    private Task? _writingTask = null;

    private TcpClient? _client;
    private GetReady _readyInfo;
    private bool _expectingAnAck;

    public MoveMessage? NextMove { get; set; } = null;

    public string? OpponentName { get; private set; }
    public bool OpponentIsWhite { get; private set; }
    
    public NetworkController()
    {
        Instance = this;
        State = NetworkState.NotConnected;
    }

    // DON'T CALL IF ALREADY CONNECTED!
    public async Task StartAsync(Action? onSuccess, Action? onFail)
    {
        State = NetworkState.Connecting;
        try
        {
            _client = await ServerConnectionHelper.ConnectToServerAsync(Settings.ServerHostname, Settings.ServerPort);
        }
        catch (Exception e)
        {
            ConsoleHelper.Log($"Failed to connect to server due to exception {e}!");
            State = NetworkState.NotConnected;
            onFail?.Invoke();
            return;
        }

        if (_client is null || _client.Connected == false)
        {
            State = NetworkState.NotConnected;
            onFail?.Invoke();
            return;    
        }

        try
        {
            var wasSuccess = ServerConnectionHelper.InitializeCommunication(_client, NetworkedBot.ROOM_ID, 
                Settings.NetworkingProtocolVersion, out var shutdownMsg);

            if (wasSuccess == false)
            {
                if (shutdownMsg is not null)
                {
                    ConsoleHelper.Log($"Server disconnected with reason: {shutdownMsg.Value.Reason}");
                }
                State = NetworkState.NotConnected;
                onFail?.Invoke();
                return;
            }
        }
        catch (Exception e)
        {
            ConsoleHelper.Log($"Failed to initialize connection to server due to exception {e}");
            State = NetworkState.NotConnected;
            onFail?.Invoke();
            return;
        }

        _stream = _client.GetStream();
        State = NetworkState.WaitingForOpponentToJoin;
        
        NetworkConnectionReady = true;
        onSuccess?.Invoke();

        StartReadingThread();
    }


    /// <summary>
    /// Should be called every frame on a new thread. Also lock this so no multithreaded madness
    /// </summary>
    public void Update()
    {
        if(NetworkConnectionReady == false || State == NetworkState.NotConnected)
            return;
        
        if(!HasNewMessage())
            return;

        var msg = GetNextMessage();

        switch (msg)
        {
            case PingMsg:
                SendMessage(new PingMsg());
                ConsoleHelper.Log("Ping!");
                break;
            case Ack:
                if(_expectingAnAck)
                    break;
                else
                {
                    ConsoleHelper.Log("Unexpected ACK. Ignoring");
                    break;
                }
            case Reject:
                ConsoleHelper.Log("Got a reject. Don't know what to do. Ignoring...");
                break;
            
            case GiveYourPrefs:
                SendMessage(new ClientPrefs
                {
                    StartFen = ChallengeController.Instance.botMatchStartFens[0],
                    PreferredClockMillis = Settings.GameDurationMilliseconds
                });
                _expectingAnAck = true;
                break;
            
            case MoveMessage move:
                ChallengeController.Instance.PlayerToMove.secondsElapsed = ((double)move.OpponentClockElapsed) / 1000.0d;
                ChallengeController.Instance.PlayerNotOnMove.secondsElapsed = ((double)move.YourClockElapsed) / 1000.0d;
                NextMove = move;
                //no ack for moves
                break;
            case PlayerJoined player:
                OpponentName = player.UserName;
                SendMessage(new Ack()); // Ack the new player
                State = NetworkState.WaitingForGameStart;
                break;
            case ShutdownMsg shutdownMsg:
                ConsoleHelper.Log($"Remote shutdown: {shutdownMsg.Reason}");
                Disconnect(false);
                State = NetworkState.NotConnected;
                ChallengeController.Instance.EndGame(GameResult.DrawByArbiter, false, false);
                break;
            case PlayerLeft:
                ConsoleHelper.Log("Player left");
                OpponentName = null;
                State = NetworkState.WaitingForOpponentToJoin;
                SendMessage(new Ack());
                break; // TODO: End ay ongoing games 
            
            case GetReady readyInfo:
                State = NetworkState.WaitingForGameStart;
                OpponentIsWhite = !readyInfo.IsWhite; // If server considers us as white that means the NetworkBot (opponent) should be black
                _readyInfo = readyInfo;
                SendMessage(new IsReady());
                break;
            case GameStart:
                if(OpponentIsWhite)
                    ChallengeController.Instance.StartNewBotMatch(
                        ChallengeController.PlayerType.NetworkedBot, 
                        ChallengeController.PlayerType.MyBot, 
                        _readyInfo.GameStartFen, (int) _readyInfo.ClockTimeMillis);
                else
                    ChallengeController.Instance.StartNewBotMatch(
                        ChallengeController.PlayerType.MyBot, 
                        ChallengeController.PlayerType.NetworkedBot, 
                        _readyInfo.GameStartFen, (int) _readyInfo.ClockTimeMillis);
                // No ack needed for game start
                State = NetworkState.GameOnGoing;
                break;
            
            case NetworkHelpers.TimeOut to:
                if (to.ItWasYou)
                {
                    if (ChallengeController.Instance.PlayerToMove == ChallengeController.Instance.PlayerWhite
                        && ChallengeController.Instance.PlayerWhite.Bot is MyBot)
                        ChallengeController.Instance.EndGame(GameResult.WhiteTimeout);

                    else if (ChallengeController.Instance.PlayerToMove == ChallengeController.Instance.PlayerBlack
                             && ChallengeController.Instance.PlayerBlack.Bot is MyBot)
                    {
                        ChallengeController.Instance.EndGame(GameResult.BlackTimeout);
                    }
                }
                else
                {
                    if (ChallengeController.Instance.PlayerToMove == ChallengeController.Instance.PlayerWhite
                        && ChallengeController.Instance.PlayerWhite.Bot is MyBot)
                        ChallengeController.Instance.EndGame(GameResult.BlackTimeout);

                    else if (ChallengeController.Instance.PlayerToMove == ChallengeController.Instance.PlayerBlack
                             && ChallengeController.Instance.PlayerBlack.Bot is MyBot)
                    {
                        ChallengeController.Instance.EndGame(GameResult.WhiteTimeout);
                    }
                }

                break;
        }
    }

    private void StartReadingThread()
    {
        _streamReadCancellationSource = new CancellationTokenSource();
        unReadMessage = null;
        _readingTask = Task.Run(() =>
        {
            try
            {
                unReadMessage = _stream.DecodeNextMessage(_streamReadCancellationSource.Token);
            }
            catch
            {
                unReadMessage = null;
                //ignored
            }
        });
    }

    public bool HasNewMessage() => unReadMessage != null;

    /// <summary>
    /// If this returns null, there is some problem with the connection. BLOCKS UNTIL NEXT MESSAGE IS RECEIVED!
    /// </summary>
    /// <returns></returns>
    public ISerializableMessage? GetNextMessage()
    {
        if (unReadMessage is null)
        {
            if (_readingTask is not null)
            {
                _readingTask.Wait();
                return unReadMessage!;
            }
            
            ConsoleHelper.Log("Reading Task was null!", true, ConsoleColor.Red);
            return null;
        }

        var msg = unReadMessage;
        unReadMessage = null;
        StartReadingThread(); // Start next read
        return msg;
    }

    public void SendMessage(ISerializableMessage message)
    {
        _streamReadCancellationSource.Cancel();

        _readingTask.Wait();

        _stream.EncodeMessage(message);
        
        StartReadingThread(); // Wait for next message
    }
    
    public void Disconnect(bool sendShutdownSignal, string reason = "Unknown")
    {
        if (sendShutdownSignal)
        {
            try
            {
                SendMessage(new ShutdownMsg
                {
                    Reason = reason
                });
            }
            catch
            {
                //ignored
            }
        }
        
        if (_streamReadCancellationSource is not null && _streamReadCancellationSource.IsCancellationRequested == false)
            _streamReadCancellationSource.Cancel();
        try
        {
            _readingTask?.Wait();
            _readingTask?.Dispose();
        }
        catch
        {
            //ignored
        }
        
        _streamReadCancellationSource?.Dispose();

        ServerConnectionHelper.Disconnect(_client);
        Instance = new NetworkController(); // Create a new instance

        OpponentName = null;
        State = NetworkState.NotConnected;
    }
    
    
    public void GameOver(GameResult reason)
    {
        // We dont send anything on timeouts
        if (reason != GameResult.WhiteTimeout && reason != GameResult.BlackTimeout)
        {
            SendMessage(new GameOver
            {
                Reason = reason.ToString()
            });
        }
        Disconnect(true, $"GameOver: {reason}");
    }

    public void JoinRoomPressed()
    {
        if(State == NetworkState.NotConnected)
            Task.Run(() => StartAsync(null, null).Wait());
        
        //TODO: Make it so that switching game mode disconnects gracefully from server automatically and resets network state.
    }

    public void UnFocused()
    {
        if (State != NetworkState.NotConnected)
            Disconnect(true, "Player Quit");
    }

    public enum NetworkState
    {
        NotConnected,
        Connecting,
        JoinedRoom,
        WaitingForOpponentToJoin,
        GameOnGoing,
        WaitingForGameStart,
    }

}

