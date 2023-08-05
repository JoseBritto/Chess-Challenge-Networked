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
using ChessChallenge.Chess;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;
using Timer = ChessChallenge.API.Timer;

namespace ChessChallenge.Example;

public class NetworkedBot : IChessBot
{
    public const string ROOM_ID = "Room1";
    public static readonly string UserName = $"Bob 99";
    public Move Think(Board board, Timer timer)
    {
        while (NetworkController.Instance.NextMove == null)
        {
            Task.Delay(2).Wait();
        }
        var move = NetworkController.Instance.NextMove;
        NetworkController.Instance.NextMove = null;
        return new Move(move.Value.MoveName, board);
    }

    /*
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
*/
}
