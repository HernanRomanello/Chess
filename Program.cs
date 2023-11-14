using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;

public class Program
{
    public static int Main(string[] args)
    {
        ChessGame game = new ChessGame();
        return game.Start();
    }
}
public class ChessGame
{
    private string[,] board;
    private bool isWhiteTurn;
    private bool isGameOver;
    private int pieceRow;
    private int moveCol;
    private string validInput;
    private int[] enPassanWhite;
    private int[] enPassanBlack;
    private bool whiteKingHasMoved;
    private bool blackKingHasMoved;
    private bool whiteLeftRookHasMoved;
    private bool blackLeftRookHasMoved;
    private bool whiteRightRookHasMoved;
    private bool blackRightRookHasMoved;
    private bool isCheck;
    private bool checkmate;
    private string[] boardHistory;
    private int moveRule;
    private bool deadPositionDraw;
    private bool stalemate;
    private bool drawThreefoldRepetition;

    public ChessGame()
    {
         this.board = new string[8, 8]
        {
            { "BR", "BN", "BB", "BQ", "BK", "BB", "BN", "BR" },
            { "BP", "BP", "BP", "BP", "BP", "BP", "BP", "BP" },
            { "  ", "  ", "  ", "  ", "  ", "  ", "  ", "  " },
            { "  ", "  ", "  ", "  ", "  ", "  ", "  ", "  " },
            { "  ", "  ", "  ", "  ", "  ", "  ", "  ", "  " },
            { "  ", "  ", "  ", "  ", "  ", "  ", "  ", "  " },
            { "WP", "WP", "WP", "WP", "WP", "WP", "WP", "WP" },
            { "WR", "WN", "WB", "WQ", "WK", "WB", "WN", "WR" },
        };
        this.isWhiteTurn = true;
        this.isGameOver = false;
        this.pieceRow = 0;
        this.moveCol = 0;
        this.validInput = "";
        this.enPassanWhite = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        this.enPassanBlack = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        this.whiteKingHasMoved = false;
        this.blackKingHasMoved = false;
        this.whiteLeftRookHasMoved = false;
        this.blackLeftRookHasMoved = false;
        this.whiteRightRookHasMoved = false;
        this.blackRightRookHasMoved = false;
        this.isCheck = false;
        this.checkmate = false;
        this.boardHistory = new string[100];
        this.moveRule = 0;
        this.deadPositionDraw = false;
        this.stalemate = false;
        this.drawThreefoldRepetition = false;
    }
    public int Start()
    {
        while (!isGameOver)
        {
            string player = isWhiteTurn ? "Black" : "White";
            string message = "\nCheckmate! " + player + " player wins!";
            if (isCheck) { checkmate = Checkmate(board, isWhiteTurn, isCheck, out checkmate); }
            printMessage(!checkmate && !stalemate && !deadPositionDraw && !drawThreefoldRepetition, "\nYou can propose a draw by typing 'draw' and pressing 'ENTER'.\n");
            deadPositionDraw = deadPosition(board, isGameOver);
            PrintBoard(board, isWhiteTurn, checkmate, deadPositionDraw, stalemate, drawThreefoldRepetition);
            if (checkmate) { return updateCheckmate(isWhiteTurn, message); }
            if (deadPositionDraw && !checkmate || stalemate && !checkmate || drawThreefoldRepetition && !checkmate) { return endGame(out isGameOver, 0, ""); }
            string userInput = GetUserInput();
            userInput = editedInput(userInput);
            if (userInput == "DRAW")
            {
                { PrintToScreen("\n" + (isWhiteTurn ? "White" : "Black") + " proposes a draw, type 'yes' to agree, or any other key to decline, then press 'ENTER.'"); }
                string answer = GetUserInput();
                answer = editedInput(answer);
                if (answer == "YES") { return endGame(out isGameOver, 0, "\nThe game ends in a draw due to mutual agreement."); }
            }
            if (IsValidInput(userInput))
            {
                if (IsValidMove(isWhiteTurn, board, userInput))
                {
                    int currentRow, currentCol, targetRow, targetCol;
                    ParseChessMove(userInput, out currentRow, out currentCol, out targetRow, out targetCol);
                    string piece = setPiece(board, currentRow, currentCol);
                    object pieceObject = setPieceObject(piece, currentRow, currentCol, targetRow, targetCol, isWhiteTurn,whiteRightRookHasMoved,
                    blackRightRookHasMoved,whiteLeftRookHasMoved,blackLeftRookHasMoved);
                    if (pieceObject is Square)
                    {
                        Square pieceClass = (Square)pieceObject;
                        bool IsValidMove = pieceClass.canReachTarget(currentRow, currentCol, piece, targetRow, targetCol, isWhiteTurn, board);
                        bool isOccupied = squareIsOccupied(board, targetRow, targetCol);
                        moveRule = fiftyMoveRule(board, moveRule, targetRow, targetCol, pieceClass, IsValidMove);
                        if (moveRule == 100 && !checkmate) { return endGame(out isGameOver, 0, ""); }
                        IsValidMove = isvalidPawnMove(currentCol, targetCol, pieceClass, IsValidMove, isOccupied);
                        updateEnPassantMoves(isWhiteTurn, enPassanWhite, enPassanBlack);
                        if (pieceClass is Pawn) { IsValidMove = pawnProperties(isWhiteTurn, currentRow, currentCol, targetRow, targetCol, pieceClass, IsValidMove); }
                        IsValidMove = checkPath(board, isWhiteTurn, currentRow, currentCol, targetRow, targetCol, IsValidMove);
                        if (pieceClass is King) { IsValidMove = kingProperties(board, isWhiteTurn, whiteKingHasMoved, blackKingHasMoved, whiteRightRookHasMoved, blackRightRookHasMoved, whiteLeftRookHasMoved, blackLeftRookHasMoved, currentRow, currentCol, targetRow, targetCol, pieceClass, IsValidMove); }
                        drawThreefoldRepetition = ThreefoldRepetition(boardHistory, board, whiteKingHasMoved, blackKingHasMoved, whiteRightRookHasMoved, blackRightRookHasMoved,
                           whiteLeftRookHasMoved, blackLeftRookHasMoved);
                        updateMove(board, ref isWhiteTurn, ref whiteKingHasMoved, ref blackKingHasMoved, ref whiteRightRookHasMoved, ref blackRightRookHasMoved, ref whiteLeftRookHasMoved, ref blackLeftRookHasMoved, isCheck, boardHistory, ref stalemate, ref drawThreefoldRepetition, currentRow, currentCol, targetRow, targetCol, pieceClass, IsValidMove);
                        if (isCheck ) { isCheck = GetOutOfCheck(board, ref isWhiteTurn, currentRow, currentCol, targetRow, targetCol, userInput); }
                    }
                }
                isCheck = IsCheck(board, isWhiteTurn);
                printMessage(isCheck, "\nCheck Alert! Your King is under threat. Immediate protection required.");
            }
            else { PrintToScreen("\nOops! Invalid move. Pick a legal move."); }
        }
        return -1;
    }
    private int updateCheckmate(bool isWhiteTurn, string message)
    {
        int result = isWhiteTurn ? 2 : 1;
        PrintToScreen(message);
        return result;
    }
    private bool isvalidPawnMove(int currentCol, int targetCol, Square pieceClass, bool IsValidMove, bool isOccupied)
    {
        if (isOccupied && pieceClass is Pawn && currentCol == targetCol)
        {
            IsValidMove = false;

        }

        if (!isOccupied && pieceClass is Pawn && currentCol != targetCol)
        {
            IsValidMove = false;

        }

        return IsValidMove;
    }
    private bool pawnProperties(bool isWhiteTurn, int currentRow, int currentCol, int targetRow, int targetCol, Square pieceClass, bool IsValidMove)
    {
        bool IsValidEnPassantMOVE = ((Pawn)pieceClass).enPassant(enPassanWhite, enPassanBlack, isWhiteTurn, board, currentRow, currentCol, targetRow, targetCol);
        if (IsValidEnPassantMOVE)
        {
            deletepiece(board, targetRow, targetCol, isWhiteTurn);
            IsValidMove = true;
        }

        return IsValidMove;
    }
    private bool kingProperties(string[,] board, bool isWhiteTurn, bool whiteKingHasMoved, bool blackKingHasMoved, bool whiteRightRookHasMoved, bool blackRightRookHasMoved, bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved, int currentRow, int currentCol, int targetRow, int targetCol, Square pieceClass, bool IsValidMove)
    {
        bool isSquareUnderAttack = isSquareUnderThreat(board, isWhiteTurn, board[targetRow, targetCol], targetRow, targetCol);
        if (isSquareUnderAttack)
        {
            IsValidMove = false;
        }
        bool IsvalidCastling = ((King)pieceClass).canCastle(currentRow, currentCol, targetRow, targetCol, isWhiteTurn, whiteKingHasMoved, blackKingHasMoved, board
        , whiteRightRookHasMoved, blackRightRookHasMoved, whiteLeftRookHasMoved, blackLeftRookHasMoved);
        if (IsvalidCastling) { IsValidMove = true; }

        return IsValidMove;
    }
    private bool checkPath(string[,] board, bool isWhiteTurn, int currentRow, int currentCol, int targetRow, int targetCol, bool IsValidMove)
    {
        if (IsValidMove)
        {
            bool PathClear = checkIfPathIsClear(board, currentRow, currentCol, targetRow, targetCol, isWhiteTurn);
            if (!PathClear)
            {
                IsValidMove = false;
            }
        }
                return IsValidMove;
    }
    private int endGame(out bool isGameOver, int result, string massage)
    {
        PrintToScreen(massage);
        isGameOver = true;
        return result;
    }
    private void updateMove(string[,] board, ref bool isWhiteTurn, ref bool whiteKingHasMoved, ref bool blackKingHasMoved, ref bool whiteRightRookHasMoved, ref bool blackRightRookHasMoved, ref bool whiteLeftRookHasMoved, ref bool blackLeftRookHasMoved, bool isCheck, string[] boardHistory, ref bool stalemate, ref bool drawThreefoldRepetition, int currentRow, int currentCol, int targetRow, int targetCol, Square pieceClass, bool IsValidMove)
    {
        if (IsValidMove && !isCheck)
        {
            movePiece(board, currentRow, currentCol, targetRow, targetCol);
            kingThatMove(isWhiteTurn, ref whiteKingHasMoved, ref blackKingHasMoved, pieceClass);
            rookThatMoved(isWhiteTurn, ref whiteRightRookHasMoved, ref blackRightRookHasMoved, ref whiteLeftRookHasMoved, ref blackLeftRookHasMoved, currentRow, currentCol, pieceClass);

            if (pieceClass is Pawn)
            {
                pawnPromotion(board, isWhiteTurn, targetRow, targetCol);
            }

            isWhiteTurn = switchTurn(isWhiteTurn);
            drawThreefoldRepetition = ThreefoldRepetition(boardHistory, board, whiteKingHasMoved, blackKingHasMoved, whiteRightRookHasMoved, blackRightRookHasMoved,
            whiteLeftRookHasMoved, blackLeftRookHasMoved);
            stalemate = Stalemate(board, isWhiteTurn);
        }
        else { printMessage(!isCheck, "\nError! Make sure you're following the allowed moves for the chess piece.\n"); }
    }
    private void printMessage(bool isCheck, string massage)
    {
        if (isCheck)
        {
            PrintToScreen(massage);
        }
    }
    private void kingThatMove(bool isWhiteTurn, ref bool whiteKingHasMoved, ref bool blackKingHasMoved, Square pieceClass)
    {
        if (pieceClass is King)
        {
            if (isWhiteTurn)
            {
                whiteKingHasMoved = true;
            }
            else
            {
                blackKingHasMoved = true;
            }
        }
    }
    private void rookThatMoved(bool isWhiteTurn, ref bool whiteRightRookHasMoved, ref bool blackRightRookHasMoved, ref bool whiteLeftRookHasMoved, ref bool blackLeftRookHasMoved, int currentRow, int currentCol, Square pieceClass)
    {
        if (pieceClass is Rook)
        {
            if (currentRow == 7 && currentCol == 0 && isWhiteTurn) {  whiteLeftRookHasMoved = true; }
            if (currentRow == 7 && currentCol == 7 && isWhiteTurn) {  whiteRightRookHasMoved = true;  }
            if (currentRow == 0 && currentCol == 0 && !isWhiteTurn) {  blackLeftRookHasMoved = true;   }
            if (currentRow == 0 && currentCol == 7 && !isWhiteTurn)   {   blackRightRookHasMoved = true;   }
        }
    }
    private void pawnPromotion(string[,] board, bool isWhiteTurn, int targetRow, int targetCol)
    {
        if (targetRow == 0 || targetRow == 7)
        {
            string choiceNew = "";
            printOptions();
            bool validChoice = false;
            string choice = GetUserInput();
            choiceNew = removeSpaces(choiceNew, choice);
            validChoice = IsValidNumericChoice(choiceNew, validChoice, choice);
            while (!validChoice)
            {
                PrintToScreen("Invalid choice! Choose a piece to promote your pawn to: 1. Queen 2. Rook 3. Bishop 4. Knight");
                choice = GetUserInput().Trim();
                choiceNew = "";
                choiceNew = removeSpaces(choiceNew, choice);
                validChoice = IsValidNumericChoice(choiceNew, validChoice, choice);
            }
            char player = isWhiteTurn ? 'W' : 'B';
            updatePiece(board, targetRow, targetCol, choiceNew, player);
        }
    }
    private bool IsValidNumericChoice(string choiceNew, bool validChoice, string choice)
    {
        if (!string.IsNullOrWhiteSpace(choiceNew) && choice.Length >= 1)
        {
            validChoice = choiceNew[0] == '1' || choiceNew[0] == '2' || choiceNew[0] == '3' || choiceNew[0] == '4';
        }

        return validChoice;
    }
    private string removeSpaces(string choiceNew, string choice)
    {
        for (int j = 0; j < choice.Length; j++)
        {
            if (choice[j] != ' ' && choice.Length >= 1)
            {
                choiceNew += choice[j];
            }
        }

        return choiceNew;
    }
    private void updatePiece(string[,] board, int targetRow, int targetCol, string choiceNew, char player)
    {
        switch (choiceNew)
        {
            case "1":
                board[targetRow, targetCol] = player + "Q";
                break;
            case "2":
                board[targetRow, targetCol] = player + "R";
                break;
            case "3":
                board[targetRow, targetCol] = player + "B";
                break;
            case "4":
                board[targetRow, targetCol] = player + "N";
                break;
            default:
                break;
        }
    }
    private void printOptions()
    {
        PrintToScreen("Choose a piece to promote your pawn to:");
        PrintToScreen(" 1. Queen");
        PrintToScreen(" 2. Rook");
        PrintToScreen(" 3. Bishop");
        PrintToScreen(" 4. Knight");
    }
    private bool GetOutOfCheck(string[,] board, ref bool isWhiteTurn, int currentRow, int currentCol, int targetRow, int targetCol,string userInput)
    {
        bool isCheck;
        string savePiece = board[targetRow, targetCol];
            ParseChessMove(userInput, out currentRow, out currentCol, out targetRow, out targetCol); 
          movePiece(board, currentRow, currentCol, targetRow, targetCol);
                    string piece = setPiece(board, targetRow, targetCol);
                    object pieceObject = setPieceObject(piece, currentRow, currentCol, targetRow, targetCol, isWhiteTurn,whiteRightRookHasMoved,
                    blackRightRookHasMoved,whiteLeftRookHasMoved,blackLeftRookHasMoved);
             Square pieceClass = (Square)pieceObject;
            bool IsValidMove = pieceClass.canReachTarget(currentRow, currentCol, piece, targetRow, targetCol, isWhiteTurn, board); 
        string king = isWhiteTurn ? "WK" : "BK";
        isCheck = IsCheck(board, isWhiteTurn);
        if(board[targetRow,targetCol]==king){
            if(currentCol == targetCol +2 ||currentCol == targetCol-2){ isCheck = true; }
        } 
        if (isCheck || !IsValidMove )
        {
            reverseMovePiece(board, currentRow, currentCol, targetRow, targetCol);
            board[targetRow, targetCol] = savePiece;
        }
        else{  isWhiteTurn = switchTurn(isWhiteTurn);   }
         return isCheck;
    }
    private int OpponentKingRow(string[,] board, bool isWhiteTurn)
    {
        string opponentKing = isWhiteTurn ? "BK" : "WK";

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] == opponentKing)
                {
                    return row;
                }
            }
        }

        return -1;
    }
    private int OpponentKingColumn(string[,] board, bool isWhiteTurn)
    {
        string opponentKing = isWhiteTurn ? "BK" : "WK";

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] == opponentKing)
                {
                    return col;
                }
            }
        }
        return -1;
    }
    private bool IsKingUnderThreat(string[,] board, bool isWhiteTurn, int currentRow, int currentCol, bool isCheck, int sumRow, int sumCol, int numberPiece1, int numberPiece2)
    {
        string[] opponentArray = isWhiteTurn ? new string[] { "BP", "BR", "BN", "BB", "BQ", "BK" } : new string[] { "WP", "WR", "WN", "WB", "WQ", "WK" };
        int targetRow, targetCol;
        int kingRow = OpponentKingRow(board, !isWhiteTurn);
        int kingCol = OpponentKingColumn(board, !isWhiteTurn);
        bool IsKingAtRisk = true;
        char player = isWhiteTurn ? 'W' : 'B';
       
        for (int i = currentRow, j = currentCol; i >= 0 && i < 8 && j >= 0 && j < 8; i += sumRow, j += sumCol)
        {
            if (board[i, j] == "  ") {  continue;  }
            else if (board[i, j] == opponentArray[numberPiece1] ||
                    board[i, j] == opponentArray[numberPiece2])
            {
                 currentCol = j; currentRow = i; 
                 for (int X = currentRow , Y = currentCol; X >= 0 && X < 8 && Y >= 0 && Y < 8; X -= sumRow, Y -= sumCol)
        {    
             if( Math.Abs(X - kingRow) > 0 || Math.Abs(Y- kingCol) > 0 ){  
              
            for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] != "  " && board[row, col][0] == player && board[row, col][1] != 'K' && board[row, col][1] != 'P' )
                {
                    int currentRow1 = row, currentCol1 = col;
                    Console.WriteLine(board[row, col]);
                      string piece = setPiece(board, currentRow1, currentCol1);
                    object pieceObject = setPieceObject(piece, currentRow1, currentCol1, X, Y, isWhiteTurn,whiteRightRookHasMoved,
                    blackRightRookHasMoved,whiteLeftRookHasMoved,blackLeftRookHasMoved);
             Square pieceClass = (Square)pieceObject;
                        bool IsValidMove = pieceClass.canReachTarget(currentRow1, currentCol1, board[currentRow1,currentCol1], X, Y, isWhiteTurn, board);
                    if(IsValidMove){IsValidMove = checkPath(board, isWhiteTurn, currentRow1, currentCol1, X, Y, IsValidMove);
                      if (IsValidMove) {   IsKingAtRisk = false; return false;  } }
                }
            }
        }
                }
        }
            }
        }
            if (!isCheck) {  IsKingAtRisk = false;  }
            return  IsKingAtRisk  ;
        }
    private bool Checkmate(string[,] board, bool isWhiteTurn, bool isCheck, out bool checkmate)
    {
        int kingRow = OpponentKingRow(board, !isWhiteTurn);
        int kingCol = OpponentKingColumn(board, !isWhiteTurn);
        char player = isWhiteTurn ? 'W' : 'B';
        bool validUp = kingRow > 0, validDown = kingRow < 7, validLeft = kingCol > 0, validRight = kingCol < 7
        , validUpLeft = kingRow > 0 && kingCol > 0, validUpRight = kingRow > 0 && kingCol < 7,
        validDownLeft = kingRow < 7 && kingCol > 0, validDownRight = kingRow < 7 && kingCol < 7;
          bool verticalUpTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validUp,-1,0,1,4,-1,0); 
          bool verticalDownTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validDown,1,0,1,4,1,0);  
            bool HorizontalRightTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validRight,0,1,1,4,0,1); 
          bool HorizontalLeftTreath = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validLeft,0,-1,1,4,0,-1); 
         bool diagonalUpLeftTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validUpLeft,-1,-1,3,4,-1,-1);  
          bool diagonalUpRightTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validUpRight,-1,1,3,4,-1,1); 
          bool diagonalDownLeftTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validDownLeft,1,-1,3,4,1,-1);  
         bool diagonalDownRightTreat = isDetermineThreatStatus(board, isWhiteTurn, isCheck, kingRow, kingCol, player, validDownRight,1,1,3,4,1,1); 
       
        checkmate = verticalUpTreat && verticalDownTreat && HorizontalRightTreat && HorizontalLeftTreath && diagonalUpRightTreat
        && diagonalUpLeftTreat && diagonalDownRightTreat && diagonalDownLeftTreat; 
        
        return   checkmate ;
    }
    private bool isDetermineThreatStatus(string[,] board, bool isWhiteTurn, bool isCheck, int kingRow, int kingCol, char player, bool validUp,
    int kingRowDiff , int kingColDiff , int pieceNumber1, int pieceNumber2,int kingRowDiff2 , int kingColDiff2)
    {
        bool verticalUpTreat=true ;
        if (validUp)
        {
            bool upBlocked = board[kingRow + kingRowDiff, kingCol + kingColDiff][0] == player;
            Console.WriteLine(upBlocked);
            if (upBlocked) { verticalUpTreat = true; }
            if (!upBlocked && verticalUpTreat)
            {
                bool upMove = isSquareUnderThreat(board, isWhiteTurn, board[kingRow + kingRowDiff, kingCol + kingColDiff], kingRow + kingRowDiff, kingCol + kingColDiff);
                if (!upMove) { verticalUpTreat = false; }
            }
        }
        else { verticalUpTreat = true; }
         if (verticalUpTreat && validUp) {verticalUpTreat =  IsKingUnderThreat(board, isWhiteTurn, kingRow + kingRowDiff, kingCol + kingColDiff, isCheck, kingRowDiff2,
                 kingColDiff2, pieceNumber1, pieceNumber2); }
        return verticalUpTreat;
    }
    private bool Stalemate(string[,] board, bool isWhiteTurn)
    {
        char player = isWhiteTurn ? 'W' : 'B';
        bool isStalemate = true;
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] != "  " && board[row, col][0] == player)
                {
                    int currentRow = row, currentCol = col;
                    isStalemate = IterateAndValidateMoves(board, isWhiteTurn, isStalemate, currentRow, currentCol);
                }
            }
        }
          if (isStalemate){ PrintToScreen("\nA draw is declared. Stalemate reached, both kings are safe, and neither player can make a legal move.\n");}
        return isStalemate;
    }
    private bool IterateAndValidateMoves(string[,] board, bool isWhiteTurn, bool isStalemate, int currentRow, int currentCol)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                int targetRow = i, targetCol = j;
                string piece = setPiece(board, currentRow, currentCol);
                object pieceObject = setPieceObject(piece, currentRow, currentCol, targetRow, targetCol, isWhiteTurn,whiteRightRookHasMoved,
                    blackRightRookHasMoved,whiteLeftRookHasMoved,blackLeftRookHasMoved);
                Square pieceClass = (Square)pieceObject;
                bool IsValidMove = pieceClass.canReachTarget(currentRow, currentCol, piece, targetRow, targetCol, isWhiteTurn, board);
                bool PathClear = checkIfPathIsClear(board, currentRow, currentCol, targetRow, targetCol, isWhiteTurn);
                IsValidMove = validateKingMove(board, isWhiteTurn, currentRow, currentCol, targetRow, targetCol, pieceClass, IsValidMove);
                if (IsValidMove && PathClear && board[currentRow, currentCol] != board[i, j]) { isStalemate = false;  }
            }
        }
                 return isStalemate;
    }
    private bool validateKingMove(string[,] board, bool isWhiteTurn, int currentRow, int currentCol, int targetRow, int targetCol, Square pieceClass, bool IsValidMove)
    {
        if (pieceClass is King)
        {
            string savePiece = board[currentRow, currentCol];
            board[currentRow, currentCol] = "  ";
            bool isSquareUnderAttack = isSquareUnderThreat(board, isWhiteTurn, board[targetRow, targetCol], targetRow, targetCol);
            board[currentRow, currentCol] = savePiece;
            if (isSquareUnderAttack)
            {
                IsValidMove = false;
            }
        }

        return IsValidMove;
    }
    private int fiftyMoveRule(string[,] board, int moveRule, int targetRow, int targetCol, Square pieceClass, bool IsValidMove)
    {
        if (IsValidMove)
        {
            moveRule++;
            if (pieceClass is Pawn || board[targetRow, targetCol] != "  ")
            {
                moveRule = 0;
            }
        }
        if (moveRule == 100)
        {
            PrintToScreen("\nThe game is a draw due to the 50 Move Rule, neither player has made a capture or pawn move in the last 50 moves.");
        }
        return moveRule;
    }
    private bool deadPosition(string[,] board, bool isGameOver)
    {
        int count = countNonEmptyCells(board);
        if (count == 3 || count == 2)
        {
            int countRepeat = countSpecificPieces(board);
            if (count == countRepeat)
            {
                PrintToScreen("\nThe game is a draw due to a Dead Position, neither player can achieve checkmate with the current board state.");
                return true;
            }
        }
                 return false;
    }
    private static int countSpecificPieces(string[,] board)
    {
        int countRepeat = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j][1] == 'K' || board[i, j][1] == 'N' || board[i, j][1] == 'B')
                {
                    countRepeat++;
                }
            }
        }
             return countRepeat;
    }
    private static int countNonEmptyCells(string[,] board)
    {
        int count = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] != "  ")
                {
                    count++;
                }
            }
        }
             return count;
    }
    private void updateEnPassantMoves(bool isWhiteTurn, int[] enPassanWhite, int[] enPassanBlack)
    {
        int sigh = isWhiteTurn ? -1 : 1;
        int[] enPassantArray = isWhiteTurn ? enPassanWhite : enPassanBlack;
        for (int i = 0; i < 8; i++)
        {
            if (enPassantArray[i] == 1)
            {
                enPassantArray[i]++;
            }
        }
    }
    private int AddBoardToHistory(string[] boardHistory, string[,] board, bool whiteKingHasMoved, bool blackKingHasMoved, bool whiteRightRookHasMoved, bool blackRightRookHasMoved,
    bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved)
    {
        string boardString = createBoardString(board, whiteKingHasMoved, blackKingHasMoved, whiteRightRookHasMoved, blackRightRookHasMoved, whiteLeftRookHasMoved, blackLeftRookHasMoved);
        int count = 0;
        for (int i = 0; i < boardHistory.Length; i++)
        {
            if (boardHistory[i] == null)
            {
                i = boardHistory.Length;
                break;
            }
            if (boardHistory[i] != boardString && boardHistory[i] != null)
            {
                count++;
            }
        }
                boardHistory[count] = boardString;
                return count;
    }
    private string createBoardString(string[,] board, bool whiteKingHasMoved, bool blackKingHasMoved, bool whiteRightRookHasMoved, bool blackRightRookHasMoved, bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved)
    {
        string boardString = "";
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                boardString += board[i, j];
            }
        }
        boardString = boardString + whiteKingHasMoved + blackKingHasMoved + whiteRightRookHasMoved + blackRightRookHasMoved + whiteLeftRookHasMoved + blackLeftRookHasMoved;
        return boardString;
    }
    private bool ThreefoldRepetition(string[] boardHistory, string[,] board, bool whiteKingHasMoved, bool blackKingHasMoved, bool whiteRightRookHasMoved, bool blackRightRookHasMoved,
        bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved)
    {
        int index = AddBoardToHistory(boardHistory, board, whiteKingHasMoved, blackKingHasMoved, whiteRightRookHasMoved, blackRightRookHasMoved,
        whiteLeftRookHasMoved, blackLeftRookHasMoved);
        int count = 0;
        count = countRepetitionsUntilIndex(boardHistory, index, count);
        if (count - 1 >= 2)
        {
            PrintToScreen("\nThe game ends in a draw due to Threefold Repetition!");
            return true;
        }
             return false;
    }
    private static int countRepetitionsUntilIndex(string[] boardHistory, int index, int count)
    {
        for (int i = 0; i <= index; i++)
        {
            if (boardHistory[i] == boardHistory[index] )
            {
                count++;
            }
        }
           return count;
    }
    private void deletepiece(string[,] board, int moveRow, int moveCol, bool isWhiteTurn)
    {
        int sign = isWhiteTurn ? -1 : 1;
        int removeRow = moveRow - sign;
        board[removeRow, moveCol] = "  ";
    }
    private bool checkIfPathIsClear(string[,] board, int pieceRow, int pieceCol, int moveRow, int moveCol, bool isWhiteTurn)
    {
        int rowDiff = (moveRow - pieceRow);
        int colDiff = (moveCol - pieceCol);
        int multiplyDiff = rowDiff * colDiff;
        bool isPathClear = true;
        if (multiplyDiff == 0)
        {
            isPathClear = IsVerticalPathClear(board, pieceRow, pieceCol, moveRow, rowDiff, isPathClear, 1);
            isPathClear = IsHorizontalPathClear(board, pieceRow, pieceCol, moveCol, colDiff, isPathClear);
        }
        if (Math.Abs(rowDiff) == Math.Abs(colDiff))
        { // diagonal
            if (rowDiff > 0 && colDiff > 0){ isPathClear = IsDiagonalPathClear(board, pieceRow, pieceCol, rowDiff, isPathClear,1,1);  }
            else if (rowDiff > 0 && colDiff < 0) { isPathClear = IsDiagonalPathClear(board, pieceRow, pieceCol, rowDiff, isPathClear,1,-1);  }
            else if (rowDiff < 0 && colDiff > 0)  {  isPathClear = IsDiagonalPathClear(board, pieceRow, pieceCol, Math.Abs(rowDiff), isPathClear,-1,1);    }
            else if (rowDiff < 0 && colDiff < 0)  {  isPathClear = IsDiagonalPathClear(board, pieceRow, pieceCol, Math.Abs(rowDiff), isPathClear,-1,-1);   }
        }
        return isPathClear;
    }
    private bool IsDiagonalPathClear(string[,] board, int pieceRow, int pieceCol, int rowDiff, bool isPathClear,int signDiffRow, int signDiffCol)
    {
        for (int i = 1; i < rowDiff; i++)
        {
            if (squareIsOccupied(board, pieceRow + (i * signDiffRow), pieceCol + (i * signDiffCol)))
            {
                isPathClear = false;
            }
        }

        return isPathClear;
    }
    private bool IsHorizontalPathClear(string[,] board, int pieceRow, int pieceCol, int moveCol, int colDiff, bool isPathClear)
    {
        if (colDiff != 0)
        { //horizontal
            if (colDiff > 0)
            {
                for (int i = pieceCol + 1; i < moveCol; i++)
                {
                    if (squareIsOccupied(board, pieceRow, i))  {  isPathClear = false; }
                }
            }
                else
                {
                    for (int i = pieceCol - 1; i > moveCol; i--)
                    {
                        if (squareIsOccupied(board, pieceRow, i))  { isPathClear = false;  }
                    }
                }
        }
            return isPathClear;
    }
    private bool IsVerticalPathClear(string[,] board, int pieceRow, int pieceCol, int moveRow, int rowDiff, bool isPathClear,int distance)
    {
        if (rowDiff != 0)
        {  //vertical
            if (rowDiff > 0)
            {
                for (int i = pieceRow + distance; i < moveRow; i+= distance)
                {
                    if (squareIsOccupied(board, i, pieceCol)) {  isPathClear = false; }
                }
            }
            else
            {
                for (int i = pieceRow - 1; i > moveRow; i--)
                {
                    if (squareIsOccupied(board, i, pieceCol))  {  isPathClear = false;  }
                }
            }
        }
              return isPathClear;
    }
    public void movePiece(string[,] board, int pieceRow, int pieceCol, int moveRow, int moveCol)
    {
        string piece = board[pieceRow, pieceCol];
        board[pieceRow, pieceCol] = "  ";
        board[moveRow, moveCol] = piece;
    }
    private string setPiece(string[,] board, int row, int col)
    {
        string piece = board[row, col];
        return piece;
    }
    private void reverseMovePiece(string[,] board, int pieceRow, int pieceCol, int moveRow, int moveCol)
    {
        string piece = board[moveRow, moveCol];
        board[moveRow, moveCol] = "  ";
        board[pieceRow, pieceCol] = piece;
    }
  private object setPieceObject(string piece, int row, int col, int targetRow, int targetCol, bool isWhiteTurn,bool whiteRightRookHasMoved,
   bool blackRightRookHasMoved, bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved)
  {
      switch (piece[1])
      {
          case 'P':
              Pawn pawn = new Pawn(row, col, piece, targetRow, targetCol, isWhiteTurn, this);
              return pawn;
          case 'R':
              Rook rook = new Rook(row, col, piece, targetRow, targetCol, isWhiteTurn, this, whiteRightRookHasMoved, blackRightRookHasMoved, whiteLeftRookHasMoved, blackLeftRookHasMoved);
              return rook;
          case 'N':
              Knight knight = new Knight(row, col, piece, targetRow, targetCol, isWhiteTurn, this);
              return knight;
          case 'B':
              Bishop bishop = new Bishop(row, col, piece, targetRow, targetCol, isWhiteTurn, this);
              return bishop;
          case 'Q':
              Queen queen = new Queen(row, col, piece, targetRow, targetCol, isWhiteTurn, this);
              return queen;
          case 'K':
              King king = new King(row, col, piece, targetRow, targetCol, isWhiteTurn, this);
              return king;
          default:

              return piece;
      }
  }
    private bool IsValidMove(bool isWhiteTurn, string[,] board, string userInput)
    {
        char player = isWhiteTurn ? 'W' : 'B';
        int currentRow, currentCol, targetRow, targetCol;
        ParseChessMove(userInput, out currentRow, out currentCol, out targetRow, out targetCol);
        if (board[currentRow, currentCol][0] == ' ')
        {
            PrintToScreen("\nInvalid move! The square you selected is empty. Choose a square with one of your pieces.\n");
            return false;
        }
        if (board[currentRow, currentCol][0] != player && board[currentRow, currentCol][0] != ' ')
        {
            PrintToScreen("\nInvalid move! You can't select your opponent's piece. Choose one of your own and try again.\n");
            return false;
        }
        if (board[targetRow, targetCol][0] == player)
        {
            PrintToScreen("\nError: You can't take down your own troops. Make a move that targets the opponent's forces.\n");
            return false;
        }
        return true;
    }
    private void ParseChessMove(string userInput, out int currentRow, out int currentCol, out int targetRow, out int targetCol)
    {
        currentRow = 0  ;  currentCol = 0 ;  targetRow = 0 ;  targetCol = 0  ;
        int[] numberRow = { 8, 7, 6, 5, 4, 3, 2, 1 };
        char[] letterCol = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };
        for (int i = 0; i < 8; i++)
        {
            if (userInput[0] == letterCol[i]) { currentCol = i;  }
            if (userInput[2] == letterCol[i]) { targetCol = i; }
            if ((int)char.GetNumericValue(userInput[1]) == numberRow[i]){  currentRow = i;  }
            if ((int)char.GetNumericValue(userInput[3]) == numberRow[i]) {  targetRow = i;   }
        }
    }
    private bool switchTurn(bool isWhiteTurn)
    {
        return (isWhiteTurn = !isWhiteTurn);
    }
    private string editedInput(string input)
    {
        input = input.ToUpper();
        input = input.Trim();
        string inputNew = "";
        int count = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] != ' ')
            {
                inputNew += input[i];
                count++;
            }
        }
        return inputNew;
    }
    private bool IsValidInput(string userInput)
    {
        if (userInput.Length != 4) {return false; }
       for(int i = 0; i <= 3; i++)
        {
            if(i % 2 == 0)
            {
                if (userInput[i] < 'A' || userInput[i] > 'H') {  return false;  }
            }
            else
            {
                if (userInput[i] < '1' || userInput[i] > '8'){  return false; }
            }
        }
                return true;
            }
    private void PrintToScreen(string message)
    {
        Console.WriteLine(message);
    }
    private string GetUserInput()
    {
        return Console.ReadLine();
    }
    private void PrintBoard(string[,] board, bool isWhiteTurn, bool checkmate, bool deadPositionDraw, bool stalemate, bool drawThreefoldRepetition)
    {
        string plyerName = isWhiteTurn ? "White" : "Black";
        string Line = "";
        PrintToScreen("\n   A  B  C  D  E  F  G  H");
        for (int row = 0; row < 8; row++)
        {
            Line += 8 - row + "  ";
            for (int col = 0; col < 8; col++)
            {
                Line += board[row, col] + " ";
            }
            PrintToScreen(Line);
            Line = "";
        }
        PrintToScreen("");
        if (!checkmate && !deadPositionDraw && !stalemate && !drawThreefoldRepetition)
        {
            PrintToScreen(plyerName + " player please enter your move : ");
        }
    }
    public bool squareIsOccupied(string[,] board, int row, int col)
    {
        if (board[row, col] != "  ")
        {
            return true;
        }
        return false;
    }
    private bool IsCheck(string[,] board, bool isWhiteTurn)
    {
        string king = isWhiteTurn ? "WK" : "BK";
        int kingRow = -1, kingCol = -1;
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] == king)
                {
                    kingRow = row;
                    kingCol = col;
                    break;
                }
            }
        }
        return isSquareUnderThreat(board, isWhiteTurn, king, kingRow, kingCol);
    }
    public bool isSquareUnderThreat(string[,] board, bool isWhiteTurn, string king, int kingRow, int kingCol)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if (board[row, col] != null && board[row, col].Substring(0, 1) == (isWhiteTurn ? "B" : "W"))
                {
                    object pieceObject = setPieceObject(board[row, col], row, col, kingRow, kingCol, isWhiteTurn, whiteRightRookHasMoved,
                     blackRightRookHasMoved, whiteLeftRookHasMoved, blackLeftRookHasMoved);
                    Square pieceClass = (Square)pieceObject;
                    bool IsValidMove = pieceClass.canReachTarget(row, col, king, kingRow, kingCol, isWhiteTurn, board);
                    if (board[row, col][1] == 'P')
                    {
                        int pawnDiff = (kingRow - row) * (kingCol - col);
                        if (Math.Abs(pawnDiff) == 1 && kingCol != col){IsValidMove = true;}
                    }
                    bool PathClear = checkIfPathIsClear(board, row, col, kingRow, kingCol, isWhiteTurn);
                    if (IsValidMove && PathClear){return true; }
                }
            }
        }
        return false;
    }
}
public class Square
{
    public int Row { get; set; }
    public int Col { get; set; }
    public string Piece { get; set; } 
    public int targetRow { get; set; }
    public int targetCol { get; set; }
    public bool IsWhiteTurn { get; set; }

    public ChessGame ChessGame { get; set; }

    public Square(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame)
    {
        Row = row;
        Col = col;
        Piece = piece;
        targetRow = targetRow;
        targetCol = targetCol;
        IsWhiteTurn = isWhiteTurn;
        ChessGame = chessGame;
    }
     public virtual bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
    {
        return true;
    }
}
public class Pawn : Square
{
    public Pawn(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame)
        : base(row, col, piece, targetRow, targetCol, isWhiteTurn, chessGame) {}
    public bool enPassant(int[] enPassanWhite, int[] enPassanBlack, bool isWhiteTurn, string[,] board, int pieceRow, int pieceCol, int moveRow, int moveCol)
    {
        int sigh = isWhiteTurn ? -1 : 1;
        int[] enPassantArray = isWhiteTurn ? enPassanWhite : enPassanBlack;
        for (int i = 0; i < 8; i++)
        {
            if (enPassantArray[i] == 1)  {  enPassantArray[i]++;  }
        }
        int enPassantValue = enPassantArray[pieceCol];
        ++enPassantArray[pieceCol];
        int validRow = isWhiteTurn ? 3 : 4;
        int[] enPassantOpponentArray = isWhiteTurn ? enPassanBlack : enPassanWhite;
        int enPassantOpponentValue = enPassantOpponentArray[moveCol];
        if (enPassantOpponentValue == 1 && validRow == pieceRow && pieceRow + sigh == moveRow)  {  return true;  }
              return false;
    }
    public bool doubleMove(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn)
    {
        int sign = isWhiteTurn ? -1 : 1;
        int diff = (targetRow - row) * (sign);
        if (diff == 2 && targetCol == col)  {  return true; }
        return false;
    }
    public override bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
    {
        int sign = isWhiteTurn ? -1 : 1;
        int rowDiff = (targetRow - row) * (sign);
        int colDiff = (targetCol - col) * (sign);
        int rowDoubleMove = isWhiteTurn ? 6 : 1;
        if (rowDiff == 1 && targetCol == col)  {  return true;  }
        if (rowDiff == 1 && (colDiff == 1 || colDiff == -1))  {  return true;  }
        if (rowDiff == 2 && targetCol == col && row == rowDoubleMove)  {  return doubleMove(row, col, piece, targetRow, targetCol, isWhiteTurn);  }
        return false;
    }
}
public class Rook : Square
{
    private bool BlackRightRookHasMoved;
    private bool WhiteRightRookHasMoved;
    private bool WhiteLeftRookHasMoved;
    private bool BlackLeftRookHasMoved;
    public Rook(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame ,bool whiteRightRookHasMoved,
     bool blackRightRookHasMoved, bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved)

        : base(row, col, piece, targetRow, targetCol, isWhiteTurn, chessGame)
    {
        WhiteRightRookHasMoved = whiteRightRookHasMoved;
        BlackRightRookHasMoved = blackRightRookHasMoved;
        WhiteLeftRookHasMoved = whiteLeftRookHasMoved;
        BlackLeftRookHasMoved = blackLeftRookHasMoved;
    }
    public override bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
    {
        int sign = isWhiteTurn ? -1 : 1;
        int rowDiff = (targetRow - row) * (sign);
        int colDiff = (targetCol - col) * (sign);
        if (rowDiff == 0 || colDiff == 0) { return true; }
        return false;
    }
     public bool GetWhiteLeftRookHasMoved
    {
        get { return this.WhiteLeftRookHasMoved; }
    }

    public bool GetBlackLeftRookHasMoved
    {
        get { return this.BlackLeftRookHasMoved; }
    }

    public bool GetWhiteRightRookHasMoved
    {
        get { return this.WhiteRightRookHasMoved; }
    }

    public bool GetBlackRightRookHasMoved
    {
        get { return this.BlackRightRookHasMoved; }
    }
}
public class Knight : Square
{
    public Knight(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame)
        : base(row, col, piece, targetRow, targetCol, isWhiteTurn, chessGame)
    {
    }
    public override bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
    {
        int sign = isWhiteTurn ? -1 : 1;
        int rowDiff = (targetRow - row) * (sign);
        int colDiff = (targetCol - col) * (sign);
        if (Math.Abs(rowDiff) == 2 && Math.Abs(colDiff) == 1) { return true; }
        if (Math.Abs(rowDiff) == 1 && Math.Abs(colDiff) == 2) { return true; }
        return false;
    }
}
public class Bishop : Square
{
    public Bishop(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame)
        : base(row, col, piece, targetRow, targetCol, isWhiteTurn, chessGame)
    {
    }
    public override bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
    {
        int sign = isWhiteTurn ? -1 : 1;
        int rowDiff = (targetRow - row) * (sign);
        int colDiff = (targetCol - col) * (sign);
        if (Math.Abs(rowDiff) == Math.Abs(colDiff)) { return true; }
        return false;
    }
}
 public class Queen : Square
{
    public Queen(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame)
        : base(row, col, piece, targetRow, targetCol, isWhiteTurn, chessGame)
    {
    }
   public override bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
{
        Bishop bishop = new Bishop(row, col, piece, targetRow, targetCol, isWhiteTurn, ChessGame);
        bool isbishopValidMove = bishop.canReachTarget(row, col, piece, targetRow, targetCol, isWhiteTurn, board);
        Rook rook = new Rook(row, col, piece, targetRow, targetCol, isWhiteTurn, ChessGame, true, true, true, true);
        bool isrookValidMove = rook.canReachTarget(row, col, piece, targetRow, targetCol, isWhiteTurn, board);
   bool isValidMove = isrookValidMove || isbishopValidMove;

    return isValidMove; 
}
}
public class King : Square {
    private bool WhiteKingHasMoved;
    private bool BlackKingHasMoved;
    public King(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, ChessGame chessGame)
        : base(row, col, piece, targetRow, targetCol, isWhiteTurn, chessGame){
             

         }
    public override bool canReachTarget(int row, int col, string piece, int targetRow, int targetCol, bool isWhiteTurn, string[,] board)
    {
        int rowDiff = Math.Abs(targetRow - row);
        int colDiff = Math.Abs(targetCol - col);
        if (rowDiff < 2 && colDiff < 2) { return true; }
        return false;
    }
        
    public bool canCastle(int row, int col, int targetRow, int targetCol, bool isWhiteTurn, bool whiteKingHasMoved, bool blackKingHasMoved
       , string[,] board, bool whiteRightRookHasMoved, bool blackRightRookHasMoved, bool whiteLeftRookHasMoved, bool blackLeftRookHasMoved)
    {
        WhiteKingHasMoved = whiteKingHasMoved;
        BlackKingHasMoved = blackKingHasMoved;
        Rook rook = new Rook(row, col, "WR", targetRow, targetCol, isWhiteTurn, ChessGame, whiteRightRookHasMoved, blackRightRookHasMoved, whiteLeftRookHasMoved, blackLeftRookHasMoved);
        bool hasMovedKing = isWhiteTurn ? WhiteKingHasMoved : BlackKingHasMoved;
        bool hasMovedRightRook = isWhiteTurn ? rook.GetWhiteRightRookHasMoved : rook.GetBlackRightRookHasMoved;
        bool hasMovedLeftRook = isWhiteTurn ? rook.GetWhiteLeftRookHasMoved : rook.GetBlackLeftRookHasMoved;
        int rowIndex = isWhiteTurn ? 7 : 0;
        bool isValidCastling = true;
        if (ChessGame.isSquareUnderThreat(board, isWhiteTurn, board[rowIndex, 4], rowIndex, 4))  {  isValidCastling = false; }
        if (hasMovedKing) {isValidCastling = false; }
        if (targetRow != rowIndex || Math.Abs(targetCol - col) != 2 || row != rowIndex) { isValidCastling = false; }
        if (targetCol == 2 && hasMovedLeftRook) {   isValidCastling = false; }
        if (targetCol == 6 && hasMovedRightRook) {   isValidCastling = false;  }
        if (targetCol == 2 && isValidCastling)
        {
            isValidCastling = canPerformCastling(targetRow, isWhiteTurn, board, rowIndex, isValidCastling,1,4);
            if (isValidCastling) { ChessGame.movePiece(board, rowIndex, 0, rowIndex, 3); }
        }
        if (targetCol == 6 && isValidCastling)
        {
            isValidCastling = canPerformCastling(targetRow, isWhiteTurn, board, rowIndex, isValidCastling,5,7); 
           if (isValidCastling)  {ChessGame.movePiece(board, rowIndex, 7, rowIndex, 5);}
        }
            return isValidCastling;
    }
    private bool canPerformCastling(int targetRow, bool isWhiteTurn, string[,] board, int rowIndex, bool isValidCastling,int minRange , int maxRange )
    {
        for (int i = minRange; i < maxRange; i++)
        {
            if (ChessGame.squareIsOccupied(board, rowIndex, i))
            {
                isValidCastling = false;
            }
            if (ChessGame.isSquareUnderThreat(board, isWhiteTurn, board[rowIndex, i], targetRow, i))
            {
                isValidCastling = false;
            }
        }

        return isValidCastling;
    }
}
            
            
       
                    

                       

               
          
              
      

    
       
            
         
          
            
        
                
                
                
                

   

    


    




               


        
           
      
       

           

      



        



     





     



        










            


           


                
           
                
           
           
            
                
           
                
           


















    
   


                   
                        
                   
                

                   
                



                
                    
               


















          
            
                
           
           
               
         
           
                 
          
               
                
               




        

       
           
                   
                       
                    
                   
                        
                   


               
           
   
    


       
        
                   
                       
                    
                   
                       
                   

       
           
       

           
        
           
       
       
           
       

       
           
       


               
           

     

                  
                

       

            
       

       
            
       

       

           
       

        
          
       





           
               
          
           
           
               
           
            
                
           
                
           
        
        
        


                
                
                    
                
                    
              
       
            
       




                        
                            
                    

                        
                   
                        



