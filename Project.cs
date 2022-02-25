using System;
using System.Diagnostics;

namespace UTTT_MCTS
{
    class Player
    {
        class Board
        {
            public static readonly ushort full_board = 0b111111111;

            public static readonly ushort[] win_conditions = {
                0b111000000,
                0b000111000,
                0b000000111,
                0b100100100,
                0b010010010,
                0b001001001,
                0b100010001,
                0b001010100
            };

            public ushort[] player;

            public Board() 
            {
                this.player = new ushort[]{
                    0b000000000,
                    0b000000000
                };
            }

            public Board(ushort[] parent_masks) 
            {
                this.player = new ushort[]{
                    parent_masks[0],
                    parent_masks[1]
                };
            }

            public ushort maskOf(ushort idx)
            {
                return (ushort)(0b1 << idx);
            }

            public bool playable(ushort idx)
            {
                ushort mask = this.maskOf(idx);
                return (((this.player[0] | this.player[1]) & mask) == 0);
            }

            public void play(ushort idx, int turn)
            {
                ushort mask = this.maskOf(idx);
                this.player[turn] ^= mask;
            }

            public bool check(int turn)
            {
                foreach (int condition in win_conditions)
                {
                    if ((this.player[turn] & condition) == condition)
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool draw()
            {
                return ((player[0] | player[1]) == full_board);
            }

            public void debug()
            {
                ushort idx = 0;
                for (int y = 0; y < 3; y++)
                {
                    string row = "|";
                    for (int x = 0; x < 3; x++, idx++)
                    {
                        int mask = this.maskOf(idx);

                        if ((this.player[0] & mask) > 0)
                        {
                            row += " X |";
                            continue;
                        }

                        if ((this.player[1] & mask) > 0)
                        {
                            row += " O |";
                            continue;
                        }

                        row += "   |";
                    }
                    Console.Error.WriteLine("|---|---|---|");
                    Console.Error.WriteLine(row);
                }
                Console.Error.WriteLine("|---|---|---|");
                Console.Error.WriteLine();
            }

            public void duplicate(Board parent)
            {
                this.player[0] = parent.player[0];
                this.player[1] = parent.player[1];
            }
        }

        class Macro : Board
        {
            public ushort neutral;
            public ushort[] count;

            public Macro() 
            {
                this.neutral = 0b000000000;
                this.count = new ushort[]{
                    0b000000001,
                    0b000000001
                };
            }

            public Macro(ushort[] player_masks, ushort neutral, ushort[] count) : base(player_masks)
            {
                this.neutral = neutral;
                this.count = new ushort[]{
                    count[0],
                    count[1]
                };
            }

            new public bool draw()
            {
                return ((player[0] | player[1] | neutral) == full_board);
            }

            new public void play(ushort idx, int turn)
            {
                ushort mask = this.maskOf(idx);
                this.player[turn] ^= mask;
                this.count[turn] <<= 0b1;
            }

            new public void debug()
            {
                ushort idx = 0;
                for (int y = 0; y < 3; y++)
                {
                    string row = "|";
                    for (int x = 0; x < 3; x++, idx++)
                    {
                        int mask = this.maskOf(idx);

                        if ((this.player[0] & mask) > 0)
                        {
                            row += " X |";
                            continue;
                        }

                        if ((this.player[1] & mask) > 0)
                        {
                            row += " O |";
                            continue;
                        }

                        if ((this.neutral & mask) > 0)
                        {
                            row += " # |";
                            continue;
                        }

                        row += "   |";
                    }
                    Console.Error.WriteLine("|---|---|---|");
                    Console.Error.WriteLine(row);
                }
                Console.Error.WriteLine("|---|---|---|");
                Console.Error.WriteLine();
            }

            new public bool playable(ushort idx)
            {
                ushort mask = this.maskOf(idx);
                return (((this.player[0] | this.player[1] | neutral) & mask) == 0);
            }

            public void neutralize(ushort idx)
            {
                ushort mask = this.maskOf(idx);
                this.neutral ^= mask;
            }

            public void duplicate(Macro parent)
            {
                base.duplicate(parent);
                this.neutral = parent.neutral;
                this.count[0] = parent.count[0];
                this.count[1] = parent.count[1];
            }
        }

        class Game
        {
            public Macro macro = new Macro();

            public Board[] micro = {
                new Board(), new Board(), new Board(),
                new Board(), new Board(), new Board(),
                new Board(), new Board(), new Board()
            };

            public ushort[] moves_micro = new ushort[81];
            public ushort[] moves_idx = new ushort[81];
            
            public ushort possibleMoves = 0b0;

            public bool playable(ushort micro, ushort idx)
            {
                return this.macro.playable(micro) && this.micro[micro].playable(idx);
            }

            public float play(ushort micro, ushort idx, int turn)
            {
                this.micro[micro].play(idx, turn);
                if (this.micro[micro].check(turn))
                {
                    this.macro.play(micro, turn);
                    if (this.macro.check(turn)) { return 1.0f; }
                    else if (this.macro.draw())
                    {
                        if (this.macro.count[turn] < this.macro.count[1 ^ turn]) { return 0.0f; }
                        else if (this.macro.count[turn] > this.macro.count[1 ^ turn]) { return 1.0f; }
                        else { return 0.5f; }
                    }
                    else { return -1.0f; }
                }
                else if (this.micro[micro].draw())
                {
                    this.macro.neutralize(micro);
                    if (this.macro.draw())
                    {
                        if (this.macro.count[turn] < this.macro.count[1 ^ turn]) { return 0.0f; }
                        else if (this.macro.count[turn] > this.macro.count[1 ^ turn]) { return 1.0f; }
                        else { return 0.5f; }
                    }
                    else { return -1.0f; }
                }
                return -1.0f;
            }

            public ushort nextTurn(ushort idx)
            {
                return macro.playable(idx) ? idx : (ushort) 9;
            }

            public void computePossibleMoves(ushort idx)
            {
                ushort nextTurn = this.nextTurn(idx);
                this.possibleMoves = 0b0;

                if (nextTurn < 9)
                {
                    for (ushort id = 0; id < 9; id++)
                    {
                        if (this.playable(nextTurn, id))
                        {
                            moves_micro[possibleMoves] = nextTurn;
                            moves_idx[possibleMoves++] = id;
                        }
                    }
                }
                else
                {
                    for (ushort micro = 0; micro < 9; micro++)
                    {
                        if (!macro.playable(micro)) { continue; }
                        for (ushort id = 0; id < 9; id++)
                        {
                            if (this.micro[micro].playable(id))
                            {
                                moves_micro[possibleMoves] = micro;
                                moves_idx[possibleMoves++] = id;
                            }
                        }
                    }
                }
            }

            public void duplicate(Game parent)
            {
                for (int i = 0; i < this.micro.Length; i++)
                    this.micro[i].duplicate(parent.micro[i]);
                this.macro.duplicate(parent.macro);
            }

            public void debug()
            {
                Console.Error.WriteLine();
                string[] rows = new string[9];
                for (int board = 0; board < 9; board++)
                {
                    int row = board / 3 * 3;
                    ushort idx = 0;
                    for (int y = 0; y < 3; y++)
                    {
                        rows[row + y] += "|";
                        for (int x = 0; x < 3; x++, idx++)
                        {
                            ushort mask = this.macro.maskOf(idx);

                            if ((this.micro[board].player[0] & mask) > 0)
                            {
                                rows[row + y] += " X |";
                                continue;
                            }

                            if ((this.micro[board].player[1] & mask) > 0)
                            {
                                rows[row + y] += " O |";
                                continue;
                            }

                            rows[row + y] += "   |";
                        }
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    if (i % 3 == 0) Console.Error.WriteLine("|---|---|---||---|---|---||---|---|---|");
                    Console.Error.WriteLine(rows[i]);
                    Console.Error.WriteLine("|---|---|---||---|---|---||---|---|---|");
                }
                Console.Error.WriteLine();
                macro.debug();
                Console.Error.WriteLine();
            }
        }

        class MCTS
        {
            private static Random random = new Random();
            private static double[] sqrt;
            private static int sqrtCacheSize = (int)1e6;

            private Stopwatch sw;

            public Game game;

            public MCTS()
            {
                if (sqrt == null)
                {
                    sqrt = new double[sqrtCacheSize];
                    for (int i = 1; i < sqrtCacheSize; i++)
                    {
                        sqrt[i] = 1 / Math.Sqrt(i);
                    }
                }
            }

            public void start_stopwatch()
            {
                sw = sw = Stopwatch.StartNew();
            }

            public void init()
            {
                action_micro[root] = 0b100;
                action_idx[root] = 0b100;
                game.play(action_micro[root], action_idx[root], myID);
            }

            public void startTurn(int x, int y)
            {
                ushort micro = (ushort)((y / 3 * 3) + (x / 3));
                ushort idx = (ushort)((y % 3 * 3) + (x % 3));

                game.play(micro, idx, 0b1 ^ myID);
                
                if (children[root] == 0)
                {
                    root = 0;
                    childFrom[0] = 0;
                    plays[0] = 0;
                    wins[0] = 0;
                    freeIndex = 1;
                    action_micro[root] = micro;
                    action_idx[root] = idx;
                }
                else
                {
                    for (int i = childFrom[root]; i < childFrom[root] + children[root]; i++)
                    {
                        if (micro == action_micro[i] && idx == action_idx[i])
                        {
                            root = i;
                            break;
                        }
                    }
                }
            }

            public void load(Game game)
            {
                this.game = game;
            }

            static string convert_move_to_string(ushort micro, ushort idx)
            {
                int move = micro / 3 * 27 + micro % 3 * 3 + idx / 3 * 9 + idx % 3;
                return (move / 9) + " " + (move % 9);
            }

            public const int myID = 0;
            private const float EMPTY = -1.0f;

            private const int size = (int)2e7;
            
            private static int[] plays = new int[size];
            private static double[] wins = new double[size];
            private static int[] childFrom = new int[size];
            private static int[] children = new int[size];
            private static ushort[] action_micro = new ushort[size];
            private static ushort[] action_idx = new ushort[size];
            
            private static int freeIndex = 1;
            private static int root = 0;
            
            private static int frequencyCheck = 256;
            private static int[] path = new int[82];
            private static int N = 0;

            private static Game currentGame = new Game();

            private static int pathIndex = 0;
            private static double sqrt_log = 0;
            private static int player = 0;

            private static int currentNode = 0;
            private static float result = 0;

            public void MonteCarloTreeSearch(int startID, long time)
            {
                if (freeIndex > size * 3 / 4)
                {
                    action_micro[0] = action_micro[root];
                    action_idx[0] = action_idx[root];
                    root = 0;
                    childFrom[0] = 0;
                    plays[0] = 0;
                    wins[0] = 0;
                    freeIndex = 1;
                }

                N = plays[root];

                start_stopwatch();

                while (N % frequencyCheck != 0 || sw.ElapsedMilliseconds < time)
                {
                    currentGame.duplicate(this.game);

                    pathIndex = 0;
                    sqrt_log = Math.Sqrt(2 * Math.Log(N++));
                    player = startID;

                    currentNode = root;
                    result = EMPTY;

                    while (result <= EMPTY)
                    {
                        path[pathIndex++] = currentNode;

                        if (pathIndex == path.Length)
                        {
                            pathIndex = 0; break;
                        }

                        int currentFrom = childFrom[currentNode];
                        if (currentFrom == 0)
                        {
                            currentGame.computePossibleMoves(action_idx[currentNode]);
                            childFrom[currentNode] = freeIndex;
                            children[currentNode] = currentGame.possibleMoves;
                            currentFrom = freeIndex;
                            for (int i = 0; i < currentGame.possibleMoves; i++)
                            {
                                action_micro[freeIndex] = currentGame.moves_micro[i];
                                action_idx[freeIndex++] = currentGame.moves_idx[i];
                            }
                        }

                        int thisExpand = currentFrom;
                        if (plays[currentNode] < children[currentNode])
                        {
                            thisExpand = currentFrom + plays[currentNode];

                            int swap = random.Next(children[currentNode] - plays[currentNode]) + thisExpand;

                            ushort tmp_micro = action_micro[thisExpand];
                            action_micro[thisExpand] = action_micro[swap];
                            action_micro[swap] = tmp_micro;

                            ushort tmp_idx = action_idx[thisExpand];
                            action_idx[thisExpand] = action_idx[swap];
                            action_idx[swap] = tmp_idx;

                            plays[thisExpand] = 0;
                            wins[thisExpand] = 0;
                            childFrom[thisExpand] = 0;
                        }
                        else
                        {
                            double score = -1;
                            for (int i = currentFrom; i < currentFrom + children[currentNode]; i++)
                            {
                                int play = plays[i];
                                double invSqrt = play < sqrtCacheSize ? sqrt[play] : (1 / Math.Sqrt(play));
                                double tmp = wins[i] / play + sqrt_log * invSqrt;
                                if (tmp > score)
                                {
                                    score = tmp;
                                    thisExpand = i;
                                }
                            }
                        }

                        currentNode = thisExpand;
                        result = currentGame.play(action_micro[thisExpand], action_idx[thisExpand], player);
                        player = 0b1 ^ player;
                    }

                    path[pathIndex++] = currentNode;
                    
                    while (--pathIndex >= 0)
                    {
                        currentNode = path[pathIndex];
                        plays[currentNode]++;
                        wins[currentNode] += result;
                        result = 1.0f - result;
                    }
                }

                debug_data();
            }

            public void pick_move() 
            {
                int bestChild = childFrom[root];
                for (int i = childFrom[root]; i < childFrom[root] + children[root]; i++)
                {
                    if (plays[i] > plays[bestChild]) { bestChild = i; }
                }

                game.play(action_micro[bestChild], action_idx[bestChild], myID);
                root = bestChild;
            }

            public void output_move()
            {
                Console.WriteLine(convert_move_to_string(action_micro[root], action_idx[root]));
            }

            public void debug_data() 
            {
                Console.Error.WriteLine("Displaying result of simulations:");
                Console.Error.WriteLine("Simulations = " + N);

                for (int i = childFrom[root]; i < childFrom[root] + children[root]; i++)
                {
                    Console.Error.WriteLine("Node = " + convert_move_to_string(action_micro[i], action_idx[i]) + " : " + wins[i] + " / " + plays[i] + " : idx = " + i);
                }

                Console.Error.WriteLine();
            }

            public void displayValidMoves()
            {
                Console.Error.WriteLine("Displaying list of valid moves:");

                if (plays[root] == 0) return;
                for (int i = childFrom[root]; i < childFrom[root] + children[root]; i++)
                {
                    Console.Error.WriteLine(convert_move_to_string(action_micro[i], action_idx[i]));
                }

                Console.Error.WriteLine();
            }
        }

        static int approx_time_per_turn = 950;

        static void Main(string[] args)
        {
            MCTS AI = new MCTS();
            Game game = new Game();
            AI.load(game);

            while (true)
            {
                Console.Error.WriteLine("Enter move: ");

                string[] line = Console.ReadLine().Split();
                int y = int.Parse(line[0]);
                int x = int.Parse(line[1]);

                /* int validActionCount = int.Parse(Console.ReadLine());
                while (validActionCount-- > 0)
                {
                    Console.ReadLine();
                } */

                if (y == -1)
                {
                    AI.init();
                    AI.MonteCarloTreeSearch(0b1 ^ MCTS.myID, approx_time_per_turn);
                }
                else
                {
                    AI.startTurn(x, y);
                    AI.game.debug();
                    AI.MonteCarloTreeSearch(MCTS.myID, approx_time_per_turn);
                    AI.pick_move();
                }

                AI.output_move();

                approx_time_per_turn = 95;

                AI.game.debug();
                AI.displayValidMoves();
            }
        }
    }
}