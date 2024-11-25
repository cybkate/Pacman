using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

class MazeGenerator
{
    private int[,] maze;
    private int rows, cols;
    private Random random = new Random();

    public MazeGenerator(int rows, int cols)
    {
        this.rows = rows;
        this.cols = cols;
        maze = new int[rows, cols];
    }

    public int[,] GenerateWithGuarantee()
    {
        Generate(); // Генеруємо лабіринт
        EnsureAllCellsReachable(); // Перевіряємо доступність усіх клітинок
        return maze;
    }

    private void Generate()
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                maze[y, x] = 1; // Створюємо стіни
            }
        }

        GenerateMaze(1, 1);
    }

    private void GenerateMaze(int x, int y)
    {
        maze[y, x] = 0; // Встановлюємо прохід
        var directions = GetShuffledDirections();

        foreach (var dir in directions)
        {
            int nx = x + dir.dx * 2;
            int ny = y + dir.dy * 2;

            if (IsInBounds(nx, ny) && maze[ny, nx] == 1)
            {
                maze[y + dir.dy, x + dir.dx] = 0; // Пробиваємо стіну
                GenerateMaze(nx, ny); // Рекурсивно створюємо шлях
            }
        }

        // Додатковий крок: видалення випадкових стін
        Random rand = new Random();
        for (int i = 0; i < (rows * cols) / 10; i++) // Видаляємо приблизно 10% стін
        {
            int randomX = rand.Next(1, cols - 1);
            int randomY = rand.Next(1, rows - 1);

            if (maze[randomY, randomX] == 1 && HasEnoughNeighbors(randomX, randomY))
            {
                maze[randomY, randomX] = 0; // Видаляємо стіну
            }
        }
    }

    // Перевірка, щоб при видаленні стіни лабіринт залишався зв'язним
    private bool HasEnoughNeighbors(int x, int y)
    {
        int passableNeighbors = 0;

        foreach (var dir in new List<(int dx, int dy)>
    {
        (0, -1), (0, 1), (-1, 0), (1, 0)
    })
        {
            int nx = x + dir.dx;
            int ny = y + dir.dy;

            if (IsInBounds(nx, ny) && maze[ny, nx] == 0)
            {
                passableNeighbors++;
            }
        }

        // Якщо у стіни вже є два або більше прохідних сусіди, її можна видалити
        return passableNeighbors >= 2;
    }


    private void EnsureAllCellsReachable()
    {
        var visited = new bool[rows, cols];
        var stack = new Stack<(int x, int y)>();

        stack.Push((1, 1));
        visited[1, 1] = true;

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            foreach (var dir in GetShuffledDirections())
            {
                int nx = x + dir.dx;
                int ny = y + dir.dy;

                if (IsInBounds(nx, ny) && !visited[ny, nx] && maze[ny, nx] == 0)
                {
                    visited[ny, nx] = true;
                    stack.Push((nx, ny));
                }
            }
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (!visited[y, x])
                {
                    maze[y, x] = 1; // Робимо всі недосяжні клітинки стінами
                }
            }
        }
    }

    private List<(int dx, int dy)> GetShuffledDirections()
    {
        var directions = new List<(int dx, int dy)>
        {
            (0, -1), (0, 1), (-1, 0), (1, 0)
        };

        for (int i = 0; i < directions.Count; i++)
        {
            int randomIndex = random.Next(i, directions.Count);
            var temp = directions[i];
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        return directions;
    }

    private bool IsInBounds(int x, int y)
    {
        return x > 0 && x < cols - 1 && y > 0 && y < rows - 1;
    }
}

namespace PacMan
{
    public partial class Form1 : Form
    {
        private int[,] maze;
        private int playerX = 1, playerY = 1;
        private const int cellSize = 20;
        private List<Point> coins = new List<Point>();
        private List<Point> ghosts = new List<Point>();
        private bool gameOver = false;
        private int collectedCoins = 0;
        private int totalCoins = 10; // Початкова кількість монет
        private int score = 0; // Загальний рахунок
        private string gameOverMessage = null; // Повідомлення про програш
        private Button restartButton; // Кнопка рестарту
        private Timer ghostTimer; // Таймер для руху привидів

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.Black;
            maze = new MazeGenerator(21, 21).GenerateWithGuarantee();
            GenerateCoins(totalCoins);
            GenerateGhosts(4);
            this.ClientSize = new Size(maze.GetLength(1) * cellSize, maze.GetLength(0) * cellSize);

            // Таймер для руху привидів
            ghostTimer = new Timer
            {
                Interval = 500 // Інтервал у мілісекундах
            };
            ghostTimer.Tick += (s, e) => MoveGhosts();
            ghostTimer.Start();

            // Створення кнопки рестарту
            restartButton = new Button
            {
                Text = "Restart",
                BackColor = Color.DarkRed, // Фон кнопки
                ForeColor = Color.White,    // Колір тексту на кнопці
                Size = new Size(100, 40),
                Location = new Point(this.ClientSize.Width / 2 - 50, this.ClientSize.Height / 2 + 30),
                Visible = false
            };
            restartButton.Click += RestartGame;
            this.Controls.Add(restartButton);
        }

        private void GenerateCoins(int number)
        {
            coins.Clear();
            Random rand = new Random();
            while (coins.Count < number)
            {
                int x = rand.Next(1, maze.GetLength(1) - 1);
                int y = rand.Next(1, maze.GetLength(0) - 1);

                if (maze[y, x] == 0 && !coins.Contains(new Point(x, y)))
                {
                    coins.Add(new Point(x, y));
                }
            }
        }

        private void GenerateGhosts(int number)
        {
            ghosts.Clear();
            Random rand = new Random();
            while (ghosts.Count < number)
            {
                int x = rand.Next(1, maze.GetLength(1) - 1);
                int y = rand.Next(1, maze.GetLength(0) - 1);

                if (maze[y, x] == 0 && !ghosts.Contains(new Point(x, y)))
                {
                    ghosts.Add(new Point(x, y));
                }
            }
        }

        private void MoveGhosts()
        {
            Random rand = new Random();

            for (int i = 0; i < ghosts.Count; i++)
            {
                Point ghost = ghosts[i];
                List<Point> possibleMoves = new List<Point>();

                // Вибір можливих напрямків
                foreach (var direction in new List<Point>
        {
            new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0)
        })
                {
                    int newX = ghost.X + direction.X;
                    int newY = ghost.Y + direction.Y;

                    if (IsInBounds(newX, newY) && maze[newY, newX] == 0)
                    {
                        possibleMoves.Add(new Point(newX, newY));
                    }
                }

                if (possibleMoves.Count > 0)
                {
                    Point targetMove = ghost;

                    if (i == 0) // Привид №1: Рандомний рух
                    {
                        targetMove = possibleMoves[rand.Next(possibleMoves.Count)];
                    }
                    else if (i == 1) // Привид №2: Переслідувач
                    {
                        targetMove = possibleMoves
                            .OrderBy(p => Math.Abs(p.X - playerX) + Math.Abs(p.Y - playerY)) // Найкоротший шлях до Пакмена
                            .FirstOrDefault();
                    }
                    else if (i == 2) // Привид №3: Охоронець верхнього лівого квадранта
                    {
                        targetMove = possibleMoves
                            .Where(p => p.X < maze.GetLength(1) / 2 && p.Y < maze.GetLength(0) / 2) // Ліва верхня зона
                            .OrderBy(p => Math.Abs(p.X - ghost.X) + Math.Abs(p.Y - ghost.Y))
                            .FirstOrDefault();

                        if (targetMove == Point.Empty) // Якщо рух в зону недоступний, рухається випадково
                        {
                            targetMove = possibleMoves[rand.Next(possibleMoves.Count)];
                        }
                    }
                    else if (i == 3) // Привид №4: Охоронець нижнього правого квадранта
                    {
                        targetMove = possibleMoves
                            .Where(p => p.X >= maze.GetLength(1) / 2 && p.Y >= maze.GetLength(0) / 2) // Права нижня зона
                            .OrderBy(p => Math.Abs(p.X - ghost.X) + Math.Abs(p.Y - ghost.Y))
                            .FirstOrDefault();

                        if (targetMove == Point.Empty) // Якщо рух в зону недоступний, рухається випадково
                        {
                            targetMove = possibleMoves[rand.Next(possibleMoves.Count)];
                        }
                    }

                    // Оновлюємо позицію привида
                    ghosts[i] = targetMove != Point.Empty ? targetMove : ghost;
                }
            }

            CheckGhostCollision(); // Перевірка зіткнення з Пакменом
            Invalidate(); // Перемальовуємо вікно
        }


        private bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < maze.GetLength(1) && y >= 0 && y < maze.GetLength(0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // Малюємо лабіринт
            for (int y = 0; y < maze.GetLength(0); y++)
            {
                for (int x = 0; x < maze.GetLength(1); x++)
                {
                    if (maze[y, x] == 1)
                    {
                        g.FillRectangle(Brushes.Blue, x * cellSize, y * cellSize, cellSize, cellSize);
                    }
                }
            }

            // Малюємо монети
            foreach (var coin in coins)
            {
                int coinSize = cellSize / 2;
                int offset = (cellSize - coinSize) / 2;
                g.FillEllipse(Brushes.Yellow, coin.X * cellSize + offset, coin.Y * cellSize + offset, coinSize, coinSize);
            }

            // Малюємо привидів
            foreach (var ghost in ghosts)
            {
                g.FillEllipse(Brushes.Red, ghost.X * cellSize, ghost.Y * cellSize, cellSize, cellSize);
            }

            // Малюємо гравця
            g.FillEllipse(Brushes.Green, playerX * cellSize, playerY * cellSize, cellSize, cellSize);

            // Малюємо рахунок
            g.DrawString($"Score: {score}", new Font("Arial", 14), Brushes.White, 10, 10);

            // Якщо гра завершена, виводимо повідомлення про програш
            if (gameOver && gameOverMessage != null)
            {
                g.DrawString(gameOverMessage, new Font("Arial", 24), Brushes.Red, this.ClientSize.Width / 2 - 120, this.ClientSize.Height / 2 - 30);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!gameOver)
            {
                if (e.KeyCode == Keys.Up && IsInBounds(playerX, playerY - 1) && maze[playerY - 1, playerX] == 0) playerY--;
                if (e.KeyCode == Keys.Down && IsInBounds(playerX, playerY + 1) && maze[playerY + 1, playerX] == 0) playerY++;
                if (e.KeyCode == Keys.Left && IsInBounds(playerX - 1, playerY) && maze[playerY, playerX - 1] == 0) playerX--;
                if (e.KeyCode == Keys.Right && IsInBounds(playerX + 1, playerY) && maze[playerY, playerX + 1] == 0) playerX++;

                CheckGhostCollision(); // Перевірка після руху Пакмена
            }

            for (int i = 0; i < coins.Count; i++)
            {
                if (coins[i].X == playerX && coins[i].Y == playerY)
                {
                    coins.RemoveAt(i);
                    collectedCoins++;
                    score++;

                    if (collectedCoins == totalCoins)
                    {
                        totalCoins += 5;
                        maze = new MazeGenerator(21, 21).GenerateWithGuarantee();
                        GenerateCoins(totalCoins);
                        GenerateGhosts(4);
                        collectedCoins = 0;
                    }

                    break;
                }
            }

            Invalidate();
        }

        private void CheckGhostCollision()
        {
            foreach (var ghost in ghosts)
            {
                if (ghost.X == playerX && ghost.Y == playerY)
                {
                    GameOver();
                    break;
                }
            }
        }

        private void GameOver()
        {
            gameOver = true;
            gameOverMessage = $"Game Over! Score: {score}";
            restartButton.Visible = true;
            ghostTimer.Stop(); // Зупиняємо рух привидів
            Invalidate();
        }

        private void RestartGame(object sender, EventArgs e)
        {
            gameOver = false;
            gameOverMessage = null;
            restartButton.Visible = false;

            playerX = 1;
            playerY = 1;
            score = 0;
            collectedCoins = 0;
            totalCoins = 10;

            maze = new MazeGenerator(21, 21).GenerateWithGuarantee();
            GenerateCoins(totalCoins);
            GenerateGhosts(4);

            ghostTimer.Start(); // Перезапускаємо рух привидів
            this.Focus();
            Invalidate();
        }
    }
}

