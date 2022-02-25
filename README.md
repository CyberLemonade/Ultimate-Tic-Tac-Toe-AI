# Ultimate-Tic-Tac-Toe-AI
Monte Carlo Tree Search implementation for playing Ultimate Tic Tac Toe.

# Ultimate Tic Tac Toe
Ultimate Tic Tac Toe is a variation of classical Tic Tac Toe, with nine small boards, each individually represening a separate game of Tic Tac Toe.

When a player plays on a small board, he also decides where the next player will be allowed to play: for example, if a player has played in the bottom left square of one of the small boards, the next player will play on the small board located at the bottom left square of the main board.

If a player is sent to a board that is either already won, or full, then that player is allowed to play in any empty square.

For full details, check: https://en.wikipedia.org/wiki/Ultimate_tic-tac-toe

# How To Use
Execute Project.cs. The program will ask for your input. your input must be of the form: <row_number> <space> <column_number>. To let the AI start first, input: -1 -1. To make it easier for the player, the program will output the board state (in ASCII art) and list of all possible moves for the player in the next turn.
  
# Monte Carlo Tree Search
The search function can be found in subclass MCTS, method MonteCarloTreeSearch. To disable debug data being outputted, comment "debug_data();" on line 548. Other parameters that can be adjusted:
- graph size -> change the value of const int size in line 421
- approximate time per turn -> change the value of approx_time_per_turn in line 632

# Bitmasking
Bitmasking was implemented on the board to allow faster simulations. Subclass Board is a bitmask implementation of classical Tic Tac Toe. Subclass Macro is a modification that allows for neutral moves (when no player wins in a mini board). Subclass Game is a simulator of Ultimate Tic Tac Toe, which also extensively uses bitmasking.
