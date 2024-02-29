using ChessChallenge.API;
using ChessChallenge.Application;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    private const int MiddleGameThreshold = 100;
    int flag = -1;
    Board board;

    int[] values = { 0, 100, 300, 320, 500, 900, 0 }, kingAttackervPieceValues = { 0, 0, 20, 20, 40, 80, 0 }, kingAttackweight = { 0, 50, 75, 88, 94, 97, 99 };

    int getPieceValue(PieceType pieceType)
    {
        return values[(int)pieceType];
    }

     readonly int[] KingAttackTable = new int[]
    {
        0, 0, 1, 2, 4, 6, 9, 12, 16, 20, 25, 30, 36,
        42, 49, 56, 64, 72, 81, 90, 100, 110, 121, 132,
        144, 156, 169, 182, 196, 210, 225, 240, 256, 272,
        289, 306, 324, 342, 361, 380, 400, 420, 441, 462,
        484, 506, 529, 552, 576, 600, 625, 650, 676, 702,
        729, 756, 784, 812, 841, 870, 900, 930, 961, 992,
        1024, 1056, 1089, 1122, 1156, 1190, 1225, 1260, 1260,
        1260, 1260, 1260, 1260, 1260, 1260, 1260, 1260, 1260,
        1260, 1260, 1260, 1260, 1260, 1260, 1260, 1260, 1260,
        1260, 1260, 1260, 1260, 1260, 1260, 1260, 1260, 1260,
    };


    struct Transposition
    {
        public ulong zobristHash;
        public sbyte depth;
        public int eval;
        public byte flag;
    }

    Transposition[] transpositiontable = new Transposition[8388608];

    int noTT;
    int bestTTmatch;
    bool betaCutOff;


    public Move Think(Board board, Timer timer, int difficulty)
    {
        this.board = board;
        Move[] moves = board.GetLegalMoves();
        moves = moves.OrderByDescending(m => m.IsCapture).ThenByDescending(m => getPieceValue(m.CapturePieceType)).ToArray();
        int highestEval = int.MinValue + 2;
        Move bestMove = Move.NullMove;
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int eval = -recursiveLookUp(difficulty, int.MinValue + 2, -highestEval) ;

            if (eval > highestEval)
            {
                highestEval = eval;
                bestMove = move;
            }
            board.UndoMove(move);
        }
        return bestMove;
    }

    
    private int InitKingSafety(Square kingSquare)
    {
        int safetyScore = 0;

        // Evaluate pawn structure around the king
        for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
        {
            for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                Square adjacentSquare = new Square(kingSquare.File + fileOffset, kingSquare.Rank + rankOffset);
                if (IsValidSquare(adjacentSquare) && board.GetPiece(adjacentSquare).IsPawn)
                {
                    safetyScore++;  // Increase safety score for each protective pawn
                }
            }
        }

        // Evaluate open files near the king
        int file = kingSquare.File;
        if (IsOpenFile(file))
        {
            safetyScore -= 2; // Penalize if the king's file is open
        }
        if (IsOpenFile(file - 1) || IsOpenFile(file + 1))
        {
            safetyScore -= 1; // Penalize if adjacent files are open
        }

        // Evaluate king's mobility
        safetyScore += EvaluateKingMobility(kingSquare);

        return safetyScore;
    }

    public bool IsValidSquare(Square adjacentSquare)
    {
        return adjacentSquare.File >= 0 && adjacentSquare.File < 8 && adjacentSquare.Rank >= 0 && adjacentSquare.Rank < 8;
    }

    // Checks if a file (column) is open, i.e., no pawns are present on that file
    private bool IsOpenFile(int file)
    {
        foreach (var rank in Enumerable.Range(0, 8))
        {
            Square square = new Square(file, rank);
            if (IsValidSquare(square) && board.GetPiece(square).IsPawn)
            {
                return false; // File is not open if there's a pawn
            }
        }
        return true; // File is open if no pawns found
    }

    // Evaluates the king's mobility based on the number of safe squares it can move to
    private int EvaluateKingMobility(Square kingSquare)
    {
        int mobilityScore = 0;
        foreach (var offset in new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) })
        {
            Square adjacentSquare = new Square(kingSquare.File + offset.Item1, kingSquare.Rank + offset.Item2);
            if (IsValidSquare(adjacentSquare) && IsSafeSquare(adjacentSquare))
            {
                mobilityScore++; // Increase score for each safe adjacent square
            }
        }
        return mobilityScore;
    }

    // Determine if a square is safe for the king to move to
    private bool IsSafeSquare(Square square)
    {
        // Check if the square is under attack by any enemy piece
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (flag ==0 ? pieceList.IsWhitePieceList != board.IsWhiteToMove : pieceList.IsWhitePieceList == board.IsWhiteToMove)
            {
                foreach (Piece piece in pieceList)
                {
                    // Get attacks for each piece and check if the square is under attack
                    ulong pieceAttacks = BitboardHelper.GetPieceAttacks(pieceList.TypeOfPieceInList, piece.Square, board, piece.IsWhite);
                    if ((pieceAttacks & (1UL << square.Index)) != 0)
                    {
                        return false; // Square is not safe if under attack
                    }
                }
            }
        }

        if (IsSquareAffectedByPinnedPiece(square))
        {
            return false; // Square is not safe if affected by a pinned piece
        }
            return true; // Square is safe if not under attack
    }

    // Check if a piece is aligned with the king along a rank, file, or diagonal
    private bool IsAlignedWithKing(Square pieceSquare)
    {
        Square kingSquare = board.GetKingSquare(flag == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove);

        // Check alignment along the same rank, file, or diagonal
        return pieceSquare.File == kingSquare.File || 
            pieceSquare.Rank == kingSquare.Rank || 
            Math.Abs(pieceSquare.File - kingSquare.File) == Math.Abs(pieceSquare.Rank - kingSquare.Rank);
    }

    private bool IsSquareAffectedByPinnedPiece(Square square)
    {
        // Iterate through all pieces to find potential pins
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (flag ==0 ? pieceList.IsWhitePieceList != board.IsWhiteToMove : pieceList.IsWhitePieceList == board.IsWhiteToMove)
            {
                foreach (Piece piece in pieceList)
                {
                    // Check if the piece is aligned with the king and a potential attacker
                    if (IsAlignedWithKing(piece.Square) && IsPotentialPinnerPresent(piece.Square))
                    {
                        if (WouldPinBeExposed(piece.Square, square))
                        {
                            return true; // The square is affected by a pinned piece
                        }
                    }
                }
            }
        }
        return false;
    }

    private bool IsPotentialPinnerPresent(Square pieceSquare)
    {
        Square kingSquare = board.GetKingSquare(flag == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove);
        bool isDiagonal = Math.Abs(pieceSquare.File - kingSquare.File) == Math.Abs(pieceSquare.Rank - kingSquare.Rank);

        // Scan the line between the piece and the king
        int fileStep = pieceSquare.File.CompareTo(kingSquare.File);
        int rankStep = pieceSquare.Rank.CompareTo(kingSquare.Rank);

        int file = pieceSquare.File;
        int rank = pieceSquare.Rank;

        while (file != kingSquare.File || rank != kingSquare.Rank)
        {
            file -= fileStep;
            rank -= rankStep;

            Square currentSquare = new Square(file, rank);
            Piece piece = board.GetPiece(currentSquare);

            if (!piece.IsNull&& currentSquare != pieceSquare && currentSquare != kingSquare)
            {
                if (flag == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove && 
                    (piece.IsRook && !isDiagonal || piece.IsBishop && isDiagonal || piece.IsQueen))
                {
                    return true; // Potential pinner found
                }
                else
                {
                    break; // A piece blocks the line, so no pinning is possible
                }
            }
        }

        return false;
    }

    private bool WouldPinBeExposed(Square pieceSquare, Square kingDestinationSquare)
    {
        Square kingSquare = board.GetKingSquare(flag == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove);
        Piece potentialPinner = FindPotentialPinner(pieceSquare, kingSquare);

        if (potentialPinner.PieceType == PieceType.None)
        {
            return false; // No potential pinner found, so no pin can be exposed
        }

        // Check if the king's move would break the line between the pinner and the pinned piece
        if (IsAligned(kingDestinationSquare, pieceSquare, potentialPinner.Square))
        {
            return false; // The king's move maintains alignment, so no pin is exposed
        }

        return true; // Moving the king breaks the alignment, exposing the pin
    }


    // Check if three squares are aligned (straight line or diagonal)
    private bool IsAligned(Square sq1, Square sq2, Square sq3)
    {
        // Aligned along a file
        if (sq1.File == sq2.File && sq2.File == sq3.File) return true;

        // Aligned along a rank
        if (sq1.Rank == sq2.Rank && sq2.Rank == sq3.Rank) return true;

        // Aligned diagonally
        if (Math.Abs(sq1.File - sq2.File) == Math.Abs(sq1.Rank - sq2.Rank) &&
            Math.Abs(sq2.File - sq3.File) == Math.Abs(sq2.Rank - sq3.Rank))
            return true;

        return false;
    }

    private Piece FindPotentialPinner(Square pieceSquare, Square kingSquare)
    {
        // Scan the line between the piece and the king for a potential pinner
        int fileStep = pieceSquare.File.CompareTo(kingSquare.File);
        int rankStep = pieceSquare.Rank.CompareTo(kingSquare.Rank);

        int file = pieceSquare.File;
        int rank = pieceSquare.Rank;
        
        while (file != kingSquare.File || rank != kingSquare.Rank)
        {
            file -= fileStep;
            rank -= rankStep;

            Square currentSquare = new Square(file, rank);
            Piece piece = board.GetPiece(currentSquare);

            if (!piece.IsNull && currentSquare != pieceSquare && currentSquare != kingSquare)
            {
                if (flag == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove && 
                    (piece.IsRook || piece.IsBishop || piece.IsQueen))
                {
                    return piece; // Found a potential pinner
                }
                else
                {
                    break; // Another piece blocks the line
                }
            }
        }
        
        return Piece.NullPiece; // No potential pinner found
    }

    private int CalculateKingAttackPoints(Square kingSquare)
    {
        int attackPoints = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (flag ==0 ? pieceList.IsWhitePieceList != board.IsWhiteToMove : pieceList.IsWhitePieceList == board.IsWhiteToMove)
            {
                foreach (Piece piece in pieceList)
                {
                    ulong attacks = BitboardHelper.GetPieceAttacks(pieceList.TypeOfPieceInList, piece.Square, board, piece.IsWhite);
                    
                    // Check if the piece attacks squares around the king
                    if ((attacks & BitboardHelper.GetKingAttacks(kingSquare)) != 0)
                    {
                        // Add points based on the type of attacking piece
                        attackPoints += GetAttackPointsByPieceType(pieceList.TypeOfPieceInList);
                    }
                }
            }
        }

        return attackPoints;
    }

    private int GetAttackPointsByPieceType(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 1;
            case PieceType.Knight:
            case PieceType.Bishop:
                return 3;
            case PieceType.Rook:
                return 5;
            case PieceType.Queen:
                return 9;
            default:
                return 0;
        }
    }

    private bool IsMiddleGame()
    {
        int totalPieceValue = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                totalPieceValue += GetAttackPointsByPieceType(piece.PieceType);
            }
        }

        return totalPieceValue > MiddleGameThreshold;
    }

    private bool HasAtLeastTwoAttackers(Square kingSquare)
    {
        int attackersCount = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (flag == 0 ? pieceList.IsWhitePieceList != board.IsWhiteToMove : pieceList.IsWhitePieceList == board.IsWhiteToMove)
            {
                foreach (Piece piece in pieceList)
                {
                    ulong attacks = BitboardHelper.GetPieceAttacks(pieceList.TypeOfPieceInList, piece.Square, board, piece.IsWhite);
                    
                    // Check if the piece attacks squares around the king
                    if ((attacks & BitboardHelper.GetKingAttacks(kingSquare)) != 0)
                    {
                        attackersCount++;
                        if (attackersCount >= 2)
                        {
                            return true; // There are at least two attackers
                        }
                    }
                }
            }
        }

        return false; // Less than two attackers
    }

    private bool HasQueen()
    {
        // Get the color of the side opposite to the current player
        bool isWhite = flag == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove;

        // Iterate through all pieces to find if there is a queen of the opposite color
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (pieceList.IsWhitePieceList == isWhite && pieceList.TypeOfPieceInList == PieceType.Queen)
            {
                return true; // The enemy has a queen
            }
        }

        return false; // No queen found for the enemy
    }


    private int EvaluateKingSafety()
    {   
        Square kingSquare ;
        if(ChallengeController.selectedPlayerColor == ChallengeController.PlayerColor.White) {
            kingSquare = board.GetKingSquare(board.IsWhiteToMove);
            flag = 0;
        }
        else {
            kingSquare = board.GetKingSquare(!board.IsWhiteToMove);
            flag = 1;
        }
        
        // Apply additional conditions for middle game and presence of a queen
        if (IsMiddleGame() && HasAtLeastTwoAttackers(kingSquare) && HasQueen())
        {
            int enemyPoints = InitKingSafety(kingSquare) + CalculateKingAttackPoints(kingSquare);
            enemyPoints = Math.Max(0, Math.Min(enemyPoints, KingAttackTable.Length - 1));
            return KingAttackTable[enemyPoints];
        }

        return 0; 
    }

    int recursiveLookUp(int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate())
        {
            return int.MinValue + 10;
        }
        else if (board.IsDraw())
        {
            return 0;
        }
        if (board.IsInCheck())
        {
            depth++;
        }

        if (depth == 0)
        {
            return qSearch(alpha, beta);
        }


        var (moves, TTeval, alphabetaCutoff) = orderAndcheckForTranspos(alpha, beta, depth, board.GetLegalMoves());
        if (alphabetaCutoff)
            return TTeval;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -recursiveLookUp(depth - 1, -beta, -alpha);

            board.UndoMove(move);

            if (eval >= beta)
            {
                return beta; // beta cutoff
            }
            alpha = Math.Max(alpha, eval); // update alpha if necessary
        }
        return alpha;
    }

    int qSearch(int alpha, int beta)
    {
        bool isWhiteToMove = board.IsWhiteToMove;


        if (board.IsInCheckmate())
        {
            return int.MinValue + 10;
        }
        else if (board.IsDraw())
        {
            return 0;
        }

        int blackScore = pieceEval(Array.FindAll(board.GetAllPieceLists(), list => !list.IsWhitePieceList));
        int whiteScore = pieceEval(Array.FindAll(board.GetAllPieceLists(), list => list.IsWhitePieceList));

        int whiteScoreUp = whiteScore - blackScore;

        int stand_pat = isWhiteToMove ? whiteScoreUp : -whiteScoreUp;

        //easy endgames
        if (((isWhiteToMove ? blackScore : whiteScore) < 300) && (stand_pat > 499))
        {
            int endgameeval = 0;
            int file = board.GetKingSquare(!isWhiteToMove).File;
            int rank = board.GetKingSquare(!isWhiteToMove).Rank;
            endgameeval += Math.Max(3 - rank, rank - 4);
            endgameeval += Math.Max(3 - file, file - 4);

            endgameeval += 14 - Math.Abs(board.GetKingSquare(isWhiteToMove).Rank - board.GetKingSquare(!isWhiteToMove).Rank) - Math.Abs(board.GetKingSquare(isWhiteToMove).File - board.GetKingSquare(!isWhiteToMove).File);

            stand_pat += endgameeval * 10;

        }

        if (stand_pat >= beta)
            return beta;

        alpha = Math.Max(stand_pat, alpha);

        Move[] moves = board.GetLegalMoves(true);
        if (moves.Length != 0)
        {
            moves = moves.OrderByDescending(m => (int)m.CapturePieceType).ThenBy(m => (int)m.MovePieceType).ToArray();
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                int eval = -qSearch(-beta, -alpha);
                board.UndoMove(move);
                if (eval >= beta)
                {
                    return beta;
                }
                alpha = Math.Max(eval, alpha);

            }
            return alpha;
        }

        return alpha;
    }

    int pieceEval(PieceList[] pieceLists)
    {
        bool isWhite = pieceLists[0].IsWhitePieceList;
        int score = 0, kingAttackingPiecesCount = 0, valueOfKingAttacks = 0;

        ulong adjacentKingSquares = BitboardHelper.GetKingAttacks(board.GetKingSquare(!isWhite));
        if (isWhite)
            adjacentKingSquares |= adjacentKingSquares >> 8;
        else
            adjacentKingSquares |= adjacentKingSquares << 8;

        foreach (PieceList pieceList in pieceLists)
        {
            int kingAttackValue = kingAttackervPieceValues[(int)pieceList.TypeOfPieceInList];
            foreach (Piece piece in pieceList)
            {
                ulong pieceAttacks = BitboardHelper.GetPieceAttacks(pieceList.TypeOfPieceInList, piece.Square, board, isWhite);
                score += BitboardHelper.GetNumberOfSetBits(pieceAttacks) * 4;

                if (pieceList.TypeOfPieceInList == PieceType.Pawn)
                {
                    int rankProgress = isWhite ? piece.Square.Rank : 7 - piece.Square.Rank;
                    score += rankProgress * rankProgress / 5; // simplified from Math.Pow for efficiency
                }

                if ((pieceAttacks & adjacentKingSquares) != 0)
                {
                    valueOfKingAttacks += kingAttackValue;
                    kingAttackingPiecesCount++;
                }
            }
            score += pieceList.Count * getPieceValue(pieceList.TypeOfPieceInList);
        }

        // Evaluate king safety only if there are attacking pieces
        if (kingAttackingPiecesCount > 0)
        {
            score += EvaluateKingSafety(); // Call this function less frequently
        }

        return score + valueOfKingAttacks * kingAttackweight[Math.Min(6, kingAttackingPiecesCount)] / 100;
    }


    (Move[], int, bool) orderAndcheckForTranspos(int alpha, int beta, int depth, Move[] moves)
    {
        noTT = 0;
        bestTTmatch = int.MinValue + 10;
        betaCutOff = false;

        Move[] moves2 = moves.OrderByDescending(
            move =>
            {
                if (betaCutOff)
                    return -1;

                board.MakeMove(move);

                ref Transposition transposition = ref transpositiontable[board.ZobristKey & 8388607];

                if (transposition.zobristHash == board.ZobristKey)
                {
                    if (transposition.depth >= depth)
                    {
                        if (transposition.flag == 1)
                        {
                            bestTTmatch = Math.Max(bestTTmatch, transposition.eval);
                            board.UndoMove(move);
                            return int.MinValue;
                        }
                        else if (transposition.flag == 2 && transposition.eval >= beta)
                        {
                            bestTTmatch = Math.Max(bestTTmatch, transposition.eval);
                            board.UndoMove(move);
                            return int.MinValue;
                        }
                        else if (transposition.flag == 3 && transposition.eval <= alpha)
                        {
                            board.UndoMove(move);
                            return int.MinValue;
                        }
                    }
                    else
                    {

                        noTT++;
                        board.UndoMove(move);
                        return transposition.eval + (int)move.CapturePieceType * 10 - (int)move.MovePieceType;
                    }
                }
                noTT++;
                board.UndoMove(move);
                return (int)move.CapturePieceType * 10 - (int)move.MovePieceType;


            }).ToArray();


        return (moves2.Take(noTT).ToArray(), bestTTmatch, betaCutOff);
    }
}