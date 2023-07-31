using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChessChallenge.API;
using ChessChallenge.Application;
using ChessChallenge.Application.NetworkHelpers;
using Timer = ChessChallenge.API.Timer;

namespace ChessChallenge.Example;

public class NetworkedBot : IChessBot
{
    public const string ROOM_ID = "Room1";

    private GameSettings? _gameSettings = null;
    private bool _inGame = false;
    
    private Move _latestKnownMove = Move.NullMove;
    private bool _lastMoveReceived;

    public void StartGame(Board board)
    {
        var cancelSource = new CancellationTokenSource();

        if (!(ServerConnectionHelper.TcpClient?.Connected ?? false))
        {
            ConsoleHelper.Log("Not Connected to server!", true, ConsoleColor.Red);
            return;
        }
        
        // Reset time until other player joins
        Task.Run(() =>
        {
            while (true)
            {
                if(cancelSource.IsCancellationRequested)
                    break;
                SetSecondsElapsed();
                Task.Delay(2000, cancelSource.Token).Wait(cancelSource.Token);
            }
        }, cancelSource.Token);

        GameStart start;
        try
        {

            _gameSettings ??= ServerConnectionHelper.ReadMessage<GameSettings>();

            if (_gameSettings.Value.IsWhite != board.IsWhiteToMove)
            {
                throw new Exception("Game state not synced!");
            }

            Console.WriteLine(board.IsWhiteToMove ? "Remote Player is White" : "Remote Player is Black");

            ServerConnectionHelper.SendMessage(new IsReady
            {
                isReady = true
            });

            start = ServerConnectionHelper.ReadMessage<GameStart>(); // Wait for start

            Console.WriteLine("Game Started!");
            cancelSource.Cancel();
        }
        catch (Exception e) when (e is IOException or SocketException)
        {
            ConsoleHelper.Log("Connection with the server ended!", true, ConsoleColor.Red);
            ServerConnectionHelper.Disconnect();
            return;
        }
        catch (Exception e)
        {
            ConsoleHelper.Log($"Failed to start game due to exception: {e}", isError: true, ConsoleColor.Red);
            ServerConnectionHelper.Disconnect();
            return;
        }
        
        SetSecondsElapsed(new TimeSpan(DateTime.UtcNow.Ticks - start.Timestamp).TotalSeconds);

        _inGame = true;
        
    }
    
    public Move Think(Board board, Timer timer)
    {
        if(!_inGame)
            StartGame(board);

        if (!_inGame)
        {
            ConsoleHelper.Log("Failed to start game!");
            return Move.NullMove;
        }

        if (board.GameMoveHistory.Length > 0)
        {
            var opponentsMove = board.GameMoveHistory.Last();

            try
            {
                SendMove(opponentsMove, false);
            }
            catch (Exception e)
            {
                ConsoleHelper.Log("Connection lost!", true, ConsoleColor.Red);
                Task.Delay(timer.MillisecondsRemaining + 100); // wait for us to timeout so as to not produce any errors
                return Move.NullMove;
            }
        }

        try
        {
            _latestKnownMove = WaitForNetworkMoveReceive(board);
        }
        catch (Exception e)
        {
            ConsoleHelper.Log("Connection lost!", true, ConsoleColor.Red);
            Task.Delay(timer.MillisecondsRemaining + 100);
            return Move.NullMove;
        }

        if (_latestKnownMove != Move.NullMove) 
            return _latestKnownMove; 
            
        // NullMove means a timeout so we wait until timer runs out
        Task.Delay(timer.MillisecondsRemaining + 1000).Wait(); // wait 1 extra second just incase
        return Move.NullMove; 
    }

    public void GameOver(Board board)
    {
        if (_lastMoveReceived || !_inGame)
        {
            return;
        }
        
        var lastMove = board.GameMoveHistory.Last();

        try
        {

            if (lastMove.ToString() == _latestKnownMove.ToString())
            {
                // This is probably a timeout so send a null move
                SendMove(Move.NullMove, true);
                return;
            }

            SendMove(lastMove, true);

        }
        catch (NullReferenceException e)
        {
            ConsoleHelper.Log($"Null reference error {e}",false, ConsoleColor.Red);
            return;
        }
        catch (Exception)
        {
            ConsoleHelper.Log("Connection lost!", true, ConsoleColor.Red);
            return;
        }
        
        Console.WriteLine($"Is Checkmate: {board.IsInCheckmate()}");
        Console.WriteLine($"Is Draw: {board.IsDraw()}");
    }
    
    
    private static void SendMove(Move move, bool isLastMoveInGame)
    {
        if (move == Move.NullMove)
        {
            ServerConnectionHelper.SendMessage(new MoveMessage
            {
                LastMove = isLastMoveInGame,
                MoveName = "",
            });
            return;
        }
        ServerConnectionHelper.SendMessage(new MoveMessage
        {
            LastMove = isLastMoveInGame,
            MoveName = move.ToString().Replace("Move: '", "").Replace("'", "").Trim(),
        });
    }

    private Move WaitForNetworkMoveReceive(Board board)
    {
        var msg = ServerConnectionHelper.ReadMessage<MoveMessage>(); // Hopefully will wait

        if(msg.MoveName == "")
            return Move.NullMove;
        
        var move = new Move(msg.MoveName, board);
            
        if (msg.LastMove)
            _lastMoveReceived = true;
            
        return move;
    }

    private static FieldInfo? secElapsedField = null;
    private static void SetSecondsElapsed(double seconds = 0)
    {
        if(secElapsedField == null)
            secElapsedField = typeof(ChessPlayer).GetField("secondsElapsed", System.Reflection.BindingFlags.NonPublic
                                                                        | System.Reflection.BindingFlags.Instance);
        
        var playerToMove = ChallengeController.Instance.PlayerToMove;
        
        secElapsedField.SetValue(playerToMove,seconds);
    }
}